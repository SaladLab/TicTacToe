using UnityEngine;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Domain.Interfaced;

public class GameScene : MonoBehaviour
{
    protected void Start()
    {
    }

    public void OnLeaveButtonClick()
    {
        // TODO: Confirmation Dialog
        Application.LoadLevel("MainScene");
    }
}
