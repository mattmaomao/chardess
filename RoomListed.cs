using System;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
public class RoomListed : MonoBehaviour
{
    Guid matchId;

    [Header("GUI Elements")]
    public Image image;
    public Text playerCount;
    public Text matchName;
    public Text hostName;

    [Header("Diagnostics - Do Not Modify")]
    public CanvasController canvasController;

    void Start() {
        canvasController = GameObject.Find("Canvas").GetComponent<CanvasController>();
    }

    [ClientCallback]
    public void OnSelect() {
        canvasController.SelectMatch(matchId);
    }

    [ClientCallback]
    public Guid GetMatchId() => matchId;

    [ClientCallback]
    public void SetMatchInfo(MatchInfo infos)
    {
        matchId = infos.matchId;
        playerCount.text = $"{infos.players} / {infos.maxPlayers}";
        matchName.text = infos.roomName;
        hostName.text = infos.hostName;
    }
}
