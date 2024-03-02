using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public CardScriptableObject cardObj;
    MainGameManager mainGameManager;

    // card info
    #region card info
    public NetworkIdentity owner;
    public int cardID;
    public string cardName;
    public CardType cardType;
    public bool played = false;
    public int playedTurn = 0;
    public int moved = 0;
    public Cell cellOn;
    public int handID = -1;
    #endregion

    // card stat
    #region card stat
    public int cost;
    public int maxMove;
    public MoveDirection manual_moveDir;
    public int manual_moveAmount;
    public MoveDirection auto_moveDir;
    public int auto_moveAmount;
    [SerializeField] Text cardDescription;
    #endregion

    // display
    #region card display
    [SerializeField] GameObject cardFace;
    [SerializeField] GameObject playedFace;
    public GameObject cardBack;
    [SerializeField] Image cardIcon;
    [SerializeField] Image playedSprite;
    [SerializeField] Image playedSprite_Frame;
    [SerializeField] Image manualMoveIcon;
    [SerializeField] Image autoMoveIcon;
    [SerializeField] Image costIcon;
    [SerializeField] Text manualMoveNum;
    [SerializeField] Text autoMoveNum;
    [SerializeField] Text costNum;
    #endregion

    void Start() {
        mainGameManager = MainGameManager.Instance;
    }
    
    // init card info from CardScriptableObject
    public void init(CardScriptableObject _cardObj) {
        mainGameManager = MainGameManager.Instance;

        // info
        cardID = _cardObj.cardID;
        cardName = _cardObj.cardName;
        cardType = _cardObj.cardType;
        // stat
        cost = _cardObj.cost;
        maxMove = _cardObj.maxMove;
        manual_moveDir = _cardObj.manual_moveDir;
        manual_moveAmount = _cardObj.manual_moveAmount;
        auto_moveDir = _cardObj.auto_moveDir;
        auto_moveAmount = _cardObj.auto_moveAmount;
        // display
        cardIcon.sprite = _cardObj.cardIcon;
        playedSprite.sprite = _cardObj.boardSprite;
        cardDescription.text = _cardObj.cardDescription;

        // move icon
        manualMoveNum.text = $"{manual_moveAmount}";
        switch (manual_moveDir) {
            case MoveDirection.None:
                manualMoveIcon.sprite = mainGameManager.sprites[7];
                manualMoveNum.text = "";
                break;
            case MoveDirection.Horizontal:
                manualMoveIcon.sprite = mainGameManager.sprites[4];
                break;
            case MoveDirection.Vertical:
                manualMoveIcon.sprite = mainGameManager.sprites[5];
                break;
            case MoveDirection.Forward:
                manualMoveIcon.sprite = mainGameManager.sprites[6];
                break;
            case MoveDirection.Ortho:
                manualMoveIcon.sprite = mainGameManager.sprites[8];
                break;
            case MoveDirection.Cross:
                manualMoveIcon.sprite = mainGameManager.sprites[9];
                break;
            case MoveDirection.AllDir:
                manualMoveIcon.sprite = mainGameManager.sprites[10];
                break;
        }

        autoMoveNum.text = $"{auto_moveAmount}";
        switch (auto_moveDir) {
            case MoveDirection.None:
                autoMoveIcon.sprite = mainGameManager.sprites[7];
                autoMoveNum.text = "";
                break;
            case MoveDirection.Horizontal:
                autoMoveIcon.sprite = mainGameManager.sprites[4];
                break;
            case MoveDirection.Vertical:
                autoMoveIcon.sprite = mainGameManager.sprites[5];
                break;
            case MoveDirection.Forward:
                autoMoveIcon.sprite = mainGameManager.sprites[6];
                break;
            case MoveDirection.Ortho:
                autoMoveIcon.sprite = mainGameManager.sprites[8];
                break;
            case MoveDirection.Cross:
                autoMoveIcon.sprite = mainGameManager.sprites[9];
                break;
            case MoveDirection.AllDir:
                autoMoveIcon.sprite = mainGameManager.sprites[10];
                break;
        }

        costNum.text = $"{cost}";
    }

    // set the card face up/ down
    public void faceUp(bool fliped) {
        cardBack.SetActive(!fliped);
    }

    // change sprite after placed on board
    public void updateSprite() {
        if (cardType == CardType.Spell) return;
        // flip unit sprite if owned by player 2
        if (played && owner == mainGameManager.playerIden2) {
            Vector3 scale = transform.localScale;
            scale.x = -1;
            transform.localScale = scale;
        }
        if (owner.isLocalPlayer)
            playedSprite_Frame.color = new Color(0, 0, 255);
        else
            playedSprite_Frame.color = new Color(255, 0, 0);
        cardFace.SetActive(!played);
        playedFace.SetActive(played);
    }

    // reset "local" data after discard
    public void onDiscardEffect() {
        played = false;
        playedTurn = 0;
    }

    // effect active on play
    public void onPlayEffect() {
        played = true;
        // card cant move when just played
        moved = maxMove;
    }

    // auto run effect on turn end
    public void onTurnEndEffect() {
        if (!played)    return;
        playedTurn++;
        moved = 0;
        // auto move
        int dir = owner == mainGameManager.playerIden1 ? 1 : -1;
        switch (auto_moveDir) {
            case MoveDirection.Horizontal:
                mainGameManager.requestMoveUnit(cellOn.id, cellOn.id + auto_moveAmount * dir, owner == mainGameManager.playerIden1, true);
                break;
            case MoveDirection.Vertical:
                mainGameManager.requestMoveUnit(cellOn.id, cellOn.id + auto_moveAmount * 100 * dir, owner == mainGameManager.playerIden1, true);
                break;
            case MoveDirection.Forward:
                mainGameManager.requestMoveUnit(cellOn.id, cellOn.id + auto_moveAmount * dir, owner == mainGameManager.playerIden1, true);
                break;
            default:
                Debug.LogError("not yet implemented");
                break;
        }
    }
}