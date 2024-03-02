using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MoveDirection {None, Horizontal, Vertical, Forward, Ortho, Cross, AllDir};
public enum CardType {Unit, Spell, Trap};

[CreateAssetMenu(menuName = "Cards")]
public class CardScriptableObject : ScriptableObject
{
    // card info
    public int cardID;
    public string cardName;
    public CardType cardType;

    // card stat
    public int cost;
    public int maxMove = 1;
    public MoveDirection manual_moveDir;
    public int manual_moveAmount;
    public MoveDirection auto_moveDir;
    public int auto_moveAmount;

    // card display
    public Sprite cardIcon;
    public Sprite boardSprite;
    public string cardDescription;
}
