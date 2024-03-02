using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CellIndicatorAnimation : MonoBehaviour
{
    [SerializeField] Image indicator;
    int alpha = 0;
    [SerializeField] int animationSpeed = 10;
    bool up = true;

    float interval = 0.1f;
    float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval) {
            changeAlpha();
            timer = 0f;
        }
    }

    void changeAlpha() {
        if (up) {
            alpha += animationSpeed;
            if (alpha >= 100) {
                alpha = 100;
                up = false;
            }
        }
        else {
            alpha -= animationSpeed;
            if (alpha <= 0) {
                alpha = 0;
                up = true;
            }
        }
        Color tempColor = indicator.color;
        tempColor.a = alpha / 100f;
        indicator.color = tempColor;
    }
}
