using UnityEngine;
using UnityEngine.UI;
using Mirror;
public class RoomManager : MonoBehaviour
{
    public GameObject playerList;
    public GameObject playerPrefab;
    public GameObject hostLeaveButton;
    public GameObject clientLeaveButton;
    public Text roomName;
    public Text joinCode;
    public Button startButton;
    public bool owner;

    [ClientCallback]
    public void RefreshRoomPlayers(PlayerInfo[] playerInfos)
    {
        foreach (Transform child in playerList.transform)
            Destroy(child.gameObject);

        startButton.interactable = false;
        bool everyoneReady = true;

        foreach (PlayerInfo playerInfo in playerInfos)
        {
            GameObject newPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            newPlayer.transform.SetParent(playerList.transform, false);
            newPlayer.GetComponent<PlayerListed>().SetPlayerInfo(playerInfo);

            if (!playerInfo.ready)
                everyoneReady = false;
        }

        startButton.interactable = everyoneReady && owner && (playerInfos.Length > 1);
    }

    [ClientCallback]
    public void ShowRoomInfo(MatchInfo matchInfo) {
        roomName.text = "Room Name: " + matchInfo.roomName;
        joinCode.text = "Join Code: " + matchInfo.joinCode;
    }

    [ClientCallback]
    public void SetOwner(bool owner)
    {
        this.owner = owner;
        hostLeaveButton.SetActive(owner);
        clientLeaveButton.SetActive(!owner);
    }
}
