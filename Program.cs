using System;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;

namespace Greed
{
    internal class Program
    {
        private const int MaxRounds = 10;
        private const int MinDieValue = 1;
        private const int MaxDieValue = 7;
        private const int PlayerCount = 2;
        private const string PlayerTypeHuman = "Human";
        private const string PlayerTypeComputer = "Computer";

        private static int round;
        private static int rollNumber;
        private static int firstDieA, firstDieB;
        private static int currentDieA, currentDieB;
        private static int firstTotal, currentTotal;
        private static int currentPlayerIndex;
        private static int roundScore;
        private static readonly Random random = new Random();
        private static bool textToSpeech = false;
        private static SpeechSynthesizer greedTalk;

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

        private static void WriteLog(string message = "")
        {
            Console.WriteLine(message);
            if (textToSpeech && !string.IsNullOrWhiteSpace(message))
            {
                greedTalk.SpeakAsync(message);
            }
        }

        private static void GetSpeechSetting()
        {
            WriteLog("Would you like text-to-speech enabled? (Y/N) ");
            ConsoleKeyInfo keyInfo;

            while (true)
            {
                keyInfo = Console.ReadKey(true);
                switch (char.ToLower(keyInfo.KeyChar))
                {
                    case 'y':
                        textToSpeech = true;
                        greedTalk = new SpeechSynthesizer();
                        greedTalk.Rate = 3;
                        greedTalk.Volume = 100;
                        WriteLog("Text-to-speech enabled.");
                        return;
                    case 'n':
                        textToSpeech = false;
                        WriteLog("Text-to-speech disabled.");
                        return;
                }
            }
        }

        private static void PressKeyForNextPlayer()
        {
            int nextPlayerIndex = (currentPlayerIndex + 1) % PlayerCount;
            WriteLog($"Press a key for {players[nextPlayerIndex].Name}'s turn.");
            Console.ReadKey(true);
        }

        private static void DisplayScores(bool isFinal = false)
        {
            if (!isFinal)
            {
                Console.Clear();
                WriteLog($"Round {round} of {MaxRounds}");
            }

            WriteLog($"{players[0].Name}: {players[0].Score}, {players[1].Name}: {players[1].Score}");

            int difference = Math.Abs(players[0].Score - players[1].Score);

            if (players[0].Score > players[1].Score)
            {
                string verb = isFinal ? "won" : "up";
                WriteLog($"{players[0].Name} {verb} by {difference}{(isFinal ? " points" : "")}.");
            }
            else if (players[1].Score > players[0].Score)
            {
                string verb = isFinal ? "won" : "up";
                WriteLog($"{players[1].Name} {verb} by {difference}{(isFinal ? " points" : "")}.");
            }
            else
            {
                WriteLog("Tie Game!");
            }
        }

        private static bool CheckForBust(int rollNum, int firstRoll, int currentRoll)
        {
            if (currentRoll == firstRoll)
            {
                WriteLog("BUSTED! No points scored.");
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
            double bustProbability = CalculateBustProbability(firstTotal);
            int opponentIndex = (currentPlayerIndex + 1) % PlayerCount;
            int scoreDifference = players[currentPlayerIndex].Score - players[opponentIndex].Score;

            // Base threshold: higher means more conservative
            double stopThreshold = 0.2;

            // Adjust strategy based on score difference
            if (scoreDifference < -50)
            {
                stopThreshold = 0.7; // Far behind - take more risks
            }
            else if (scoreDifference < -20)
            {
                stopThreshold = 0.6; // Behind - slightly more aggressive
            }
            else if (scoreDifference > 50)
            {
                stopThreshold = 0.3; // Far ahead - play it safe
            }
            else if (scoreDifference > 20)
            {
                stopThreshold = 0.4; // Ahead - more conservative
            }

            // Late game adjustment
            if (round >= MaxRounds - 2)
            {
                if (scoreDifference < 0)
                {
                    stopThreshold += 0.1; // Behind in late game - more aggressive
                }
                else if (scoreDifference > 0)
                {
                    stopThreshold -= 0.1; // Ahead in late game - more conservative
                }
            }

            // Factor in current round score value
            double scoreValue = currentRoundScore / 100.0;

            // Risk assessment: higher values mean should stop
            double riskFactor = bustProbability * (rollsRemaining + 1) + (scoreValue * 0.1);

            // Add some randomness to make AI less predictable (±0.1)
            double randomFactor = (random.NextDouble() * 0.2) - 0.1;
            riskFactor += randomFactor;

            // Decision: continue if risk is below threshold
            bool shouldContinue = riskFactor < stopThreshold;

            // Always roll at least twice if we have rolls remaining
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

            WriteLog($"Roll 1: {firstDieA} & {firstDieB} - Total {firstTotal}");

            int maxRolls = firstTotal + 1;
            rollNumber = 2;

            while (rollNumber <= maxRolls)
            {
                int rollsRemaining = maxRolls - rollNumber + 1;

                if (!ComputerShouldContinue(rollsRemaining, roundScore))
                {
                    WriteLog($"{players[currentPlayerIndex].Name} decides to stop rolling.");
                    break;
                }

                System.Threading.Thread.Sleep(800); // Pause for dramatic effect

                currentDieA = random.Next(MinDieValue, MaxDieValue);
                currentDieB = random.Next(MinDieValue, MaxDieValue);
                currentTotal = currentDieA + currentDieB;

                WriteLog($"Roll {rollNumber}: {currentDieA} & {currentDieB} - Total {currentTotal}");

                if (CheckForBust(rollNumber, firstTotal, currentTotal))
                {
                    return;
                }

                roundScore += currentTotal;
                rollNumber++;
            }

            players[currentPlayerIndex].Score += roundScore;
            WriteLog($"{players[currentPlayerIndex].Name} scored {roundScore} points.");
            PressKeyForNextPlayer();
        }

        private static void HumanRoll()
        {
            firstDieA = random.Next(MinDieValue, MaxDieValue);
            firstDieB = random.Next(MinDieValue, MaxDieValue);
            firstTotal = firstDieA + firstDieB;
            roundScore = firstTotal;
            rollNumber = 2;

            WriteLog($"Roll 1: {firstDieA} & {firstDieB} - Total {firstTotal}");

            int maxRolls = firstTotal + 1;

            while (rollNumber <= maxRolls)
            {
                WriteLog($"Press any key to roll again, or 'S' to stop and keep {roundScore} points.");
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if (keyInfo.KeyChar == 's' || keyInfo.KeyChar == 'S')
                {
                    break;
                }

                currentDieA = random.Next(MinDieValue, MaxDieValue);
                currentDieB = random.Next(MinDieValue, MaxDieValue);
                currentTotal = currentDieA + currentDieB;

                WriteLog($"Roll {rollNumber}: {currentDieA} & {currentDieB} - Total {currentTotal}");

                if (CheckForBust(rollNumber, firstTotal, currentTotal))
                {
                    return;
                }

                roundScore += currentTotal;
                rollNumber++;
            }

            players[currentPlayerIndex].Score += roundScore;
            WriteLog($"{players[currentPlayerIndex].Name} scored {roundScore} points.");
            PressKeyForNextPlayer();
        }

        private static void GameLoop()
        {
            for (round = 1; round <= MaxRounds; round++)
            {
                for (currentPlayerIndex = 0; currentPlayerIndex < PlayerCount; currentPlayerIndex++)
                {
                    DisplayScores();
                    WriteLog();
                    WriteLog($"{players[currentPlayerIndex].Name}'s Turn");

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
            WriteLog("FINAL SCORES:");
            WriteLog();
            DisplayScores(isFinal: true);
        }

        private static void GetPlayerName(int playerIndex)
        {
            int displayNumber = playerIndex + 1;
            string input;

            do
            {
                Console.Write($"\nName of Player {displayNumber}: ");
                input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input))
                {
                    WriteLog($"Please give a name for player {displayNumber}");
                }
            } while (string.IsNullOrWhiteSpace(input));

            players[playerIndex].Name = input;
        }

        private static void GetPlayerType(int playerIndex)
        {
            WriteLog();
            Console.Write($"Is {players[playerIndex].Name} a Human or Computer Player? (H/C) ");

            ConsoleKeyInfo keyInfo;
            while (true)
            {
                keyInfo = Console.ReadKey(true);

                switch (char.ToLower(keyInfo.KeyChar))
                {
                    case 'h':
                        players[playerIndex].PlayerType = PlayerTypeHuman;
                        WriteLog("Human");
                        return;
                    case 'c':
                        players[playerIndex].PlayerType = PlayerTypeComputer;
                        WriteLog("Computer");
                        return;
                }
            }
        }

        private static void Main(string[] args)
        {
            Console.Title = "Greed by Charles Martin";
            Console.ForegroundColor = ConsoleColor.White;

            WriteLog("This is a game of luck and skill. First you roll a pair of dice.");
            WriteLog("Additional rolls add to your score, and you can stop at any");
            WriteLog("time. If you repeat your first roll, you lose all points for");
            WriteLog("the round. The winner is the player with the highest score.");
            WriteLog();
            WriteLog("During a human player's turn, press the 's' key to end your turn.");

            for (int i = 0; i < PlayerCount; i++)
            {
                GetPlayerName(i);
                GetPlayerType(i);
            }

            GetSpeechSetting();

            GameLoop();
            EndGame();

            // Proper cleanup of disposable resources
            if (greedTalk != null)
            {
                greedTalk.Dispose();
            }

            Console.ReadKey(true);
        }
    }
}