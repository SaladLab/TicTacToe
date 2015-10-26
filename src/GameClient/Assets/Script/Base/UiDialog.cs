using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

public class UiDialogHandle
{
    public UiDialog Dialog;
    public Action<UiDialog, object> Hidden;
    public bool Visible;
    public object ReturnValue;

    public IEnumerator WaitForHide()
    {
        while (Visible)
        {
            yield return null;
        }
    }
}
