using System;
using System.Collections.Generic;
using UnityEngine;

public class Spells {
    static MainGameManager GM = MainGameManager.Instance;
    static Card card;

    public static void castSpell(Card c, SpellType spellType, int targetCellID, bool byServer) {
        card = c;
        switch(spellType) {
            case SpellType.None:
                Debug.LogError("this spell has no type");
                break;

            case SpellType.Damage:
                dealDamage(targetCellID, byServer);
                break;
            case SpellType.Heal:
                heal(byServer);
                break;
            // case SpellType.Spawn:
            //     // later
            //     break;
            // case SpellType.Stun:
            //     // later
            //     break;
            // case SpellType.Move:
            //     // later
            //     break;
            default:
                Debug.LogError("not yet implemented");
                break;
        }
    }

    // return list of cell available for spell casting
    public static List<Cell> getCastRange(Card spellCard, int playerIndex) {
        List<Cell> cells = new();
        // GM.unitMovableCell(spellCard, playerIndex);
        switch (spellCard.spellCaster) {
            case SpellFrom.None:
                Debug.LogError("spell caster is not set");
                break;
            // return whole board (all cells)
            case SpellFrom.All:
                foreach (Cell cell in GM.boardCell.Values)
                    cells.Add(cell);
                break;
            // get all played unit position
            case SpellFrom.Targeted:
                foreach (GameObject gameObject in GM.boardCard.Values) {
                    Card card = gameObject.GetComponent<Card>();
                    cells.Add(GM.boardCell[card.cellOnID]);
                }
                break;
            // ray from current player's king
            case SpellFrom.King:
                int cellID = GM.allCards[playerIndex-1].cellOnID;
                GM.unitMovableCell(spellCard, cellID, playerIndex);
                break;
            // ray from all current playere's units
            case SpellFrom.Unit:
                foreach (GameObject gameObject in GM.boardCard.Values) {
                    Card card = gameObject.GetComponent<Card>();
                    // if card is owned by calling player
                    if ((card.owner == GM.playerIden1 && playerIndex == 1) || (card.owner == GM.playerIden2 && playerIndex == 2))
                        cells.Add(GM.boardCell[card.cellOnID]);
                }
                break;
            // get all cells without unit on it
            case SpellFrom.NonUnit:
                foreach (int k in GM.boardCell.Keys)
                    if (!GM.boardCard.ContainsKey(k))
                        cells.Add(GM.boardCell[k]);
                break;
            // ray from specific type of units
            // case SpellFrom.SpecUnit:
            //     break;
            default:
                Debug.LogError("not yet implemented");
                break;
        }
        return cells;
    }

    static void dealDamage(int targetCellID, bool byServer) {
        Debug.Log("dmg unit");
        if (GM.boardCard.ContainsKey(targetCellID)) {
            Card card = GM.boardCard[targetCellID].GetComponent<Card>();
            // damage king, player hp -1
            if (card.cardID == 0) {
                if (byServer) {
                    if (card.owner == GM.playerIden1)
                        GM.player1Data.hp--;
                    else
                        GM.player2Data.hp--;
                }
            }
            // damage unit, discard unit
            else {
                if (byServer)
                    GM.server_discardCard(3, targetCellID);
                else
                    GM.client_discardCard(3, targetCellID);
            }
        }
        else {
            Debug.LogError("no unit on target cell");
        }
    }

    static void heal(bool byServer) {
        if (!byServer)  return;
        Debug.Log("heal player");
        if (card.owner == GM.playerIden1) {
            GM.player1Data.hp++;
            GM.player1Data.hp %= GM.player1Data.maxHp;
        }
        else {
            GM.player2Data.hp++;
            GM.player2Data.hp %= GM.player2Data.maxHp;
        }
    }
}