using System;
using System.Linq;
using System.Collections.Generic;
using Common.Logging;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UiManager
{
    private static readonly ILog _logger = LogManager.GetLogger("UiManager");

    private RectTransform _canvasRoot;
    private RectTransform _contentRoot;
    private RectTransform _dialogRoot;

    private RectTransform _inputBlocker;
    private int _inputBlockCount;

    private class ModalEntity
    {
        public UiDialogHandle Handle;
        public bool IsTemporary;
        public RectTransform Curtain;
        public ShowModalOption Option;
    }

    private List<ModalEntity> _modals = new List<ModalEntity>();
    private int _blackCurtainCount;
    private RectTransform _fadingOutBlackCurtain;

    public static UiManager Instance { get; private set; }

    public static void Initialize()
    {
        Instance = new UiManager();
    }

    public UiManager()
    {
        _canvasRoot = GameObject.Find("Canvas").GetComponent<RectTransform>();
        _contentRoot = GameObject.Find("Canvas/Content").GetComponent<RectTransform>();
        _dialogRoot = GameObject.Find("Canvas/DialogBox").GetComponent<RectTransform>();
    }

    public RectTransform CanvasRoot
    {
        get { return _canvasRoot; }
    }

    public RectTransform ContentRoot
    {
        get { return _contentRoot; }
    }

    public RectTransform DialogRoot
    {
        get { return _dialogRoot; }
    }

    public RectTransform FindFromDialogRoot(string name)
    {
        var transform = _dialogRoot.Find(name);
        return transform != null ? transform.GetComponent<RectTransform>() : null;
    }

    public bool InputBlocked
    {
        get { return _inputBlockCount > 0; }
    }

    private RectTransform CreateCurtain(Color color, RectTransform parent)
    {
        var go = new GameObject("Curtain");
        var rt = go.AddComponent<RectTransform>();
        var image = go.AddComponent<Image>();
        image.color = color;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        return rt;
    }

    public void ShowInputBlocker()
    {
        if (_inputBlockCount == 0)
        {
            if (_inputBlocker != null)
            {
                _logger.WarnFormat("Blocker already exists on ShowInputBlocker count={0}", _inputBlockCount);
                return;
            }

            _inputBlocker = CreateCurtain(new Color(0, 0, 0, 0.5f), _canvasRoot);
        }

        _inputBlockCount++;
    }

    public void HideInputBlocker()
    {
        if (_inputBlockCount <= 0)
        {
            _logger.WarnFormat("Invalid count on HideInputBlocker count={0}", _inputBlockCount);
            return;
        }

        _inputBlockCount--;

        if (_inputBlockCount == 0)
        {
            UnityEngine.Object.Destroy(_inputBlocker.gameObject);
            _inputBlocker = null;
        }
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
    }

    public UiDialogHandle ShowModalPrefab(GameObject prefab, object param = null,
                                          ShowModalOption option = ShowModalOption.BlackCurtain)
    {
        _logger.InfoFormat("ShowModalPrefab({0})", prefab.name);

        var dialogGo = UiHelper.AddChild(_dialogRoot.gameObject, prefab);

        var dialog = dialogGo.GetComponent<UiDialog>();
        return ShowModalInternal(dialog, true, param, option);
    }

    public UiDialogHandle ShowModalTemplate(GameObject gameObject, object param = null,
                                            ShowModalOption option = ShowModalOption.BlackCurtain)
    {
        _logger.InfoFormat("ShowModalTemplate({0})", gameObject.transform.name);

        var dialogGo = UiHelper.AddChild(_dialogRoot.gameObject, gameObject);
        dialogGo.SetActive(true);

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

    private UiDialogHandle ShowModalInternal(UiDialog dialog, bool isTemporary, object param, ShowModalOption option)
    {
        // create curtain for blocking input

        RectTransform curtain = null;
        if ((option & ShowModalOption.BlackCurtain) != 0 && _fadingOutBlackCurtain != null)
        {
            // When there is a curtain fading out, reuse it to reduce flickering

            curtain = _fadingOutBlackCurtain;

            DOTween.Kill(curtain.GetComponentInChildren<Image>());
            _fadingOutBlackCurtain = null;
        }
        else
        {
            curtain = CreateCurtain(new Color(0, 0, 0, 0), _dialogRoot);
        }

        curtain.SetSiblingIndex(dialog.GetComponent<RectTransform>().GetSiblingIndex());

        // unnecessary but it works as workaround for setting sibling index
        dialog.GetComponent<RectTransform>().SetSiblingIndex(curtain.GetSiblingIndex() + 1);

        if ((option & ShowModalOption.BlackCurtain) != 0)
        {
            _blackCurtainCount += 1;
            var image = curtain.GetComponentInChildren<Image>();
            image.DOColor(new Color(0, 0, 0, 0.7f), 0.15f).SetUpdate(true);
        }

        // fade in dialog

        var dialogRt = dialog.GetComponent<RectTransform>();
        dialogRt.localScale = Vector3.one * 1.1f;
        dialogRt.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);

        var dialogCg = dialog.GetComponent<CanvasGroup>();
        dialogCg.alpha = 0.2f;
        dialogCg.DOFade(1f, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);

        // create handle and register to modals

        var handle = new UiDialogHandle
        {
            Dialog = dialog,
            Visible = true
        };

        _modals.Add(new ModalEntity
        {
            Handle = handle,
            IsTemporary = isTemporary,
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

        // trigger UiDialog.OnHide

        var uiDialog = entity.Handle.Dialog.GetComponent<UiDialog>();
        if (uiDialog != null)
            uiDialog.OnHide();

        // remove dialog

        var dialogCg = dialog.GetComponent<CanvasGroup>();
        dialogCg.DOFade(0f, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() =>
        {
            if (entity.IsTemporary)
            {
                UnityEngine.Object.Destroy(entity.Handle.Dialog.gameObject);
            }
            else
            {
                entity.Handle.Dialog.gameObject.SetActive(false);
            }
        });

        // remove curtain

        if (entity.Curtain != null)
        {
            if ((entity.Option & ShowModalOption.BlackCurtain) != 0)
            {
                _blackCurtainCount -= 1;
                _fadingOutBlackCurtain = entity.Curtain;

                var image = entity.Curtain.GetComponentInChildren<Image>();
                image.DOColor(new Color(0, 0, 0, 0), 0.1f).SetEase(Ease.InQuad).SetUpdate(true)
                     .OnComplete(() =>
                     {
                         if (_fadingOutBlackCurtain == entity.Curtain)
                             _fadingOutBlackCurtain = null;
                         UnityEngine.Object.Destroy(entity.Curtain.gameObject);
                     });
            }
            else
            {
                UnityEngine.Object.Destroy(entity.Curtain.gameObject);
            }
        }

        // trigger Hidden event

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
