using System;
using UnityEngine;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Domain.Interfaced;
using DG.Tweening;
using UnityEngine.UI;

public class MainScene : MonoBehaviour
{
    public RectTransform LoginPanel;
    public RectTransform LoadingPanel;
    public RectTransform MainPanel;

    public InputField IdInput;
    public InputField PasswordInput;
    public Text LoadingText;

    protected void Start()
    {
        ApplicationComponent.TryInit();
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
            var loginId = PlayerPrefs.GetString("LoginId");
            var loginPassword = PlayerPrefs.GetString("LoginPassword");
            if (string.IsNullOrEmpty(loginId))
            {
                LoginPanel.gameObject.SetActive(true);
            }
            else
            {
                IdInput.text = loginId;
                StartCoroutine(ProcessLoginUser(loginId, loginPassword));
            }
        }
    }

    private IEnumerator ProcessLoginUser(string id, string password)
    {
        G.Logger.Info("ProcessLoginUser");
        SwitchPanel(LoginPanel, LoadingPanel);

        var task = LoginProcessor.Login(G.ServerEndPoint, id, password, p => LoadingText.text = p + "...");
        yield return task.WaitHandle;

        if (task.Status == TaskStatus.RanToCompletion)
        {
            SwitchPanel(LoadingPanel, MainPanel);

            PlayerPrefs.SetString("LoginId", id);
            PlayerPrefs.SetString("LoginPassword", password);
        }
        else
        {
            UiMessageBox.ShowMessageBox(task.Exception.Message);
            SwitchPanel(LoadingPanel, LoginPanel);

            PlayerPrefs.DeleteKey("LoginId");
            PlayerPrefs.DeleteKey("LoginPassword");
        }
    }

    public void OnLoginButtonClick()
    {
        var id = IdInput.text;
        var password = PasswordInput.text;

        if (string.IsNullOrEmpty(id))
        {
            UiMessageBox.ShowMessageBox("ID is required.");
            return;
        }

        StartCoroutine(ProcessLoginUser(id, password));
    }

    public void OnPlayButtonClick()
    {
        if (G.User == null)
            return;

        Application.LoadLevel("GameScene");
    }

    public void OnSpectateButtonClick()
    {
        if (G.User == null)
            return;

        Application.LoadLevel("GameScene");
    }

    public void OnLogoutButtonClick()
    {
        if (G.User == null)
            return;

        PlayerPrefs.DeleteKey("LoginId");
        PlayerPrefs.DeleteKey("LoginPassword");

        G.Comm.Stop();
        G.Comm = null;
        G.User = null;

        SwitchPanel(MainPanel, LoginPanel);
    }

    private void SwitchPanel(RectTransform panelFrom, RectTransform panelTo)
    {
        var y = panelFrom.anchoredPosition.y;
        panelFrom.anchoredPosition = new Vector2(0, y);
        panelFrom.DOAnchorPos(new Vector2(-700, y), 0.25f)
                 .OnComplete(() => panelFrom.gameObject.SetActive(false));

        panelTo.gameObject.SetActive(true);
        panelTo.anchoredPosition = new Vector2(700, y);
        panelTo.DOAnchorPos(new Vector2(0, y), 0.25f);
    }
}
