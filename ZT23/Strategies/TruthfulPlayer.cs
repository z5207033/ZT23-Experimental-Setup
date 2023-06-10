using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class TruthfulPlayer<MoveT, GameT, GameStateT> : IPlayer<MoveT, GameT, GameStateT>
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly Random random = new Random();

        GameT game;
        IEnumerable<GameStateT> possibleGameStates;

        public void ProvideRulesAndInitialKnowledge(GameT game, int player, IEnumerable<(GameStateT state, int weight, int id)> possibleStartingStates)
        {
            this.game = game;

            possibleGameStates = possibleStartingStates
                .Select(stateAndWeight => stateAndWeight.state)
                .ToList();
        }

        public MoveT RequestMove(IEnumerable<MoveT> legalMoves)
        {
            var trueClaims = legalMoves.Where(move => game.IsClaim(move, out _, out var claim, out var id) && possibleGameStates.All(claim));

            return random.Choose(trueClaims.Count() > 0 ? trueClaims : legalMoves);
        }

        public void ProvidePercepts(Func<GameStateT, bool> percepts, int id) => possibleGameStates = possibleGameStates
                .Where(state => percepts(state))
                .ToList();

        public void ProvideClaims(IEnumerable<(int sender, Func<GameStateT, bool> claim, int id)> claims) {}
    }
}
