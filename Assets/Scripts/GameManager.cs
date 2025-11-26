using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public enum PanelType { Menu, Game, Result };
    public enum GameState { GameStart, TurnStart, PlayerEndTurn, RevealCards, GameEnd };

    public int maxPlayerCount = 2;
    private int maxTurn = 6;
    private int currentTurn = 1;
    private int initialHand = 3;
    private float timer = 30f;

    private Dictionary<int, CardSO> cards = new Dictionary<int, CardSO>();

    private Dictionary<ulong, PlayerState> playerStates = new Dictionary<ulong, PlayerState>();
    private List<ulong> playerIds = new List<ulong>();


    [SerializeField] private MenuController menuController;
    [SerializeField] private GameController gameController;
    [SerializeField] public ResultController resultController;

    [SerializeField] public NetworkAdapter networkAdapter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        LoadCardData();
        EnablePanel(PanelType.Menu);
    }

    void OnEnable()
    {
        GameEventSystem.OnJSONNetworkEvent += OnJSONNetworkEvent;
    }
    void OnDisable()
    {
        GameEventSystem.OnJSONNetworkEvent -= OnJSONNetworkEvent;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            playerIds = NetworkManager.Singleton.ConnectedClients.Keys.ToList();
        }
    }

    public void EnablePanel(PanelType type)
    {
        switch (type)
        {
            case PanelType.Menu:
                menuController.gameObject.SetActive(true);
                gameController.gameObject.SetActive(false);
                resultController.gameObject.SetActive(false);
                break;
            case PanelType.Game:
                menuController.gameObject.SetActive(false);
                gameController.gameObject.SetActive(true);
                resultController.gameObject.SetActive(false);
                break;
            case PanelType.Result:
                menuController.gameObject.SetActive(false);
                gameController.gameObject.SetActive(false);
                resultController.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }

    //Card utils
    private void LoadCardData()
    {
        CardSO[] cardsData = Resources.LoadAll<CardSO>("Cards");
        foreach (CardSO c in cardsData)
        {
            cards.Add(c.id, c);
        }
    }

    public CardSO GetCard(int id)
    {
        return cards.ContainsKey(id) ? cards[id] : null;
    }
    List<int> Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
        return list;
    }

    //serverside implementation
    public void OnAllPlayersConnected()
    {
        if (!IsServer) return;
        playerIds = NetworkManager.Singleton.ConnectedClients.Keys.ToList();

        StartMatchServerSide();
        OpenGamePanelForAllClientRpc();
        StartTurnServerSide(1);
    }
    void StartMatchServerSide()
    {
        List<int> allCardIds = cards.Keys.ToList<int>();

        // initializing the playerstate for each player
        foreach (ulong clientId in playerIds)
        {
            PlayerState ps = new PlayerState { clientId = clientId };
            var ids = new List<int>(allCardIds);
            ps.deck = Shuffle(ids);

            // pulling 3 cards from the deck
            for (int i = 0; i < initialHand; i++)
            {
                ps.DrawTopToHand();
            }
            playerStates.Add(clientId, ps);
        }

        if (playerIds.Count < 2) return;
        ulong p0Id = playerIds[0];
        ulong p1Id = playerIds[1];

        // sending targeted event to each client

        JSONEvent p0Event = new JSONEvent
        {
            action = GameState.GameStart,
            playerIds = new string[] { p0Id.ToString(), p1Id.ToString() },
            handCards = playerStates[p0Id].hand.ToArray(),
            opponentDeckCount = playerStates[p1Id].deck.Count,
            currentCost = 1,
            p0DeckCount = playerStates[p0Id].deck.Count,
            p1DeckCount = playerStates[p1Id].deck.Count
        };
        networkAdapter.SendEventToClient(p0Event, p0Id);

        JSONEvent p1Event = new JSONEvent
        {
            action = GameState.GameStart,
            playerIds = new string[] { p0Id.ToString(), p1Id.ToString() },
            handCards = playerStates[p1Id].hand.ToArray(),
            opponentDeckCount = playerStates[p0Id].deck.Count,
            currentCost = 1,
            p0DeckCount = playerStates[p0Id].deck.Count,
            p1DeckCount = playerStates[p1Id].deck.Count
        };
        networkAdapter.SendEventToClient(p1Event, p1Id);
    }

    void StartTurnServerSide(int turn)
    {
        if (!IsServer) return;
        currentTurn = turn;
        StopAllCoroutines();

        ulong p0Id = playerIds[0];
        ulong p1Id = playerIds[1];

        // draw for each player
        foreach (var p in playerStates.Values)
            p.DrawTopToHand();
        // reset submitted flags
        foreach (var p in playerStates.Values)
        {
            p.playedThisTurn.Clear();
            p.hasSubmitted = false;
        }

        // P0 event
        JSONEvent p0TurnEvent = new JSONEvent
        {
            action = GameState.TurnStart,
            turnNumber = currentTurn,
            totalTurns = maxTurn,
            handCards = playerStates[p0Id].hand.ToArray(),
            currentCost = currentTurn,
            opponentDeckCount = playerStates[p1Id].deck.Count,
            p0DeckCount = playerStates[p0Id].deck.Count,
            p1DeckCount = playerStates[p1Id].deck.Count
        };
        networkAdapter.SendEventToClient(p0TurnEvent, p0Id);

        // P1 event
        JSONEvent p1TurnEvent = new JSONEvent
        {
            action = GameState.TurnStart,
            turnNumber = currentTurn,
            totalTurns = maxTurn,
            handCards = playerStates[p1Id].hand.ToArray(),
            currentCost = currentTurn,
            opponentDeckCount = playerStates[p0Id].deck.Count,
            p0DeckCount = playerStates[p0Id].deck.Count,
            p1DeckCount = playerStates[p1Id].deck.Count
        };
        networkAdapter.SendEventToClient(p1TurnEvent, p1Id);

        // start turn timeout coroutine
        StartCoroutine(TurnTimeoutCoroutine(turn));
    }
    private IEnumerator TurnTimeoutCoroutine(int turn)
    {
        float t = 0f;
        while (t < timer)
        {
            t += Time.deltaTime;
            yield return null;
        }
        // if any player hasn't submitted, treat them as submitted with empty set
        foreach (var p in playerStates.Values) if (!p.hasSubmitted) p.hasSubmitted = true;
        ResolveTurnServerSide();
    }

    [ClientRpc]
    void OpenGamePanelForAllClientRpc()
    {
        EnablePanel(PanelType.Game);
    }

    [ClientRpc]
    public void OpenResultPanelForAllClientRpc()
    {
        EnablePanel(PanelType.Result);
    }

    private void OnJSONNetworkEvent(JSONEventFromNetwork netEvt)
    {
        var ev = JsonUtility.FromJson<JSONEvent>(netEvt.json);
        if (ev.action == GameState.PlayerEndTurn)
        {
            HandleClientEndTurn(netEvt.senderClientId, ev.cardIds);
        }
    }

    private void HandleClientEndTurn(ulong senderClientId, int[] cardIds)
    {
        if (!IsServer) return;
        if (!playerStates.TryGetValue(senderClientId, out PlayerState ps)) return;

        int totalCost = 0;
        foreach (var id in cardIds)
        {
            if (!ps.hand.Contains(id)) return;
            CardSO card = GetCard(id);
            if (card == null) return;
            totalCost += card.cost;
        }

        if (totalCost > currentTurn) return;

        ps.playedThisTurn = new List<int>(cardIds);
        ps.hasSubmitted = true;

        if (playerStates.Values.All(p => p.hasSubmitted))
        {
            ResolveTurnServerSide();
        }
    }

    private void ExecuteAbilities(PlayerState player, PlayerState opponent, List<int> playedCards)
    {
        foreach (var id in playedCards)
        {
            CardSO so = GetCard(id);
            if (so == null || so.ability.abilityType == CardSO.AbilityTypes.None) continue;

            switch (so.ability.abilityType)
            {
                case CardSO.AbilityTypes.GainPoints:
                    player.score += so.ability.value;
                    break;
                case CardSO.AbilityTypes.StealPoints:
                    int stealAmount = Mathf.Min(so.ability.value, opponent.score);
                    player.score += stealAmount;
                    opponent.score -= stealAmount;
                    break;
                case CardSO.AbilityTypes.DrawExtraCard:
                    for (int i = 0; i < so.ability.value; i++)
                    {
                        player.DrawTopToHand();
                    }
                    break;
                case CardSO.AbilityTypes.DoublePower:
                    player.score *= 2;
                    break;
                default:
                    break;
            }
        }
    }

    void ResolveTurnServerSide()
    {
        if (playerIds.Count < 2) return;

        StopAllCoroutines(); // Stop the timeout timer now that we are resolving

        ulong p0Id = playerIds[0];
        ulong p1Id = playerIds[1];

        var p0 = playerStates[p0Id];
        var p1 = playerStates[p1Id];

        ExecuteAbilities(p0, p1, p0.playedThisTurn);
        ExecuteAbilities(p1, p0, p1.playedThisTurn);

        var result = RuleEngine.Resolve(p0.playedThisTurn, p1.playedThisTurn, GetCard);
        p0.score += result.p0Delta;
        p1.score += result.p1Delta;

        foreach (var id in p0.playedThisTurn) p0.RemoveFromHand(id);
        foreach (var id in p1.playedThisTurn) p1.RemoveFromHand(id);

        var reveal = new JSONEvent
        {
            action = GameState.RevealCards,
            playerIds = new string[] { p0Id.ToString(), p1Id.ToString() },
            cardIds = p0.playedThisTurn.Concat(p1.playedThisTurn).ToArray(),
            p0Score = p0.score,
            p1Score = p1.score,
            p0DeckCount = p0.deck.Count,
            p1DeckCount = p1.deck.Count
        };
        networkAdapter.BroadcastEventAsServer(reveal);

        if (currentTurn < maxTurn)
        {
            StartCoroutine(StartNextTurnAfterDelay(currentTurn + 1));
        }
        else
        {
            //OpenResultPanelForAllClientRpc();
            var endGameEvent = new JSONEvent
            {
                action = GameState.GameEnd,
                p0Score = p0.score,
                p1Score = p1.score
            };
            networkAdapter.BroadcastEventAsServer(endGameEvent);
        }
    }

    private IEnumerator StartNextTurnAfterDelay(int nextTurn)
    {
        yield return new WaitForSeconds(3f);
        StartTurnServerSide(nextTurn);
    }
}