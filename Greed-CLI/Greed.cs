using System;
using ShadowFlame;

namespace Greed_CLI
{
    internal class Greed
    {
        #region constants and fields
        private static string ApplicationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Greed");

        private const int MaxRounds = 10;
        private const int MinDieValue = 1;
        private const int MaxDieValue = 7;
        private const int PlayerCount = 2;
        private const string PlayerTypeHuman = "Human";
        private const string PlayerTypeComputer = "Computer";
        private static readonly string LastGameFile = Path.Combine(ApplicationDirectory, "last game.txt");
        private static readonly string FullLogFile = Path.Combine(ApplicationDirectory, "full log.txt");
                private static int round;
        private static int rollNumber;
        private static int firstDieA, firstDieB;
        private static int currentDieA, currentDieB;
        private static int firstTotal, currentTotal;
        private static int currentPlayerIndex;
        private static int roundScore;
        private static readonly Random random = new Random();

        private static readonly ShadowFlame.TTS tts = new ShadowFlame.TTS();
        #endregion

        private class PlayerInfo
        {
            public string Name { get; set; }
            public string PlayerType { get; set; }
            public int Score { get; set; }
        }

        private static readonly PlayerInfo[] players = new PlayerInfo[PlayerCount]
        {
            new PlayerInfo(),
            new PlayerInfo()
        };

        private static bool YesNoPrompt(string prompt)
        {
            tts.SpeakAndDisplay(prompt, true);
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            return keyInfo.Key == ConsoleKey.Y ? true : false;
        }

        private static void PressKeyForNextPlayer()
        {
            int nextPlayerIndex = (currentPlayerIndex + 1) % PlayerCount;

            if (nextPlayerIndex == 2 && round == 10)
            {
                tts.SpeakAndDisplay("Press a key  for final Scores.");
            }
            else
            {
                tts.SpeakAndDisplay($"Press a key for {players[nextPlayerIndex].Name}'s turn.");
            }
            Console.ReadKey(true);
        }

        private static void DisplayScores(bool isFinal = false)
        {
            if (!isFinal)
            {
                Console.Clear();
                tts.SpeakAndDisplay($"Round {round} of {MaxRounds}");
            }

            tts.SpeakAndDisplay($"{players[0].Name}: {players[0].Score}, {players[1].Name}: {players[1].Score}");

            int difference = Math.Abs(players[0].Score - players[1].Score);

            if (players[0].Score > players[1].Score)
            {
                string verb = isFinal ? "won" : "up";
                tts.SpeakAndDisplay($"{players[0].Name} {verb} by {difference}{(isFinal ? " points" : "")}.");
            }
            else if (players[1].Score > players[0].Score)
            {
                string verb = isFinal ? "won" : "up";
                tts.SpeakAndDisplay($"{players[1].Name} {verb} by {difference}{(isFinal ? " points" : "")}.");
            }
            else
            {
                tts.SpeakAndDisplay("Tie Game!");
            }
        }

        private static bool CheckForBust(int rollNum, int firstRoll, int currentRoll)
        {
            if (currentRoll == firstRoll)
            {
                tts.SpeakAndDisplay("BUSTED! No points scored.");
                PressKeyForNextPlayer();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the probability of rolling a specific total with 2 dice
        /// </summary>
        private static double CalculateBustProbability(int targetTotal)
        {
            int ways = 0;
            int totalOutcomes = (MaxDieValue - MinDieValue) * (MaxDieValue - MinDieValue);

            for (int die1 = MinDieValue; die1 < MaxDieValue; die1++)
            {
                for (int die2 = MinDieValue; die2 < MaxDieValue; die2++)
                {
                    if (die1 + die2 == targetTotal)
                    {
                        ways++;
                    }
                }
            }

            return (double)ways / totalOutcomes;
        }

        /// <summary>
        /// AI decision logic for whether to continue rolling
        /// </summary>
        private static bool ComputerShouldContinue(int rollsRemaining, int currentRoundScore)
        {
            // Improved decision based on expected value (EV) of continuing versus stopping.
            // Compute bust probability for the "firstTotal" and the expected roll total when not busting.
            double pBust = CalculateBustProbability(firstTotal);

            // Compute distribution statistics for two dice to get expected non-bust total
            int waysTarget = 0;
            double sumAll = 0.0;
            int totalOutcomes = (MaxDieValue - MinDieValue) * (MaxDieValue - MinDieValue);

            for (int d1 = MinDieValue; d1 < MaxDieValue; d1++)
            {
                for (int d2 = MinDieValue; d2 < MaxDieValue; d2++)
                {
                    int tot = d1 + d2;
                    sumAll += tot;
                    if (tot == firstTotal) waysTarget++;
                }
            }

            double expectedNonBustTotal = 0.0;
            if (totalOutcomes - waysTarget > 0)
            {
                expectedNonBustTotal = (sumAll - (waysTarget * firstTotal)) / (totalOutcomes - waysTarget);
            }

            // Expected value if AI commits to attempting all remaining rolls (survive all):
            // EV = P(survive all) * (currentRoundScore + expectedNonBustTotal * rollsRemaining)
            double pSurviveAll = Math.Pow(1.0 - pBust, Math.Max(0, rollsRemaining));
            double evIfCommitAll = pSurviveAll * (currentRoundScore + expectedNonBustTotal * rollsRemaining);

            // Also consider single-roll lookahead (safer estimate):
            double evIfOneMore = (1.0 - pBust) * (currentRoundScore + expectedNonBustTotal);

            // Base decision: continue if either one-roll EV or full-commit EV is better than stopping now.
            bool shouldContinue = evIfOneMore > currentRoundScore || evIfCommitAll > currentRoundScore;

            // Adjust behavior based on score difference and late game urgency
            int opponentIndex = (currentPlayerIndex + 1) % PlayerCount;
            int scoreDifference = players[currentPlayerIndex].Score - players[opponentIndex].Score;

            // If far behind, bias toward taking more risks
            if (scoreDifference < -50)
            {
                shouldContinue = shouldContinue || (evIfOneMore > currentRoundScore * 0.9);
            }
            else if (scoreDifference < -20)
            {
                shouldContinue = shouldContinue || (evIfOneMore > currentRoundScore * 0.95);
            }

            // If far ahead, be more conservative
            if (scoreDifference > 50)
            {
                shouldContinue = shouldContinue && (pBust < 0.2);
            }

            // Late-game: if behind, be more aggressive; if ahead, be more conservative
            if (round >= MaxRounds - 2)
            {
                if (scoreDifference < 0)
                {
                    shouldContinue = shouldContinue || (evIfOneMore > currentRoundScore * 0.85);
                }
                else if (scoreDifference > 0)
                {
                    shouldContinue = shouldContinue && (evIfOneMore > currentRoundScore * 1.05);
                }
            }

            // Add small randomness so the AI is not deterministic
            double randomFactor = (random.NextDouble() * 0.2) - 0.1;
            if (randomFactor > 0.08)
            {
                shouldContinue = !shouldContinue;
            }

            // Always roll at least twice if this is the second roll and there are rolls remaining
            if (rollNumber == 2 && rollsRemaining > 0)
            {
                shouldContinue = true;
            }

            return shouldContinue;
        }

        private static void ComputerRoll()
        {
            firstDieA = random.Next(MinDieValue, MaxDieValue);
            firstDieB = random.Next(MinDieValue, MaxDieValue);
            firstTotal = firstDieA + firstDieB;
            roundScore = firstTotal;

            tts.SpeakAndDisplay($"Roll 1: {firstDieA} & {firstDieB} - Total {firstTotal}");

            int maxRolls = firstTotal + 1;
            rollNumber = 2;

            while (rollNumber <= maxRolls)
            {
                int rollsRemaining = maxRolls - rollNumber + 1;

                if (!ComputerShouldContinue(rollsRemaining, roundScore))
                {
                    tts.SpeakAndDisplay($"{players[currentPlayerIndex].Name} decides to stop rolling.");
                    break;
                }

                System.Threading.Thread.Sleep(800); // Pause for dramatic effect

                currentDieA = random.Next(MinDieValue, MaxDieValue);
                currentDieB = random.Next(MinDieValue, MaxDieValue);
                currentTotal = currentDieA + currentDieB;

                tts.SpeakAndDisplay($"Roll {rollNumber}: {currentDieA} & {currentDieB} - Total {currentTotal}");

                if (CheckForBust(rollNumber, firstTotal, currentTotal))
                {
                    return;
                }

                roundScore += currentTotal;
                rollNumber++;
            }

            players[currentPlayerIndex].Score += roundScore;
            tts.SpeakAndDisplay($"{players[currentPlayerIndex].Name} scored {roundScore} points.");
            PressKeyForNextPlayer();
        }

        private static void HumanRoll()
        {
            firstDieA = random.Next(MinDieValue, MaxDieValue);
            firstDieB = random.Next(MinDieValue, MaxDieValue);
            firstTotal = firstDieA + firstDieB;
            roundScore = firstTotal;
            rollNumber = 2;

            tts.SpeakAndDisplay($"Roll 1: {firstDieA} & {firstDieB} - Total {firstTotal}");

            int maxRolls = firstTotal + 1;

            while (rollNumber <= maxRolls)
            {
                tts.SpeakAndDisplay($"Press any key to roll again,");
                tts.SpeakAndDisplay($" or 'S' to stop and keep {roundScore} points.");
                Console.WriteLine();
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if (keyInfo.KeyChar == 's' || keyInfo.KeyChar == 'S')
                {
                    break;
                }

                currentDieA = random.Next(MinDieValue, MaxDieValue);
                currentDieB = random.Next(MinDieValue, MaxDieValue);
                currentTotal = currentDieA + currentDieB;



                tts.SpeakAndDisplay($"Roll {rollNumber}: {currentDieA} & {currentDieB} - Total {currentTotal}");

                if (CheckForBust(rollNumber, firstTotal, currentTotal))
                {
                    return;
                }

                roundScore += currentTotal;
                rollNumber++;
            }

            players[currentPlayerIndex].Score += roundScore;
            tts.SpeakAndDisplay($"{players[currentPlayerIndex].Name} scored {roundScore} points.");
            PressKeyForNextPlayer();
        }

        private static void GameLoop()
        {
            for (round = 1; round <= MaxRounds; round++)
            {
                for (currentPlayerIndex = 0; currentPlayerIndex < PlayerCount; currentPlayerIndex++)
                {
                    DisplayScores();
                    Console.WriteLine();
                    tts.SpeakAndDisplay($"{players[currentPlayerIndex].Name}'s Turn");

                    if (players[currentPlayerIndex].PlayerType == PlayerTypeComputer)
                    {
                        ComputerRoll();
                    }
                    else
                    {
                        HumanRoll();
                    }
                }
            }
        }

        private static void EndGame()
        {
            tts.SpeakAndDisplay("FINAL SCORES:");
            Console.WriteLine();
            DisplayScores(isFinal: true);
        }

        private static void GetPlayerName(int playerIndex)
        {
            int displayNumber = playerIndex + 1;
            string input;

            do
            {
                Console.WriteLine();
                tts.SpeakAndDisplay($"Name of Player {displayNumber}: ", true);
                input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input))
                {
                    tts.SpeakAndDisplay($"Please give a name for player {displayNumber}"); return;
                }
            } while (string.IsNullOrWhiteSpace(input));

            players[playerIndex].Name = input;
        }

        private static void GetPlayerType(int playerIndex)
        {
            Console.WriteLine();
            tts.SpeakAndDisplay($"Is {players[playerIndex].Name} a Human or Computer Player? (H/C) ", true);

            ConsoleKeyInfo keyInfo;
            while (true)
            {
                keyInfo = Console.ReadKey(true);

                switch (char.ToLower(keyInfo.KeyChar))
                {
                    case 'h':
                        players[playerIndex].PlayerType = PlayerTypeHuman;
                        tts.SpeakAndDisplay("Human");
                        return;
                    case 'c':
                        players[playerIndex].PlayerType = PlayerTypeComputer;
                        tts.SpeakAndDisplay("Computer");
                        return;
                }
            }
        }

        private static void Main(string[] args)
        {
            Console.Title = "Greed by Charles Martin";
            Console.ForegroundColor = ConsoleColor.White;

            Directory.CreateDirectory(ApplicationDirectory);

            Console.WriteLine();
            tts.SpeakAndDisplay("Welcome to Greed!  A dice ggame by HiTechCharles");
            tts.SpeakAndDisplay("\n\nThis is a game of luck and skill. First you roll a pair of dice.");
            tts.SpeakAndDisplay("Additional rolls add to your score, and you can stop at any");
            tts.SpeakAndDisplay("time. If you repeat your first roll, you lose all points for");
            tts.SpeakAndDisplay("the round. The winner is the player with the highest score.");
            Console.WriteLine();

            for (int i = 0; i < PlayerCount; i++)
            {
                GetPlayerName(i);
                GetPlayerType(i);
            }

            GameLoop();
            EndGame();

            Console.ReadKey(true);
        }
    }
}