# Greed-CLI

A console-based dice game of luck and strategy where players compete to achieve the highest score over 10 rounds.

## 🎲 About the Game

Greed is a two-player dice game that combines luck with strategic decision-making. Players take turns rolling a pair of dice, with the opportunity to keep rolling to increase their score - but be careful! Rolling the same total as your first roll will **bust** and you'll lose all points for that round.

## 🎮 How to Play

### Game Setup
1. Enter names for both players
2. Choose whether each player is Human or Computer
3. Optionally enable text-to-speech for an enhanced experience

### Gameplay Rules
- The game consists of **10 rounds**
- Each player gets one turn per round
- On your first roll, you roll two dice (values 1-6)
- The total of your first roll determines:
  - Your starting score for the round
  - How many additional rolls you can make (first roll total + 1)
- Each additional roll adds to your round score
- **BUST**: If you roll the same total as your first roll, you lose ALL points for that round
- Human players can press 'S' at any time to stop rolling and bank their points
- The player with the highest total score after 10 rounds wins

### Example Turn
```
Roll 1: 3 & 5 - Total 8
(You now have 9 possible rolls total and 8 points)

Roll 2: 2 & 4 - Total 6
(Score now: 14 points)

Roll 3: 4 & 4 - Total 8
BUSTED! (Rolled the same total as first roll - lose all points)
```

## 🤖 Computer AI

The computer opponent uses intelligent decision-making based on:
- **Bust probability**: Calculates the odds of rolling the initial total
- **Score difference**: Plays more aggressively when behind, conservatively when ahead
- **Round position**: Adjusts strategy in late-game situations
- **Current round score**: Weighs the value of banked points
- **Randomization**: Adds unpredictability to make each game unique

## 🔧 Technical Details

- **Framework**: .NET Framework 4.8.1
- **Language**: C# 
- **Platform**: Windows (Console Application)
- **Features**:
  - Text-to-speech support (optional)
  - Human vs Human, Human vs Computer, or Computer vs Computer gameplay
  - Strategic AI opponent with risk assessment
  - Clear score tracking and round-by-round display

## 🚀 Getting Started

### Prerequisites
- Windows OS
- .NET Framework 4.8.1 or higher
- Visual Studio 2022 or later (for development)

### Running the Game

#### From Source
1. Clone the repository:
   ```bash
   git clone https://github.com/HiTechCharles/Greed-CLI.git
   cd Greed-CLI
   ```

2. Open `Greed.sln` in Visual Studio

3. Build and run the project (F5)

#### From Executable
1. Build the project in Release mode
2. Navigate to `bin\Release\`
3. Run `Greed.exe`

## 🎯 Strategy Tips

- **Early rounds**: Be more conservative - you have many rounds to catch up
- **When ahead**: Play it safe to protect your lead
- **When behind**: Take calculated risks to close the gap
- **Low first rolls**: You have fewer opportunities, so each roll counts more
- **High first rolls**: More chances to build your score, but higher bust probability

## 📝 Controls

- **Any key**: Roll the dice (human player)
- **S key**: Stop rolling and bank your points (human player)
- **Y/N**: Enable/disable text-to-speech at game start
- **H/C**: Choose Human or Computer player type

## 🛠️ Development

### Project Structure
```
Greed-CLI/
├── Program.cs           # Main game logic and AI
├── Greed.csproj         # Project configuration
├── App.config           # Application configuration
├── Resources/
│   └── Icon.ico         # Application icon
└── Properties/
	├── AssemblyInfo.cs
	└── Resources.resx
```

### Key Classes and Methods
- `PlayerInfo`: Stores player name, type, and score
- `ComputerRoll()`: AI decision-making and automated gameplay
- `HumanRoll()`: Human player interaction and input handling
- `CalculateBustProbability()`: Probability calculations for AI
- `ComputerShouldContinue()`: AI risk assessment logic

## 🤝 Contributing

Contributions are welcome! Feel free to:
- Report bugs
- Suggest new features
- Submit pull requests
- Improve documentation

## 📄 License

This project is developed by Charles Martin.

## 🎨 Credits

**Author**: Charles Martin  
**Repository**: [HiTechCharles/Greed-CLI](https://github.com/HiTechCharles/Greed-CLI)

---

*Have fun playing Greed! May the odds be ever in your favor!* 🎲
