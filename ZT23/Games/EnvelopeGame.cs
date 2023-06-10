using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class EnvelopeGame : IGame<EnvelopeGameMove, EnvelopeGameState>
    {
        public int NumberOfPlayers => 2;

        public IEnumerable<(IEnumerable<EnvelopeGameMove> combinedMove, EnvelopeGameState nextState)> GetHistory(EnvelopeGameState state) => state.history;

        public EnvelopeGameState GetInitialStateFromCurrent(EnvelopeGameState state) => state.initialState;

        public IEnumerable<(EnvelopeGameState state, int weight, int id)> GetInitialStates()
        {
            var possibleValues = new[] { 0, 25, 50, 75, 100 };

            return new[] { possibleValues, possibleValues }
                .CartesianProduct()
                .Select((pair, i) => (new EnvelopeGameState(pair.First(), pair.Last()), 1, 300 + i));
        }

        public IEnumerable<(EnvelopeGameState state, int weight, int id)> GetPerceivedInitialStatesFromActual(EnvelopeGameState state, int player)
            => player == 0 ? GetInitialStates() : GetInitialStates().Where(initial => initial.state.amountInFirst == state.amountInFirst && initial.state.amountInSecond == state.amountInSecond);

        public (Func<EnvelopeGameState, bool>, int)[] GetPerceptsFromMove(EnvelopeGameState state, IEnumerable<EnvelopeGameMove> combinedMove)
        {
            var moveSeen = state.history.First().move.ElementAt(1);

            return new (Func<EnvelopeGameState, bool>, int)[] {
                (state => state.history.First().move.ElementAt(1) == moveSeen, 201 + moveSeen.GetIndex()),
                (state => true, 200),
            };
        }

        public EnvelopeGameState GetStateAfterCombinedMove(EnvelopeGameState state, IEnumerable<EnvelopeGameMove> combinedMove) => new EnvelopeGameState(state, combinedMove);

        public bool IsClaim(EnvelopeGameMove move, out int[] receivers, out Func<EnvelopeGameState, bool> claim, out int id)
        {
            switch (move)
            {
                case EnvelopeGameMove.CLAIM_FIRST_HAS_0:
                    receivers = new[] { 0 };
                    claim = state => state.amountInFirst == 0;
                    id = 100;
                    return true;
                case EnvelopeGameMove.CLAIM_FIRST_HAS_25:
                    receivers = new[] { 0 };
                    claim = state => state.amountInFirst == 25;
                    id = 101;
                    return true;
                case EnvelopeGameMove.CLAIM_FIRST_HAS_50:
                    receivers = new[] { 0 };
                    claim = state => state.amountInFirst == 50;
                    id = 102;
                    return true;
                case EnvelopeGameMove.CLAIM_FIRST_HAS_75:
                    receivers = new[] { 0 };
                    claim = state => state.amountInFirst == 75;
                    id = 103;
                    return true;
                case EnvelopeGameMove.CLAIM_FIRST_HAS_100:
                    receivers = new[] { 0 };
                    claim = state => state.amountInFirst == 100;
                    id = 104;
                    return true;
                case EnvelopeGameMove.CLAIM_SECOND_HAS_0:
                    receivers = new[] { 0 };
                    claim = state => state.amountInSecond == 0;
                    id = 105;
                    return true;
                case EnvelopeGameMove.CLAIM_SECOND_HAS_25:
                    receivers = new[] { 0 };
                    claim = state => state.amountInSecond == 25;
                    id = 106;
                    return true;
                case EnvelopeGameMove.CLAIM_SECOND_HAS_50:
                    receivers = new[] { 0 };
                    claim = state => state.amountInSecond == 50;
                    id = 107;
                    return true;
                case EnvelopeGameMove.CLAIM_SECOND_HAS_75:
                    receivers = new[] { 0 };
                    claim = state => state.amountInSecond == 75;
                    id = 108;
                    return true;
                case EnvelopeGameMove.CLAIM_SECOND_HAS_100:
                    receivers = new[] { 0 };
                    claim = state => state.amountInSecond == 100;
                    id = 109;
                    return true;
                default:
                    receivers = null;
                    claim = null;
                    id = 0;
                    return false;
            }
        }
    }

    class EnvelopeGameState : IGameState<EnvelopeGameMove>
    {
        internal readonly EnvelopeGameState initialState;
        internal readonly IEnumerable<(IEnumerable<EnvelopeGameMove> move, EnvelopeGameState nextState)> history;
        internal readonly int amountInFirst, amountInSecond;

        internal EnvelopeGameState(int amountInFirst, int amountInSecond)
        {
            initialState = this;

            history = Enumerable.Empty<(IEnumerable<EnvelopeGameMove> move, EnvelopeGameState nextState)>();
            this.amountInFirst = amountInFirst;
            this.amountInSecond = amountInSecond;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal EnvelopeGameState(EnvelopeGameState lastState, IEnumerable<EnvelopeGameMove> move)
        {
            initialState = lastState.initialState;

            var history = lastState.history.ToList();
            history.Add((move, this));
            this.history = history;

            amountInFirst = lastState.amountInFirst;
            amountInSecond = lastState.amountInSecond;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        public IEnumerable<IEnumerable<EnvelopeGameMove>> LegalMovesByPlayer { get; }

        private IEnumerable<IEnumerable<EnvelopeGameMove>> CreateLegalMoves() => Turn == 0 ? new[] {
            new[] { EnvelopeGameMove.NO_OP },
            new[] {
                EnvelopeGameMove.CLAIM_FIRST_HAS_0,
                EnvelopeGameMove.CLAIM_FIRST_HAS_25,
                EnvelopeGameMove.CLAIM_FIRST_HAS_50,
                EnvelopeGameMove.CLAIM_FIRST_HAS_75,
                EnvelopeGameMove.CLAIM_FIRST_HAS_100,
                EnvelopeGameMove.CLAIM_SECOND_HAS_0,
                EnvelopeGameMove.CLAIM_SECOND_HAS_25,
                EnvelopeGameMove.CLAIM_SECOND_HAS_50,
                EnvelopeGameMove.CLAIM_SECOND_HAS_75,
                EnvelopeGameMove.CLAIM_SECOND_HAS_100,
            },
        } : new[] {
            new[] { EnvelopeGameMove.CHOOSE_FIRST, EnvelopeGameMove.CHOOSE_SECOND },
            new[] { EnvelopeGameMove.NO_OP },
        };

        public bool IsTerminal => Turn == 2;

        public int Turn => history.Count();

        public int[] GetUtilities()
        {
            if (!IsTerminal) throw new InvalidOperationException("Utilities can only be retrieved from a terminal state");

            var chosenEnvelope = history.Last().move.First();
            var utility = chosenEnvelope == EnvelopeGameMove.CHOOSE_FIRST ? amountInFirst : amountInSecond;

            return new[] { utility, utility };
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

            var other = (EnvelopeGameState)obj;

            return amountInFirst == other.amountInFirst
                && amountInSecond == other.amountInSecond
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

    enum EnvelopeGameMove
    {
        NO_OP,
        CHOOSE_FIRST,
        CHOOSE_SECOND,
        CLAIM_FIRST_HAS_0,
        CLAIM_FIRST_HAS_25,
        CLAIM_FIRST_HAS_50,
        CLAIM_FIRST_HAS_75,
        CLAIM_FIRST_HAS_100,
        CLAIM_SECOND_HAS_0,
        CLAIM_SECOND_HAS_25,
        CLAIM_SECOND_HAS_50,
        CLAIM_SECOND_HAS_75,
        CLAIM_SECOND_HAS_100,
    }
}
