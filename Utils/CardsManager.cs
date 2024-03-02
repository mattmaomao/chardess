// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using Mirror;

// public class CardsManager : NetworkBehaviour
// {
//     #region Singleton
//     public static CardsManager Instance { get; private set; }
//     private void Awake() {
//         if (Instance != null && Instance != this)
//             Destroy(gameObject);
//         else {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//     }
//     #endregion

//     MainGameManager mainGameManager;

//     // in-game objects
//     #region in-game objects
//     [SerializeField] GameObject deck1;
//     [SerializeField] GameObject deck2;
//     [SerializeField] GameObject discard1;
//     [SerializeField] GameObject discard2;
//     public GameObject playerHand;
//     public GameObject opponentHand;
//     #endregion
//     [SerializeField] GameObject tempCardObj;
//     Card selectedCard;

//     // deck lists
//     #region deck lists
//     public List<Card> player1Deck = new();
//     public List<Card> player2Deck = new();
//     public List<Card> player1Discard = new();
//     public List<Card> player2Discard = new();
//     public List<Card> player1Hand = new();
//     public List<Card> player2Hand = new();
//     #endregion

//     // UI display
//     #region UI display
//     [SerializeField] Card bigCardDisplay;
//     [SerializeField] Card smallCardDisplay;
//     [SerializeField] GameObject cancelSelectPanel;

//     public Text player1DeckText;
//     public Text player2DeckText;
//     public Text player1DiscardText;
//     public Text player2DiscardText;
//     #endregion

//     // moving direction icon
//     public Sprite[] sprites;
//     public CardScriptableObject[] cardsScriptObjs;

//     [SerializeField] List<CardScriptableObject> fixedDeck = new();

//     public override void OnStartClient() {
//         base.OnStartClient();

//         mainGameManager = MainGameManager.Instance;

//         playerHand = GameObject.Find("player hand");
//         opponentHand = GameObject.Find("opponent hand");
//         player1DeckText = GameObject.Find("pDeckT").GetComponent<Text>();
//         player1DiscardText = GameObject.Find("pDisT").GetComponent<Text>();
//         player2DeckText = GameObject.Find("oDeckT").GetComponent<Text>();
//         player2DiscardText = GameObject.Find("oDisT").GetComponent<Text>();
//     }
    
//     [ServerCallback]
//     public void initCardDeck() {
//         clearAllDeck();
//         // init fixed deck with index
//         int[] temp = {0, 5, 3};
//         for (int i = 0; i < temp.Length; i++)
//             for (int j = 0; j < temp[i]; j++)
//                 fixedDeck.Add(cardsScriptObjs[i]);

//         loadCards(fixedDeck, fixedDeck);
//     }

//     // init card into deck
//     [ServerCallback]
//     public void loadCards(List<CardScriptableObject> cards1, List<CardScriptableObject> cards2) {
//         foreach (CardScriptableObject c in cards1) {
//             Card card = Instantiate(tempCardObj, deck1.transform).GetComponent<Card>();
//             card.init(c);
//             card.gameObject.SetActive(false);
//             player1Deck.Add(card);

//             NetworkServer.Spawn(card.gameObject);
//         }
//         foreach (CardScriptableObject c in cards2) {
//             Card card = Instantiate(tempCardObj, deck2.transform).GetComponent<Card>();
//             card.init(c);
//             card.gameObject.SetActive(false);
//             player2Deck.Add(card);

//             NetworkServer.Spawn(card.gameObject);
//         }
//         Rpc_updateCardCount();
//     }

//     // draw ONE random card to player x
//     [ServerCallback]
//     public void drawCards(int deckNum) {
//         GameObject cardObj = default;
//         if (deckNum == 1) {
//             // handle empty deck
//             // todo
//             if (player1Deck.Count <= 0) {
//                 Debug.Log($"deck{deckNum} is empty");
//                 return;
//             }

//             int rand = Random.Range(0, player1Deck.Count-1);
//             player1Hand.Add(player1Deck[rand]);
//             player1Deck.RemoveAt(rand);
//             cardObj = player1Hand[^1].gameObject;
//         }
//         else if (deckNum == 2) {
//             // handle empty deck
//             // todo
//             if (player2Deck.Count <= 0) {
//                 Debug.Log($"deck{deckNum} is empty");
//                 return;
//             }

//             int rand = Random.Range(0, player2Deck.Count-1);
//             player2Hand.Add(player2Deck[rand]);
//             player2Deck.RemoveAt(rand);
//             cardObj = player2Hand[^1].gameObject;
//         }

//         Rpc_showDrewCard(cardObj);
//     }

//     // show card after draw
//     [ClientRpc]
//     void Rpc_showDrewCard(GameObject cardObj) {
//         cardObj.SetActive(true);
//         if (isLocalPlayer) {
//             cardObj.transform.SetParent(playerHand.transform);
//             cardObj.GetComponent<Card>().faceUp(true);
//         }
//         else {
//             cardObj.transform.SetParent(opponentHand.transform);
//             cardObj.GetComponent<Card>().faceUp(false);
//         }
//         Rpc_updateCardCount();
//     }

//     [ServerCallback]
//     void clearAllDeck() {
//         player1Deck = new();
//         player2Deck = new();
//         player1Discard = new();
//         player2Discard = new();
//         player1Hand = new();
//         player2Hand = new();

//         fixedDeck = new();
//     }

//     // show card preview
//     [ClientCallback]
//     public void showCardPreview(int id) {
//         bigCardDisplay.init(Resources.Load<CardScriptableObject>("Cards/card_" + id));
//         bigCardDisplay.gameObject.SetActive(true);
//     }

//     // hide card preview
//     [ClientCallback]
//     public void hideCardPreview() {
//         bigCardDisplay.gameObject.SetActive(false);
//     }

//     // show small card preview, avaliable move
//     [ClientCallback]
//     public void selectCard(int id) {
//         smallCardDisplay.init(Resources.Load<CardScriptableObject>("Cards/card_" + id));
//         smallCardDisplay.gameObject.SetActive(true);
//         cancelSelectPanel.SetActive(true);
//         selectedCard = smallCardDisplay;

//         showAvaliableMove(selectedCard);
//     }

//     // cancel card selection
//     [ClientCallback]
//     public void cancelSelect() {
//         smallCardDisplay.gameObject.SetActive(false);
//         cancelSelectPanel.SetActive(false);
//     }

//     // show avaliable move after selecting a card
//     [ClientCallback]
//     public void showAvaliableMove(Card card) {
//         // todo
//         Debug.Log("show avaliable moves");
//     }

//     // run card play effect/ place chess on borad
//     [Command(requiresAuthority = false)]
//     public void playCard(NetworkConnectionToClient sender = null) {
//         if (sender.identity != mainGameManager.currentPlayer || mainGameManager.gameState != GameState.PlayCards) return;

//         // todo
//     }

//     // update card count
//     [ClientRpc]
//     public void Rpc_updateCardCount() {
//         player1DeckText.text = $"{player1Deck.Count}";
//         player2DeckText.text = $"{player2Deck.Count}";
//         player1DiscardText.text = $"{player1Discard.Count}";
//         player2DiscardText.text = $"{player2Discard.Count}";
//     }
// }
