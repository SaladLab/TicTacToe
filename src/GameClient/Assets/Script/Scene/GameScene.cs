using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Domain.Interfaced;

public class GameScene : MonoBehaviour
{
    public RectTransform LoadingPanel;
    public RectTransform GamePanel;

    public GameBoard Board;
    public GamePlayerPlate[] PlayerPlate;

    protected void Start()
    {
        ApplicationComponent.TryInit();
        UiManager.Initialize();

        StartJoin();
    }

    private void StartJoin()
    {
        LoadingPanel.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(false);

        if (G.User == null)
        {
            LoadingPanel.gameObject.SetActive(true);
            StartCoroutine(ProcessJoinGame());
        }
        else
        {
            // For Developer
            GamePanel.gameObject.SetActive(true);
        }
    }

    private IEnumerator ProcessJoinGame()
    {
        G.Logger.Info("ProcessLoginUser");
        LoadingPanel.gameObject.SetActive(true);

        // yield return G.User.RegisterPairing().WaitHandle;

        // TODO: while waiting for 5sec, observe pairing events
        // yield return G.User.UnregisterPairing().WaitHandle;
        // yield return G.User.JoinGame();



        // yield return G.User.JoinGame(gameId, observerId);

        yield return new WaitForSeconds(5);

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
}
