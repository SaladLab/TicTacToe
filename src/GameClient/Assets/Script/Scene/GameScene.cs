using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Domain.Interfaced;

public class GameScene : MonoBehaviour, IGameObserver
{
    public RectTransform LoadingPanel;
    public RectTransform GamePanel;

    public GameBoard Board;
    public GamePlayerPlate[] PlayerPlate;

    protected void Start()
    {
        ApplicationComponent.TryInit();
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
        var loginId = PlayerPrefs.GetString("LoginId");
        var loginPassword = PlayerPrefs.GetString("LoginPassword");

        if (string.IsNullOrEmpty(loginId))
        {
            UiMessageBox.ShowMessageBox("Cannot find id");
            yield break;
        }

        yield return StartCoroutine(ProcessLoginUser("test", "1234"));
        if (G.User == null)
        {
            UiMessageBox.ShowMessageBox("Failed to login");
            yield break;
        }
        yield return StartCoroutine(ProcessJoinGame());
    }

    private IEnumerator ProcessLoginUser(string id, string password)
    {
        G.Logger.Info("ProcessLoginUser");

        var task = LoginProcessor.Login(G.ServerEndPoint, id, password, null);
        yield return task.WaitHandle;
    }

    private IEnumerator ProcessJoinGame()
    {
        G.Logger.Info("ProcessJoinGame");

        // TODO: pariing
        // TODO: while waiting for 5sec, observe pairing events
        // yield return G.User.RegisterPairing().WaitHandle;
        // yield return G.User.UnregisterPairing().WaitHandle;

        // Join !

        var roomId = 1L;
        var observerId = G.Comm.IssueObserverId();
        var joinRet = G.User.JoinGame(roomId, observerId);
        yield return joinRet.WaitHandle;

        if (joinRet.Exception != null)
        {
            UiMessageBox.ShowMessageBox("Failed to join\n" + joinRet.Exception);
            yield break;
        }

        G.Comm.AddObserver(observerId, this);

        // TODO: Joined but wait for the opponent

        LoadingPanel.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(true);

        StartGame();
    }

    private void StartGame()
    {
        Board.GridClicked += OnBoardGridClicked;

        PlayerPlate[0].SetGrid(1);
        PlayerPlate[0].SetName("Opponent");
        PlayerPlate[0].SetTimerOn(true, 30);
        PlayerPlate[0].SetTurn(true);

        PlayerPlate[1].SetGrid(2);
        PlayerPlate[1].SetName("You");
        PlayerPlate[1].SetTimerOn(false);
        PlayerPlate[1].SetTurn(false);
    }

    private int _markCount = 0;

    private void OnBoardGridClicked(int x, int y)
    {
        if (Board.GetMark(x, y) != 0)
            return;

        var i = _markCount % 2;
        Board.SetMark(x, y, i + 1, true);

        _markCount += 1;
        if (_markCount < 9)
        {
            PlayerPlate[i].SetTurn(false);
            PlayerPlate[i].SetTimerOn(false);
            PlayerPlate[1 - i].SetTurn(true);
            PlayerPlate[1 - i].SetTimerOn(true, 30);
        }
        else
        {
            PlayerPlate[0].SetTurn(false);
            PlayerPlate[0].SetTimerOn(false);
            PlayerPlate[1].SetTurn(false);
            PlayerPlate[1].SetTimerOn(false);
        }
    }

    public void OnLeaveButtonClick()
    {
        // TODO: Confirmation Dialog
        Application.LoadLevel("MainScene");
    }

    void IGameObserver.Join(int playerId, string userId)
    {
        Debug.Log(string.Format("IGameObserver.Join {0} {1}", playerId, userId));
    }

    void IGameObserver.Leave(int playerId)
    {
        Debug.Log(string.Format("IGameObserver.Leave {0} {1}", playerId, playerId));
    }

    void IGameObserver.MakeMove(int playerId, PlacePosition pos)
    {
        Debug.Log(string.Format("IGameObserver.MakeMove {0} {1}", playerId, pos));
    }

    void IGameObserver.Say(int playerId, string msg)
    {
        Debug.Log(string.Format("IGameObserver.Say {0} {1}", playerId, msg));
    }
}
