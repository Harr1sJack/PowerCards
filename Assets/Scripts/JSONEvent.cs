using System;
using static GameManager;

public class JSONEvent
{
    //game specific
    public GameState action;
    public string[] playerIds;
    public int[] cardIds;
    public int totalTurns;
    public int turnNumber;
    public int p0Score;
    public int p1Score;
    public int p0DeckCount;
    public int p1DeckCount;

    //player specific
    public int[] handCards;
    public int opponentDeckCount;
    public int currentCost;
}