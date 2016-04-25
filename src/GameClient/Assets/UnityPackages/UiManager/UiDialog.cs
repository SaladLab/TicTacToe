using System;
using UnityEngine;

public class UiDialog : MonoBehaviour
{
    public virtual void OnShow(object param)
    {
    }

    public virtual void OnHide()
    {
    }

    public void Hide(object returnValue = null)
    {
        UiManager.Instance.HideModal(this, returnValue);
    }

    public void OnDialogCloseButtonClick()
    {
        Hide();
    }
}
