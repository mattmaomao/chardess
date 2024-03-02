using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

public class CanvasController : MonoBehaviour
{
    /// <summary>
    /// Match Controllers listen for this to terminate their match and clean up
    /// </summary>
    public event Action<NetworkConnectionToClient> OnPlayerDisconnected;

    /// <summary>
    /// Cross-reference of client that created the corresponding match in openMatches below
    /// </summary>
    internal static readonly Dictionary<NetworkConnectionToClient, Guid> playerMatches = new Dictionary<NetworkConnectionToClient, Guid>();

    /// <summary>
    /// Open matches that are available for joining
    /// </summary>
    internal static readonly Dictionary<Guid, MatchInfo> openMatches = new Dictionary<Guid, MatchInfo>();

    /// <summary>
    /// Network Connections of all players in a match
    /// </summary>
    internal static readonly Dictionary<Guid, HashSet<NetworkConnectionToClient>> matchConnections = new Dictionary<Guid, HashSet<NetworkConnectionToClient>>();

    /// <summary>
    /// Player informations by Network Connection
    /// </summary>
    internal static readonly Dictionary<NetworkConnection, PlayerInfo> playerInfos = new Dictionary<NetworkConnection, PlayerInfo>();

    /// <summary>
    /// Network Connections that have neither started nor joined a match yet
    /// </summary>
    internal static readonly List<NetworkConnectionToClient> waitingConnections = new List<NetworkConnectionToClient>();

    /// <summary>
    /// GUID of a match the local player has created
    /// </summary>
    internal Guid localPlayerMatch = Guid.Empty;

    /// <summary>
    /// GUID of a match the local player has joined
    /// </summary>
    internal Guid localJoinedMatch = Guid.Empty;

    internal Guid selectedMatch = Guid.Empty;

    // Used in UI for "Player #"
    int playerIndex = 1;

    [Header("GUI References")]
    public GameObject matchList;
    public GameObject matchPrefab;
    public GameObject gameViewPrefab;
    public InputField roomIDInput;
    public InputField playerNameInput;
    public Text roomIDWarning;
    public Text playerNameWarning;
    public Button createButton;
    public Button joinButton;
    public GameObject lobbyView;
    public GameObject roomView;
    public RoomManager roomManager;

    void Start() {
        roomIDInput.onValueChanged.AddListener(input => {
            roomIDWarning.gameObject.SetActive(false);
        });
        playerNameInput.onValueChanged.AddListener(input => {
            playerNameWarning.gameObject.SetActive(false);
        });
    }

    // RuntimeInitializeOnLoadMethod -> fast playmode without domain reload
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetStatics()
    {
        playerMatches.Clear();
        openMatches.Clear();
        matchConnections.Clear();
        playerInfos.Clear();
        waitingConnections.Clear();
    }

    #region UI Functions

    // Called from several places to ensure a clean reset
    //  - MatchNetworkManager.Awake
    //  - OnStartServer
    //  - OnStartClient
    //  - OnClientDisconnect
    //  - ResetCanvas
    internal void InitializeData()
    {
        playerMatches.Clear();
        openMatches.Clear();
        matchConnections.Clear();
        waitingConnections.Clear();
        localPlayerMatch = Guid.Empty;
        localJoinedMatch = Guid.Empty;
    }

    // Called from OnStopServer and OnStopClient when shutting down
    void ResetCanvas()
    {
        InitializeData();
        gameObject.SetActive(false);
        // lobbyView.SetActive(false);
        // roomView.SetActive(false);
        ShowLobbyView();
    }

    #endregion

    #region Button Calls

    [ClientCallback]
    public void SelectMatch(Guid matchId)
    {
        roomIDInput.text = openMatches[matchId].joinCode;
        selectedMatch = matchId;
        MatchInfo infos = openMatches[matchId];
        joinButton.interactable = infos.players < infos.maxPlayers;
    }

    /// <summary>
    /// Assigned in inspector to Create button
    /// </summary>
    [ClientCallback]
    public void RequestCreateMatch()
    {
        if (!validInput()) return;

        NetworkClient.Send(new ServerMatchMessage { 
            serverMatchOperation = ServerMatchOperation.Create, 
            roomName = roomIDInput.text, 
            playerName = playerNameInput.text 
        });
    }

    /// <summary>
    /// Assigned in inspector to Cancel button
    /// </summary>
    [ClientCallback]
    public void RequestCancelMatch()
    {
        if (localPlayerMatch == Guid.Empty) return;

        NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Cancel });
    }

    /// <summary>
    /// Assigned in inspector to Join button
    /// </summary>
    [ClientCallback]
    public void RequestJoinMatch()
    {
        selectedMatch = findGuid(roomIDInput.text);
        if (!validInput()) return;
        if (selectedMatch == Guid.Empty) {
            roomIDWarning.text = "Cannot find room!";
            roomIDWarning.gameObject.SetActive(true);
            return;
        }

        NetworkClient.Send(new ServerMatchMessage { 
            serverMatchOperation = ServerMatchOperation.Join, 
            matchId = selectedMatch, 
            roomName = roomIDInput.text, 
            playerName = playerNameInput.text
        });
    }

    /// <summary>
    /// Assigned in inspector to Leave button
    /// </summary>
    [ClientCallback]
    public void RequestLeaveMatch()
    {
        if (localJoinedMatch == Guid.Empty) return;
        NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Leave, matchId = localJoinedMatch });
    }

    /// <summary>
    /// Assigned in inspector to Ready button
    /// </summary>
    [ClientCallback]
    public void RequestReadyChange()
    {
        if (localPlayerMatch == Guid.Empty && localJoinedMatch == Guid.Empty) return;

        Guid matchId = localPlayerMatch == Guid.Empty ? localJoinedMatch : localPlayerMatch;

        NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Ready, matchId = matchId });
    }

    /// <summary>
    /// Assigned in inspector to Start button
    /// </summary>
    [ClientCallback]
    public void RequestStartMatch()
    {
        if (localPlayerMatch == Guid.Empty) return;

        NetworkClient.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Start });
    }

    /// <summary>
    /// Called from <see cref="MainGameManager.Rpc_ExitGame"/>
    /// </summary>
    [ClientCallback]
    public void OnMatchEnded()
    {
        localPlayerMatch = Guid.Empty;
        localJoinedMatch = Guid.Empty;
        ShowLobbyView();
    }

    // leave lobby and go back to main menu
    [ClientCallback]
    public void LeaveLobby() {
        NetworkManager networkManager = NetworkManager.singleton;
        networkManager.StopClient();
        SceneManager.LoadScene("MainMenu");
    }

    // ensure user name, room id are input
    bool validInput() {
        bool valid = true;
        if (roomIDInput.text.Length <= 0) {
            roomIDWarning.gameObject.SetActive(true);
            valid = false;
        }
        if (playerNameInput.text.Length <= 0) {
            playerNameWarning.gameObject.SetActive(true);
            valid = false;
        }
        return valid;
    }
    #endregion

    #region Server & Client Callbacks

    // Methods in this section are called from MatchNetworkManager's corresponding methods

    [ServerCallback]
    internal void OnStartServer()
    {
        InitializeData();
        NetworkServer.RegisterHandler<ServerMatchMessage>(OnServerMatchMessage);
    }

    [ServerCallback]
    internal void OnServerReady(NetworkConnectionToClient conn)
    {
        waitingConnections.Add(conn);
        playerInfos.Add(conn, new PlayerInfo { playerIndex = this.playerIndex, ready = false });
        playerIndex++;

        SendMatchList();
    }

    [ServerCallback]
    internal IEnumerator OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Invoke OnPlayerDisconnected on all instances of MatchController
        OnPlayerDisconnected?.Invoke(conn);

        if (playerMatches.TryGetValue(conn, out Guid matchId))
        {
            playerMatches.Remove(conn);
            openMatches.Remove(matchId);

            foreach (NetworkConnectionToClient playerConn in matchConnections[matchId])
            {
                PlayerInfo _playerInfo = playerInfos[playerConn];
                _playerInfo.ready = false;
                _playerInfo.matchId = Guid.Empty;
                playerInfos[playerConn] = _playerInfo;
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
            }
        }

        foreach (KeyValuePair<Guid, HashSet<NetworkConnectionToClient>> kvp in matchConnections)
            kvp.Value.Remove(conn);

        PlayerInfo playerInfo = playerInfos[conn];
        if (playerInfo.matchId != Guid.Empty)
        {
            if (openMatches.TryGetValue(playerInfo.matchId, out MatchInfo matchInfo))
            {
                matchInfo.players--;
                openMatches[playerInfo.matchId] = matchInfo;
            }

            HashSet<NetworkConnectionToClient> connections;
            if (matchConnections.TryGetValue(playerInfo.matchId, out connections))
            {
                PlayerInfo[] infos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();

                foreach (NetworkConnectionToClient playerConn in matchConnections[playerInfo.matchId])
                    if (playerConn != conn)
                        playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos });
            }
        }

        SendMatchList();

        yield return null;
    }

    [ServerCallback]
    internal void OnStopServer()
    {
        ResetCanvas();
    }

    [ClientCallback]
    internal void OnClientConnect()
    {
        playerInfos.Add(NetworkClient.connection, new PlayerInfo { playerIndex = this.playerIndex, ready = false });
    }

    [ClientCallback]
    internal void OnStartClient()
    {
        InitializeData();
        ShowLobbyView();
        createButton.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);
        NetworkClient.RegisterHandler<ClientMatchMessage>(OnClientMatchMessage);
    }

    [ClientCallback]
    internal void OnClientDisconnect()
    {
        InitializeData();
    }

    [ClientCallback]
    internal void OnStopClient()
    {
        ResetCanvas();
    }

    #endregion

    #region Server Match Message Handlers

    [ServerCallback]
    void OnServerMatchMessage(NetworkConnectionToClient conn, ServerMatchMessage msg)
    {
        switch (msg.serverMatchOperation)
        {
            case ServerMatchOperation.None:
                {
                    Debug.LogWarning("Missing ServerMatchOperation");
                    break;
                }
            case ServerMatchOperation.Create:
                {
                    OnServerCreateMatch(conn, msg);
                    break;
                }
            case ServerMatchOperation.Cancel:
                {
                    OnServerCancelMatch(conn);
                    break;
                }
            case ServerMatchOperation.Join:
                {
                    OnServerJoinMatch(conn, msg);
                    break;
                }
            case ServerMatchOperation.Leave:
                {
                    OnServerLeaveMatch(conn, msg.matchId);
                    break;
                }
            case ServerMatchOperation.Ready:
                {
                    OnServerPlayerReady(conn, msg.matchId);
                    break;
                }
            case ServerMatchOperation.Start:
                {
                    OnServerStartMatch(conn);
                    break;
                }
        }
    }

    [ServerCallback]
    void OnServerCreateMatch(NetworkConnectionToClient conn, ServerMatchMessage msg)
    {
        if (playerMatches.ContainsKey(conn)) return;

        Guid newMatchId = Guid.NewGuid();
        matchConnections.Add(newMatchId, new HashSet<NetworkConnectionToClient>());
        matchConnections[newMatchId].Add(conn);
        playerMatches.Add(conn, newMatchId);

        // generate random unique code for each new create room
        System.Random random = new();
        string jCode = random.Next(100000, 999999).ToString();
        while (findGuid(jCode) != Guid.Empty)
            jCode = random.Next(100000, 999999).ToString();
        openMatches.Add(newMatchId, new MatchInfo { 
            matchId = newMatchId, 
            joinCode = jCode,
            roomName = msg.roomName,
            hostName = msg.playerName,
            maxPlayers = 2, 
            players = 1 
        });

        PlayerInfo playerInfo = playerInfos[conn];
        playerInfo.playerName = msg.playerName;
        playerInfo.ready = false;
        playerInfo.matchId = newMatchId;
        playerInfos[conn] = playerInfo;

        PlayerInfo[] infos = matchConnections[newMatchId].Select(playerConn => playerInfos[playerConn]).ToArray();
        SendMatchList();

        conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Created, matchId = newMatchId, playerInfos = infos });

        // roomManager.ShowRoomInfo(openMatches[newMatchId]);
    }

    [ServerCallback]
    void OnServerCancelMatch(NetworkConnectionToClient conn)
    {
        if (!playerMatches.ContainsKey(conn)) return;

        conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Cancelled });

        Guid matchId;
        if (playerMatches.TryGetValue(conn, out matchId))
        {
            playerMatches.Remove(conn);
            openMatches.Remove(matchId);

            foreach (NetworkConnectionToClient playerConn in matchConnections[matchId])
            {
                PlayerInfo playerInfo = playerInfos[playerConn];
                playerInfo.ready = false;
                playerInfo.matchId = Guid.Empty;
                playerInfos[playerConn] = playerInfo;
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
            }

            SendMatchList();
        }
    }

    [ServerCallback]
    void OnServerJoinMatch(NetworkConnectionToClient conn, ServerMatchMessage msg)
    {
        Guid matchId = msg.matchId;
        if (!matchConnections.ContainsKey(matchId) || !openMatches.ContainsKey(matchId)) return;

        MatchInfo matchInfo = openMatches[matchId];
        matchInfo.players++;
        openMatches[matchId] = matchInfo;
        matchConnections[matchId].Add(conn);

        PlayerInfo playerInfo = playerInfos[conn];
        playerInfo.playerName = msg.playerName;
        playerInfo.ready = false;
        playerInfo.matchId = matchId;
        playerInfos[conn] = playerInfo;

        PlayerInfo[] infos = matchConnections[matchId].Select(playerConn => playerInfos[playerConn]).ToArray();
        SendMatchList();

        conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Joined, matchId = matchId, playerInfos = infos });

        foreach (NetworkConnectionToClient playerConn in matchConnections[matchId])
            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos });

        // roomManager.ShowRoomInfo(openMatches[matchId]);
    }

    [ServerCallback]
    void OnServerLeaveMatch(NetworkConnectionToClient conn, Guid matchId)
    {
        MatchInfo matchInfo = openMatches[matchId];
        matchInfo.players--;
        openMatches[matchId] = matchInfo;

        PlayerInfo playerInfo = playerInfos[conn];
        playerInfo.ready = false;
        playerInfo.matchId = Guid.Empty;
        playerInfos[conn] = playerInfo;

        foreach (KeyValuePair<Guid, HashSet<NetworkConnectionToClient>> kvp in matchConnections)
            kvp.Value.Remove(conn);

        HashSet<NetworkConnectionToClient> connections = matchConnections[matchId];
        PlayerInfo[] infos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();

        foreach (NetworkConnectionToClient playerConn in matchConnections[matchId])
            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos });

        SendMatchList();

        conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
    }

    [ServerCallback]
    void OnServerPlayerReady(NetworkConnectionToClient conn, Guid matchId)
    {
        PlayerInfo playerInfo = playerInfos[conn];
        playerInfo.ready = !playerInfo.ready;
        playerInfos[conn] = playerInfo;

        HashSet<NetworkConnectionToClient> connections = matchConnections[matchId];
        PlayerInfo[] infos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();

        foreach (NetworkConnectionToClient playerConn in matchConnections[matchId])
            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos });
    }

    [ServerCallback]
    void OnServerStartMatch(NetworkConnectionToClient conn)
    {
        if (!playerMatches.ContainsKey(conn)) return;

        Guid matchId;
        if (playerMatches.TryGetValue(conn, out matchId)) {
            GameObject gamePrefab = Instantiate(gameViewPrefab);
            gamePrefab.GetComponent<NetworkMatch>().matchId = matchId;
            NetworkServer.Spawn(gamePrefab);

            MainGameManager mainGameManager = gamePrefab.GetComponent<MainGameManager>();
            foreach (NetworkConnectionToClient playerConn in matchConnections[matchId])
            {
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Started });

                GameObject player = Instantiate(NetworkManager.singleton.playerPrefab);
                player.GetComponent<NetworkMatch>().matchId = matchId;
                NetworkServer.AddPlayerForConnection(playerConn, player);

                if (mainGameManager.playerIden1 == null) {
                    mainGameManager.playerIden1 = playerConn.identity;
                    mainGameManager.player1Name = playerInfos[playerConn].playerName;
                }
                else {
                    mainGameManager.playerIden2 = playerConn.identity;
                    mainGameManager.player2Name = playerInfos[playerConn].playerName;
                }

                /* Reset ready state for after the match. */
                PlayerInfo playerInfo = playerInfos[playerConn];
                playerInfo.ready = false;
                playerInfos[playerConn] = playerInfo;
            }
            mainGameManager.resetGame();

            playerMatches.Remove(conn);
            openMatches.Remove(matchId);
            matchConnections.Remove(matchId);
            SendMatchList();

            // OnPlayerDisconnected += mainGameManager.OnPlayerDisconnected;
        }
    }

    /// <summary>
    /// Sends updated match list to all waiting connections or just one if specified
    /// </summary>
    /// <param name="conn"></param>
    [ServerCallback]
    internal void SendMatchList(NetworkConnectionToClient conn = null)
    {
        if (conn != null)
            conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.List, matchInfos = openMatches.Values.ToArray() });
        else
            foreach (NetworkConnectionToClient waiter in waitingConnections)
                waiter.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.List, matchInfos = openMatches.Values.ToArray() });
    }

    #endregion

    #region Client Match Message Handler

    [ClientCallback]
    void OnClientMatchMessage(ClientMatchMessage msg)
    {
        switch (msg.clientMatchOperation)
        {
            case ClientMatchOperation.None:
                {
                    Debug.LogWarning("Missing ClientMatchOperation");
                    break;
                }
            case ClientMatchOperation.List:
                {
                    openMatches.Clear();
                    foreach (MatchInfo matchInfo in msg.matchInfos)
                        openMatches.Add(matchInfo.matchId, matchInfo);

                    RefreshMatchList();
                    break;
                }
            case ClientMatchOperation.Created:
                {
                    localPlayerMatch = msg.matchId;
                    ShowRoomView();
                    roomManager.RefreshRoomPlayers(msg.playerInfos);
                    roomManager.ShowRoomInfo(openMatches[msg.matchId]);
                    roomManager.SetOwner(true);
                    break;
                }
            case ClientMatchOperation.Cancelled:
                {
                    localPlayerMatch = Guid.Empty;
                    ShowLobbyView();
                    break;
                }
            case ClientMatchOperation.Joined:
                {
                    localJoinedMatch = msg.matchId;
                    ShowRoomView();
                    roomManager.RefreshRoomPlayers(msg.playerInfos);
                    roomManager.ShowRoomInfo(openMatches[msg.matchId]);
                    roomManager.SetOwner(false);
                    break;
                }
            case ClientMatchOperation.Departed:
                {
                    localJoinedMatch = Guid.Empty;
                    ShowLobbyView();
                    break;
                }
            case ClientMatchOperation.UpdateRoom:
                {
                    roomManager.RefreshRoomPlayers(msg.playerInfos);
                    break;
                }
            case ClientMatchOperation.Started:
                {
                    lobbyView.SetActive(false);
                    roomView.SetActive(false);
                    break;
                }
        }
    }

    [ClientCallback]
    void ShowLobbyView()
    {
        roomIDInput.text = "";
        selectedMatch = Guid.Empty;
        lobbyView.SetActive(true);
        roomView.SetActive(false);
    }

    [ClientCallback]
    void ShowRoomView()
    {
        lobbyView.SetActive(false);
        roomView.SetActive(true);
    }

    [ClientCallback]
    void RefreshMatchList()
    {
        foreach (Transform child in matchList.transform)
            Destroy(child.gameObject);

        // joinButton.interactable = false;

        foreach (MatchInfo matchInfo in openMatches.Values)
        {
            GameObject newMatch = Instantiate(matchPrefab, Vector3.zero, Quaternion.identity);
            newMatch.transform.SetParent(matchList.transform, false);
            newMatch.GetComponent<RoomListed>().SetMatchInfo(matchInfo);
        }
    }

    #endregion

    // search open match by join code
    Guid findGuid(string jCode) {
        foreach (var i in openMatches.Keys) {
            if (openMatches[i].joinCode == jCode) {
                return i;
            }
        }
        return Guid.Empty;
    }

    // debug
    public void genRandomName() {
        System.Random random = new();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789";
        int length = 4;
        string randRID = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        string randPName = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        roomIDInput.text = randRID;
        playerNameInput.text = randPName;
    }
}
