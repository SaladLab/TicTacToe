using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class GameBoard : MonoBehaviour
{
    public RectTransform GridTemplate;

    public event Action<int, int> GridClicked;

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
            text.GetComponent<Transform>().DOShakePosition(duration, 20, 20);
        }
    }
}
