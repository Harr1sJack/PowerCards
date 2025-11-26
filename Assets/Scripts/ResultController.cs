using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultController : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text p1FinalScoreText;
    [SerializeField] private TMP_Text p2FinalScoreText;
    [SerializeField] private Button returnToMenuButton;

    private void Start()
    {
        // Assuming GameManager is the root object and persists across scenes/panels
        returnToMenuButton.onClick.AddListener(ReturnToMenu);
        gameObject.SetActive(false);
    }

    public void ShowResults(int myScore, int opponentScore)
    {
        p1FinalScoreText.text = $"Your Score: {myScore}";
        p2FinalScoreText.text = $"Opponent Score: {opponentScore}";

        if (myScore > opponentScore)
        {
            resultText.text = "VICTORY!";
            resultText.color = Color.green;
        }
        else if (myScore < opponentScore)
        {
            resultText.text = "DEFEAT!";
            resultText.color = Color.red;
        }
        else
        {
            resultText.text = "TIE!";
            resultText.color = Color.yellow;
        }
    }

    private void ReturnToMenu()
    {
        // Simple disconnect and return to menu
        if (Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }
        else if (Unity.Netcode.NetworkManager.Singleton.IsClient)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        GameManager.Instance.EnablePanel(GameManager.PanelType.Menu);
    }
}