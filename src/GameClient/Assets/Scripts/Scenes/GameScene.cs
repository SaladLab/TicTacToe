using System;
using System.Collections;
using Akka.Interfaced.SlimSocket.Client;
using DG.Tweening;
using Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameScene : MonoBehaviour, IUserPairingObserver, IGameObserver
{
    public RectTransform LoadingPanel;
    public RectTransform GamePanel;

    public Text LoadingText;
    public GameBoard Board;
    public GamePlayerPlate[] PlayerPlate;
    public RectTransform ResultBox;
    public Text ResultText;

    private Tuple<long, string> _pairedGame;
    private int _myPlayerId;
    private IGameObserver _gameObserver;
    private GameInfo _gameInfo;
    private GamePlayerRef _myPlayer;

    protected void Start()
    {
        UiManager.Initialize();

        StartJoinGame();
    }

    private void StartJoinGame()
    {
        LoadingPanel.gameObject.SetActive(true);
        GamePanel.gameObject.SetActive(false);

        StartCoroutine(G.User == null
                           ? ProcessLoginAndJoinGame()
                           : ProcessJoinGame());
    }

    private IEnumerator ProcessLoginAndJoinGame()
    {
        var loginServer = PlayerPrefs.GetString("LoginServer");
        var loginId = PlayerPrefs.GetString("LoginId");
        var loginPassword = PlayerPrefs.GetString("LoginPassword");

        // TEST
        loginServer = "";
        loginId = "editor";
        loginPassword = "1234";

        if (string.IsNullOrEmpty(loginId))
        {
            UiMessageBox.ShowMessageBox("Cannot find id");
            yield break;
        }

        yield return StartCoroutine(ProcessLoginUser(loginServer, loginId, loginPassword));
        if (G.User == null)
        {
            UiMessageBox.ShowMessageBox("Failed to login");
            yield break;
        }

        yield return StartCoroutine(ProcessJoinGame());
    }

    private IEnumerator ProcessLoginUser(string server, string id, string password)
    {
        G.Logger.Info("ProcessLoginUser");

        var endPoint = LoginProcessor.GetEndPointAddress(server);
        var task = LoginProcessor.Login(this, endPoint, id, password, null);
        yield return task.WaitHandle;
    }

    private IEnumerator ProcessJoinGame()
    {
        G.Logger.Info("ProcessJoinGame");

        // Finding Game
        // Register user to pairing queue and waiting for 5 secs.

        LoadingText.text = "Finding Game...";

        _pairedGame = null;

        var pairingObserver = G.Communicator.ObserverRegistry.Create<IUserPairingObserver>(this);
        yield return G.User.RegisterPairing(pairingObserver).WaitHandle;

        var startTime = DateTime.Now;
        while ((DateTime.Now - startTime).TotalSeconds < 5 && _pairedGame == null)
        {
            yield return null;
        }

        G.Communicator.ObserverRegistry.Remove(pairingObserver);

        if (_pairedGame == null)
        {
            yield return G.User.UnregisterPairing().WaitHandle;
            var box = UiMessageBox.ShowMessageBox("Cannot find game");
            yield return StartCoroutine(box.WaitForHide());
            SceneManager.LoadScene("MainScene");
            yield break;
        }

        // Join Game

        var gameObserver = G.Communicator.ObserverRegistry.Create<IGameObserver>(this, startPending: true);
        gameObserver.GetEventDispatcher().KeepingOrder = true; // remove after Akka.NET network layer is upgraded

        var roomId = _pairedGame.Item1;
        var joinRet = G.User.JoinGame(roomId, gameObserver);
        yield return joinRet.WaitHandle;

        if (joinRet.Exception != null)
        {
            UiMessageBox.ShowMessageBox("Failed to join\n" + joinRet.Exception);
            G.Communicator.ObserverRegistry.Remove(gameObserver);
            yield break;
        }

        _gameObserver = gameObserver;
        _gameInfo = joinRet.Result.Item3;
        _myPlayerId = joinRet.Result.Item2;
        _myPlayer = (GamePlayerRef)joinRet.Result.Item1;

        if (_myPlayer.IsChannelConnected() == false)
        {
            var connectTask = _myPlayer.ConnectChannelAsync();
            yield return connectTask.WaitHandle;
            if (connectTask.Exception != null)
            {
                UiMessageBox.ShowMessageBox("Failed to connect\n" + joinRet.Exception);
                G.Communicator.ObserverRegistry.Remove(gameObserver);
                yield break;
            }
        }

        gameObserver.GetEventDispatcher().Pending = false;
        LoadingText.text = "Waiting for " + _pairedGame.Item2 + "...";
    }

    private void BeginGame(int playerId)
    {
        var opponentName = _gameInfo.PlayerNames[2 - _myPlayerId];

        PlayerPlate[0].SetGrid(2);
        PlayerPlate[0].SetName(opponentName);

        PlayerPlate[1].SetGrid(1);
        PlayerPlate[1].SetName(G.UserContext.Data.Name);

        LoadingPanel.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(true);

        SetPlayerTurn(playerId);

        ResultBox.gameObject.SetActive(false);
    }

    private void EndGame(int playerId)
    {
        PlayerPlate[0].SetTimerOn(false);
        PlayerPlate[0].SetTurn(false);

        PlayerPlate[1].SetTimerOn(false);
        PlayerPlate[1].SetTurn(false);

        if (playerId == 0)
        {
            PlayerPlate[0].SetGrid(1);
            ResultText.text = "DRAW";
            ResultText.color = Color.black;
        }
        else
        {
            var myWinning = _myPlayerId == playerId;

            var row = Logic.FindMatchedRow(Board.Grid);
            if (row != null)
            {
                var delay = 0f;
                foreach (var pos in Logic.RowPositions[row.Item1])
                {
                    Board.SetHighlight(pos.X, pos.Y, delay);
                    delay += 0.3f;
                }
            }

            PlayerPlate[myWinning ? 1 : 0].SetWin();

            ResultText.text = myWinning ? "WIN" : "LOSE";
            ResultText.color = myWinning ? Color.white : Color.black;
        }

        ResultBox.gameObject.SetActive(true);
        var ap = ResultBox.anchoredPosition;
        ResultBox.anchoredPosition = new Vector2(ap.x, ap.y - 500);
        ResultBox.DOAnchorPosY(-550, 0.5f).SetEase(Ease.OutBounce).SetDelay(playerId != 0 ? 1 : 0);
    }

    private void SetPlayerTurn(int playerId)
    {
        if (_myPlayerId == playerId)
        {
            Board.GridClicked = OnBoardGridClicked;

            PlayerPlate[0].SetTimerOn(false);
            PlayerPlate[0].SetTurn(false);

            PlayerPlate[1].SetTimerOn(true, (int)Rule.TurnTimeout.TotalSeconds);
            PlayerPlate[1].SetTurn(true);
        }
        else
        {
            Board.GridClicked = null;

            PlayerPlate[0].SetTimerOn(true, (int)Rule.TurnTimeout.TotalSeconds);
            PlayerPlate[0].SetTurn(true);

            PlayerPlate[1].SetTimerOn(false);
            PlayerPlate[1].SetTurn(false);
        }
    }

    private void OnBoardGridClicked(int x, int y)
    {
        if (Board.GetMark(x, y) != 0)
            return;

        Board.SetMark(x, y, 1);
        _myPlayer.MakeMove(new PlacePosition(x, y));
    }

    public void OnLeaveButtonClick()
    {
        if (_gameInfo != null)
        {
            G.User.LeaveGame(_gameInfo.Id);
            G.Communicator.ObserverRegistry.Remove(_gameObserver);
        }

        SceneManager.LoadScene("MainScene");
    }

    void IUserPairingObserver.MakePair(long gameId, string opponentName)
    {
        Debug.Log(string.Format("IUserPairingObserver.MakePair {0} {1}", gameId, opponentName));
        _pairedGame = Tuple.Create(gameId, opponentName);
    }

    void IGameObserver.Join(int playerId, long userId, string userName)
    {
        Debug.Log(string.Format("IGameObserver.Join {0} {1} {2}", playerId, userId, userName));
        _gameInfo.PlayerNames.Add(userName);
    }

    void IGameObserver.Leave(int playerId)
    {
        Debug.Log(string.Format("IGameObserver.Leave {0} {1}", playerId, playerId));
    }

    void IGameObserver.Begin(int currentPlayerId)
    {
        Debug.Log(string.Format("IGameObserver.Begin {0}", currentPlayerId));
        BeginGame(currentPlayerId);
    }

    void IGameObserver.End(int winnerPlayerId)
    {
        Debug.Log(string.Format("IGameObserver.End {0}", winnerPlayerId));
        EndGame(winnerPlayerId);
    }

    void IGameObserver.Abort()
    {
        Debug.Log("IGameObserver.Abort");
    }

    void IGameObserver.MakeMove(int playerId, PlacePosition pos, int nextTurnPlayerId)
    {
        Debug.Log(string.Format("IGameObserver.MakeMove {0} {1} {2}", playerId, pos, nextTurnPlayerId));
        Board.SetMark(pos.X, pos.Y, _myPlayerId == playerId ? 1 : 2);
        SetPlayerTurn(nextTurnPlayerId);
    }

    void IGameObserver.Say(int playerId, string msg)
    {
        Debug.Log(string.Format("IGameObserver.Say {0} {1}", playerId, msg));
    }
}
