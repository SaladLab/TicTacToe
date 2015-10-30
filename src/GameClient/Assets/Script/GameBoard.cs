using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class GameBoard : MonoBehaviour
{
    public RectTransform GridTemplate;

    public Action<int, int> GridClicked;

    private int[,] _gridMarks = new int[3, 3];
    private RectTransform[,] _gridRects = new RectTransform[3, 3];

    void Start()
    {
        BuildGrids();
    }

    private void BuildGrids()
    {
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                var grid = UiHelper.AddChild(gameObject, GridTemplate.gameObject);
                grid.transform.SetParent(transform, false);
                grid.transform.localPosition = new Vector3(x * 210 - 210, y * -210 + 210, 0);

                var et = new EventTrigger.TriggerEvent();
                var px = x;
                var py = y;
                et.AddListener(_ => OnGridClick(px, py));
                grid.GetComponent<EventTrigger>().triggers[0].callback = et;

                _gridRects[x, y] = grid.GetComponent<RectTransform>();

                SetMark(x, y, 0);
            }
        }
        GridTemplate.gameObject.SetActive(false);
    }

    private void OnGridClick(int x, int y)
    {
        if (GridClicked != null)
            GridClicked(x, y);
    }

    public int[,] Grid
    {
        get { return _gridMarks; }
    }

    public int GetMark(int x, int y)
    {
        return _gridMarks[x, y];
    }

    public void SetMark(int x, int y, int value, bool withAnimation = false)
    {
        _gridMarks[x, y] = value;

        var image = _gridRects[x, y].GetComponent<Image>();
        var text = _gridRects[x, y].GetComponentInChildren<Text>();

        if (value == 0)
        {
            image.color = new Color(1, 1, 1, 0.8f);
            text.text = "";
        }
        else
        {
            switch (value)
            {
                case 1:
                    text.text = "\xf10c";
                    text.fontSize = 140;
                    text.color = Color.red;
                    break;

                case 2:
                    text.text = "\xf00d";
                    text.fontSize = 160;
                    text.color = Color.blue;
                    break;
            }

            var duration = withAnimation ? 0.5f : 0;
            image.DOFade(0.4f, duration);

            // TODO: comment out because it causes exception by setting localPosition as (NaN, NaN, NaN)
            // text.GetComponent<RectTransform>().DOShakePosition(duration, 20, 20);
        }
    }

    public void SetHighlight(int x, int y, float delay)
    {
        var image = _gridRects[x, y].GetComponent<Image>();
        image.DOFade(0.8f, 0.5f).SetDelay(delay);
        image.DOColor(Color.black, 0.5f).SetDelay(delay);
    }
}
