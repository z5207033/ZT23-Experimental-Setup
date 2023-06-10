using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class TrustingPlayer<MoveT, GameT, GameStateT> : IPlayer<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly Random random = new Random();
        readonly TrustingStrategy<MoveT, GameT, GameStateT> strategy;

        GameT game;
        int player;
        internal IEnumerable<(GameStateT state, double weight)> PossibleGameStates { get; private set; }
        internal List<int> historyId;

        public TrustingPlayer(TrustingStrategy<MoveT, GameT, GameStateT> strategy = null)
        {
            this.strategy = strategy ?? new TrustingStrategy<MoveT, GameT, GameStateT>(
                new Dictionary<(GameStateT state, int player, MoveT move), double[]>(),
                new Dictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>>()
            );
        }

        public void ProvideRulesAndInitialKnowledge(GameT game, int player, IEnumerable<(GameStateT state, int weight, int id)> possibleStartingStates)
        {
            this.game = game;
            this.player = player;

            PossibleGameStates = possibleStartingStates
                .Select(stateAndWeight => (stateAndWeight.state, 1000d * stateAndWeight.weight))
                .ToList();

            historyId = new List<int>(possibleStartingStates.Select(e => e.id));
            historyId.Sort();
        }

        public MoveT RequestMove(IEnumerable<MoveT> legalMoves)
        {
            // Choose a move
            var move = random.Choose(strategy.RequestMoves(legalMoves, historyId, game, player, PossibleGameStates));

            historyId.Add(-1 - move.GetIndex());

            // Advance the game state
            PossibleGameStates = PossibleGameStates
                .Select(possibility => {
                    var possibleNextStates = game.GetPossibleStatesAfterMove(possibility.state, player, move);
                    return possibleNextStates.Select(state => (state, possibility.weight / possibleNextStates.Count()));
                })
                .Flatten()
                .ToList();

            return move;
        }

        public IEnumerable<MoveT> GetAllBestMoves(IEnumerable<MoveT> legalMoves) => strategy.RequestMoves(legalMoves, historyId, game, player, PossibleGameStates);

        public void ProvidePercepts(Func<GameStateT, bool> percepts, int id)
        {
            historyId.Add(id);
            PossibleGameStates = strategy.ProvidePercepts(percepts, PossibleGameStates);
        }

        public void ProvideClaims(IEnumerable<(int sender, Func<GameStateT, bool> claim, int id)> claims)
        {
            foreach (var (_, _, id) in claims) historyId.Add(id);
            PossibleGameStates = strategy.ProvidePercepts(state => claims.All(communication => communication.claim(state)), PossibleGameStates);
        }
    }

    class TrustingStrategy<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache;
        readonly IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache;

        readonly List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache;

        internal TrustingStrategy(
            IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache,
            IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache,
            List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache = null)
        {
            this.moveValueCache = moveValueCache;
            this.beliefsInStateCache = beliefsInStateCache;
            this.bestMoveCache = bestMoveCache ?? new List<(IEnumerable<int>, IEnumerable<MoveT>)>();
        }

        internal List<(GameStateT state, double weight)> ProvidePercepts(Func<GameStateT, bool> percepts, IEnumerable<(GameStateT state, double weight)> possibleGameStates)
        {
            return possibleGameStates
                .Where(possibility => percepts(possibility.state))
                .ToList();
        }

        internal List<MoveT> RequestMoves(IEnumerable<MoveT> legalMoves, IEnumerable<int> historyId, GameT game, int player, IEnumerable<(GameStateT state, double weight)> possibleGameStates)
        {
            if (legalMoves.Count() == 1) return legalMoves.ToList();

            if (historyId != null)
            {
                var matchInCache = bestMoveCache.FirstOrDefault(e => historyId.SequenceEqual(e.historyId)).bestMoves;

                if (matchInCache != null) return matchInCache.ToList();
            }

            double maximumUtilitySum = 0;
            var bestMoves = new List<MoveT>();
            var totalWeight = possibleGameStates.Sum(possibility => possibility.weight);

            foreach (var move in legalMoves)
            {
                double utilitySum = 0;

                foreach ((var state, var weight) in possibleGameStates)
                {
                    utilitySum += weight * GetExpectedUtilitiesWithChoice(game, state, player, move)[player];
                }

                if (utilitySum > maximumUtilitySum)
                {
                    bestMoves.Clear();
                    bestMoves.Add(move);
                    maximumUtilitySum = utilitySum;
                }
                else if (utilitySum == maximumUtilitySum)
                {
                    bestMoves.Add(move);
                }
            }

            if (historyId != null)
            {
                bestMoveCache.Add((historyId.ToList(), bestMoves));
            }

            return bestMoves;
        }

        private double[] GetExpectedUtilitiesByPlayer(GameT game, GameStateT state)
        {
            if (state.IsTerminal) return state.GetUtilities()
                    .Select(x => (double)x)
                    .ToArray();

            var legalMovesByPlayer = state.LegalMovesByPlayer;
            var playersWithChoice = new List<int>();

            for (int player = 0; player < game.NumberOfPlayers; player++)
            {
                if (legalMovesByPlayer.ElementAt(player).Count() > 1) playersWithChoice.Add(player);
            }

            if (playersWithChoice.Count() > 1) throw new NotImplementedException("This system does not yet support simultaneous games");

            var playerWithChoice = playersWithChoice.First();
            var movesChosen = RequestMoves(legalMovesByPlayer.ElementAt(playerWithChoice), null, game, playerWithChoice, GetBeliefsInRealState(game, state, playerWithChoice));

            var utilitySums = new double[game.NumberOfPlayers];

            movesChosen.ForEach(move => {
                var utilities = GetExpectedUtilitiesWithChoice(game, state, playerWithChoice, move);

                for (int player = 0; player < game.NumberOfPlayers; player++) utilitySums[player] += utilities[player];
            });

            return utilitySums
                .Select(sum => sum / movesChosen.Count())
                .ToArray();
        }

        private IEnumerable<(GameStateT state, double weight)> GetBeliefsInRealState(GameT game, GameStateT state, int playerId)
        {
            if (beliefsInStateCache.TryGetValue((state, playerId), out var cachedBeliefs)) return cachedBeliefs;

            var simulatedPlayer = new TrustingPlayer<MoveT, GameT, GameStateT>(
                new TrustingStrategy<MoveT, GameT, GameStateT>(moveValueCache, beliefsInStateCache, bestMoveCache));

            GameManager.ReplayToState(game, state, playerId, simulatedPlayer);

            beliefsInStateCache.Add((state, playerId), simulatedPlayer.PossibleGameStates);

            return simulatedPlayer.PossibleGameStates;
        }

        private double[] GetExpectedUtilitiesWithChoice(GameT game, GameStateT state, int player, MoveT move)
        {
            if (moveValueCache.TryGetValue((state, player, move), out var cachedUtilities)) return cachedUtilities;

            var possibleMovesByPlayer = state.LegalMovesByPlayer.ToList();
            possibleMovesByPlayer[player] = new List<MoveT> { move };

            var combinedMove = possibleMovesByPlayer
                .Select(movesForPlayer => movesForPlayer.First());

            var utilities = GetExpectedUtilitiesByPlayer(game, game.GetStateAfterCombinedMove(state, combinedMove));

            moveValueCache.Add((state, player, move), utilities);

            return utilities;
        }
    }
}
