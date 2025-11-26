using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Button joinRoomBtn;
    [SerializeField] private TMP_InputField hostInputField;
    [SerializeField] private TMP_Text hostIpText;
    [SerializeField] private UnityTransport utp;
    [SerializeField] private TMP_Text msgText;

    [SerializeField] private ushort port;

    void Awake()
    {
        if (NetworkManager.Singleton != null) 
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }
        createRoomBtn.onClick.AddListener(CreateRoom);
        joinRoomBtn.onClick.AddListener(JoinRoom);
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            msgText.text = "Connected! Waiting for other player...";
            ButtonsInteractable(false);
            return;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            int count = NetworkManager.Singleton.ConnectedClientsList.Count;
            if (count >= GameManager.Instance.maxPlayerCount)
            {
                GameManager.Instance.OnAllPlayersConnected();
            }
        }
    }

    private void OnClientDisconnectCallback(ulong cliedId)
    {
        msgText.text = "Connection failed!\n Please check your host IP.";
        ButtonsInteractable(true);
    }

    private void CreateRoom() 
    {
        msgText.text = "";
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.StartHost())
        {
            hostIpText.text = GetLocalIPAddress();
            msgText.text = "Room created,\nPlease wait for your friend to connect!";
            ButtonsInteractable(false);
        }
        else 
        {
            msgText.text = "Room creation failed!";
            ButtonsInteractable(true);
        }
    }

    private void JoinRoom()
    {
        msgText.text = "";
        string ip = hostInputField.text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            msgText.text = "Enter a Host IP!";
            return;
        }
        //validating the ip by parsing it into IP obj
        if (!IPAddress.TryParse(ip, out IPAddress address))
        {
            msgText.text = "Enter a valid IP!";
            return;
        }

        ButtonsInteractable(false);
        utp!.SetConnectionData(ip, port, "0.0.0.0");
        if (NetworkManager.Singleton.StartClient())
        {
            msgText.text = "Trying to connect......";
        }
        else
        {
            msgText.text = "Connection attempt failed!";
            ButtonsInteractable(true);
        }
    }

    public string GetLocalIPAddress()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ua.Address))
                {
                    return ua.Address.ToString();
                }
            }
        }
        return "IP not found!";
    }


    private void ButtonsInteractable(bool value) 
    {
        createRoomBtn.interactable = value;
        joinRoomBtn.interactable = value;
        hostInputField.interactable = value;
    }
}
