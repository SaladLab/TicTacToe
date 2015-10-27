using UnityEngine;
using System.Collections;

public class SpinRotator : MonoBehaviour
{
    private RectTransform _transform;
    private int _curRotate;

    protected void Start()
    {
        _transform = GetComponent<RectTransform>();
    }

    protected void OnEnable()
    {
        StartCoroutine(RotateCoroutine());
    }

    private IEnumerator RotateCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            _curRotate = (_curRotate + 1) % 8;
            _transform.localRotation = Quaternion.Euler(0, 0, -_curRotate * 45f);
        }
    }
}
