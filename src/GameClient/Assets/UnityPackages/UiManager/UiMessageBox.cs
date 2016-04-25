using UnityEngine;
using UnityEngine.UI;

public class UiMessageBox : UiDialog
{
    public enum QuestionType
    {
        Ok,
        OkCancel,
        RetryCancel,
        YesNo,
        ContinueStop
    }

    public enum QuestionResult
    {
        Close,
        Ok,
        Cancel,
        Retry,
        Yes,
        No,
        Continue,
        Stop
    }

    public delegate void QuestionResultDelegate(QuestionResult result);

    public Text MessageText;
    public Button[] Buttons;

    public static UiDialogHandle ShowMessageBox(string msg, QuestionResultDelegate callback = null)
    {
        UiDialogHandle handle;
        {
            var builtin = UiManager.Instance.FindFromDialogRoot("MessageBox");
            if (builtin != null)
            {
                handle = UiManager.Instance.ShowModalTemplate(builtin.gameObject);
            }
            else
            {
                var msgBoxPrefab = Resources.Load("MessageBox") as GameObject;
                handle = UiManager.Instance.ShowModalPrefab(msgBoxPrefab);
            }
        }

        if (callback != null)
            handle.Hidden += (dlg, returnValue) => callback((QuestionResult)returnValue);

        var msgBox = (UiMessageBox)handle.Dialog;
        msgBox.MessageText.text = msg;
        msgBox.Buttons[0].onClick.AddListener(msgBox.OnMessageBoxOkButtonClick);

        return handle;
    }

    private void OnMessageBoxOkButtonClick()
    {
        Hide(QuestionResult.Ok);
    }

    public static UiDialogHandle ShowQuestionBox(
        string msg, QuestionType questionType,
        QuestionResultDelegate callback = null, string customOkName = null)
    {
        UiDialogHandle handle;
        {
            var builtin = UiManager.Instance.FindFromDialogRoot("MessageQuestionBox");
            if (builtin != null)
            {
                handle = UiManager.Instance.ShowModalTemplate(builtin.gameObject);
            }
            else
            {
                var msgBoxPrefab = Resources.Load("MessageQuestionBox") as GameObject;
                handle = UiManager.Instance.ShowModalPrefab(msgBoxPrefab);
            }
        }

        if (callback != null)
            handle.Hidden += (dlg, returnValue) => callback((QuestionResult)returnValue);

        var msgBox = (UiMessageBox)handle.Dialog;
        msgBox.MessageText.text = msg;

        var b0 = msgBox.Buttons[0];
        var b0Text = b0.transform.Find("Text").GetComponent<Text>();
        var b1 = msgBox.Buttons[1];
        var b1Text = b1.transform.Find("Text").GetComponent<Text>();

        switch (questionType)
        {
            case QuestionType.Ok:
                b0Text.text = customOkName ?? "Ok";
                b0.onClick.AddListener(() => msgBox.OnQuestionBoxButtonClick(QuestionResult.Ok));
                break;

            case QuestionType.OkCancel:
                b0Text.text = "Ok";
                b0.onClick.AddListener(() => msgBox.OnQuestionBoxButtonClick(QuestionResult.Ok));
                b1Text.text = "Cancel";
                b1.onClick.AddListener(() => msgBox.OnQuestionBoxButtonClick(QuestionResult.Cancel));
                break;

            case QuestionType.RetryCancel:
                b0Text.text = "Retry";
                b0.onClick.AddListener(() => msgBox.OnQuestionBoxButtonClick(QuestionResult.Retry));
                b1Text.text = "Cancel";
                b1.onClick.AddListener(() => msgBox.OnQuestionBoxButtonClick(QuestionResult.Cancel));
                break;

            case QuestionType.YesNo:
                b0Text.text = "Yes";
                b0.onClick.AddListener(() => msgBox.OnQuestionBoxButtonClick(QuestionResult.Yes));
                b1Text.text = "No";
                b1.onClick.AddListener(() => msgBox.OnQuestionBoxButtonClick(QuestionResult.No));
                break;

            case QuestionType.ContinueStop:
                b0Text.text = "Continue";
                b0.onClick.AddListener(() => msgBox.OnQuestionBoxButtonClick(QuestionResult.Continue));
                b1Text.text = "Stop";
                b1.onClick.AddListener(() => msgBox.OnQuestionBoxButtonClick(QuestionResult.Stop));
                break;
        }

        return handle;
    }

    private void OnQuestionBoxButtonClick(QuestionResult result)
    {
        Hide(result);
    }
}
