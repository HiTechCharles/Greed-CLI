using System;

namespace Greed
{
    internal class Program
    {
        #region PUBLIC VARIABLES
        /*
            round              each player rolls 10 times, starting at 1
            diff               difference between scores
            RollNum           number of dice rolls this turn
            FDA, FDB         first roll dice
            CDA, CDB         subsequent dice rolls
            FTotal, CTotal  first, and current roll dice totals  
            turn            keeps track of which player is rolling
            RoundScore      score for current round
            RNG             random number generator
            */
        public static int Round, Diff, RollNum, FDA, FDB, CDA, CDB, FTotal, CTotal, Turn, RoundScore;
        public static uint seed = (uint)Math.Pow(System.DateTime.Now.TimeOfDay.TotalMilliseconds, 11.0 / 7.0);  //rng seed based on time of day
        public static Random RNG = new Random((int)seed);  //create random number generator

        struct PInfo  //stores player data
        {
            public string Name;  //player name
            public string PlayerType;  //human or computer
            public int Score;  //current score
        }

        static PInfo[] PlayerData = new PInfo[2];  //a 2 element array of playerinfo
        #endregion

        static void WriteLog(string msg = "")  //write to console.  default is blank line.
        {
            Console.WriteLine(msg);  //write to screen
        }

        static void PressKey()  //press a key for next player's turn
        {
            int OTurn = 0;  //opposite player
            switch (Turn)  
            {
                case 0:  //currently player 0, opposite is 1
                    OTurn = 1;
                    break;
                case 1:
                    OTurn = 0;  // current turn player 1, opposite 0
                    break;
            }
            
            //write press a key message with player's name
            WriteLog("Press a key for " + PlayerData[OTurn].Name + "'s turn.");
            Console.ReadKey(true);  //get a single keypress, 
        }

        static void ScoreBlock()  //print score data to screen after each player takes a turn
        {
            Console.Clear();  //clear console
            WriteLog("Round " + Round + " of 10"); //wanna go 10 rounds? :)
            WriteLog(PlayerData[0].Name + ": " + PlayerData[0].Score + ", " +  //write names and scores
                PlayerData[1].Name + ": " + PlayerData[1].Score);

            if (PlayerData[0].Score > PlayerData[1].Score)  //determine who is leading
            {
                Diff = PlayerData[0].Score - PlayerData[1].Score;  //p1>p2
                WriteLog(PlayerData[0].Name + " up by " + Diff);
            }
            else if (PlayerData[1].Score > PlayerData[0].Score)  //p2>p1
            {
                Diff = PlayerData[1].Score - PlayerData[0].Score;
                WriteLog(PlayerData[1].Name + " up by " + Diff);
            }
            else
            {
                WriteLog("Tie Game!");  //tie game
            }
        }

        static void ComputerRoll()  //performs rolls for computer players
        {
            FDA = RNG.Next(1, 7);
            FDB = RNG.Next(1, 7);  //roll first 2 dice
            FTotal = FDA + FDB;  //first roll total
            RoundScore = FTotal;  //each successful dice roll total adds to score including first

            //write first roll to console
            WriteLog("Roll 1: " + FDA + " & " + FDB + " - Total " + FTotal);

            //for computer players, FTotal is the number of dice rolls they do.
            for (RollNum = 2; RollNum <= FTotal+1; RollNum++)
            {
                CDA = RNG.Next(1, 7);  //roll second pair of dice until busted or loop ends
                CDB = RNG.Next(1, 7);
                CTotal = CDA + CDB;  //current roll total
                //write dice roll
                WriteLog("Roll " + RollNum + ":  " + CDA + " & " + CDB + " - Total " + CTotal);

                if (CTotal == FTotal)  //if first roll = other dice roll, turn over
                {
                    WriteLog("BUSTED!  No points scored.");
                    PressKey();  //press a key for other player's turn
                    return;  //end function immediately
                }
                else
                {
                    RoundScore += CTotal;  //each successful dice roll total adds to score including first
                }  //end if

            }  //end for.    error } expected?  Where?  I can't see!
            PlayerData[Turn].Score += RoundScore;  //add round score to player's total score
            //show points scored this round
            WriteLog(PlayerData[Turn].Name + " scored " + RoundScore + " points.");
            PressKey();  //press key for next player
        }

        static void GameLoop()  //main game loop
        {
            for (Round = 1; Round < 11; Round++)  //each player gets 10 turns
            {
                Turn = 0;   //first player up  (get the zero turn John Deer mower)
                ScoreBlock();  //display scores
                WriteLog();  //blank line
                WriteLog(PlayerData[0].Name + "'s Turn");

                //run the appropriate function for human or computer input
                if (PlayerData[0].PlayerType == "Computer")
                {
                    ComputerRoll();  //first player is a computer  (KITT, HAL9000)
                }
                else
                {
                    HumanRoll(); //first player is human
                }

                Turn = 1;  //second player
                ScoreBlock();
                WriteLog();
                WriteLog(PlayerData[1].Name + "'s Turn");

                if (PlayerData[1].PlayerType == "Computer")
                {
                    ComputerRoll();  //Second player is a computer  (ENIAC, Apple IIe Platinum)
                }
                else
                {
                    HumanRoll();
                }
            }
        }

        static void EndGame() //show final scores and the game winner
        {
            WriteLog("FINAL SCORES:");
            WriteLog();

            //same as ScoreBlock();  didn't use it because it shows the round and clears the screen
            WriteLog(PlayerData[0].Name + ": " + PlayerData[0].Score + ", " +  //write names and scores
                PlayerData[1].Name + ": " + PlayerData[1].Score);

            if (PlayerData[0].Score > PlayerData[1].Score)  //determine who is leading
            {
                Diff = PlayerData[0].Score - PlayerData[1].Score;  //p1>p2
                WriteLog(PlayerData[0].Name + " won by " + Diff + " points.");
            }
            else if (PlayerData[1].Score > PlayerData[0].Score)  //p2>p1
            {
                Diff = PlayerData[1].Score - PlayerData[0].Score;
                WriteLog(PlayerData[1].Name + " won by " + Diff + " points.");
            }
            else
            {
                WriteLog("Tie Game!");  //tie game
            }
        }

        static void HumanRoll()  //roll dice and take input for humans
        {
            //variables and structure are nearly identical
            FDA = RNG.Next(1, 7);
            FDB = RNG.Next(1, 7);  //roll first 2 dice
            FTotal = FDA + FDB;  //first roll total
            RoundScore = FTotal;  //each successful dice roll total adds to score including first
            bool TurnOver = false;  //is human done with their turn?
            RollNum = 2;  //number of dice rolls this turn

            WriteLog("Roll 1: " + FDA + " & " + FDB + " - Total " + FTotal);

            do
            {
                CDA = RNG.Next(1, 7);
                CDB = RNG.Next(1, 7);
                CTotal = CDA + CDB;
                WriteLog("Roll " + RollNum + ":  " + CDA + " & " + CDB + " - Total " + CTotal);

                if (CTotal == FTotal)  //if first roll = other dice roll, turn over
                {
                    WriteLog("BUSTED!  No points scored.");
                    PressKey();
                    return;  //end function immediately
                }
                else
                {
                    RoundScore += CTotal;  //each successful dice roll total adds to score including first
                    RollNum++;
                }  //end if
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);  //get single keypress
                switch (keyInfo.KeyChar)
                {
                    case 's':  //end turn
                        TurnOver = true;
                        break;
                    case 'S':  //end turn
                        TurnOver = true;
                        break;
                    default:
                        TurnOver = false;  //keep playing
                        break;
                }
            }   while ( TurnOver ==  false );  //loop while turnover == false
            PlayerData[Turn].Score += RoundScore;
            WriteLog(PlayerData[Turn].Name + " scored " + RoundScore + " points.");
            PressKey();
            
        }

        static void GetName(int Player)  //get a player's name
        {
            string Line;  //input from user
            int pn = Player + 1;  //makes it player 1 instead of 0 when printing
            Console.Write("\nName of Player " + pn + ":  ");             
            Line = Console.ReadLine();  //get line of input
            Line = Line.Trim();  //remove spaces from ends
            if (Line.Length > 0)  // if name input is not blank
            {
                PlayerData[Player].Name = Line;  //store name in struct
            }
            else
            {
                WriteLog("Please give a name for player " + pn);
                GetName(Player);  //relaunch function
            }
        }

        static void GetType(int Player)  //get player type human or computer
        {
            int pn = Player + 1;  //1's base player number
            WriteLog();
            Console.Write("Is " + PlayerData[Player].Name + " a Human or Computer Player? (H/C)  ");
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);  //get single key
            switch (keyInfo.KeyChar)
            {
                case 'h':  //human
                    PlayerData[Player].PlayerType = "Human";  //store type
                    WriteLog("Human");  //wite selection to screen
                    break;
                case 'H':
                    PlayerData[Player].PlayerType = "Human";
                    WriteLog("Human");
                    break;
                case 'c':
                    PlayerData[Player].PlayerType = "Computer";  //computer
                    WriteLog("Computer");
                    break;
                case 'C':
                    PlayerData[Player].PlayerType = "Computer";
                    WriteLog("Computer");
                    break;
                default:
                    GetType(Player);  //not h or c, try again
                    break;
            }            
        }

        static void Main(string[] args)  //entrypoint of program
        {
            Console.Title = "Greed by Charles Martin"; //console window title
            WriteLog("This is a game of luck and skill.  First you roll a pair of dice.");
            WriteLog("Additional rolls add to your score, and you can stop at any");
            WriteLog("time.  If you repeat your first roll, you lose all points for");
            WriteLog("the round.  The winner is the player with the highest score");
            WriteLog();  //explain the game
            WriteLog("During a human player's turn, press the 's' key to end your turn.");

            GetName(0);  //get names and types for both players
            GetType(0);
            GetName(1);
            GetType(1);
            
            GameLoop();  //start the game
            EndGame();  //end of game

            Console.ReadKey(true);  //make sure final output is shown before console window closes
        }
    }
}