using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CellSelction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    MainGameManager GM;
    [SerializeField] Cell cell;

    void Start() {
        GM = MainGameManager.Instance;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GM.pointingCell = cell;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        GM.pointingCell = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GM.selectedCard == null)   return;
        if (cell.isPlaceable) {
            Card card = GM.selectedCard;
            if (card == null)   return;
            
            if (!card.played)
                GM.requestPlayCard(GM.selectedCard.handID, GM.pointingCell.id, GM.playerIden1.isLocalPlayer);
            else if (card.played && card.cardType == CardType.Unit)
                GM.requestMoveUnit(GM.selectedCard.cellOnID, GM.pointingCell.id, GM.playerIden1.isLocalPlayer, false);
        }
    }
}
