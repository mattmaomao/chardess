using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using Unity.VisualScripting;

public enum GameState {Wait, Init, DrawCards, PlayCards, TurnEnd, GameEnd, Debug};
public class MainGameManager : NetworkBehaviour
{
    #region Singleton
    public static MainGameManager Instance { get; private set; }
    private void Awake() {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    #endregion -----------------------------------------

    // control game flow
    #region game logic
    [SyncVar] [SerializeField] GameState gameState;
    GameState nextState = GameState.Debug;
    [SyncVar] bool readyToGoOn = false;

    #region player
    [SyncVar] [SerializeField] Player player1Data, player2Data;
    [SyncVar] public NetworkIdentity playerIden1, playerIden2;
    [SyncVar] public string player1Name, player2Name;
    [SyncVar] [SerializeField] NetworkIdentity startingPlayer, currentPlayer;
    [SyncVar] [SerializeField] int playerIndex = 1;
    #endregion -----------------------------------------
    
    // board
    public Cell pointingCell;
    Dictionary<int, Cell> boardCell = new();
    Dictionary<int, GameObject> boardCard = new();
    int boardRows, boardCols;
    // card
    public Card selectedCard;
    // turn
    [SyncVar] int currentTurn = 0;
    // public bool playedUnit, movedUnit, playedSpell;
    #endregion -----------------------------------------

    // deck lists
    #region deck lists
    public List<GameObject> playerDeck = new();
    public List<GameObject> opponentDeck = new();
    public List<GameObject> playerDiscard = new();
    public List<GameObject> opponentDiscard = new();
    public List<GameObject> playerHandList = new();
    public List<GameObject> opponentHandList = new();
    #endregion -----------------------------------------

    // in-game objects
    #region in-game objects
    [SerializeField] GameObject boardObj;
    [SerializeField] GameObject deckPObj;
    [SerializeField] GameObject deckOObj;
    [SerializeField] GameObject discardPObj;
    [SerializeField] GameObject discardOObj;
    [SerializeField] GameObject handPObj;
    [SerializeField] GameObject handOObj;
    #endregion -----------------------------------------

    // UI display
    #region UI display
    [SerializeField] MessageManager messageManager;
    [SerializeField] Card bigCardDisplay;
    [SerializeField] Card smallCardDisplay;
    [SerializeField] GameObject cancelSelectPanel;

    [SerializeField] Text playerDeckText, opponentDeckText, playerDiscardText, opponentDiscardText;
    [SerializeField] Text currentTurnText, currentPlayerText;
    [SerializeField] Text playerNameText, opponentNameText, playerHpText, opponentHpText, playerManaText, opponentManaText, playerHandText, opponentHandText;
    #endregion -----------------------------------------

    // cardss
    #region card
    // moving direction icon
    public Sprite[] sprites;
    // blank card prefab
    [SerializeField] GameObject tempCardObj;
    // cards pre-set
    [SerializeField] CardScriptableObject[] cardsScriptObjs;
    [SerializeField] Sprite[] cardsScriptObjs_Icon;
    [SerializeField] Sprite[] cardsScriptObjs_BoardSprite;
    #endregion -----------------------------------------

    // show while loading
    [SerializeField] GameObject waitingPane;
    // debug
    bool gameEnded = false;

    [ServerCallback]
    void Update() {
        playerIndex = currentPlayer == playerIden1 ? 1 : 2;
        // state control
        switch (gameState) {
            // wait, do nth
            case GameState.Wait:
                if (nextState != GameState.Debug) {
                    gameState = nextState;
                    nextState = GameState.Debug;
                }
                break;
            // init everything needed to run the game
            case GameState.Init:
                resetGame();
                initPlayerData();
                initCardDeck();
                Rpc_initBoard();
                // init both players' hand
                StartCoroutine(slowInitHand());
                gameState = GameState.Wait;
                break;
            // draw card
            case GameState.DrawCards:
                Rpc_updateUIInfo(player1Data, player2Data);
                // increase max mana
                if (playerIndex == 1) {
                    player1Data.incMaxMana();
                    Server_drawCard(currentPlayer, playerIden1);
                }
                else {
                    player2Data.incMaxMana();
                    Server_drawCard(currentPlayer, playerIden2);
                }

                // go to next state (play)
                gameState = GameState.PlayCards;
                break;
            // wait for player to play card, move unit
            case GameState.PlayCards:
                break;
            // switch player
            case GameState.TurnEnd:
                runTurnEndEffect();
                setTurnInfo();

                // go back to draw card state
                gameState = GameState.DrawCards;
                break;
            // terminate game
            case GameState.GameEnd:
                // todo
                Debug.Log("game end");
                gameEnded = true;
                break;
            // debug
            case GameState.Debug:
                break;
        }
    }

    #region init game
    // load card is slow...
    IEnumerator slowInitHand()
    {
        yield return new WaitUntil(() => readyToGoOn);
        for (int i = 0; i < 3; i++) {
            Server_drawCard(playerIden1, playerIden1);
            Server_drawCard(playerIden2, playerIden2);
        }
        readyToGoOn = false;
        Rpc_updateUIInfo(player1Data, player2Data);

        // skip drawing card, jump to play state
        nextState = GameState.PlayCards;
    }

    // reset game for restart/ clear
    [ServerCallback]
    public void resetGame() {
        startingPlayer = playerIden1;
        currentPlayer = startingPlayer;
        currentTurn = 1;
        currentTurnText.text = $"Turn: {currentTurn}";
        currentPlayerText.text = $"Current player: \n\t{currentPlayer.name}";
        gameState = GameState.Init;
    }

    // init player data
    [ServerCallback]
    void initPlayerData() {
        player1Data = new();
        player2Data = new();
        player1Data.init(player1Name);
        player2Data.init(player2Name);
    }
    
    // init board, assign each cell unique id
    [ClientRpc]
    void Rpc_initBoard() {
        GameObject boardGrid = boardObj.transform.GetChild(0).gameObject;
        int cellID = 0;
        for (int i = 0; i < boardGrid.transform.childCount; i++) {
            Transform row = boardGrid.transform.GetChild(i);
            for (int j = 0; j < row.childCount; j++) {
                GameObject cell = row.transform.GetChild(j).gameObject;
                cell.GetComponent<Cell>().id = cellID;
                boardCell[cellID] = cell.GetComponent<Cell>();
                cellID++;
            }
            if (boardCols == 0) 
                boardCols = row.childCount;
            cellID -= row.childCount;
            cellID += 100;
        }
        boardRows = cellID/100;
    }
    #endregion -----------------------------------------

    #region card manager (init deck, load card, draw card)
    // init fix card deck (set #each card)
    [ServerCallback]
    void initCardDeck() {
        clearAllDeck();
        // init fixed deck with index
        int[] fixedDeck = {0, 5, 0};

        // instantiate and spawn cards
        for (int i = 0; i < fixedDeck.Length; i++) {
            for (int j = 0; j < fixedDeck[i] ; j++) {
                CardScriptableObject c = cardsScriptObjs[i];
                GameObject cardObj = Instantiate(tempCardObj);
                cardObj.GetComponent<Card>().init(c);
                cardObj.SetActive(false);

                cardObj.GetComponent<Card>().owner = playerIden1;
                player1Data.deck.Add(cardObj);
            }
        }
        for (int i = 0; i < fixedDeck.Length; i++) {
            for (int j = 0; j < fixedDeck[i] ; j++) {
                CardScriptableObject c = cardsScriptObjs[i];
                GameObject cardObj = Instantiate(tempCardObj);
                cardObj.GetComponent<Card>().init(c);
                cardObj.SetActive(false);

                cardObj.GetComponent<Card>().owner = playerIden2;
                player2Data.deck.Add(cardObj);
            }
        }
        // update UI
        updateDeckCount();

        readyToGoOn = true;

        Rpc_loadCards(fixedDeck, fixedDeck);
    }

    // init card into deck
    [ClientRpc]
    void Rpc_loadCards(int[] fixDeck1, int[] fixDeck2) {
        // instantiate and spawn cards
        for (int i = 0; i < fixDeck1.Length; i++) {
            for (int j = 0; j < fixDeck1[i]; j++) {
                CardScriptableObject c = cardsScriptObjs[i];
                GameObject cardObj = Instantiate(tempCardObj);
                cardObj.GetComponent<Card>().init(c);
                cardObj.SetActive(false);
                cardObj.GetComponent<Card>().owner = playerIden1;

                if (playerIden1.isLocalPlayer) {
                    cardObj.transform.SetParent(deckPObj.transform, false);
                    playerDeck.Add(cardObj);
                }
                else {
                    cardObj.transform.SetParent(deckOObj.transform, false);
                    opponentDeck.Add(cardObj);
                }
            }
        }
        for (int i = 0; i < fixDeck2.Length; i++) {
            for (int j = 0; j < fixDeck2[i]; j++) {
                CardScriptableObject c = cardsScriptObjs[i];
                GameObject cardObj = Instantiate(tempCardObj);
                cardObj.GetComponent<Card>().init(c);
                cardObj.SetActive(false);
                cardObj.GetComponent<Card>().owner = playerIden2;

                if (playerIden2.isLocalPlayer) {
                    cardObj.transform.SetParent(deckPObj.transform, false);
                    playerDeck.Add(cardObj);
                }
                else {
                    cardObj.transform.SetParent(deckOObj.transform, false);
                    opponentDeck.Add(cardObj);
                }
            }
        }
        // update UI
        updateDeckCount();

        readyToGoOn = true;
    }

    #region draw card
    // draw ONE random card to player x
    [ServerCallback]
    void Server_drawCard(NetworkIdentity playerFrom, NetworkIdentity playerTo) {
        int rand;
        List<GameObject> deckFrom = playerFrom == playerIden1? player1Data.deck : player2Data.deck;
        List<GameObject> deckTo = playerTo == playerIden1 ? player1Data.hand : player2Data.hand;
        // reduce hp by 1 if draw from empty deck
        if (deckFrom.Count <= 0) {
            messageManager.addMsg(playerTo.connectionToClient, $"your deck is empty!");
            messageManager.addMsg(playerTo.connectionToClient, "you suffer from fatigue damage!");
            return;
        }

        // draw a rand card from deck
        rand = Random.Range(0, deckFrom.Count-1);
        deckTo.Add(deckFrom[rand]);
        deckTo[^1].GetComponent<Card>().handID = deckTo.Count-1;
        deckFrom.RemoveAt(rand);
        Rpc_drawCard(playerFrom == playerIden1? playerIden1 : playerIden2, playerTo, rand);
    }

    // request draw card form effect of card??
    // [Command]
    // void requestDrawCard() {
    //     int deckNum = playerIden1.isLocalPlayer? 1 : 2;
    //     Server_drawCard(deckNum, netIdentity);
    // }

    // show card after draw
    [ClientRpc]
    void Rpc_drawCard(NetworkIdentity playerFrom, NetworkIdentity playerTo, int newCardIndex) {
        List<GameObject> deckFrom = playerFrom.isLocalPlayer? playerDeck : opponentDeck;
        List<GameObject> deckTo = playerTo.isLocalPlayer? playerHandList : opponentHandList;

        deckTo.Add(deckFrom[newCardIndex]);
        deckFrom.RemoveAt(newCardIndex);
        deckTo[^1].GetComponent<Card>().handID = deckTo.Count-1;
        GameObject cardObj = deckTo[^1];

        cardObj.SetActive(true);
        // check ownership of the card
        if (playerTo.isLocalPlayer) {
            cardObj.transform.SetParent(handPObj.transform);
            cardObj.GetComponent<Card>().faceUp(true);
        }
        else {
            cardObj.transform.SetParent(handOObj.transform);
            cardObj.GetComponent<Card>().faceUp(false);
        }

        updateDeckCount();
    }
    #endregion -----------------------------------------

    // empty all deck
    [ServerCallback]
    void clearAllDeck() {
        playerDeck.Clear();
        opponentDeck.Clear();
        playerDiscard.Clear();
        opponentDiscard.Clear();
        playerHandList.Clear();
        opponentHandList.Clear();
    }
    
    #endregion -----------------------------------------

    #region card control (preview, select, show indicator, play card)
    // show card preview
    [ClientCallback]
    public void showCardPreview(int id) {
        bigCardDisplay.init(cardsScriptObjs[id]);
        bigCardDisplay.gameObject.SetActive(true);
    }
    // hide card preview
    [ClientCallback]
    public void hideCardPreview() {
        bigCardDisplay.gameObject.SetActive(false);
    }

    // show small card preview, available move
    [ClientCallback]
    public void selectCard(GameObject card) {
        selectedCard = card.GetComponent<Card>();
        // change to init with current card (incase of update/upgrade card info)
        CardScriptableObject cardTemp = cardsScriptObjs[card.GetComponent<Card>().cardID];
        smallCardDisplay.init(cardTemp); // change this function**
        smallCardDisplay.gameObject.SetActive(true);
        cancelSelectPanel.SetActive(true);

        showavailableMove(selectedCard);
    }
    // cancel card selection
    [ClientCallback]
    public void cancelSelect() {
        smallCardDisplay.gameObject.SetActive(false);
        cancelSelectPanel.SetActive(false);
        selectedCard = null;
        resetValidCell();
    }

    #region unit control (move unit, play card)
    #region board ui
    // show available move after selecting a card
    [ClientCallback]
    public void showavailableMove(Card card) {
        resetValidCell();
        int player = playerIden1.isLocalPlayer? 1 : 2;
        switch (card.cardType)
        {
            case CardType.Unit:
                // show valid cell to place a unit
                if (!card.played) {
                    foreach (Cell cell in unitPlacableCell(player))
                        cell.showValidity(true);
                }
                // show valid cell to move a unit
                else {
                    // show depends on card
                    foreach (Cell cell in unitMovableCell(card, player))
                        cell.showValidity(true);
                }
                break;
            case CardType.Spell:
                // todo
                break;

            default:
                Debug.Log("not yet implemented?");
                break;
        }
    }

    // cal the valid cell for each player to place a unit
    List<Cell> unitPlacableCell(int playerIndex) {
        List<Cell> cells = new();
        // player 1: left two cols
        if (playerIndex == 1) {
            int cellID = 0;
            for (int i = 0; i < boardRows; i++) {
                for (int j = 0; j < 2; j++) {
                    if (!boardCard.ContainsKey(cellID))
                        cells.Add(boardCell[cellID]);
                    cellID++;
                }
                cellID -= 2;
                cellID += 100;
            }
        }
        // player 2: right two cols
        else {
            int cellID = boardRows-1;
            for (int i = 0; i < boardRows; i++) {
                for (int j = 0; j < 2; j++) {
                    if (!boardCard.ContainsKey(cellID))
                        cells.Add(boardCell[cellID]);
                    cellID--;
                }
                cellID += 2;
                cellID += 100;
            }
        }
        return cells;
    }

    // get all cell to move manually to of a card
    List<Cell> unitMovableCell(Card card, int playerIndex) {
        List<Cell> cells = new();
        int id = card.cellOn.id;
        int x = id;
        switch (card.manual_moveDir) {
            case MoveDirection.None:    break;
            case MoveDirection.Horizontal:
                x = id - card.manual_moveAmount;
                for (int i = 0; i < 2*card.manual_moveAmount + 1; i++) {
                    if (x % 100 >= 0 && x != id && x % 100 < boardCols)
                        cells.Add(boardCell[x]);
                    x++;
                }
                break;
            case MoveDirection.Vertical:
                x = id - 100 * card.manual_moveAmount;
                for (int i = 0; i < 2*card.manual_moveAmount + 1; i++) {
                    if (x / 100 >= 0 && x != id && x / 100 < boardRows)
                        cells.Add(boardCell[x]);
                    x += 100;
                }
                break;
            case MoveDirection.Forward: // only move right for player 1, revserse for player 2
                x = playerIndex == 1? id+1: id-1;
                for (int i = 0; i < card.manual_moveAmount; i++) {
                    if (x % 100 >= 0 && x != id && x / 100 < boardCols)
                        cells.Add(boardCell[x]);
                    x = playerIndex == 1? x+1: x-1;
                }
                break;
            case MoveDirection.Ortho:
                // hori
                x = id - card.manual_moveAmount;
                for (int i = 0; i < 2*card.manual_moveAmount + 1; i++) {
                    if (x % 100 >= 0 && x != id && x % 100 < boardCols)
                        cells.Add(boardCell[x]);
                    x++;
                }
                // verti
                x = id - 100 * card.manual_moveAmount;
                for (int i = 0; i < 2*card.manual_moveAmount + 1; i++) {
                    if (x / 100 >= 0 && x != id && x / 100 < boardRows)
                        cells.Add(boardCell[x]);
                    x += 100;
                }
                break;
            case MoveDirection.Cross:
                // x -> top left
                x = id - 100 * card.manual_moveAmount - card.manual_moveAmount;
                for (int i = 0; i < 2*card.manual_moveAmount + 1; i++) {
                    if (x / 100 >= 0 && x % 100 >= 0 && x != id && x / 100 < boardRows && x % 100 < boardCols)
                        cells.Add(boardCell[x]);
                    x += 101;
                }
                // x -> bot left
                x = id + 100 * card.manual_moveAmount - card.manual_moveAmount;
                for (int i = 0; i < 2*card.manual_moveAmount + 1; i++) {
                    if (x / 100 >= 0 && x % 100 >= 0 && x != id && x / 100 < boardRows && x % 100 < boardCols)
                        cells.Add(boardCell[x]);
                    x -= 99;
                }
                break;
            case MoveDirection.AllDir:
                x = id - 100 * card.manual_moveAmount - card.manual_moveAmount;
                for (int i = 0; i < 2*card.manual_moveAmount + 1; i++) {
                    for (int j = 0; j < 2*card.manual_moveAmount + 1; j++) {
                        if (x / 100 >= 0 && x % 100 >= 0 && x != id && x / 100 < boardRows && x % 100 < boardCols)
                            cells.Add(boardCell[x]);
                        x++;
                    }
                    x -= 2*card.manual_moveAmount + 1;
                    x += 100;
                }
                break;
        }
        return cells;
    }

    // hide all cell placable indicator
    public void resetValidCell() {
        foreach (Cell cell in boardCell.Values) {
            cell.showValidity(false);
        }
    }
    #endregion -----------------------------------------

    // run card play effect/ place unit on borad
    [Command(requiresAuthority = false)]
    public void requestPlayCard(int cardHandID, int cellID, bool isP1) {
        Card card = isP1 ? player1Data.hand[cardHandID].GetComponent<Card>() : player2Data.hand[cardHandID].GetComponent<Card>();
        Cell cell = boardCell[cellID];
        NetworkIdentity calledPlayer = isP1 ? playerIden1 : playerIden2;
        Player playerData = isP1 ? player1Data : player2Data;

        // return if not supposed to play card
        if (currentPlayer != calledPlayer || gameState != GameState.PlayCards) {
            messageManager.addMsg(calledPlayer.connectionToClient, "you should not play card now!");
            return;
        }

        bool successPlayCard = false;
        // check if player has enough mana
        if (playerData.mana < card.cost) {
            messageManager.addMsg(calledPlayer.connectionToClient, "you don't have enough mana!");
        }
        else {
            successPlayCard = true;
            switch (card.cardType)
            {
                case CardType.Unit:
                    // save card on board
                    boardCard[cell.id] = card.gameObject;
                    card.cellOn = boardCell[cell.id].GetComponent<Cell>();

                    // play onPlay effect
                    card.onPlayEffect();

                    Rpc_playCard(card.handID, cell.id, isP1? playerIden1 : playerIden2);
                    break;
                case CardType.Spell:
                    // add card from discard
                    if (isP1)
                        player1Data.discard.Add(card.gameObject);
                    else
                        player2Data.discard.Add(card.gameObject);

                    // play onPlay effect
                    // todo

                    Rpc_playCard(card.handID, cell.id, isP1? playerIden1 : playerIden2);
                    break;
            }
        }

        // remove card from hand
        if (successPlayCard) {
            playerData.mana -= card.cost;
            playerData.hand.Remove(card.gameObject);
            // re-calculate cards' hand id
            for (int i = 0; i < playerData.hand.Count; i++)
                playerData.hand[i].GetComponent<Card>().handID = i;
        }

        pointingCell = null;
        cancelSelect();
        Rpc_updateUIInfo(player1Data, player2Data);
    }

    // update client ui after someone played a card
    [ClientRpc]
    void Rpc_playCard(int cardHandID, int cellID, NetworkIdentity player) {
        Card card;
        Cell cell = boardCell[cellID].GetComponent<Cell>();
        bool isMe = player.isLocalPlayer;
        // remove card from hand
        if (isMe) {
            card = playerHandList[cardHandID].GetComponent<Card>();
            playerHandList.Remove(card.gameObject);
            card.handID = -1;
            // re-calculate cards' hand id
            for (int i = 0; i < playerHandList.Count; i++)
                playerHandList[i].GetComponent<Card>().handID = i;
        }
        else {
            card = opponentHandList[cardHandID].GetComponent<Card>();
            opponentHandList.Remove(card.gameObject);
            card.handID = -1;
            // re-calculate cards' hand id
            for (int i = 0; i < opponentHandList.Count; i++)
                opponentHandList[i].GetComponent<Card>().handID = i;
        }

        switch (card.cardType)
        {
            case CardType.Unit:
                boardCard[cell.id] = card.gameObject;
                card.cellOn = boardCell[cell.id].GetComponent<Cell>();
                
                // put card on cell
                card.gameObject.transform.SetParent(cell.gameObject.transform);
                RectTransform cardRT = card.gameObject.GetComponent<RectTransform>();
                cardRT.anchoredPosition = new Vector2(0, 0);
                cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);

                // set card into placed UI (unit/ trap graphics)
                card.onPlayEffect();
                card.updateSprite();
                break;
            case CardType.Spell:
                // add card to discard
                if (isMe)
                    playerDiscard.Add(card.gameObject);
                else
                    opponentDiscard.Add(card.gameObject);

                // play onPlay effect
                // todo

                break;
        }
    }

    [Command (requiresAuthority = false)]
    public void requestMoveUnit(int cardBoardID, int cellID, bool isP1, bool autoMove) {
        Card card = boardCard[cardBoardID].GetComponent<Card>();
        Cell cell = boardCell[cellID];
        NetworkIdentity calledPlayer = isP1? playerIden1 : playerIden2;
        Player playerData = isP1 ? player1Data : player2Data;

        // return if not supposed to move card
        if (!autoMove && (currentPlayer != calledPlayer || gameState != GameState.PlayCards)) {
            messageManager.addMsg(calledPlayer.connectionToClient, "you should not move a card now!");
            return;
        }

        bool successMoveCard = false;
        // move card on board
        // step on others effect
        if (card.moved >= card.maxMove || autoMove) {
            successMoveCard = true;
            if (boardCard.ContainsKey(cell.id)) {
                Card cardOnTargetCell = boardCard[cell.id].GetComponent<Card>();
                if (cardOnTargetCell.cardType == CardType.Unit) {
                    // cant eat king, reduce hp instead, then destroy self
                    if (cardOnTargetCell.cardID == 0) {
                        playerData.hp--;
                        boardCard[card.cellOn.id] = null;
                        // discard moving unit
                        if (card.owner == playerIden1)
                            player1Data.discard.Add(card.gameObject);
                        else 
                            player2Data.discard.Add(card.gameObject);
                        // reset card player state
                        card.onDiscardEffect();
                    }
                    // eat normal units
                    else {
                        // discard eaten unit
                        if (cardOnTargetCell.owner == playerIden1)
                            player1Data.discard.Add(cardOnTargetCell.gameObject);
                        else 
                            player2Data.discard.Add(cardOnTargetCell.gameObject);
                        // reset card player state
                        cardOnTargetCell.onDiscardEffect();

                        boardCard[cell.id] = card.gameObject;
                        boardCard[card.cellOn.id] = null;
                        card.cellOn = cell;
                    }
                }
                // if (cardOnTargetCell.GetComponent<Card>().cardType == CardType.Trap) 
                // later
            }
            // just move
            else {
                boardCard[cell.id] = card.gameObject;
                boardCard[card.cellOn.id] = null;
                card.cellOn = cell;
            }
        }
        else if (card.moved >= card.maxMove) {
            successMoveCard = false;
            messageManager.addMsg(calledPlayer.connectionToClient, "you cannot move this unit yet!");
        }

        if (successMoveCard)
            Rpc_moveCard(cardBoardID, cellID, player1Data, player2Data);

        pointingCell = null;
        cancelSelect();
        Rpc_updateUIInfo(player1Data, player2Data);
    }

    // update client ui after someone moved a unit
    [ClientRpc]
    public void Rpc_moveCard(int cardBoardID, int cellID, Player player1Data, Player player2Data) {
        Card card = boardCard[cardBoardID].GetComponent<Card>();
        Cell cell = boardCell[cellID].GetComponent<Cell>();
        Player player = playerIden1.isLocalPlayer ? player1Data : player2Data;

        // move card on board
        // step on others effect
        if (boardCard.ContainsKey(cell.id)) {
            Card cardOnTargetCell = boardCard[cell.id].GetComponent<Card>();
            if (cardOnTargetCell.cardType == CardType.Unit) {
                // cant eat king, reduce hp instead, then destroy self
                if (cardOnTargetCell.cardID == 0) {
                    boardCard[card.cellOn.id] = null;
                    // discard moving unit
                    if (card.owner.isLocalPlayer)
                        player.discard.Add(card.gameObject);
                    else 
                        player.discard.Add(card.gameObject);
                    // reset card player state
                    card.onDiscardEffect();
                }
                // eat normal units
                else {
                    // discard eaten unit
                    if (cardOnTargetCell.owner.isLocalPlayer)
                        player.discard.Add(cardOnTargetCell.gameObject);
                    else 
                        player.discard.Add(cardOnTargetCell.gameObject);
                    // reset card player state
                    cardOnTargetCell.onDiscardEffect();

                    boardCard[cell.id] = card.gameObject;
                    boardCard[card.cellOn.id] = null;
                    card.cellOn = cell;

                    // put card on cell
                    card.gameObject.transform.SetParent(cell.gameObject.transform, false);
                }
            }
            // if (cardOnTargetCell.GetComponent<Card>().cardType == CardType.Trap) 
            // later
        }
        // just move
        else {
            boardCard[cell.id] = card.gameObject;
            boardCard[card.cellOn.id] = null;
            card.cellOn = cell;

            // put card on cell
            card.gameObject.transform.SetParent(cell.gameObject.transform, false);  
        }
    }
    #endregion -----------------------------------------

    #endregion -----------------------------------------

    #region turn end (handle what happen between player's turn) ---- auto move unit!!
    // manually end turn by current player
    public void turnEndButton() {
        turnEnd(playerIden1.isLocalPlayer ? playerIden1 : playerIden2);
    }

    [Command (requiresAuthority = false)]
    void turnEnd(NetworkIdentity player) {
        if (player != currentPlayer)    return;
        gameState = GameState.TurnEnd;
    }

    // card auto effect on turn end
    [ServerCallback]
    void runTurnEndEffect() {
        // todo
        foreach (GameObject cardObj in boardCard.Values) {
            Card card = cardObj.GetComponent<Card>();
            card.onTurnEndEffect();
        }
    }

    // ready next turn
    [ServerCallback]
    void setTurnInfo() {
        // reset in-turn counter
        currentTurn++;

        // set next player
        currentPlayer = currentPlayer == playerIden1 ? playerIden2 : playerIden1;
        playerIndex = playerIndex == 1 ? 2 : 1;
    }
    #endregion -----------------------------------------

    #region UI
    void updateDeckCount() {
        playerDeckText.text = $"{playerDeck.Count}";
        opponentDeckText.text = $"{opponentDeck.Count}";
        playerDiscardText.text = $"{playerDiscard.Count}";
        opponentDiscardText.text = $"{opponentDiscard.Count}";
    }

    void updateCardHandID() {
        for (int i = 0; i < playerHandList.Count; i++)
            playerHandList[i].GetComponent<Card>().handID = i;
        for (int i = 0; i < opponentHandList.Count; i++)
            opponentHandList[i].GetComponent<Card>().handID = i;
    }

    void updatePlayerInfo(Player player1Data, Player player2Data) {
        Player player = playerIden1.isLocalPlayer ? player1Data : player2Data;
        Player opponent = playerIden1.isLocalPlayer ? player2Data : player1Data;
        
        // playerNameText.text = playerIden1.isLocalPlayer ? player1Name : player2Name;
        playerNameText.text = player.name;
        // opponentNameText.text = playerIden1.isLocalPlayer ? player2Name : player1Name;
        opponentNameText.text = opponent.name;
        playerHpText.text = $"{player.hp} / {player.maxHp}";
        opponentHpText.text = $"{opponent.hp} / {opponent.maxHp}";
        playerManaText.text = $"{player.mana} / {player.maxMana}";
        opponentManaText.text = $"{opponent.mana} / {opponent.maxMana}";
        playerHandText.text = $"{playerHandList.Count}";
        opponentHandText.text = $"{opponentHandList.Count}";
    }

    [ClientRpc]
    public void Rpc_updateUIInfo(Player player1Data, Player player2Data) {
        updateDeckCount();
        updateCardHandID();
        updatePlayerInfo(player1Data, player2Data);
        currentTurnText.text = $"Turn: {currentTurn}";
        string name = playerIndex == 1? player1Name : player2Name;
        currentPlayerText.text = $"Current player: \n\t{name}";
    }
    #endregion -----------------------------------------

    [ClientRpc]
    public void Rpc_ExitGame() {}

    public override void OnStopClient()
    {
        base.OnStopClient();
        // clear all card objects
        foreach (GameObject cardObj in boardCard.Values)    Destroy(cardObj);
        foreach (GameObject cardObj in playerDeck)          Destroy(cardObj);
        foreach (GameObject cardObj in opponentDeck)        Destroy(cardObj);
        foreach (GameObject cardObj in playerDiscard)       Destroy(cardObj);
        foreach (GameObject cardObj in opponentDiscard)     Destroy(cardObj);
        foreach (GameObject cardObj in playerHandList)      Destroy(cardObj);
        foreach (GameObject cardObj in opponentHandList)    Destroy(cardObj);
    }

    #region pause menu
    public GameObject pauseMenu;
    public GameObject quitConfirmMenu;
    [ClientCallback]
    public void onButton_Pause() {
        // show pause menu
        pauseMenu.SetActive(true);
    }
    [ClientCallback]
    public void onButton_Resume() {
        // show pause menu
        pauseMenu.SetActive(false);
    }
    [ClientCallback]
    public void onButton_Quit()  {
        // show confirmation menu before quit
        quitConfirmMenu.SetActive(true);
    }
    [ClientCallback]
    public void onButton_QuitYes()  {
        // close confirmation menu
        quitConfirmMenu.SetActive(false);
        // to main menu
        SceneManager.LoadScene("MainMenu");
    }
    [ClientCallback]
    public void onButton_QuitNo()  {
        // close confirmation menu
        quitConfirmMenu.SetActive(false);
    }
    #endregion -----------------------------------------
}
