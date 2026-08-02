using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Security.Policy;
using System.Speech.Synthesis;

namespace Greed
{
    internal class Program
    {
        #region constants and fields
        private static string ApplicationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Greed");

        private const int MaxRounds = 10;
        private const int MinDieValue = 1;
        private const int MaxDieValue = 7;
        private const int PlayerCount = 2;
        private const string PlayerTypeHuman = "Human";
        private const string PlayerTypeComputer = "Computer";
        private const int SpeechRate = 4;
        private const int SpeechVolume = 100;
        private static readonly string LastGameFile  = Path.Combine(ApplicationDirectory, "last game.txt");   
        private static readonly string FullLogFile = Path.Combine(ApplicationDirectory, "full log.txt");
        private static readonly string OptionsFile = Path.Combine(ApplicationDirectory, "options.txt");
        
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

        private static void WriteLog(string message = "", bool SameLine = false)
        {
            if (SameLine)
            {
                Console.Write(message);
            }
            else
            {
                Console.WriteLine(message);
            }

            if (textToSpeech && greedTalk != null && !string.IsNullOrWhiteSpace(message))
            {
                greedTalk.SpeakAsync(message);
            }
        }

        private static bool YesNoPrompt(string prompt)
        {
            WriteLog(prompt, true);
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            return keyInfo.Key == ConsoleKey.Y ? true : false;
        }

        // Original AskToggleSpeech removed. Use AskToggleSpeechSafe instead.


        private static void SaveOptions()
        {
            using (StreamWriter writer = new StreamWriter(OptionsFile, false))

            {
                writer.WriteLine($"TextToSpeech={textToSpeech}");
            }
        }

        private static void InitializeSpeechSynthesizer()
        {
            if (textToSpeech)
            {
                greedTalk = new SpeechSynthesizer
                {
                    Rate = SpeechRate,
                    Volume = SpeechVolume
                };
            }
        }

        private static void LoadOptions()
        {
            if (File.Exists(OptionsFile))
            {
                using (StreamReader reader = new StreamReader(OptionsFile))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("TextToSpeech="))
                        {
                            textToSpeech = line.Substring("TextToSpeech=".Length).Trim().ToLower() == "true";
                        }
                    }
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
                WriteLog($"Press any key to roll again,"); 
                WriteLog($" or 'S' to stop and keep {roundScore} points.");
                Console.WriteLine();
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
                    Console.WriteLine();
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
                WriteLog($"Name of Player {displayNumber}: ", true);
                input = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(input))
                {
                    WriteLog($"Please give a name for player {displayNumber}"); return;
                }
            } while (string.IsNullOrWhiteSpace(input));

            players[playerIndex].Name = input;
        }

        private static void GetPlayerType(int playerIndex)
        {
            Console.WriteLine();
            WriteLog($"Is {players[playerIndex].Name} a Human or Computer Player? (H/C) ", true);

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

        private static void AskToggleSpeechSafe()
        {
            try
            {
                bool enable = YesNoPrompt("Enable text-to-speech? (Y/N) ");

                if (enable)
                {
                    // If already initialized, keep it; otherwise attempt to (re)initialize safely.
                    if (!textToSpeech || greedTalk == null)
                    {
                        try
                        {
                            greedTalk?.Dispose();
                            greedTalk = new SpeechSynthesizer
                            {
                                Rate = SpeechRate,
                                Volume = SpeechVolume
                            };
                            textToSpeech = true;
                            greedTalk.SpeakAsync("Text to speech enabled.");
                        }
                        catch (Exception ex)
                        {
                            // Initialization failed; disable TTS to avoid later NullReference.
                            textToSpeech = false;
                            greedTalk = null;
                            WriteLog($"Unable to initialize text-to-speech: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // User chose to disable TTS
                    textToSpeech = false;
                    if (greedTalk != null)
                    {
                        greedTalk.Dispose();
                        greedTalk = null;
                    }
                }

                SaveOptions();
            }
            catch (Exception ex)
            {
                // Catch-all safety to ensure application remains usable even if something goes wrong.
                textToSpeech = false;
                greedTalk = null;
                WriteLog($"Text-to-speech toggle failed: {ex.Message}");
                SaveOptions();
            }
        }

        private static void Main(string[] args)
        {
            Console.Title = "Greed by Charles Martin";
            Console.ForegroundColor = ConsoleColor.White;

            Directory.CreateDirectory(ApplicationDirectory);
            
            LoadOptions();
            InitializeSpeechSynthesizer();
            AskToggleSpeechSafe();

            // Fail-safe: if the user enabled text-to-speech but the
            // synthesizer failed to initialize, disable the option to
            // avoid NullReferenceException later.
            if (textToSpeech && greedTalk == null)
            {
                textToSpeech = false;
                WriteLog("Text-to-speech could not be initialized and has been disabled.");
            }
            

            Console.WriteLine();
            WriteLog("This is a game of luck and skill. First you roll a pair of dice.");
            WriteLog("Additional rolls add to your score, and you can stop at any");
            WriteLog("time. If you repeat your first roll, you lose all points for");
            WriteLog("the round. The winner is the player with the highest score.");
            Console.WriteLine();

            for (int i = 0; i < PlayerCount; i++)
            {
                GetPlayerName(i);
                GetPlayerType(i);
            }

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