using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    interface IPlayer<MoveT, GameT, GameStateT>
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        void ProvideRulesAndInitialKnowledge(GameT game, int player, IEnumerable<(GameStateT state, int weight, int id)> possibleStartingStates);
        MoveT RequestMove(IEnumerable<MoveT> legalMoves);
        void ProvidePercepts(Func<GameStateT, bool> percepts, int id);
        void ProvideClaims(IEnumerable<(int sender, Func<GameStateT, bool> claim, int id)> claims);
    }

    interface IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        int NumberOfPlayers { get; }

        GameStateT GetInitialStateFromCurrent(GameStateT state);
        IEnumerable<(GameStateT state, int weight, int id)> GetInitialStates();
        IEnumerable<(GameStateT state, int weight, int id)> GetPerceivedInitialStatesFromActual(GameStateT state, int player);
        (Func<GameStateT, bool> percepts, int id)[] GetPerceptsFromMove(GameStateT state, IEnumerable<MoveT> combinedMove);
        bool IsClaim(MoveT move, out int[] receivers, out Func<GameStateT, bool> claim, out int id);
        GameStateT GetStateAfterCombinedMove(GameStateT state, IEnumerable<MoveT> combinedMove);
        IEnumerable<(IEnumerable<MoveT> combinedMove, GameStateT nextState)> GetHistory(GameStateT state);

        IEnumerable<GameStateT> GetPossibleStatesAfterMove(GameStateT state, int player, MoveT move)
        {
            var legalMovesByPlayer = state.LegalMovesByPlayer.ToList();
            legalMovesByPlayer[player] = new[] { move };

            return legalMovesByPlayer
                .CartesianProduct()
                .Select(move => GetStateAfterCombinedMove(state, move));
        }

        IEnumerable<(int sender, Func<GameStateT, bool> claim, int id)>[] GetClaimsFromMove(GameStateT state, IEnumerable<MoveT> combinedMove)
        {
            var claimsForPlayer = Enumerable.Range(0, NumberOfPlayers)
                .Select(_ => new List<(int sender, Func<GameStateT, bool> claim, int id)>())
                .ToArray();

            for (int sender = 0; sender < NumberOfPlayers; sender++)
            {
                if (IsClaim(combinedMove.ElementAt(sender), out var receivers, out var claim, out var id))
                {
                    foreach (var receiver in receivers) claimsForPlayer[receiver].Add((sender, claim, id));
                }
            }

            return claimsForPlayer;
        }
    }

    interface IGameState<MoveT>
    {
        IEnumerable<IEnumerable<MoveT>> LegalMovesByPlayer { get; }
        bool IsTerminal { get; }
        int Turn { get; }

        int[] GetUtilities();
    }
}
