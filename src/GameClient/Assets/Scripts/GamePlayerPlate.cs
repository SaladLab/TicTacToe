using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayerPlate : MonoBehaviour
{
    public RectTransform Grid;
    public Text GridText;
    public Text Name;
    public RectTransform Timer;
    public Text TimerValue;

    private Tween _timerTween;

    public void SetTurn(bool turn)
    {
        var image = GetComponent<Image>();
        image.color = new Color(1, 0, 0, turn ? 0.2f : 0.05f);
    }

    public void SetWin()
    {
        var image = GetComponent<Image>();
        image.color = new Color(1, 1, 0, 0.3f);
    }

    public void SetGrid(int value)
    {
        switch (value)
        {
            case 1:
                GridText.text = "\xf10c";
                GridText.color = Color.red;
                break;

            case 2:
                GridText.text = "\xf00d";
                GridText.color = Color.blue;
                break;

            default:
                GridText.text = "";
                GridText.color = Color.black;
                break;
        }
    }

    public void SetName(string text)
    {
        Name.text = text;
    }

    public void SetTimerOn(bool visible, int initialValue = 0)
    {
        Timer.gameObject.SetActive(visible);

        if (_timerTween != null)
        {
            _timerTween.Kill();
            _timerTween = null;
        }

        if (visible)
        {
            TimerValue.text = initialValue.ToString();
            _timerTween = DOTween.To(() => int.Parse(TimerValue.text),
                                     v => TimerValue.text = v.ToString(),
                                     0, initialValue)
                                 .SetEase(Ease.Linear);
        }
    }
}
