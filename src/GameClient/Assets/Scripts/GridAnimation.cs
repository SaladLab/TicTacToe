using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GridAnimation : MonoBehaviour
{
    public Text LetterTemplate;

    private Text[] _letters;

    protected void Start()
    {
        BuildLetters();
        StartAnimation();
    }

    private void BuildLetters()
    {
        var caption = "TICTACTOE";
        _letters = new Text[9];
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                var index = y * 3 + x;
                var text = (Text)Instantiate(LetterTemplate);
                text.transform.SetParent(LetterTemplate.transform.parent, false);
                text.transform.localPosition = new Vector3(x * 200 - 200, y * -200 + 200, 0);
                text.text = caption[index].ToString();
                _letters[index] = text;
            }
        }
        LetterTemplate.gameObject.SetActive(false);
    }

    private void StartAnimation()
    {
        Debug.Log("StartAnimation");
        foreach (var letter in _letters)
        {
            var time = Random.Range(0f, 5f);

            var l = letter;
            var x = letter.text + "";
            var sequence = DOTween.Sequence()
                                  .AppendInterval(time)
                                  .Append(l.transform.DOShakePosition(1, 10, 20, 90))
                                  .AppendCallback(() => l.text = Random.Range(0, 2) == 0 ? "X" : "O")
                                  .AppendInterval(2)
                                  .Append(l.transform.DOShakePosition(1, 10, 20, 90))
                                  .AppendCallback(() => l.text = x)
                                  .AppendInterval(5 - time)
                                  .SetLoops(-1, LoopType.Restart);
        }
    }
}
