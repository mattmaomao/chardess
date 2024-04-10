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
    public int uniqueID;
    public int cardID;
    public string cardName;
    public CardType cardType;
    public bool played = false;
    public int playedTurn = 0;
    public int moved = 0;
    public bool autoMoved = false;
    public int cellOnID;
    public int handID = -1;
    #endregion

    // card stat
    #region card stat
    public int cost;
    public int maxMove;
    public Direction manual_moveDir;
    public int manual_moveAmount;
    public Direction auto_moveDir;
    public int auto_moveAmount;
    [SerializeField] Text cardDescription;

    // for spell
    public SpellFrom spellCaster;
    public SpellType spellType;
    public Direction spellDir = Direction.None;
    public int spellRange = 0;
    #endregion

    // display
    #region card display
    [SerializeField] GameObject cardFace;
    [SerializeField] GameObject playedFace;
    [SerializeField] GameObject cardBack;
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
        costNum.text = $"{cost}";
        maxMove = _cardObj.maxMove;
        manual_moveDir = _cardObj.manual_moveDir;
        manual_moveAmount = _cardObj.manual_moveAmount;
        auto_moveDir = _cardObj.auto_moveDir;
        auto_moveAmount = _cardObj.auto_moveAmount;

        // spell
        spellType = _cardObj.spellType;
        spellCaster = _cardObj.spellFrom;
        spellDir = _cardObj.spellDir;
        spellRange = _cardObj.spellRange;

        // display
        cardIcon.sprite = _cardObj.cardIcon;
        playedSprite.sprite = _cardObj.boardSprite;
        cardDescription.text = _cardObj.cardDescription;

        // move icon
        setManualMove();
        setAutoMove();
        // set icon for spell
        setSpellFrom();
        setSpellRange();

    }

    // reset "local" data after discard
    public void onDiscardEffect() {
        played = false;
        playedTurn = 0;
        cellOnID = 0;
        handID = -1;
        gameObject.SetActive(false);
        updateSprite();
    }

    // effect active on play
    public void onPlayEffect(int cellID, bool byServer) {
        switch (cardType) {
            case CardType.None:
                Debug.LogError("this card has no type");
                break;

            case CardType.Unit:
                RectTransform cardRT = gameObject.GetComponent<RectTransform>();
                cardRT.anchoredPosition = new Vector2(0, 0);
                cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                gameObject.SetActive(true);

                played = true;
                // card cant move when just played
                moved = maxMove;
                updateSprite();
                break;

            case CardType.Spell:
                Spells.castSpell(this, spellType, cellID, byServer);
                break;

            // later
            // case CardType.Trap:
            //     break;

            default:
                Debug.LogError("not yet implemented");
                break;
        }
    }

    // auto run effect on turn end
    public void onTurnEndEffect() {
        if (!played) return;
        playedTurn++;
        moved = 0;
        // auto move
        int dir = owner == mainGameManager.playerIden1 ? 1 : -1;
        switch (auto_moveDir) {
            case Direction.None:
                mainGameManager.readyToGoOn = true;
                break;
            case Direction.Hori:
                mainGameManager.requestMoveUnit(cellOnID, cellOnID + auto_moveAmount * dir, owner == mainGameManager.playerIden1, true);
                break;
            case Direction.Verti:
                mainGameManager.requestMoveUnit(cellOnID, cellOnID + auto_moveAmount * 100 * dir, owner == mainGameManager.playerIden1, true);
                break;
            case Direction.Forward:
                mainGameManager.requestMoveUnit(cellOnID, cellOnID + auto_moveAmount * dir, owner == mainGameManager.playerIden1, true);
                break;
            default:
                Debug.LogError("not yet implemented");
                break;
        }
        return;
    }

    #region UI
    void setManualMove() {
        manualMoveNum.text = $"{manual_moveAmount}";
        switch (manual_moveDir) {
            case Direction.None:
                manualMoveIcon.sprite = mainGameManager.sprites[7];
                manualMoveNum.text = "";
                break;
            case Direction.Hori:
                manualMoveIcon.sprite = mainGameManager.sprites[4];
                break;
            case Direction.Verti:
                manualMoveIcon.sprite = mainGameManager.sprites[5];
                break;
            case Direction.Forward:
                manualMoveIcon.sprite = mainGameManager.sprites[6];
                break;
            case Direction.Ortho:
                manualMoveIcon.sprite = mainGameManager.sprites[8];
                break;
            case Direction.Cross:
                manualMoveIcon.sprite = mainGameManager.sprites[9];
                break;
            case Direction.AllDir:
                manualMoveIcon.sprite = mainGameManager.sprites[10];
                break;
        }
    }

    void setAutoMove() {
        autoMoveNum.text = $"{auto_moveAmount}";
        switch (auto_moveDir) {
            case Direction.None:
                autoMoveIcon.sprite = mainGameManager.sprites[7];
                autoMoveNum.text = "";
                break;
            case Direction.Hori:
                autoMoveIcon.sprite = mainGameManager.sprites[4];
                break;
            case Direction.Verti:
                autoMoveIcon.sprite = mainGameManager.sprites[5];
                break;
            case Direction.Forward:
                autoMoveIcon.sprite = mainGameManager.sprites[6];
                break;
            case Direction.Ortho:
                autoMoveIcon.sprite = mainGameManager.sprites[8];
                break;
            case Direction.Cross:
                autoMoveIcon.sprite = mainGameManager.sprites[9];
                break;
            case Direction.AllDir:
                autoMoveIcon.sprite = mainGameManager.sprites[10];
                break;
        }
    }

    void setSpellFrom() {
        manualMoveNum.text = $"{spellType}";
        switch (spellCaster) {
            case SpellFrom.None:
                manualMoveIcon.sprite = mainGameManager.sprites[7];
                manualMoveNum.text = "";
                break;
            case SpellFrom.All:
                manualMoveIcon.sprite = mainGameManager.sprites[0]; // todo
                break;
            case SpellFrom.Targeted:
                manualMoveIcon.sprite = mainGameManager.sprites[0]; // todo
                break;
            case SpellFrom.King:
                manualMoveIcon.sprite = mainGameManager.sprites[0]; // todo
                break;
            case SpellFrom.Unit:
                manualMoveIcon.sprite = mainGameManager.sprites[0]; // todo
                break;
            case SpellFrom.NonUnit:
                manualMoveIcon.sprite = mainGameManager.sprites[0]; // later
                break;
            case SpellFrom.SpecUnit:
                manualMoveIcon.sprite = mainGameManager.sprites[0]; // later
                break;
        }
    }

    void setSpellRange() {
        autoMoveNum.text = $"{spellRange}";
        switch (spellDir) {
            case Direction.None:
                autoMoveIcon.sprite = mainGameManager.sprites[7];
                autoMoveNum.text = "";
                break;
            case Direction.Hori:
                autoMoveIcon.sprite = mainGameManager.sprites[4];
                break;
            case Direction.Verti:
                autoMoveIcon.sprite = mainGameManager.sprites[5];
                break;
            case Direction.Forward:
                autoMoveIcon.sprite = mainGameManager.sprites[6];
                break;
            case Direction.Ortho:
                autoMoveIcon.sprite = mainGameManager.sprites[8];
                break;
            case Direction.Cross:
                autoMoveIcon.sprite = mainGameManager.sprites[9];
                break;
            case Direction.AllDir:
                autoMoveIcon.sprite = mainGameManager.sprites[10];
                break;
        }
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
    #endregion -----------------------------------------

}