using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card")]
public class CardSO : ScriptableObject
{
    public enum AbilityTypes { None, GainPoints, StealPoints, DoublePower, DrawExtraCard, DiscardOpponentRandomCard, DestroyOpponentCardInPlay };

    public int id;
    public string cardName;
    public int cost;
    public int power;

    [Serializable]
    public class Ability
    {
        public AbilityTypes abilityType;
        public int value;
    }
    public Ability ability;
}