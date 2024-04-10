using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CellIndicatorAnimation : MonoBehaviour
{
    [SerializeField] Image indicator;
    int alpha = 0;
    int maxAlpha = 70;
    [SerializeField] int animationSpeed = 10;
    bool up = true;

    float interval = 0.1f;
    float timer = 0f;
    bool showing = false;

    void Update()
    {
        if (showing) {
            timer += Time.deltaTime;

            if (timer >= interval) {
                updateAlpha();
                changeAlpha(alpha);
                timer = 0f;
            }
        }
    }

    public void visible(bool show) {
        alpha = 0;
        timer = 0f;
        changeAlpha(alpha);
        showing = show;
    }

    void updateAlpha() {
        if (up) {
            alpha += animationSpeed;
            if (alpha >= maxAlpha) {
                alpha = maxAlpha;
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
        changeAlpha(alpha);
    }

    void changeAlpha(float aa) {
        Color tempColor = indicator.color;
        tempColor.a = aa / maxAlpha;
        indicator.color = tempColor;
    }
}
