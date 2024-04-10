using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    MainGameManager GM;
    // indicate cell is valid
    public CellIndicatorAnimation avaliableIndicator;
    public bool isPlaceable = false;
    // row: 100*r;  col: 1*c
    public int id;

    void Start() {
        GM = MainGameManager.Instance;
        Invoke("resizeCollider", 0.2f);
    }

    // resize box collider
    void resizeCollider() {
        BoxCollider2D boxCollider = gameObject.GetComponent<BoxCollider2D>();
        RectTransform rt = gameObject.transform.GetComponent<RectTransform>();

        float width = rt.sizeDelta.x * rt.localScale.x;
        float height = rt.sizeDelta.y * rt.localScale.y;
        boxCollider.size = new Vector2(width, height);
    }

    public void moveIndicatorSiblingIndex() {
        for (int i = 0; i < transform.childCount; i++) {
            if (transform.GetChild(i).gameObject == avaliableIndicator.gameObject) {
                transform.GetChild(0).SetAsLastSibling();
                break;
            }
        }
    }

    // show cell is placable
    public void showValidity(bool valid) {
        // make animation indicator, instead of just image
        if (valid) {
            avaliableIndicator.gameObject.SetActive(true);
            avaliableIndicator.visible(true);
            isPlaceable = true;
        }
        else {
            avaliableIndicator.gameObject.SetActive(false);
            avaliableIndicator.visible(false);
            isPlaceable = false;
        }
    }
}
