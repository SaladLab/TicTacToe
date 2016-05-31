using System;
using DG.Tweening;
using Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameBoard : MonoBehaviour
{
    public RectTransform GridTemplate;

    public Action<int, int> GridClicked;

    private int[,] _gridMarks = new int[Rule.BoardSize, Rule.BoardSize];
    private RectTransform[,] _gridRects = new RectTransform[Rule.BoardSize, Rule.BoardSize];

    private void Start()
    {
        BuildGrids();
    }

    private void BuildGrids()
    {
        for (int y = 0; y < Rule.BoardSize; y++)
        {
            for (int x = 0; x < Rule.BoardSize; x++)
            {
                var grid = UiHelper.AddChild(gameObject, GridTemplate.gameObject);
                grid.transform.SetParent(transform, false);
                grid.transform.localPosition = new Vector3((x * 210) - 210, (y * -210) + 210, 0);

                var et = new EventTrigger.TriggerEvent();
                var localX = x;
                var localY = y;
                et.AddListener(_ => OnGridClick(localX, localY));
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
