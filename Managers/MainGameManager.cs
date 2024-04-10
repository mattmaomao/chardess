using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

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
    // [SyncVar] 
    public bool readyToGoOn = false;

    #region player
    [SyncVar] public Player player1Data, player2Data;
    [SyncVar] public NetworkIdentity playerIden1, playerIden2;
    [SyncVar] public string player1Name, player2Name;
    [SyncVar] [SerializeField] NetworkIdentity startingPlayer, currentPlayer;
    [SyncVar] [SerializeField] int playerIndex = 1;
    #endregion -----------------------------------------
    
    #region board
    public Cell pointingCell;
    Dictionary<int, GameObject> boardCard_Server = new();
    public Dictionary<int, Cell> boardCell = new();
    public Dictionary<int, GameObject> boardCard = new();
    int boardRows, boardCols;
    #endregion -----------------------------------------
    
    #region card
    public Card selectedCard;
    // init fixed deck with index
    // debug
    int[] fixedDeck = {0, 0, 2, 3};    //first element must be 0 (king)
    const int UniqueCardIDStart = 2;
    public Dictionary<int, Card> allCards = new();
    #endregion -----------------------------------------

    // turn
    [SyncVar] int currentTurn = 0;
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
    #region card obj
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

    [Server]
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
                GF_init();
                gameState = GameState.Wait;
                break;
            // draw card
            case GameState.DrawCards:
                GF_drawCard();
                gameState = GameState.PlayCards;
                break;
            // wait for player to play card, move unit
            case GameState.PlayCards:
                break;
            // switch player
            case GameState.TurnEnd:
                GF_turnEnd();
                gameState = GameState.Wait;
                break;
            // terminate game
            case GameState.GameEnd:
                // todo
                Debug.Log("game end");
                gameEnded = true;
                gameState = GameState.Wait;
                break;
            // debug
            case GameState.Debug:
                break;
        }
    }

    #region init game
    void GF_init() {
        resetGame();
        initPlayerData();
        Server_loadCards();
        Rpc_initBoard();
        Server_initKing();
        // init both players' hand
        StartCoroutine(slowInitHand());
    }

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
    [Server]
    public void resetGame() {
        startingPlayer = playerIden1;
        currentPlayer = startingPlayer;
        currentTurn = 1;
        currentTurnText.text = $"Turn: {currentTurn}";
        currentPlayerText.text = $"Current player: \n\t{currentPlayer.name}";
        gameState = GameState.Init;
    }

    // init player data
    [Server]
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

    [Server]
    void Server_initKing() {
        readyToGoOn = false;
        for (int i = 0; i < 2; i++) {
            // init king data
            CardScriptableObject c = cardsScriptObjs[0];
            GameObject cardObj = Instantiate(tempCardObj);
            Card card = cardObj.GetComponent<Card>();
            card.init(c);
            card.uniqueID = i;
            allCards[i] = card;
            card.owner = i == 0 ? playerIden1 : playerIden2;

            // put king at position (edge middle)
            int cellID = i == 0 ? 200 : 204;
            boardCard_Server[cellID] = card.gameObject;
            card.cellOnID = cellID;

            card.onPlayEffect(cellID, true);
        }
        Rpc_initKing();
        readyToGoOn = true;
    }

    [ClientRpc]
    void Rpc_initKing() {
        readyToGoOn = false;
        for (int i = 0; i < 2; i++) {
            // init king data
            CardScriptableObject c = cardsScriptObjs[0];
            GameObject cardObj = Instantiate(tempCardObj);
            Card card = cardObj.GetComponent<Card>();
            card.init(c);
            card.uniqueID = i;
            allCards[i] = card;
            card.owner = i == 0 ? playerIden1 : playerIden2;

            // put king at position (edge middle)
            int cellID = i == 0 ? 200 : 204;
            boardCard[cellID] = card.gameObject;
            card.cellOnID = cellID;
            
            // put card on cell
            Cell cell = boardCell[cellID];
            card.cellOnID = cellID;
            card.gameObject.transform.SetParent(cell.gameObject.transform);
            cell.moveIndicatorSiblingIndex();
            card.onPlayEffect(cellID, false);
        }
        readyToGoOn = true;
    }
    #endregion -----------------------------------------

    #region card manager (init deck, load card, draw card)
    // init fix card deck (set #each card)
    [Server]
    void Server_loadCards() {
        readyToGoOn = false;
        clearAllDeck();
        
        int x = UniqueCardIDStart;
        // instantiate and spawn cards
        // player 1
        for (int i = 0; i < fixedDeck.Length; i++) {
            for (int j = 0; j < fixedDeck[i] ; j++) {
                GameObject cardObj = Instantiate(tempCardObj);
                Card card = cardObj.GetComponent<Card>();
                card.init(cardsScriptObjs[i]);
                card.uniqueID = x;
                x++;
                allCards[i] = card;
                cardObj.SetActive(false);

                card.owner = playerIden1;
                player1Data.deck.Add(cardObj);
            }
        }
        // player 2
        for (int i = 0; i < fixedDeck.Length; i++) {
            for (int j = 0; j < fixedDeck[i] ; j++) {
                GameObject cardObj = Instantiate(tempCardObj);
                Card card = cardObj.GetComponent<Card>();
                card.init(cardsScriptObjs[i]);
                card.uniqueID = x;
                x++;
                allCards[i] = card;
                cardObj.SetActive(false);

                card.owner = playerIden2;
                player2Data.deck.Add(cardObj);
            }
        }
        // update UI
        updateDeckCount();

        Rpc_loadCards(fixedDeck, fixedDeck);

        readyToGoOn = true;
    }

    // init card into deck
    [ClientRpc]
    void Rpc_loadCards(int[] fixDeck1, int[] fixDeck2) {
        readyToGoOn = false;
        int x = UniqueCardIDStart;
        // instantiate and spawn cards
        for (int i = 0; i < fixDeck1.Length; i++) {
            for (int j = 0; j < fixDeck1[i]; j++) {
                GameObject cardObj = Instantiate(tempCardObj);
                Card card = cardObj.GetComponent<Card>();
                card.init(cardsScriptObjs[i]);
                card.uniqueID = x;
                x++;
                allCards[i] = card;
                cardObj.SetActive(false);
                card.owner = playerIden1;

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
                GameObject cardObj = Instantiate(tempCardObj);
                Card card = cardObj.GetComponent<Card>();
                card.init(cardsScriptObjs[i]);
                card.uniqueID = x;
                x++;
                allCards[i] = card;
                cardObj.SetActive(false);
                card.owner = playerIden2;

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

    #region give card //later
    [Server]
    void Server_giveCard(NetworkIdentity playerTo, int cardID) {
        // later
        Rpc_giveCard(playerTo, cardID);
    }
    [ClientRpc]
    void Rpc_giveCard(NetworkIdentity playerTo, int cardID) {
        // later
    }
    #endregion -----------------------------------------

    #region draw card
    void GF_drawCard() {
        if (playerIndex == 1) {
            player1Data.incMaxMana();
            Server_drawCard(currentPlayer, playerIden1);
        }
        else {
            player2Data.incMaxMana();
            Server_drawCard(currentPlayer, playerIden2);
        }
        Rpc_updateUIInfo(player1Data, player2Data);
    }
    // draw ONE random card to player x
    [Server]
    void Server_drawCard(NetworkIdentity playerFrom, NetworkIdentity playerTo) {
        readyToGoOn = false;
        int rand;
        Player playerData = playerFrom == playerIden1? player1Data : player2Data;
        List<GameObject> deckFrom = playerData.deck;
        List<GameObject> deckTo = playerTo == playerIden1 ? player1Data.hand : player2Data.hand;
        // reduce hp by 1 if draw from empty deck
        if (deckFrom.Count <= 0) {
            playerData.hp--;
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
        readyToGoOn = true;
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
        readyToGoOn = false;
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
        readyToGoOn = true;
    }
    #endregion -----------------------------------------

    #region discard card
    public void server_discardCard(int where, int id) {
        switch(where) {
            // in deck
            case 1:
                // later
                break;
            // in hand
            case 2:
                // later
                break;
            // on board
            case 3:
                Card card = boardCard[id].GetComponent<Card>();
                boardCard_Server.Remove(card.cellOnID);
                // discard moving unit
                if (card.owner == playerIden1)
                    player1Data.discard.Add(card.gameObject);
                else 
                    player2Data.discard.Add(card.gameObject);
                // reset card played state
                card.onDiscardEffect();
                break;
            default:
                Debug.LogError("not yet implemented?");
                break;
        }
    }

    public void client_discardCard(int where, int id) {
        switch(where) {
            // in deck
            case 1:
                // later
                break;
            // in hand
            case 2:
                // later
                break;
            // on board
            case 3:
                Card card = boardCard[id].GetComponent<Card>();
                boardCard.Remove(card.cellOnID);
                // discard moving unit
                if (card.owner.isLocalPlayer) {
                    playerDiscard.Add(card.gameObject);
                    card.gameObject.transform.SetParent(discardPObj.transform, false);
                }
                else {
                    opponentDiscard.Add(card.gameObject);
                    card.gameObject.transform.SetParent(discardOObj.transform, false);
                }
                // reset card played state
                card.onDiscardEffect();
                break;
            default:
                Debug.LogError("not yet implemented?");
                break;
        }
    }
    #endregion -----------------------------------------

    // empty all deck
    [Server]
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
    [Client]
    public void showCardPreview(int id) {
        bigCardDisplay.init(cardsScriptObjs[id]);
        bigCardDisplay.gameObject.SetActive(true);
    }
    // hide card preview
    [Client]
    public void hideCardPreview() {
        bigCardDisplay.gameObject.SetActive(false);
    }

    // show small card preview, available move
    [Client]
    public void selectCard(GameObject card) {
        selectedCard = card.GetComponent<Card>();
        // change to init with current card (incase of update/upgrade card info)
        CardScriptableObject cardTemp = cardsScriptObjs[card.GetComponent<Card>().cardID];
        smallCardDisplay.init(cardTemp); // change this function, for dynamic card** later
        smallCardDisplay.gameObject.SetActive(true);
        cancelSelectPanel.SetActive(true);

        showavailableMove(selectedCard);
    }
    // cancel card selection
    [Client]
    public void cancelSelect() {
        pointingCell = null;
        smallCardDisplay.gameObject.SetActive(false);
        cancelSelectPanel.SetActive(false);
        selectedCard = null;
        resetValidCell();
    }

    #region unit control (move unit, play card)
    #region board ui
    // show available move after selecting a card
    [Client]
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
                    foreach (Cell cell in unitMovableCell(card, -1, player))
                        cell.showValidity(true);
                }
                break;
            case CardType.Spell:
                foreach (Cell cell in Spells.getCastRange(card, player))
                    cell.showValidity(true);
                break;

            default:
                Debug.LogError("not yet implemented?");
                break;
        }
    }

    // cal the valid cell for each player to place a unit
    [Client]
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
    [Client]
    public List<Cell> unitMovableCell(Card card, int cellID, int playerIndex) {
        List<Cell> cells = new();
        int id = card.cardType == CardType.Unit ? card.cellOnID : cellID;
        int x = id;

        // get direction, amonut
        Direction direction = card.manual_moveDir;
        int range = card.manual_moveAmount;
        if (card.cardType == CardType.Spell) {
            direction = card.spellDir;
            range = card.spellRange;
        }

        switch (direction) {
            case Direction.None:    break;
            case Direction.Hori:
                x = id - range;
                for (int i = 0; i < 2*range + 1; i++) {
                    if (checkInBoardBound(x) && x != id)
                        cells.Add(boardCell[x]);
                    x++;
                }
                break;
            case Direction.Verti:
                x = id - 100 * range;
                for (int i = 0; i < 2*range + 1; i++) {
                    if (checkInBoardBound(x) && x != id)
                        cells.Add(boardCell[x]);
                    x += 100;
                }
                break;
            case Direction.Forward: // only move right for player 1, revserse for player 2
                x = playerIndex == 1? id+1: id-1;
                for (int i = 0; i < range; i++) {
                    if (checkInBoardBound(x) && x != id)
                        cells.Add(boardCell[x]);
                    x = playerIndex == 1? x+1: x-1;
                }
                break;
            case Direction.Ortho:
                // hori
                x = id - range;
                for (int i = 0; i < 2*range + 1; i++) {
                    if (checkInBoardBound(x) && x != id)
                        cells.Add(boardCell[x]);
                    x++;
                }
                // verti
                x = id - 100 * range;
                for (int i = 0; i < 2*range + 1; i++) {
                    if (checkInBoardBound(x) && x != id)
                        cells.Add(boardCell[x]);
                    x += 100;
                }
                break;
            case Direction.Cross:
                // x -> top left
                x = id - 100 * range - range;
                for (int i = 0; i < 2*range + 1; i++) {
                    if (checkInBoardBound(x) && x != id)
                        cells.Add(boardCell[x]);
                    x += 101;
                }
                // x -> bot left
                x = id + 100 * range - range;
                for (int i = 0; i < 2*range + 1; i++) {
                    if (checkInBoardBound(x) && x != id)
                        cells.Add(boardCell[x]);
                    x -= 99;
                }
                break;
            case Direction.AllDir:
                x = id - 100 * range - range;
                for (int i = 0; i < 2*range + 1; i++) {
                    for (int j = 0; j < 2*range + 1; j++) {
                        if (checkInBoardBound(x) && x != id)
                            cells.Add(boardCell[x]);
                        x++;
                    }
                    x -= 2*range + 1;
                    x += 100;
                }
                break;
        }
        return cells;
    }

    // check if cell id x is within boundary of board
    bool checkInBoardBound(int x) {
        return x / 100 >= 0 && x % 100 >= 0 && x / 100 < boardRows && x % 100 < boardCols;
    }

    // hide all cell placable indicator
    [Client]
    public void resetValidCell() {
        foreach (Cell cell in boardCell.Values)
            cell.showValidity(false);
    }
    #endregion -----------------------------------------

    // run card play effect/ place unit on borad
    [Command(requiresAuthority = false)]
    public void requestPlayCard(int cardHandID, int cellID, bool isP1) {
        readyToGoOn = false;
        Card card = isP1 ? player1Data.hand[cardHandID].GetComponent<Card>() : player2Data.hand[cardHandID].GetComponent<Card>();
        NetworkIdentity calledPlayer = isP1 ? playerIden1 : playerIden2;
        Player playerData = isP1 ? player1Data : player2Data;

        // return if not supposed to play card
        if (currentPlayer != calledPlayer || gameState != GameState.PlayCards) {
            messageManager.addMsg(calledPlayer.connectionToClient, "you should not play card now!");
            cancelSelect();
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
                    boardCard_Server[cellID] = card.gameObject;
                    card.cellOnID = cellID;

                    // play onPlay effect
                    card.onPlayEffect(cellID, true);

                    Rpc_playCard(card.handID, cellID, isP1? playerIden1 : playerIden2);
                    break;
                case CardType.Spell:
                    // add card from discard
                    if (isP1)
                        player1Data.discard.Add(card.gameObject);
                    else
                        player2Data.discard.Add(card.gameObject);

                    // play onPlay effect
                    card.onPlayEffect(cellID, true);

                    Rpc_playCard(card.handID, cellID, isP1? playerIden1 : playerIden2);
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

        cancelSelect();
        Rpc_updateUIInfo(player1Data, player2Data);
        readyToGoOn = true;
    }

    // update client ui after someone played a card
    [ClientRpc]
    void Rpc_playCard(int cardHandID, int cellID, NetworkIdentity player) {
        readyToGoOn = false;
        Card card;
        Cell cell = boardCell[cellID];
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
                boardCard[cellID] = card.gameObject;
                card.cellOnID = cellID;
                
                // put card on cell
                card.gameObject.transform.SetParent(cell.gameObject.transform);
                cell.moveIndicatorSiblingIndex();

                // set card into placed UI (unit/ trap graphics)
                card.onPlayEffect(cellID, false);
                break;
            case CardType.Spell:
                // add card to discard
                if (isMe)
                    playerDiscard.Add(card.gameObject);
                else
                    opponentDiscard.Add(card.gameObject);

                // play onPlay effect
                card.onPlayEffect(cellID, false);

                break;
        }

        cancelSelect();
        readyToGoOn = true;
    }

    [Command (requiresAuthority = false)]
    public void requestMoveUnit(int cardBoardID, int cellID, bool isP1, bool autoMove) {
        readyToGoOn = false;
        Card card = boardCard_Server[cardBoardID].GetComponent<Card>();
        NetworkIdentity calledPlayer = isP1? playerIden1 : playerIden2;

        // return if not supposed to move card
        if (!autoMove && (currentPlayer != calledPlayer || gameState != GameState.PlayCards)) {
            messageManager.addMsg(calledPlayer.connectionToClient, "you should not move a card now!");
            cancelSelect();
            return;
        }

        bool successMoveCard = false;
        // move card on board
        if (card.moved < card.maxMove || autoMove) {
            successMoveCard = true;
            // step on others effect
            if (boardCard_Server.ContainsKey(cellID)) {
                Card cardOnTargetCell = boardCard_Server[cellID].GetComponent<Card>();
                if (cardOnTargetCell.cardType == CardType.Unit) {
                    // cant eat king, reduce hp instead, then destroy self
                    if (cardOnTargetCell.cardID == 0) {
                        // reduce player hp if king get beat by opponent unit
                        // dun if "beat" by own unit, unit still discarded
                        Debug.Log("someone step on king");
                        if (calledPlayer != cardOnTargetCell.owner) {
                            Debug.Log("some king took dmg");
                            Player playerData = isP1 ? player2Data : player1Data;
                            playerData.hp--;
                        }
                        // discard moving unit
                        server_discardCard(3, card.cellOnID);
                    }
                    // eat normal units
                    else {
                        // discard eaten unit
                        server_discardCard(3, cellID);

                        boardCard_Server[cellID] = card.gameObject;
                        boardCard_Server.Remove(cardBoardID);
                        card.cellOnID = cellID;
                    }
                }
                // if (cardOnTargetCell.GetComponent<Card>().cardType == CardType.Trap) 
                // later
            }
            // fall off edge unit from auto move
            else if (autoMove && checkInBoardBound(cellID)) {
                // discard dead unit
                server_discardCard(3, card.cellOnID);
            }
            // just move
            else {
                boardCard_Server[cellID] = card.gameObject;
                boardCard_Server.Remove(cardBoardID);
                card.cellOnID = cellID;
            }
        }
        else if (card.moved >= card.maxMove) {
            successMoveCard = false;
            messageManager.addMsg(calledPlayer.connectionToClient, "you cannot move this unit yet!");
        }

        if (successMoveCard) {
            card.moved++;
            Debug.Log($"server: card cell id: {card.cellOnID}, board: {cardBoardID}");
            Rpc_moveCard(cardBoardID, cellID);
        }

        cancelSelect();
        Rpc_updateUIInfo(player1Data, player2Data);
        readyToGoOn = true;
    }

    // update client ui after someone moved a unit
    [ClientRpc]
    public void Rpc_moveCard(int cardBoardID, int cellID) {
        readyToGoOn = false;
        Card card = boardCard[cardBoardID].GetComponent<Card>();
        Debug.Log($"move card({card.uniqueID}) from: {card.cellOnID}, to: {cellID}");
        Cell cell;

        // move card on board
        if (boardCard.ContainsKey(cellID)) {
            cell = boardCell[cellID];
            // step on others effect
            Card cardOnTargetCell = boardCard[cellID].GetComponent<Card>();
            if (cardOnTargetCell.cardType == CardType.Unit) {
                // cant eat king, reduce hp instead, then destroy self
                if (cardOnTargetCell.cardID == 0) {
                    // discard moving unit
                    Debug.Log($"dmg king, cell id: {card.cellOnID}");
                    client_discardCard(3, card.cellOnID);
                }
                // eat normal units
                else {
                    // discard eaten unit
                    client_discardCard(3, cellID);
                    
                    boardCard[cellID] = card.gameObject;
                    boardCard.Remove(cardBoardID);
                    card.cellOnID = cellID;

                    // put card on cell
                    card.gameObject.transform.SetParent(cell.gameObject.transform, false);
                    cell.moveIndicatorSiblingIndex();
                }
            }
            // if (cardOnTargetCell.GetComponent<Card>().cardType == CardType.Trap) 
            // later
        }
        // fall off edge unit from auto move
        else if (!boardCell.ContainsKey(cellID)) {
            boardCard.Remove(card.cellOnID);
            // discard dead unit
            client_discardCard(3, card.cellOnID);
        }
        // just move
        else {
            cell = boardCell[cellID];

            boardCard[cellID] = card.gameObject;
            boardCard.Remove(cardBoardID);
            card.cellOnID = cellID;

            // put card on cell
            card.gameObject.transform.SetParent(cell.gameObject.transform, false);
            cell.moveIndicatorSiblingIndex();
        }

        card.moved++;
        cancelSelect();
        readyToGoOn = true;
    }
    #endregion -----------------------------------------

    #endregion -----------------------------------------

    #region turn end (handle what happen between player's turn) ---- auto move unit!!
    void GF_turnEnd() {
        StartCoroutine(slowTurnEnd());
    }
    
    // manually end turn by current player
    public void turnEndButton() {
        turnEnd(playerIden1.isLocalPlayer ? playerIden1 : playerIden2);
    }

    [Command (requiresAuthority = false)]
    void turnEnd(NetworkIdentity player) {
        if (player != currentPlayer) {
            messageManager.addMsg(player.connectionToClient, $"this is not your turn!");
            return;
        }
        gameState = GameState.TurnEnd;
    }

    // move all cards from discard to deck
    [Server]
    public void Server_resetDeck(int _playerIndex) {
        readyToGoOn = false;
        Player player = _playerIndex == 1? player1Data : player2Data;
        if (player.deck.Count == 0 && player.discard.Count > 0) {
            foreach (GameObject card in player.discard) {
                player.deck.Add(card);
            }
            player.discard.Clear();
        }

        Rpc_resetDeck(_playerIndex);
        readyToGoOn = true;
    }

    [ClientRpc]
    void Rpc_resetDeck(int _playerIndex) {
        readyToGoOn = false;
        List<GameObject> deck = _playerIndex == 1? playerDeck : opponentDeck;
        List<GameObject> discard = _playerIndex == 1? playerDiscard : opponentDiscard;
        GameObject deckObj = _playerIndex == 1? deckPObj : deckOObj;

        if (deck.Count == 0 && discard.Count > 0) {
            foreach (GameObject card in discard) {
                card.transform.SetParent(deckObj.transform, false);
                deck.Add(card);
            }
            discard.Clear();
        }
        readyToGoOn = true;
    }

    // card auto effect on turn end
    [Server]
    IEnumerator runTurnEndEffect() {
        // increase max mana
        if (playerIndex == 1)
            player1Data.incMaxMana();
        else 
            player2Data.incMaxMana();

        // auto move cards
        List<GameObject> onBoardCards = boardCard_Server.OrderBy(k => k.Key).Select(k => k.Value).ToList();
        if (currentTurn%2 == 0)
            onBoardCards.Reverse();

        foreach (GameObject cardObj in onBoardCards) {
            Card card = cardObj.GetComponent<Card>();
            if (card.played) {
                readyToGoOn = false;
                card.onTurnEndEffect();
                yield return new WaitUntil(() => readyToGoOn);
                readyToGoOn = false;
            }
        }
        Rpc_updateUIInfo(player1Data, player2Data);
    }

    // ready next turn
    [Server]
    void setTurnInfo() {
        // reset in-turn counter
        currentTurn++;

        // set next player
        currentPlayer = currentPlayer == playerIden1 ? playerIden2 : playerIden1;
        playerIndex = playerIndex == 1 ? 2 : 1;
    }

    IEnumerator slowTurnEnd() {
        // reset deck from discard if deck is empty
        Server_resetDeck(playerIndex);
        
        // auto move units
        yield return StartCoroutine(runTurnEndEffect());
        setTurnInfo();

        Rpc_updateUIInfo(player1Data, player2Data);
        nextState = GameState.DrawCards;
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
        
        playerNameText.text = player.name;
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
        // resetCellAnime();
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
        foreach (GameObject cardObj in boardCard_Server.Values)    Destroy(cardObj);
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
    [Client]
    public void onButton_Pause() {
        // show pause menu
        pauseMenu.SetActive(true);
    }
    [Client]
    public void onButton_Resume() {
        // show pause menu
        pauseMenu.SetActive(false);
    }
    [Client]
    public void onButton_Quit()  {
        // show confirmation menu before quit
        quitConfirmMenu.SetActive(true);
    }
    [Client]
    public void onButton_QuitYes()  {
        // close confirmation menu
        quitConfirmMenu.SetActive(false);
        // to main menu
        SceneManager.LoadScene("MainMenu");
    }
    [Client]
    public void onButton_QuitNo()  {
        // close confirmation menu
        quitConfirmMenu.SetActive(false);
    }
    #endregion -----------------------------------------
}
