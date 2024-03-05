using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public class Cell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    MainGameManager mainGameManager;
    // indicate cell is valid
    public GameObject avaliableIndicator;
    public bool isPlaceable = false;
    // row: 100*r;  col: 1*c
    public int id;

    void Start() {
        mainGameManager = MainGameManager.Instance;
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        mainGameManager.pointingCell = gameObject.GetComponent<Cell>();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        mainGameManager.pointingCell = default;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPlaceable) {
            Card card = mainGameManager.selectedCard;
            if (!card.played)
                mainGameManager.requestPlayCard(mainGameManager.selectedCard.handID, mainGameManager.pointingCell.id, mainGameManager.playerIden1.isLocalPlayer);
            else if (card.played && card.cardType == CardType.Unit)
                mainGameManager.requestMoveUnit(mainGameManager.selectedCard.cellOnID, mainGameManager.pointingCell.id, mainGameManager.playerIden1.isLocalPlayer, false);
        }
    }

    // show cell is placable
    public void showValidity(bool valid) {
        // make animation indicator, instead of just image
        // todo
        if (valid) {
            avaliableIndicator.SetActive(true);
            isPlaceable = true;
        }
        else {
            avaliableIndicator.SetActive(false);
            isPlaceable = false;
        }
    }
}
