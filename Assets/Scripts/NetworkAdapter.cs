using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkAdapter : NetworkBehaviour
{
    [Rpc(SendTo.Server,InvokePermission =RpcInvokePermission.Everyone)]
    public void SendJsonToServerRpc(string json, RpcParams rpcParams = default)
    {
        var evt = new JSONEventFromNetwork { json = json, senderClientId = rpcParams.Receive.SenderClientId };
        GameEventSystem.CallFromNetwork(evt);
    }

    [ClientRpc]
    public void BroadcastJsonClientRpc(string json, ClientRpcParams clientRpcParams = default)
    {
        JSONEvent parsed = JsonUtility.FromJson<JSONEvent>(json);
        GameEventSystem.Call(parsed);
    }

    //sending to server from client
    public void SendEventAsClient(JSONEvent e)
    {
        SendJsonToServerRpc(JsonUtility.ToJson(e));
    }

    //broadcasting to all client
    public void BroadcastEventAsServer(JSONEvent e)
    {
        BroadcastJsonClientRpc(JsonUtility.ToJson(e));
    }

    //sending to a particular client
    public void SendEventToClient(JSONEvent e, ulong clientId)
    {
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { clientId }
            }
        };
        BroadcastJsonClientRpc(JsonUtility.ToJson(e), clientRpcParams);
    }
}

//separate type to keep track of senders
public class JSONEventFromNetwork
{
    public string json;
    public ulong senderClientId;
}
