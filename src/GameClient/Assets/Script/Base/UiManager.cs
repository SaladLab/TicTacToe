using System;
using System.Linq;
using Common.Logging;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UiManager
{
    private static readonly ILog _logger = LogManager.GetLogger("UiManager");

    private GameObject _panelRoot;
    private GameObject _dialogRoot;

    private class ModalEntity
    {
        public UiDialogHandle Handle;
        public bool IsPrefab;
        public GameObject Curtain;
        public ShowModalOption Option;
    }

    private List<ModalEntity> _modals = new List<ModalEntity>();
    private int _blackCurtainCount;

    public static UiManager Instance { get; private set; }

    public static void Initialize()
    {
        Instance = new UiManager();
    }

    public UiManager()
    {
        _panelRoot = GameObject.Find("PanelRoot");
        _dialogRoot = GameObject.Find("DialogRoot");
    }

    public GameObject PanelRoot
    {
        get { return _panelRoot; }
    }

    public GameObject DialogRoot
    {
        get { return _dialogRoot; }
    }

    public GameObject FindFromDialogRoot(string name)
    {
        var obj = _dialogRoot.transform.Find(name);
        return obj ? obj.gameObject : null;
    }

    private GameObject _inputBlocker;
    private int _inputBlockCount;

    public bool InputBlocked
    {
        get { return _inputBlockCount > 0; }
    }

    public void ShowInputBlocker()
    {
        /*
        if (_inputBlockCount == 0)
        {
            if (_inputBlocker != null)
            {
                _logger.WarnFormat("Blocker already exists on ShowInputBlocker count={0}", _inputBlockCount);
                return;
            }

            _inputBlocker = new GameObject("InputBlocker");
            var image = _inputBlocker.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.5f);
            UiHelper.AddChild(_inputBlocker,);
        }

        _inputBlockCount++;
        */
    }

    public void HideInputBlocker()
    {
        /*
        if (_inputBlockCount <= 0)
        {
            _logger.WarnFormat("Invalid count on HideInputBlocker count={0}", _inputBlockCount);
            return;
        }

        _inputBlockCount--;

        if (_inputBlockCount == 0)
        {
            UnityEngine.Object.Destroy(_inputBlocker);
            _inputBlocker = null;
        }
        */
    }

    public bool BlackCurtainVisible
    {
        get { return _blackCurtainCount > 0; }
    }

    [Flags]
    public enum ShowModalOption
    {
        None,
        BlackCurtain = 1,
        ClickCurtainToClose = 2,
    }

    public UiDialogHandle ShowModalPrefab(GameObject prefab, object param = null,
                                          ShowModalOption option = ShowModalOption.BlackCurtain)
    {
        _logger.InfoFormat("ShowModalPrefab({0})", prefab.name);

        var dialogGo = UiHelper.AddChild(_dialogRoot, prefab);

        var dialog = dialogGo.GetComponent<UiDialog>();
        return ShowModalInternal(dialog, true, param, option);
    }

    public UiDialogHandle ShowModal<T>(T dialog, object param = null,
                                       ShowModalOption option = ShowModalOption.BlackCurtain)
        where T : UiDialog
    {
        _logger.InfoFormat("ShowModal({0})", dialog.GetType().Name);

        if (dialog.gameObject.activeSelf)
        {
            _logger.InfoFormat("Failed to show modal because already shown");
            return null;
        }

        dialog.gameObject.SetActive(true);
        return ShowModalInternal(dialog, false, param, option);
    }

    public UiDialogHandle ShowModalRoot<T>(object param = null,
                                           ShowModalOption option = ShowModalOption.BlackCurtain)
        where T : UiDialog
    {
        var dialogGo = FindFromDialogRoot(typeof(T).Name);
        if (dialogGo == null)
            throw new Exception("ShowModalRoot not found: " + typeof(T).Name);

        var dialog = dialogGo.GetComponent<T>();
        if (dialog == null)
            throw new Exception("ShowModalRoot type mismatched: " + typeof(T).Name);

        return ShowModal(dialog, param, option);
    }

    private UiDialogHandle ShowModalInternal(UiDialog dialog, bool isPrefab, object param, ShowModalOption option)
    {
        float z = (_modals.Count + 2) * -10;

        // 커튼 생성
        
        GameObject curtain = null;
        /*
        {
            var curtainPrefab = Resources.Load("Curtain") as GameObject;
            curtain = UiHelper.AddChild(_dialogRoot, curtainPrefab);
            curtain.transform.localPosition = new Vector3(curtain.transform.localPosition.x,
                                                          curtain.transform.localPosition.y,
                                                          z + 0.1f);
            curtain.GetComponent<UIPanel>().depth = (_modals.Count + 2) * 10;

            if ((option & ShowModalOption.BlackCurtain) != 0)
            {
                // 블랙 커튼인 경우

                _blackCurtainCount += 1;

                // 커튼 FadeIn

                var sprite = curtain.GetComponentInChildren<UISprite>();
                sprite.alpha = 0f;
                sprite.TweenTo("alpha", 0.7f, 0.15f, realTimeUpdate: true);
            }
            else
            {
                curtain.transform.Find("Black").gameObject.SetActive(false);
            }

            if ((option & ShowModalOption.ClickCurtainToClose) != 0)
            {
                curtain.BindOnClick(delegate { dialog.Hide(); });
            }
        }
        */
        // 대화상자 생성 및 등록

        /*
        dialog.transform.localPosition =
            new Vector3(dialog.transform.localPosition.x, dialog.transform.localPosition.y, z);
        var dialogPanel = dialog.GetComponent<UIPanel>();
        var depthDiff = ((_modals.Count + 2) * 10) + 1 - dialogPanel.depth;

        foreach (var panel in dialog.GetComponentsInChildren<UIPanel>())
            panel.depth += depthDiff;

        dialog.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        dialog.transform.TweenTo("localScale", new Vector3(1, 1, 1), 0.2f,
                                 ease: EaseType.EaseOutCubic, realTimeUpdate: true,
                                 onUpdate: delegate
                                 {
                                     // 패널의 worldToLocal 값이 한 프레임 늦게 반영되는 경우가 있어
                                     // 명시적으로 worldToLocal 재계산을 업데이트 동안에는 매번 요청
                                     dialogPanel.InvalidateTransformMatrix();
                                 });

        dialogPanel.alpha = 0;
        dialogPanel.TweenTo("alpha", 1f, 0.2f,
                            ease: EaseType.EaseOutCubic, realTimeUpdate: true);
        */

        var handle = new UiDialogHandle
        {
            Dialog = dialog,
            Visible = true
        };

        _modals.Add(new ModalEntity
        {
            Handle = handle,
            IsPrefab = isPrefab,
            Curtain = curtain,
            Option = option
        });

        dialog.OnShow(param);

        return handle;
    }

    internal bool HideModal(UiDialog dialog, object returnValue)
    {
        _logger.InfoFormat("HideModal({0})", dialog.name);

        var i = _modals.FindIndex(m => m.Handle.Dialog == dialog);
        if (i == -1)
        {
            _logger.Info("Failed to hide modal because not shown");
            return false;
        }

        var entity = _modals[i];
        _modals.RemoveAt(i);

        // UiDialog.OnHide 이벤트 호출

        var uiDialog = entity.Handle.Dialog.GetComponent<UiDialog>();
        if (uiDialog != null)
            uiDialog.OnHide();

        // 대화상자 제거

        if (entity.IsPrefab)
        {
            UnityEngine.Object.Destroy(entity.Handle.Dialog.gameObject);
        }
        else
        {
            entity.Handle.Dialog.gameObject.SetActive(false);
        }

        // 커튼 제거

        if (entity.Curtain != null)
        {
            /*
            if ((entity.Option & ShowModalOption.BlackCurtain) != 0)
            {
                // 블랙 커튼 FadeOut

                _blackCurtainCount -= 1;

                var sprite = entity.Curtain.GetComponentInChildren<UISprite>();
                sprite.alpha = 0.7f;
                sprite.TweenTo("alpha", 0f, 0.1f, realTimeUpdate: true,
                               onComplete: () => UnityEngine.Object.Destroy(entity.Curtain));
            }
            else
            {
                UnityEngine.Object.Destroy(entity.Curtain);
            }
            */
        }

        // 핸들에 연결된 이벤트 처리

        entity.Handle.Visible = false;
        entity.Handle.ReturnValue = returnValue;
        if (entity.Handle.Hidden != null)
            entity.Handle.Hidden(entity.Handle.Dialog, returnValue);

        return true;
    }

    public int GetModalCount()
    {
        return _modals.Count;
    }

    public UiDialogHandle GetModalHandle<T>()
        where T : UiDialog
    {
        var dialogGo = FindFromDialogRoot(typeof(T).Name);
        if (dialogGo == null)
            throw new Exception("ModalRoot not found: " + typeof(T).Name);

        var dialog = dialogGo.GetComponent<T>();
        if (dialog == null)
            throw new Exception("ModalRoot type mismatched: " + typeof(T).Name);

        var entity = _modals.Find(m => m.Handle.Dialog == dialog);
        return (entity != null) ? entity.Handle : null;
    }

    public UiDialogHandle GetLastModalHandle()
    {
        return _modals.Any() ? _modals.Last().Handle : null;
    }

    public UiDialogHandle GetOwnerModalHandle(GameObject go)
    {
        var cur = go.transform;
        while (cur != null)
        {
            var dialog = cur.GetComponent<UiDialog>();
            if (dialog != null)
            {
                var modal = _modals.Find(m => m.Handle.Dialog);
                if (modal != null)
                    return modal.Handle;
            }
            cur = cur.transform.parent;
        }
        return null;
    }
}
