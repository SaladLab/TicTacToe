using UnityEngine;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Domain.Interfaced;

public class GameScene : MonoBehaviour
{
    protected void Start()
    {
        ApplicationComponent.TryInit();
        UiManager.Initialize();
    }

    public void OnLeaveButtonClick()
    {
        // TODO: Confirmation Dialog
        Application.LoadLevel("MainScene");
    }
}
