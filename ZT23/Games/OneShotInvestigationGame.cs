using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class OneShotInvestigationGame : IGame<OneShotInvestigationGameMove, OneShotInvestigationGameState>
    {
        public int NumberOfPlayers => 3;

        public IEnumerable<(IEnumerable<OneShotInvestigationGameMove> combinedMove, OneShotInvestigationGameState nextState)> GetHistory(OneShotInvestigationGameState state) => state.history;

        public OneShotInvestigationGameState GetInitialStateFromCurrent(OneShotInvestigationGameState state) => state.initialState;

        public IEnumerable<(OneShotInvestigationGameState state, int weight, int id)> GetInitialStates() => new[] {
            (new OneShotInvestigationGameState(true, true), 1, 300),
            (new OneShotInvestigationGameState(false, true), 1, 301),
            (new OneShotInvestigationGameState(true, false), 1, 302),
            (new OneShotInvestigationGameState(false, false), 1, 303),
        };

        public IEnumerable<(OneShotInvestigationGameState state, int weight, int id)> GetPerceivedInitialStatesFromActual(OneShotInvestigationGameState state, int player)
            => player == 0 ? GetInitialStates() : GetInitialStates().Where(initial => initial.state.isFirstInnocent == state.isFirstInnocent && initial.state.isSecondInnocent == state.isSecondInnocent);

        public (Func<OneShotInvestigationGameState, bool> percepts, int id)[] GetPerceptsFromMove(OneShotInvestigationGameState state, IEnumerable<OneShotInvestigationGameMove> combinedMove)
        {
            var playerSeen = state.Turn == 1 ? 1 : 2;
            var moveSeen = state.history.ElementAt(state.Turn - 1).move.ElementAt(playerSeen);

            return new (Func<OneShotInvestigationGameState, bool>, int)[] {
                (state => state.history.ElementAt(state.Turn - 1).move.ElementAt(playerSeen) == moveSeen, 201 + moveSeen.GetIndex()),
                (state => true, 200),
                (state => true, 200),
            };
        }

        public OneShotInvestigationGameState GetStateAfterCombinedMove(OneShotInvestigationGameState state, IEnumerable<OneShotInvestigationGameMove> combinedMove)
            => new OneShotInvestigationGameState(state, combinedMove);

        public bool IsClaim(OneShotInvestigationGameMove move, out int[] receivers, out Func<OneShotInvestigationGameState, bool> claim, out int id)
        {
            switch (move)
            {
                case OneShotInvestigationGameMove.CLAIM_BOTH_INNOCENT:
                    receivers = new[] { 0 };
                    claim = state => state.isFirstInnocent && state.isSecondInnocent;
                    id = 100;
                    return true;

                case OneShotInvestigationGameMove.CLAIM_FIRST_EVIL:
                    receivers = new[] { 0 };
                    claim = state => !state.isFirstInnocent && state.isSecondInnocent;
                    id = 101;
                    return true;

                case OneShotInvestigationGameMove.CLAIM_SECOND_EVIL:
                    receivers = new[] { 0 };
                    claim = state => state.isFirstInnocent && !state.isSecondInnocent;
                    id = 102;
                    return true;

                case OneShotInvestigationGameMove.CLAIM_BOTH_EVIL:
                    receivers = new[] { 0 };
                    claim = state => !state.isFirstInnocent && !state.isSecondInnocent;
                    id = 103;
                    return true;

                default:
                    receivers = null;
                    claim = null;
                    id = 0;
                    return false;
            }
        }
    }

    class OneShotInvestigationGameState : IGameState<OneShotInvestigationGameMove>
    {
        internal readonly OneShotInvestigationGameState initialState;
        internal readonly IEnumerable<(IEnumerable<OneShotInvestigationGameMove> move, OneShotInvestigationGameState nextState)> history;
        internal readonly bool isFirstInnocent, isSecondInnocent;

        internal OneShotInvestigationGameState(bool isFirstInnocent, bool isSecondInnocent)
        {
            initialState = this;

            history = Enumerable.Empty<(IEnumerable<OneShotInvestigationGameMove> move, OneShotInvestigationGameState nextState)>();
            this.isFirstInnocent = isFirstInnocent;
            this.isSecondInnocent = isSecondInnocent;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal OneShotInvestigationGameState(OneShotInvestigationGameState lastState, IEnumerable<OneShotInvestigationGameMove> move)
        {
            initialState = lastState.initialState;

            var history = lastState.history.ToList();
            history.Add((move, this));
            this.history = history;

            isFirstInnocent = lastState.isFirstInnocent;
            isSecondInnocent = lastState.isSecondInnocent;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        public IEnumerable<IEnumerable<OneShotInvestigationGameMove>> LegalMovesByPlayer { get; }

        private IEnumerable<IEnumerable<OneShotInvestigationGameMove>> CreateLegalMoves()
        {
            var claims = new[] {
                OneShotInvestigationGameMove.CLAIM_BOTH_INNOCENT,
                OneShotInvestigationGameMove.CLAIM_FIRST_EVIL,
                OneShotInvestigationGameMove.CLAIM_SECOND_EVIL,
                OneShotInvestigationGameMove.CLAIM_BOTH_EVIL,
            };

            return Turn switch
            {
                0 => new[] {
                        new[] { OneShotInvestigationGameMove.NO_OP },
                        claims,
                        new[] { OneShotInvestigationGameMove.NO_OP },
                    },
                1 => new[] {
                        new[] { OneShotInvestigationGameMove.NO_OP },
                        new[] { OneShotInvestigationGameMove.NO_OP },
                        claims,
                    },
                2 => new[] {
                        new[] {
                            OneShotInvestigationGameMove.GUESS_BOTH_INNOCENT,
                            OneShotInvestigationGameMove.GUESS_FIRST_EVIL,
                            OneShotInvestigationGameMove.GUESS_SECOND_EVIL,
                            OneShotInvestigationGameMove.GUESS_BOTH_EVIL,
                        },
                        new[] { OneShotInvestigationGameMove.NO_OP },
                        new[] { OneShotInvestigationGameMove.NO_OP },
                    },
                _ => null,
            };
        }

        public bool IsTerminal => Turn == 3;

        public int Turn => history.Count();

        public int[] GetUtilities()
        {
            if (!IsTerminal) throw new InvalidOperationException("Utilities can only be retrieved from a terminal state");

            var guessersMove = history.Last().move.First();

            var guesserWasRight = (guessersMove == OneShotInvestigationGameMove.GUESS_BOTH_INNOCENT && isFirstInnocent && isSecondInnocent)
                || (guessersMove == OneShotInvestigationGameMove.GUESS_FIRST_EVIL && !isFirstInnocent && isSecondInnocent)
                || (guessersMove == OneShotInvestigationGameMove.GUESS_SECOND_EVIL && isFirstInnocent && !isSecondInnocent)
                || (guessersMove == OneShotInvestigationGameMove.GUESS_BOTH_EVIL && !isFirstInnocent && !isSecondInnocent);

            var someGuiltyWasGuessed = guessersMove switch
            {
                OneShotInvestigationGameMove.GUESS_FIRST_EVIL => !isFirstInnocent,
                OneShotInvestigationGameMove.GUESS_SECOND_EVIL => !isSecondInnocent,
                OneShotInvestigationGameMove.GUESS_BOTH_EVIL => !isFirstInnocent || !isSecondInnocent,
                _ => false,
            };

            return new[]
            {
                guesserWasRight ? 100 : 0,
                isFirstInnocent ? (guesserWasRight ? 100 : 0) : (someGuiltyWasGuessed ? 0 : 100),
                isSecondInnocent ? (guesserWasRight ? 100 : 0) : (someGuiltyWasGuessed ? 0 : 100),
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

            var other = (OneShotInvestigationGameState)obj;

            return isFirstInnocent == other.isFirstInnocent
                && isSecondInnocent == other.isSecondInnocent
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

    enum OneShotInvestigationGameMove
    {
        NO_OP,
        CLAIM_BOTH_INNOCENT,
        CLAIM_FIRST_EVIL,
        CLAIM_SECOND_EVIL,
        CLAIM_BOTH_EVIL,
        GUESS_BOTH_INNOCENT,
        GUESS_FIRST_EVIL,
        GUESS_SECOND_EVIL,
        GUESS_BOTH_EVIL
    }
}
