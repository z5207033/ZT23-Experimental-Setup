using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamePlayer
{
    class GoodOrEvilGame : IGame<GoodOrEvilGameMove, GoodOrEvilGameState>
    {
        public int NumberOfPlayers => 2;

        public IEnumerable<(IEnumerable<GoodOrEvilGameMove> combinedMove, GoodOrEvilGameState nextState)> GetHistory(GoodOrEvilGameState state) => state.history;

        public GoodOrEvilGameState GetInitialStateFromCurrent(GoodOrEvilGameState state) => state.initialState;

        public virtual IEnumerable<(GoodOrEvilGameState state, int weight, int id)> GetInitialStates() => new[] {
            (new GoodOrEvilGameState(true), 1, 300),
            (new GoodOrEvilGameState(false), 1, 301),
        };

        public IEnumerable<(GoodOrEvilGameState state, int weight, int id)> GetPerceivedInitialStatesFromActual(GoodOrEvilGameState state, int player)
            => player == 0 ? GetInitialStates() : GetInitialStates().Where(initial => initial.state.isSubjectGood == state.isSubjectGood);

        public (Func<GoodOrEvilGameState, bool>, int)[] GetPerceptsFromMove(GoodOrEvilGameState state, IEnumerable<GoodOrEvilGameMove> combinedMove)
        {
            var moveSeen = state.history.First().move.ElementAt(1);

            return new (Func<GoodOrEvilGameState, bool>, int)[] {
                (state => state.history.First().move.ElementAt(1) == moveSeen, 201 + moveSeen.GetIndex()),
                (state => true, 200),
            };
        }

        public GoodOrEvilGameState GetStateAfterCombinedMove(GoodOrEvilGameState state, IEnumerable<GoodOrEvilGameMove> combinedMove) => new GoodOrEvilGameState(state, combinedMove);

        public bool IsClaim(GoodOrEvilGameMove move, out int[] receivers, out Func<GoodOrEvilGameState, bool> claim, out int id)
        {
            switch (move)
            {
                case GoodOrEvilGameMove.CLAIM_GOOD:
                    receivers = new[] { 0 };
                    claim = state => state.isSubjectGood;
                    id = 100;
                    return true;

                case GoodOrEvilGameMove.CLAIM_EVIL:
                    receivers = new[] { 0 };
                    claim = state => !state.isSubjectGood;
                    id = 101;
                    return true;

                default:
                    receivers = null;
                    claim = null;
                    id = 102;
                    return false;
            }
        }
    }

    class WeightedGoodOrEvilGame : GoodOrEvilGame
    {
        public override IEnumerable<(GoodOrEvilGameState state, int weight, int id)> GetInitialStates() => new[] {
            (new GoodOrEvilGameState(true), 1, 300),
            (new GoodOrEvilGameState(false), 3, 301),
        };
    }

    class GoodOrEvilGameState : IGameState<GoodOrEvilGameMove>
    {
        internal readonly GoodOrEvilGameState initialState;
        internal readonly IEnumerable<(IEnumerable<GoodOrEvilGameMove> move, GoodOrEvilGameState nextState)> history;
        internal readonly bool isSubjectGood;

        internal GoodOrEvilGameState(bool isSubjectGood)
        {
            initialState = this;

            history = Enumerable.Empty<(IEnumerable<GoodOrEvilGameMove> move, GoodOrEvilGameState nextState)>();
            this.isSubjectGood = isSubjectGood;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal GoodOrEvilGameState(GoodOrEvilGameState lastState, IEnumerable<GoodOrEvilGameMove> move)
        {
            initialState = lastState.initialState;

            var history = lastState.history.ToList();
            history.Add((move, this));
            this.history = history;

            isSubjectGood = lastState.isSubjectGood;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        public IEnumerable<IEnumerable<GoodOrEvilGameMove>> LegalMovesByPlayer { get; }

        private IEnumerable<IEnumerable<GoodOrEvilGameMove>> CreateLegalMoves() => Turn == 0 ? new[] {
            new[] { GoodOrEvilGameMove.NO_OP },
            new[] { GoodOrEvilGameMove.CLAIM_GOOD, GoodOrEvilGameMove.CLAIM_EVIL },
        } : new[] {
            new[] { GoodOrEvilGameMove.GUESS_GOOD, GoodOrEvilGameMove.GUESS_EVIL },
            new[] { GoodOrEvilGameMove.NO_OP },
        };

        public bool IsTerminal => Turn == 2;

        public int Turn => history.Count();

        public int[] GetUtilities()
        {
            if (!IsTerminal) throw new InvalidOperationException("Utilities can only be retrieved from a terminal state");

            var guessersMove = history.Last().move.First();

            return new[]
            {
                (guessersMove == GoodOrEvilGameMove.GUESS_GOOD) == isSubjectGood ? 100 : 0,
                guessersMove == GoodOrEvilGameMove.GUESS_GOOD ? 100 : 0,
            };
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (GoodOrEvilGameState)obj;

            return isSubjectGood == other.isSubjectGood
                && Turn == other.Turn
                && Enumerable
                    .Range(0, Turn)
                    .All(turn => history.ElementAt(turn).move.SequenceEqual(other.history.ElementAt(turn).move));
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return Turn;
        }
    }

    enum GoodOrEvilGameMove
    {
        GUESS_GOOD,
        GUESS_EVIL,
        NO_OP,
        CLAIM_GOOD,
        CLAIM_EVIL,
    }
}
