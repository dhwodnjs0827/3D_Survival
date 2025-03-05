using System;
using UnityEngine;
using UnityEngine.UI;

// Condition 개별 바는 같은 로직을 공유
// 코드 재활용을 위해 개별 스크립트로 작업
public class Condition : MonoBehaviour
{
    public float curValue;
    public float startValue;
    public float maxValue;
    public float passiveValue;
    public Image uiBar;

    private void Start()
    {
        curValue = startValue;
    }

    private void Update()
    {
        uiBar.fillAmount = GetPercentage();
    }

    private float GetPercentage()
    {
        return curValue / maxValue;
    }

    public void Add(float value)
    {
        // 둘 중의 작은 값 (ex. maxValue보다 커지면 maxValue)
        curValue = Math.Min(curValue + value, maxValue);
    }

    public void Subtract(float value)
    {
        // 둘 중의 큰 값 (ex. 0보다 작아지면 0)
        curValue = Math.Max(curValue - value, 0);
    }
}