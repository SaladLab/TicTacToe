using System;
using System.Collections;
using UnityEngine;

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
