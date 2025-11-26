using System.Collections.Generic;

//player data
public class PlayerState
{
    public ulong clientId;
    public List<int> deck = new List<int>();
    public List<int> hand = new List<int>();
    public List<int> playedThisTurn = new List<int>();
    public int score = 0;
    public bool hasSubmitted = false;

    public void DrawTopToHand()
    {
        if (deck.Count == 0) return;
        hand.Add(deck[0]);
        deck.RemoveAt(0);
    }

    public bool RemoveFromHand(int cardId)
    {
        return hand.Remove(cardId);
    }
}
