// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Mirror;

// public class PlayerManager : NetworkBehaviour
// {
//     CardsManager _cardsManager;

//     void Start()
//     {
//         // base.OnStartClient();
//         _cardsManager = GameObject.Find("Cards Manager").GetComponent<CardsManager>();
//     }

//     [Command]
//     public void Cmd_drawCard(int player) {
//         if (player == 1) {
//             // check if deck empty
//             if (_cardsManager.player1Deck.Count <= 0) {
//                 Debug.Log($"deck{player} is empty");
//                 return;
//             }
//             // draw a random card
//             int rand = Random.Range(0, _cardsManager.player1Deck.Count-1);
//             GameObject card = _cardsManager.player1Deck[rand].gameObject;
//             NetworkServer.Spawn(card, connectionToClient);
//             Rpc_showCard(card, "In-hand");

//             _cardsManager.player1Hand.Add(_cardsManager.player1Deck[rand]);
//             _cardsManager.player1Deck.RemoveAt(rand);
//         }
//         else if (player == 2) {
//             // check if deck empty
//             if (_cardsManager.player2Deck.Count <= 0) {
//                 Debug.Log($"deck{player} is empty");
//                 return;
//             }
//             // draw a random card
//             int rand = Random.Range(0, _cardsManager.player2Deck.Count-1);
//             GameObject card = _cardsManager.player2Deck[rand].gameObject;
//             NetworkServer.Spawn(card, connectionToClient);
//             Rpc_showCard(card, "In-hand");

//             _cardsManager.player2Hand.Add(_cardsManager.player2Deck[rand]);
//             _cardsManager.player2Deck.RemoveAt(rand);
//         }
//         else {
//             Debug.LogError("no such player: " + player);
//         }
//         // _cardsManager.updateCardCount();
//         Rpc_updateUIInfo();
//     }

//     public void playCard(GameObject card) {
//         Cmd_playCard(card);
//     }

//     [Command]
//     public void Cmd_playCard(GameObject card) {
//         Debug.Log("played" + card);
//         Rpc_showCard(card, "played");
//     }

//     [ClientRpc]
//     void Rpc_showCard(GameObject card, string type) {
//         switch (type) {
//             case "In-hand":
//                 // check if player has authority of this card
//                 if (card.GetComponent<NetworkIdentity>().isOwned) {
//                     card.transform.SetParent(_cardsManager.playerHand.transform);
//                     card.SetActive(true);
//                 }
//                 else {
//                     card.transform.SetParent(_cardsManager.opponentHand.transform);
//                     card.SetActive(true);
//                     card.GetComponent<Card>().cardBack.SetActive(true);
//                 }
//                 break;
//             case "played":
//                 CardSelection cardSelection = card.gameObject.GetComponent<CardSelection>();
//                 cardSelection.putCard(card);
//                 break;
//             default:
//                 break;
//         }
//     }

//     [ClientRpc]
//     void Rpc_updateUIInfo() {
//         CardsManager cardsManager = GameObject.Find("Cards Manager").GetComponent<CardsManager>();
//         cardsManager.Rpc_updateCardCount();
//     }
// }
