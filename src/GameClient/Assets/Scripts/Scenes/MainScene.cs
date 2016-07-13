using System;
using System.Collections;
using System.Linq;
using System.Net;
using Akka.Interfaced;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainScene : MonoBehaviour
{
    public RectTransform LoginPanel;
    public RectTransform LoadingPanel;
    public RectTransform MainPanel;

    public InputField ServerInput;
    public InputField IdInput;
    public InputField PasswordInput;
    public Text LoadingText;

    protected void Start()
    {
        UiManager.Initialize();

        StartLogin();
    }

    private void StartLogin()
    {
        LoginPanel.gameObject.SetActive(false);
        LoadingPanel.gameObject.SetActive(false);
        MainPanel.gameObject.SetActive(false);

        if (G.User != null)
        {
            MainPanel.gameObject.SetActive(true);
        }
        else
        {
            if (G.Communicator != null)
            {
                UiMessageBox.Show("Connection Closed!");
                G.Communicator = null;
            }

            LoginPanel.gameObject.SetActive(true);

            var loginServer = PlayerPrefs.GetString("LoginServer");
            var loginId = PlayerPrefs.GetString("LoginId");
            var loginPassword = PlayerPrefs.GetString("LoginPassword");

            if (string.IsNullOrEmpty(loginId) == false)
            {
                ServerInput.text = loginServer;
                IdInput.text = loginId;
                PasswordInput.text = loginPassword;
            }
        }
    }

    private IEnumerator ProcessLoginUser(string server, string id, string password)
    {
        G.Logger.Info("ProcessLoginUser");

        IPEndPoint endPoint;
        try
        {
            endPoint = LoginProcessor.GetEndPointAddress(server);
        }
        catch (Exception e)
        {
            UiMessageBox.Show("Server EndPoint Error: " + e);
            yield break;
        }

        SwitchPanel(LoginPanel, LoadingPanel);

        var task = LoginProcessor.Login(this, endPoint, id, password, p => LoadingText.text = p + "...");
        yield return task.WaitHandle;

        if (task.Status == TaskStatus.RanToCompletion)
        {
            SwitchPanel(LoadingPanel, MainPanel);

            PlayerPrefs.SetString("LoginServer", server);
            PlayerPrefs.SetString("LoginId", id);
            PlayerPrefs.SetString("LoginPassword", password);
        }
        else
        {
            UiMessageBox.Show(task.Exception.Message);
            SwitchPanel(LoadingPanel, LoginPanel);

            PlayerPrefs.DeleteKey("LoginServer");
            PlayerPrefs.DeleteKey("LoginId");
            PlayerPrefs.DeleteKey("LoginPassword");
        }
    }

    public void OnLoginButtonClick()
    {
        var server = ServerInput.text;
        var id = IdInput.text;
        var password = PasswordInput.text;

        if (string.IsNullOrEmpty(id))
        {
            UiMessageBox.Show("ID is required.");
            return;
        }

        StartCoroutine(ProcessLoginUser(server, id, password));
    }

    public void OnPlayButtonClick()
    {
        if (G.User == null)
        {
            UiMessageBox.Show("Login reqruied.");
            return;
        }

        SceneManager.LoadScene("GameScene");
    }

    public void OnInfoButtonClick()
    {
        if (G.User == null)
        {
            UiMessageBox.Show("Login reqruied.");
            return;
        }

        UiManager.Instance.ShowModalRoot<UserInfoDialogBox>(
            new UserInfoDialogBox.Argument { UserContext = G.UserContext });
    }

    public void OnLogoutButtonClick()
    {
        if (G.User == null)
            return;

        PlayerPrefs.DeleteKey("LoginId");
        PlayerPrefs.DeleteKey("LoginPassword");

        foreach (var channel in G.Communicator.Channels.ToList())
            channel.Close();

        G.Communicator = null;
        G.User = null;

        SwitchPanel(MainPanel, LoginPanel);
    }

    private void SwitchPanel(RectTransform panelFrom, RectTransform panelTo)
    {
        DOTween.Kill(panelFrom.transform, true);
        DOTween.Kill(panelTo.transform, true);

        var y = panelFrom.anchoredPosition.y;
        panelFrom.anchoredPosition = new Vector2(0, y);
        panelFrom.DOAnchorPos(new Vector2(-700, y), 0.25f)
                 .OnComplete(() => panelFrom.gameObject.SetActive(false));

        panelTo.gameObject.SetActive(true);
        panelTo.anchoredPosition = new Vector2(700, y);
        panelTo.DOAnchorPos(new Vector2(0, y), 0.25f);
    }
}
