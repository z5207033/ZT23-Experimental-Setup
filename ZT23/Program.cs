using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class Program
    {
        // Change to configure the number of simulations to run
        const int N_RUNS = 1000000;

        static void Main()
        {
            EvaluateGoodOrEvilGame(N_RUNS);
            EvaluateWeightedGoodOrEvilGame(N_RUNS);

            Console.WriteLine(new string('-', 59));
            Console.WriteLine();

            EvaluateCooperativeSpiesGame(N_RUNS);
            EvaluateEnvelopeGame(N_RUNS);

            Console.WriteLine(new string('-', 59));
            Console.WriteLine();

            EvaluateProbableSharedEnvelopeGame(N_RUNS);

            Console.WriteLine(new string('-', 59));
            Console.WriteLine();

            EvaluateOneShotInvestigationGame(N_RUNS);
        }

        static void EvaluateGoodOrEvilGame(int numberOfRuns)
        {
            var game = new GoodOrEvilGame();

            var player1 = new IPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>[]
            {
                new RandomPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
                new TrustingPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
                new ProposedPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
            };

            var player2 = new IPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>[]
            {
                new RandomPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
                new TruthfulPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
                new ProposedPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
            };

            EvaluateTwoPlayerGame(game, player1, player2, numberOfRuns);
        }

        static void EvaluateWeightedGoodOrEvilGame(int numberOfRuns)
        {
            var game = new WeightedGoodOrEvilGame();

            var player1 = new IPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>[]
            {
                new RandomPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
                new RegularPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
                new TrustingPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
                new ProposedPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
            };

            var player2 = new IPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>[]
            {
                new RandomPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
                new TruthfulPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
                new ProposedPlayer<GoodOrEvilGameMove, GoodOrEvilGame, GoodOrEvilGameState>(),
            };

            EvaluateTwoPlayerGame(game, player1, player2, numberOfRuns);
        }

        static void EvaluateCooperativeSpiesGame(int numberOfRuns)
        {
            var game = new CooperativeSpiesGame();

            var player1 = new IPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>[]
            {
                new RandomPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new TrustingPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new ProposedPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
            };

            var player2 = new IPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>[]
            {
                new RandomPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new TruthfulPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new ProposedPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
            };

            EvaluateTwoPlayerGame(game, player1, player2, numberOfRuns);
        }

        static void EvaluateEnvelopeGame(int numberOfRuns)
        {
            var game = new EnvelopeGame();

            var player1 = new IPlayer<EnvelopeGameMove, EnvelopeGame, EnvelopeGameState>[]
            {
                new RandomPlayer<EnvelopeGameMove, EnvelopeGame, EnvelopeGameState>(),
                new TrustingPlayer<EnvelopeGameMove, EnvelopeGame, EnvelopeGameState>(),
                new ProposedPlayer<EnvelopeGameMove, EnvelopeGame, EnvelopeGameState>(),
            };

            var player2 = new IPlayer<EnvelopeGameMove, EnvelopeGame, EnvelopeGameState>[]
            {
                new RandomPlayer<EnvelopeGameMove, EnvelopeGame, EnvelopeGameState>(),
                new TruthfulPlayer<EnvelopeGameMove, EnvelopeGame, EnvelopeGameState>(),
                new ProposedPlayer<EnvelopeGameMove, EnvelopeGame, EnvelopeGameState>(),
            };

            EvaluateTwoPlayerGame(game, player1, player2, numberOfRuns);
        }

        static void EvaluateTwoPlayerGame<MoveT, GameT, GameStateT>(GameT game, IEnumerable<IPlayer<MoveT, GameT, GameStateT>> player1, IEnumerable<IPlayer<MoveT, GameT, GameStateT>> player2, int numberOfRuns)
            where GameT : IGame<MoveT, GameStateT>
            where GameStateT : IGameState<MoveT>
        {
            Console.WriteLine(game.GetType().Name.Substring(0, game.GetType().Name.Length));
            Console.WriteLine();
            Console.WriteLine($"\t   {string.Join(" | ", player2.Select(p2 => p2.GetType().Name[..^8].PadLeft(14)))}");

            foreach (var p1 in player1)
            {
                Console.Write(p1.GetType().Name[..^8].PadRight(8));

                foreach (var p2 in player2)
                {
                    var totals = new double[game.NumberOfPlayers];

                    for (int i = 0; i < numberOfRuns; i++)
                    {
                        var terminalState = GameManager.Play(game, p1, p2);
                        var utilities = terminalState.GetUtilities();

                        for (int player = 0; player < utilities.Length; player++) totals[player] += utilities[player];
                    }

                    Console.Write(" | " + string.Join(", ", totals.Select(total => string.Format("{0:0.00}", total / numberOfRuns))).PadLeft(14));
                }

                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        static void EvaluateProbableSharedEnvelopeGame(int numberOfRuns)
        {
            var game = new ProbableSharedEnvelopeGame();

            var player1 = new IPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>[]
            {
                new RandomPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>(),
                new TruthfulPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>(),
                new ProposedPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>(),
            };

            var player2 = new IPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>[]
            {
                new RandomPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>(),
                new TrustingPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>(),
                new ProposedPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>(),
            };

            var player3 = new IPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>[]
            {
                new RandomPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>(),
                new TrustingPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>(),
                new ProposedPlayer<ProbableSharedEnvelopeGameMove, ProbableSharedEnvelopeGame, ProbableSharedEnvelopeGameState>(),
            };

            Console.WriteLine(game.GetType().Name.Substring(0, game.GetType().Name.Length));
            Console.WriteLine();
            Console.WriteLine($"\t   {string.Join(" | ", player1.Select(p1 => p1.GetType().Name[..^8].PadLeft(14)))}");

            foreach (var p3 in player3)
            {
               Console.Write(p3.GetType().Name[..^8].PadRight(8));

               foreach (var p1 in player1)
               {
                   var totals = new double[2];

                   for (int i = 0; i < numberOfRuns; i++)
                   {
                       var terminalState = GameManager.Play(game, p1, player2.First(), p3);
                       var utilities = terminalState.GetUtilities();

                       totals[0] += utilities[2];
                       totals[1] += utilities[0];
                   }

                   Console.Write(" | " + string.Join(", ", totals.Select(total => string.Format("{0:0.00}", total / numberOfRuns))).PadLeft(14));
               }

               Console.WriteLine();
            }

            Console.WriteLine(new string('-', 59));

            foreach (var p2 in player2)
            {
                Console.Write(p2.GetType().Name[..^8].PadRight(8));

                foreach (var p1 in player1)
                {
                    double total = 0;

                    for (int i = 0; i < numberOfRuns; i++)
                    {
                        var terminalState = GameManager.PlayWithFilteredInitialStates(game, state => !state.isCooperative, p1, p2, player3.First());
                        var utilities = terminalState.GetUtilities();

                        total += utilities[1];
                    }

                    Console.Write(" | " + string.Format("{0:0.00}", total / numberOfRuns).PadLeft(14));
                }

                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        static void EvaluateOneShotInvestigationGame(int numberOfRuns)
        {
            var game = new OneShotInvestigationGame();

            var player1 = new IPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>[]
            {
                new RandomPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>(),
                new TrustingPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>(),
                new ProposedPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>(),
            };

            var player2 = new IPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>[]
            {
                new RandomPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>(),
                new TruthfulPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>(),
                new ProposedPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>(),
            };

            var player3 = new IPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>[]
            {
                new RandomPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>(),
                new TruthfulPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>(),
                new ProposedPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>(),
            };

            var suspectPairs = new List<(
                IPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState> p2,
                IPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState> p3)>();

            for (int p2i = 0; p2i < player2.Length; p2i++)
            {
                for (int p3i = p2i; p3i < player3.Length; p3i++)
                {
                    suspectPairs.Add((player2[p2i], player3[p3i]));
                }
            }

            Console.WriteLine(game.GetType().Name.Substring(0, game.GetType().Name.Length));
            Console.WriteLine();
            Console.WriteLine(new string(' ', 11) + string.Join(" | ", suspectPairs.Select(pair => $"{pair.p2.GetType().Name[..^8]}, {pair.p3.GetType().Name[..^8]}".PadLeft(22))));

            foreach (var p1 in player1)
            {
                Console.Write(p1.GetType().Name[..^8].PadRight(8));

                foreach (var (p2, p3) in suspectPairs)
                {
                    var totals = new double[game.NumberOfPlayers];

                    for (int i = 0; i < numberOfRuns; i++)
                    {
                        var terminalState = GameManager.Play(game, p1, p2, p3);
                        var utilities = terminalState.GetUtilities();

                        for (int player = 0; player < utilities.Length; player++) totals[player] += utilities[player];
                    }

                    Console.Write(" | " + string.Join(", ", totals.Select(total => string.Format("{0:0.00}", total / numberOfRuns))).PadLeft(22));
                }

                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine();
        }

        static void TestOsiResponses()
        {
            var game = new OneShotInvestigationGame();
            var player = new ProposedPlayer<OneShotInvestigationGameMove, OneShotInvestigationGame, OneShotInvestigationGameState>();

            var claims = new[] {
                OneShotInvestigationGameMove.CLAIM_BOTH_INNOCENT,
                OneShotInvestigationGameMove.CLAIM_FIRST_EVIL,
                OneShotInvestigationGameMove.CLAIM_SECOND_EVIL,
                OneShotInvestigationGameMove.CLAIM_BOTH_EVIL,
            };

            var possibleStarts = new[] { claims, claims }
                .CartesianProduct()
                .Select(claims => (new[] {
                    new[] { OneShotInvestigationGameMove.NO_OP, claims.First(), OneShotInvestigationGameMove.NO_OP },
                    new[] { OneShotInvestigationGameMove.NO_OP, OneShotInvestigationGameMove.NO_OP, claims.Last() },
                }, claims));

            foreach (var (start, claimsMade) in possibleStarts)
            {
                var state = game.GetInitialStates().First().state;
                state = game.GetStateAfterCombinedMove(state, start.First());
                state = game.GetStateAfterCombinedMove(state, start.Last());

                GameManager.ReplayToState(game, state, 0, player);

                var bestMoves = player.GetAllBestMoves(state.LegalMovesByPlayer.First());

                Console.WriteLine($"For claims {string.Join(", ", claimsMade)}, possible moves are [{string.Join(", ", bestMoves)}]");
            }
        }
    }
}
