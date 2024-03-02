using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Data.SqlTypes;
public class PlayerListed : MonoBehaviour
{
    public Text playerName;

    [ClientCallback]
    public void SetPlayerInfo(PlayerInfo info)
    {
        string name = info.playerName == "" ? $"Player {info.playerIndex}" : info.playerName;
        playerName.text = name;
        playerName.color = info.ready ? Color.green : Color.red;
    }
}
