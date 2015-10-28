using System;
using UnityEngine;
using System.Collections;

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
