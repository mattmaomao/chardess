using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Player
{
    // playe stat
    public string name;
    public int hp;
    public int mana;
    public int maxHp;
    public int maxMana;
    public int maxManaCap;
    public int cardHoldingNum;
    // decks
    public List<GameObject> deck;
    public List<GameObject> discard;
    public List<GameObject> hand;

    [ServerCallback]
    public void init(string _name)
    {
        name = _name;
        maxHp = 10;
        maxMana = 1;
        hp = maxHp;
        mana = maxMana;
        maxManaCap = 5;

        deck = new();
        discard = new();
        hand = new();
    }

    [ServerCallback]
    public void incMaxMana() {
        if (maxMana >= maxManaCap) return;

        maxMana += 1;
        mana = maxMana;
    }
}
