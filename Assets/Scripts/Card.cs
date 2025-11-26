using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private TMP_Text cardName;
    [SerializeField] private TMP_Text abilityValue;
    [SerializeField] private TMP_Text abilityType;
    [SerializeField] private TMP_Text power;
    [SerializeField] private TMP_Text cost;

    private Button cardBtn;
    private Image cardImage;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color normalColor;

    private int id;
    private int cardCost;
    private bool isSelected = false;

    public Action<int, bool> OnSelectionChanged;

    private void Start()
    {
        cardBtn = GetComponent<Button>();
        cardImage = GetComponent<Image>();
        cardBtn.onClick.AddListener(OnCardClick);
        cardImage.color = normalColor;
    }

    public void Set(CardSO so)
    {
        id = so.id;
        cardName.text = so.cardName;
        cost.text = so.cost.ToString();
        power.text = so.power.ToString();
        abilityType.text = so.ability.abilityType.ToString();
        abilityValue.text = so.ability.value.ToString();
        cardCost = so.cost;
    }
    public int GetId()
    {
        return id;
    }
    public int GetCost() 
    {
        return cardCost;
    }
    public bool IsSelected() 
    { 
        return isSelected; 
    }
    private void OnCardClick() 
    {
        isSelected = !isSelected;
        if (isSelected)
        {
            cardImage.color = selectedColor;
        }
        else
        {
            cardImage.color = normalColor;
        }
        OnSelectionChanged?.Invoke(cardCost, isSelected);
    }
}
