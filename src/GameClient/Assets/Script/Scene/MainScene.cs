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

    private static readonly IPEndPoint ServerEndPoint =
        new IPEndPoint(IPAddress.Parse("192.168.100.8"), 9001); // new IPEndPoint(IPAddress.Loopback, 9001);

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

        // Connect

        LoadingText.text = "Connecting";
        if (G.Comm == null || G.Comm.State == Communicator.StateType.Stopped)
        {
            G.Comm = new Communicator(G.Logger, ApplicationComponent.Instance);
            G.Comm.ServerEndPoint = ServerEndPoint;
            G.Comm.Start();
        }
        while (true)
        {
            if (G.Comm.State == Communicator.StateType.Connected)
                break;

            if (G.Comm.State == Communicator.StateType.Stopped)
            {
                UiMessageBox.ShowMessageBox("Failed to connect");
                SwitchPanel(LoadingPanel, LoginPanel);
                yield break;
            }

            yield return null;
        }

        // Login

        LoadingText.text = "Login";
        var userLogin = new UserLoginRef(new SlimActorRef { Id = 1 }, new SlimRequestWaiter { Communicator = G.Comm }, null);
        var observerId = G.Comm.IssueObserverId();
        var t1 = userLogin.Login(id, password, observerId);
        yield return t1.WaitHandle;
        ShowResult(t1, "Login");
        if (t1.Exception != null)
        {
            UiMessageBox.ShowMessageBox("Login Error\n" + t1.Exception);
            SwitchPanel(LoadingPanel, LoginPanel);
            yield break;
        }

        G.Comm.AddObserver(observerId, ApplicationComponent.Instance);
        G.User = new UserRef(new SlimActorRef { Id = t1.Result }, new SlimRequestWaiter { Communicator = G.Comm }, null);
        SwitchPanel(LoadingPanel, MainPanel);
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

    void ShowResult(Task task, string name)
    {
        if (task.Status == TaskStatus.RanToCompletion)
            Debug.Log(string.Format("{0}: Done", name));
        else if (task.Status == TaskStatus.Faulted)
            Debug.Log(string.Format("{0}: Exception = {1}", name, task.Exception));
        else if (task.Status == TaskStatus.Canceled)
            Debug.Log(string.Format("{0}: Canceled", name));
        else
            Debug.Log(string.Format("{0}: Illegal Status = {1}", name, task.Status));
    }

    void ShowResult<TResult>(Task<TResult> task, string name)
    {
        if (task.Status == TaskStatus.RanToCompletion)
            Debug.Log(string.Format("{0}: Result = {1}", name, task.Result));
        else
            ShowResult((Task)task, name);
    }
}
