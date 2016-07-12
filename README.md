# Tic Tac Toe

[![Build status](https://ci.appveyor.com/api/projects/status/8vk6qnrts10p3mt4?svg=true)](https://ci.appveyor.com/project/veblush/tictactoe)
[![Coverage Status](https://coveralls.io/repos/github/SaladLab/TicTacToe/badge.svg?branch=master)](https://coveralls.io/github/TicTacToe/Chatty?branch=master)

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
  - Address of MongoDB can be configured on src/GameServer/App.config.
- Run Server
  - Open TicTacToe.sln with Visual Studio.
  - Run GameServer.
- Run Client
  - Open src/GameClient with Unity
  - Open Scenes/MainScene and run.
