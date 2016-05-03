# Tic Tac Toe

Reference game for using Akka.Interfaced, Akka.Interfaced.SlimSocket and TrackableData.

![Screenshot](https://raw.githubusercontent.com/SaladLab/TicTacToe/master/docs/ScreenShot.jpg)

## How to run

### Prerequisites

- MongoDB 3 or later
- Visual Studio 2015 or later (it's not mandatory if you can build projects)
- Unity 5.3 or later

### Steps

- Make sure MongoDB is running well.
  - By default server connects to local MongoDB.
  - Address of MongoDB can be configured on src/GameServer-Console/App.config.
- Run Server
  - Open TicTacToe.sln with Visual Studio.
  - Run GameServer-Console.
- Run Client
  - Open src/GameClient with Unity
  - Open Scenes/MainScene and run.
