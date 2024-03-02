// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;
// using Mirror;

// public class LobbyManager : NetworkBehaviour
// {
//     // scenes
//     [SerializeField] GameObject lobby;
//     [SerializeField] GameObject room;

//     // room list scroll view
//     [SerializeField] GameObject lobby_scrollContent;
//     [SerializeField] GameObject roomObj;
//     public List<RoomListed> roomList = new();

//     string roomID;

//     // UI
//     public Text roomNameHolder;
//     public Text playerNameHolder;
//     public InputField roomNameInput;
//     public InputField playerNameInput;

//     Guid selectedMatch = Guid.Empty;
//     static readonly Dictionary<Guid, MatchInfo> openMatches = new();
//     Guid localPlayerMatch = Guid.Empty;
//     Guid localJoinedMatch = Guid.Empty;


//     [Header("GUI References")]
//     public GameObject matchList;
//     public GameObject matchPrefab;
//     public Button createButton;
//     public Button joinButton;
//     public GameObject lobbyView;
//     public GameObject roomView;


//     void Start() {
//         lobby.SetActive(true);
//         room.SetActive(false);

//         roomNameInput.onValueChanged.AddListener(input => {
//             roomNameHolder.text = "Enter room name.../n(to create/ join room)";
//             roomNameHolder.color = Color.black;
//         });
//         playerNameInput.onValueChanged.AddListener(input => {
//             playerNameHolder.text = "Enter player name.../n(name to display)";
//             playerNameHolder.color = Color.black;
//         });

//         // debug
//         roomNameInput.text = "room 1";
//         playerNameInput.text = "player 1";
//     }

//     public void createRoom() {
//         if (!validInput()) return;
//         roomID = roomNameInput.text;
//         RequestCreateMatch();
//     }

//     public void joinRoom() {
//         if (!validInput()) return;
//         roomID = roomNameInput.text;
//         RequestJoinMatch();
//     }

//     #region Button Calls

//     [ClientCallback]
//     public void SelectMatch(Guid matchId)
//     {
//         if (matchId == Guid.Empty)
//         {
//             selectedMatch = Guid.Empty;
//             joinButton.interactable = false;
//         }
//         else
//         {
//             if (!openMatches.ContainsKey(matchId))
//             {
//                 joinButton.interactable = false;
//                 return;
//             }

//             selectedMatch = matchId;
//             MatchInfo infos = openMatches[matchId];
//             joinButton.interactable = infos.players < infos.maxPlayers;
//         }
//     }

//     /// <summary>
//     /// Assigned in inspector to Create button
//     /// </summary>
//     [ClientCallback]
//     public void RequestCreateMatch()
//     {
//         NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Create });
//     }

//     /// <summary>
//     /// Assigned in inspector to Cancel button
//     /// </summary>
//     [ClientCallback]
//     public void RequestCancelMatch()
//     {
//         if (localPlayerMatch == Guid.Empty) return;

//         NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Cancel });
//     }

//     /// <summary>
//     /// Assigned in inspector to Join button
//     /// </summary>
//     [ClientCallback]
//     public void RequestJoinMatch()
//     {
//         if (selectedMatch == Guid.Empty) return;

//         NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Join, matchId = selectedMatch });
//     }

//     /// <summary>
//     /// Assigned in inspector to Leave button
//     /// </summary>
//     [ClientCallback]
//     public void RequestLeaveMatch()
//     {
//         if (localJoinedMatch == Guid.Empty) return;

//         NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Leave, matchId = localJoinedMatch });
//     }

//     /// <summary>
//     /// Assigned in inspector to Ready button
//     /// </summary>
//     [ClientCallback]
//     public void RequestReadyChange()
//     {
//         if (localPlayerMatch == Guid.Empty && localJoinedMatch == Guid.Empty) return;

//         Guid matchId = localPlayerMatch == Guid.Empty ? localJoinedMatch : localPlayerMatch;

//         NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Ready, matchId = matchId });
//     }

//     /// <summary>
//     /// Assigned in inspector to Start button
//     /// </summary>
//     [ClientCallback]
//     public void RequestStartMatch()
//     {
//         if (localPlayerMatch == Guid.Empty) return;

//         NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Start });
//     }

//     /// <summary>
//     /// Called from <see cref="MatchController.RpcExitGame"/>
//     /// </summary>
//     [ClientCallback]
//     public void OnMatchEnded()
//     {
//         localPlayerMatch = Guid.Empty;
//         localJoinedMatch = Guid.Empty;
//         ShowLobbyView();
//     }

//     #endregion

//     [ClientCallback]
//     void ShowLobbyView()
//     {
//         lobbyView.SetActive(true);
//         roomView.SetActive(false);

//         // foreach (Transform child in matchList.transform)
//         //     if (child.gameObject.GetComponent<MatchGUI>().GetMatchId() == selectedMatch)
//         //     {
//         //         Toggle toggle = child.gameObject.GetComponent<Toggle>();
//         //         toggle.isOn = true;
//         //     }
//     }

//     public void onButton_back() {
//         SceneManager.LoadScene("MainMenu");
//     }

//     // auto input room name when select room from list
//     public void selectRoom(Text roomName) {
//         roomNameInput.text = roomName.text;
//     }
    
//     // check if input of room, player name are valid (non-empty)
//     bool validInput() {
//         bool valid = true;
//         if (roomNameInput.text.Length <= 0) {
//             roomNameHolder.text = "Please input room name";
//             roomNameHolder.color = Color.red;
//             valid = false;
//         }
//         if (playerNameInput.text.Length <= 0) {
//             playerNameHolder.text = "Please input player name";
//             playerNameHolder.color = Color.red;
//             valid = false;
//         }
//         return valid;
//     }
// }
