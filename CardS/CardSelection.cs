using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

// IBeginDragHandler, IDragHandler, IEndDragHandler
public class CardSelection : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    MainGameManager mainGameManager;
    Card thisCard;

    void Start() {
        mainGameManager = MainGameManager.Instance;

        // disable drag by other player
        // canDrag = GetComponent<NetworkIdentity>().isOwned;
        thisCard = GetComponent<Card>();
    }

    // disable?
    #region drag
    // // can only drag card in player hand
    // public bool canDrag;
    // bool _isDragging = false;
    // Vector2 originalPos;
    // Vector3 originalScale;
    // public void OnBeginDrag(PointerEventData eventData)
    // {
    //     if (!canDrag | !thisCard.owner) return;

    //     mainGameManager.selectCard(gameObject);

    //     // control card movement
    //     _isDragging = true;
    //     // save original position
    //     if (originalPos == default) {
    //         originalPos = transform.position;
    //         originalScale = transform.localScale;
    //     }
    //     transform.localScale *= 0.8f;
    // }
    // public void OnDrag(PointerEventData eventData)
    // {
    //     // card follow mouse movement
    //     if (!canDrag) return;
    //     transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

    //     // show avaliable pos
    //     // todo
    // }
    // public void OnEndDrag(PointerEventData eventData)
    // {
    //     if (!canDrag) return;
    //     _isDragging = false;
    //     canDrag = false;

    //     // check drop position
    //     // if on valid cell, play effect
    //     // todo
    //     putCard(gameObject);
    // }
    
    // public void putCard(GameObject card) {
    //     // put card on cell, disable cell
    //     if (mainGameManager.pointingCell != null) {
    //         mainGameManager.requestPlayCard(card, mainGameManager.pointingCell);
    //     }
    //     // return to original position if not drop on valid cell
    //     else {
    //         card.transform.position = originalPos;
    //         card.transform.localScale = originalScale;
    //     }
    // }
    #endregion

    #region pointAt
    public void OnPointerEnter(PointerEventData eventData)
    {
        // if (_isDragging) return;
        if (!thisCard.owner.isLocalPlayer && !thisCard.played) return;
        //show card description
        int id = gameObject.GetComponent<Card>().cardID;
        mainGameManager.showCardPreview(id);
        if (mainGameManager.selectedCard == null)
            mainGameManager.showavailableMove(GetComponent<Card>());
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        // if (_isDragging) return;
        //hide card description
        mainGameManager.hideCardPreview();
        if (mainGameManager.selectedCard != null) {
            mainGameManager.showavailableMove(mainGameManager.selectedCard);
            return;
        }
        mainGameManager.resetValidCell();
    }
    #endregion

    #region click
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!thisCard.owner.isLocalPlayer) return;
        if (mainGameManager.selectedCard == null)
            mainGameManager.selectCard(gameObject);
    }
    #endregion

    private void OnTriggerEnter2D(Collider2D collider) {
        if (collider.gameObject.CompareTag("Cell")) {
            mainGameManager.pointingCell = collider.gameObject.GetComponent<Cell>();
        }
    }
    private void OnTriggerExit2D(Collider2D collider) {
        if (collider.gameObject.CompareTag("Cell")) {
            mainGameManager.pointingCell = null;
        }
    }
}
