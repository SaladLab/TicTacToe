using UnityEngine;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Domain.Interfaced;

public class GameScene : MonoBehaviour
{
    public RectTransform LoadingPanel;
    public RectTransform GamePanel;

    protected void Start()
    {
        ApplicationComponent.TryInit();
        UiManager.Initialize();

        StartGame();
    }

    private void StartGame()
    {
        LoadingPanel.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(false);

        if (G.User != null)
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

        yield return G.User.RegisterPairing().WaitHandle;

        // TODO: while waiting for 5sec, observe pairing events
        yield return G.User.UnregisterPairing().WaitHandle;
        // yield return G.User.JoinGame();


        // yield return G.User.JoinGame(gameId, observerId);

        yield return new WaitForSeconds(5);

        LoadingPanel.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(true);
    }

    public void OnLeaveButtonClick()
    {
        // TODO: Confirmation Dialog
        Application.LoadLevel("MainScene");
    }
}
