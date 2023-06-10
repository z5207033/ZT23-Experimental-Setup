using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamePlayer
{
    class ProbableSharedEnvelopeGame : IGame<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGameState>
    {
        public int NumberOfPlayers => 3;

        public IEnumerable<(IEnumerable<ProbableSharedEnvelopeGameMove> combinedMove, ProbableSharedEnvelopeGameState nextState)> GetHistory(ProbableSharedEnvelopeGameState state) => state.history;

        public ProbableSharedEnvelopeGameState GetInitialStateFromCurrent(ProbableSharedEnvelopeGameState state) => state.initialState;

        public IEnumerable<(ProbableSharedEnvelopeGameState state, int weight, int id)> GetInitialStates() => new[] {
            (new ProbableSharedEnvelopeGameState(isCooperative: true, isInFirst: true), 9, 300),
            (new ProbableSharedEnvelopeGameState(isCooperative: true, isInFirst: false), 9, 301),
            (new ProbableSharedEnvelopeGameState(isCooperative: false, isInFirst: true), 1, 302),
            (new ProbableSharedEnvelopeGameState(isCooperative: false, isInFirst: false), 1, 303),
        };

        public IEnumerable<(ProbableSharedEnvelopeGameState state, int weight, int id)> GetPerceivedInitialStatesFromActual(ProbableSharedEnvelopeGameState state, int player)
        {
            return player switch
            {
                0 => GetInitialStates().Where(initial => initial.state.isCooperative == state.isCooperative && initial.state.isInFirst == state.isInFirst),
                1 => GetInitialStates().Where(initial => initial.state.isCooperative == state.isCooperative),
                _ => GetInitialStates(),
            };
        }

        public (Func<ProbableSharedEnvelopeGameState, bool>, int)[] GetPerceptsFromMove(ProbableSharedEnvelopeGameState state, IEnumerable<ProbableSharedEnvelopeGameMove> combinedMove)
        {
            var moveSeen = state.history.First().move.First();
            var isTerminal = state.IsTerminal;

            return state.Turn == 1 ? new (Func<ProbableSharedEnvelopeGameState, bool>, int)[] {
                (state => true, 200),
                (state => state.history.First().move.First() == moveSeen, 203 + moveSeen.GetIndex()),
                (state => state.history.First().move.First() == moveSeen, 203 + moveSeen.GetIndex()),
            } : new (Func<ProbableSharedEnvelopeGameState, bool>, int)[] {
                (state => state.IsTerminal == isTerminal, isTerminal ? 201 : 202),
                (state => state.IsTerminal == isTerminal, isTerminal ? 201 : 202),
                (state => state.IsTerminal == isTerminal, isTerminal ? 201 : 202),
            };
        }

        public ProbableSharedEnvelopeGameState GetStateAfterCombinedMove(ProbableSharedEnvelopeGameState state, IEnumerable<ProbableSharedEnvelopeGameMove> combinedMove)
            => new ProbableSharedEnvelopeGameState(state, combinedMove);

        public bool IsClaim(ProbableSharedEnvelopeGameMove move, out int[] receivers, out Func<ProbableSharedEnvelopeGameState, bool> claim, out int id)
        {
            switch (move)
            {
                case ProbableSharedEnvelopeGameMove.CLAIM_IN_FIRST:
                    receivers = new[] { 1, 2 };
                    claim = state => state.isInFirst;
                    id = 100;
                    return true;

                case ProbableSharedEnvelopeGameMove.CLAIM_IN_SECOND:
                    receivers = new[] { 1, 2 };
                    claim = state => !state.isInFirst;
                    id = 101;
                    return true;

                default:
                    receivers = null;
                    claim = null;
                    id = 0;
                    return false;
            }
        }
    }

    class ProbableSharedEnvelopeGameState : IGameState<ProbableSharedEnvelopeGameMove>
    {
        internal readonly ProbableSharedEnvelopeGameState initialState;
        internal readonly IEnumerable<(IEnumerable<ProbableSharedEnvelopeGameMove> move, ProbableSharedEnvelopeGameState nextState)> history;
        internal readonly bool isCooperative, isInFirst;

        internal ProbableSharedEnvelopeGameState(bool isCooperative, bool isInFirst)
        {
            initialState = this;

            history = Enumerable.Empty<(IEnumerable<ProbableSharedEnvelopeGameMove> move, ProbableSharedEnvelopeGameState nextState)>();
            this.isCooperative = isCooperative;
            this.isInFirst = isInFirst;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal ProbableSharedEnvelopeGameState(ProbableSharedEnvelopeGameState lastState, IEnumerable<ProbableSharedEnvelopeGameMove> move)
        {
            initialState = lastState.initialState;

            var history = lastState.history.ToList();
            history.Add((move, this));
            this.history = history;

            isCooperative = lastState.isCooperative;
            isInFirst = lastState.isInFirst;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        public IEnumerable<IEnumerable<ProbableSharedEnvelopeGameMove>> LegalMovesByPlayer { get; }

        private IEnumerable<IEnumerable<ProbableSharedEnvelopeGameMove>> CreateLegalMoves()
        {
            return Turn switch
            {
                0 => new[] {
                        new[] { ProbableSharedEnvelopeGameMove.CLAIM_IN_FIRST, ProbableSharedEnvelopeGameMove.CLAIM_IN_SECOND },
                        new[] { ProbableSharedEnvelopeGameMove.NO_OP },
                        new[] { ProbableSharedEnvelopeGameMove.NO_OP },
                    },
                1 => new[] {
                        new[] { ProbableSharedEnvelopeGameMove.NO_OP },
                        new[] { ProbableSharedEnvelopeGameMove.NO_OP },
                        new[] { ProbableSharedEnvelopeGameMove.CHOOSE_FIRST, ProbableSharedEnvelopeGameMove.CHOOSE_SECOND },
                    },
                2 => isCooperative ? null : new[] {
                        new[] { ProbableSharedEnvelopeGameMove.NO_OP },
                        new[] { ProbableSharedEnvelopeGameMove.CHOOSE_FIRST, ProbableSharedEnvelopeGameMove.CHOOSE_SECOND },
                        new[] { ProbableSharedEnvelopeGameMove.NO_OP },
                    },
                _ => null,
            };
        }
            
        public bool IsTerminal => isCooperative ? Turn == 2 : Turn == 3;

        public int Turn => history.Count();

        public int[] GetUtilities()
        {
            if (!IsTerminal) throw new InvalidOperationException("Utilities can only be retrieved from a terminal state");

            if (isCooperative)
            {
                var selection = history.Last().move.ElementAt(2);

                return (selection == ProbableSharedEnvelopeGameMove.CHOOSE_FIRST) == isInFirst ? new[] { 100, 100, 100 } : new[] { 0, 0, 0 };
            }
            else
            {
                var firstSelection = history.ElementAt(Turn - 2).move.ElementAt(2);
                var secondSelection = history.Last().move.ElementAt(1);

                return new[]
                {
                    (firstSelection == ProbableSharedEnvelopeGameMove.CHOOSE_FIRST) == isInFirst ? 0 : 100,
                    (secondSelection == ProbableSharedEnvelopeGameMove.CHOOSE_FIRST) == isInFirst ? 100 : 0,
                    (firstSelection == ProbableSharedEnvelopeGameMove.CHOOSE_FIRST) == isInFirst ? 100 : 0,
                };
            }
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

            var other = (ProbableSharedEnvelopeGameState)obj;

            return isCooperative == other.isCooperative
                && isInFirst == other.isInFirst
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

    enum ProbableSharedEnvelopeGameMove
    {
        CHOOSE_FIRST,
        CHOOSE_SECOND,
        NO_OP,
        CLAIM_IN_FIRST,
        CLAIM_IN_SECOND,
    }
}
