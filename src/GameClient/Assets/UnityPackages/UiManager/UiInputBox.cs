using System;
using UnityEngine;
using UnityEngine.UI;

public class UiInputBox : UiDialog
{
    public delegate void InputResultDelegate(string input);

    public enum ButtonType
    {
        Ok,
        OkCancel,
    }

    public Text MessageText;
    public InputField ValueInput;
    public Button[] Buttons;

    public static UiDialogHandle Show(
        string msg, string value = null, ButtonType buttonType = ButtonType.Ok,
        InputResultDelegate callback = null, string customOkName = null)
    {
        UiDialogHandle handle;
        {
            var builtin = UiManager.Instance.FindFromDialogRoot("InputBox");
            if (builtin != null)
            {
                handle = UiManager.Instance.ShowModalTemplate(builtin.gameObject);
            }
            else
            {
                var msgBoxPrefab = Resources.Load("InputBox") as GameObject;
                handle = UiManager.Instance.ShowModalPrefab(msgBoxPrefab);
            }
        }

        if (callback != null)
            handle.Hidden += (dlg, returnValue) => callback((string)returnValue);

        var msgBox = (UiInputBox)handle.Dialog;
        msgBox.MessageText.text = msg;
        msgBox.ValueInput.text = value ?? "";

        var b0 = msgBox.Buttons[0];
        var b0Text = b0.transform.Find("Text").GetComponent<Text>();
        var b1 = msgBox.Buttons[1];
        var b1Text = b1.transform.Find("Text").GetComponent<Text>();

        b1.gameObject.SetActive(buttonType != ButtonType.Ok);

        switch (buttonType)
        {
            case ButtonType.Ok:
                b0Text.text = customOkName ?? "Ok";
                b0.onClick.AddListener(() => msgBox.OnButtonClick(true));
                break;

            case ButtonType.OkCancel:
                b0Text.text = customOkName ?? "Ok";
                b0.onClick.AddListener(() => msgBox.OnButtonClick(true));
                b1Text.text = "Cancel";
                b1.onClick.AddListener(() => msgBox.OnButtonClick(false));
                break;
        }

        return handle;
    }

    private void OnButtonClick(bool ok)
    {
        Hide(ok ? ValueInput.text : null);
    }

    public void OnInputSubmit()
    {
        OnButtonClick(true);
    }
}
