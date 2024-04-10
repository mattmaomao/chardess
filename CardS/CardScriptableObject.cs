using UnityEngine;

public enum Direction {None, Hori, Verti, Forward, Ortho, Cross, AllDir};
public enum CardType {None, Unit, Spell, Trap};
public enum SpellType {None, Damage, Heal}; //spawn, stun, move
public enum SpellFrom {None, All, Targeted, King, Unit, NonUnit, SpecUnit};

[CreateAssetMenu(menuName = "Cards")]
public class CardScriptableObject : ScriptableObject
{
    // card info
    public int cardID;
    public string cardName;
    public CardType cardType;

    // card stat
    public int cost = 1;
    public int maxMove = 1;
    public Direction manual_moveDir = Direction.None;
    public int manual_moveAmount = 0;
    public Direction auto_moveDir = Direction.None;
    public int auto_moveAmount = 0;

    // for spell
    public SpellType spellType = SpellType.None;
    public SpellFrom spellFrom = SpellFrom.None;
    public Direction spellDir = Direction.None;
    public int spellRange = 0;

    // card display
    public Sprite cardIcon;
    public Sprite boardSprite;
    public string cardDescription;
}
