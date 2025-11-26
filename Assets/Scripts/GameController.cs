using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefeb;

    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text p1ScoreText;
    [SerializeField] private TMP_Text p1CostText;
    [SerializeField] private TMP_Text p2ScoreText;
    [SerializeField] private TMP_Text p2CostText;
    [SerializeField] private TMP_Text timerCountText;

    [SerializeField] private Transform p1Content;
    [SerializeField] private Transform p2Content;
    [SerializeField] private Transform handContent;

    [SerializeField] private Button playTurn;
    [SerializeField] private Button endTurn;

    private ulong myClientId;
    private ulong opponentClientId;
    private int currentAvailableCost = 0;
    private int currentSpentCost = 0;

    private int myIndex;
    private int myCardsPlayedCountThisTurn = 0;

    // Timer variables
    private float currentTimerValue = 0f;
    private bool isTimerRunning = false;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            myClientId = NetworkManager.Singleton.LocalClientId;
        }

        playTurn.onClick.AddListener(OnPlayTurnClicked);
        endTurn.onClick.AddListener(OnEndTurnClicked);

        SetInteractionButtons(false);
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            currentTimerValue -= Time.deltaTime;
            if (currentTimerValue < 0) currentTimerValue = 0;
            timerCountText.text = Mathf.CeilToInt(currentTimerValue).ToString();
        }
    }

    private void OnEnable()
    {
        GameEventSystem.OnJSONEvent += OnGameEvent;
    }

    private void OnDisable()
    {
        GameEventSystem.OnJSONEvent -= OnGameEvent;
    }

    private void OnGameEvent(JSONEvent ev)
    {
        switch (ev.action)
        {
            case GameManager.GameState.GameStart:
                HandleGameStart(ev);
                break;
            case GameManager.GameState.TurnStart:
                HandleTurnStart(ev);
                break;
            case GameManager.GameState.RevealCards:
                HandleRevealCards(ev);
                break;
            case GameManager.GameState.GameEnd:
                HandleGameEnd(ev);
                break;
            default:
                break;
        }
    }

    private void HandleGameStart(JSONEvent ev)
    {
        myIndex = ev.playerIds.ToList().IndexOf(myClientId.ToString());
        int oppIndex = (myIndex == 0) ? 1 : 0;
        opponentClientId = ulong.Parse(ev.playerIds[oppIndex]);

        p1ScoreText.text = "Score: 0";
        p2ScoreText.text = "Score: 0";
        p2CostText.text = $"Opponent Deck: {ev.opponentDeckCount}";
        turnText.text = "Turn 1/6";

        currentAvailableCost = ev.currentCost;
        currentSpentCost = 0;
        UpdateCostUI();
        PopulateHand(ev.handCards);

        SetInteractionButtons(true);
        playTurn.interactable = false;

        // Start Timer
        currentTimerValue = 30f;
        isTimerRunning = true;
        timerCountText.text = "30";
    }

    private void HandleTurnStart(JSONEvent ev)
    {
        ClearPlayedCards();

        currentSpentCost = 0;
        currentAvailableCost = ev.currentCost;
        myCardsPlayedCountThisTurn = 0;

        turnText.text = $"Turn {ev.turnNumber}/{ev.totalTurns}";
        // Now valid because GameManager sends specific counts
        p2CostText.text = $"Opponent Deck: {(myIndex == 0 ? ev.p1DeckCount : ev.p0DeckCount)}";

        UpdateCostUI();
        PopulateHand(ev.handCards);
        SetInteractionButtons(true);
        playTurn.interactable = false;

        // Reset Timer
        currentTimerValue = 30f;
        isTimerRunning = true;
        timerCountText.text = "30";
    }

    private void HandleRevealCards(JSONEvent ev)
    {
        // Stop timer
        isTimerRunning = false;
        timerCountText.text = "0";

        SetInteractionButtons(false);
        ClearHand();
        ClearPlayedCards();

        p1ScoreText.text = $"Score: {(myIndex == 0 ? ev.p0Score : ev.p1Score)}";
        p2ScoreText.text = $"Score: {(myIndex == 0 ? ev.p1Score : ev.p0Score)}";
        p2CostText.text = $"Opponent Deck: {(myIndex == 0 ? ev.p1DeckCount : ev.p0DeckCount)}";

        List<int> allPlayedCards = ev.cardIds.ToList();
        List<int> myPlayedCards = new List<int>();
        List<int> oppPlayedCards = new List<int>();

        int totalCards = allPlayedCards.Count;
        int oppCardsCount = totalCards - myCardsPlayedCountThisTurn;

        if (myIndex == 0)
        {
            if (myCardsPlayedCountThisTurn > 0)
                myPlayedCards = allPlayedCards.GetRange(0, myCardsPlayedCountThisTurn);

            if (oppCardsCount > 0)
                oppPlayedCards = allPlayedCards.GetRange(myCardsPlayedCountThisTurn, oppCardsCount);
        }
        else
        {
            if (oppCardsCount > 0)
                oppPlayedCards = allPlayedCards.GetRange(0, oppCardsCount);

            if (myCardsPlayedCountThisTurn > 0)
                myPlayedCards = allPlayedCards.GetRange(oppCardsCount, myCardsPlayedCountThisTurn);
        }

        DisplayPlayedCards(myPlayedCards, p1Content);
        DisplayPlayedCards(oppPlayedCards, p2Content);
    }

    private void DisplayPlayedCards(List<int> cardIds, Transform parent)
    {
        foreach (int id in cardIds)
        {
            CardSO so = GameManager.Instance.GetCard(id);
            if (so != null)
            {
                GameObject cardGO = Instantiate(cardPrefeb, parent);
                Card cardComponent = cardGO.GetComponent<Card>();
                cardComponent.Set(so);
                Button btn = cardGO.GetComponent<Button>();
                if (btn) btn.interactable = false;
            }
        }
    }

    private void HandleGameEnd(JSONEvent ev)
    {
        isTimerRunning = false;
        SetInteractionButtons(false);
        ClearHand();
        ClearPlayedCards();

        int myFinalScore = (myIndex == 0) ? ev.p0Score : ev.p1Score;
        int oppFinalScore = (myIndex == 0) ? ev.p1Score : ev.p0Score;

        GameManager.Instance.OpenResultPanelForAllClientRpc();

        if (GameManager.Instance.resultController != null)
        {
            GameManager.Instance.resultController.ShowResults(myFinalScore, oppFinalScore);
        }
    }

    public void OnPlayTurnClicked()
    {
        var selectedCardIds = handContent.GetComponentsInChildren<Card>()
            .Where(c => c.IsSelected())
            .Select(c => c.GetId())
            .ToArray();

        myCardsPlayedCountThisTurn = selectedCardIds.Length;

        var playerEndTurnEvent = new JSONEvent { action = GameManager.GameState.PlayerEndTurn, cardIds = selectedCardIds };
        GameManager.Instance.networkAdapter.SendEventAsClient(playerEndTurnEvent);
        SetInteractionButtons(false);

        // Optional: Provide visual feedback that we are waiting
        turnText.text = "Waiting for opponent...";
    }

    public void OnEndTurnClicked()
    {
        myCardsPlayedCountThisTurn = 0;

        var endTurnEvent = new JSONEvent { action = GameManager.GameState.PlayerEndTurn, cardIds = new int[0] };
        GameManager.Instance.networkAdapter.SendEventAsClient(endTurnEvent);

        SetInteractionButtons(false);
        turnText.text = "Waiting for opponent...";
    }

    private void SetInteractionButtons(bool interactable)
    {
        playTurn.interactable = interactable;
        endTurn.interactable = interactable;
    }

    private void PopulateHand(int[] cardIds)
    {
        ClearHand();

        foreach (int id in cardIds)
        {
            CardSO so = GameManager.Instance.GetCard(id);
            if (so != null)
            {
                GameObject cardGO = Instantiate(cardPrefeb, handContent);
                Card cardComponent = cardGO.GetComponent<Card>();
                cardComponent.Set(so);
                cardComponent.OnSelectionChanged += OnCardSelectionChanged;
            }
        }
    }

    private void ClearHand()
    {
        foreach (Transform child in handContent)
        {
            Card card = child.GetComponent<Card>();
            if (card != null)
            {
                card.OnSelectionChanged -= OnCardSelectionChanged;
            }
            Destroy(child.gameObject);
        }
    }

    private void ClearPlayedCards()
    {
        foreach (Transform child in p1Content) Destroy(child.gameObject);
        foreach (Transform child in p2Content) Destroy(child.gameObject);
    }

    private void OnCardSelectionChanged(int cost, bool isSelected)
    {
        if (isSelected) currentSpentCost += cost;
        else currentSpentCost -= cost;

        UpdateCostUI();
        ValidatePlayButton();
    }

    private void UpdateCostUI()
    {
        p1CostText.text = $"Cost: {currentAvailableCost - currentSpentCost}/{currentAvailableCost}";
    }

    private void ValidatePlayButton()
    {
        var selectedCards = handContent.GetComponentsInChildren<Card>().Where(c => c.IsSelected());
        int totalSelectedCost = selectedCards.Sum(c => c.GetCost());

        if (selectedCards.Any() && totalSelectedCost <= currentAvailableCost)
        {
            playTurn.interactable = true;
        }
        else
        {
            playTurn.interactable = false;
        }
    }
}