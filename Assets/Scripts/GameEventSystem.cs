using System;

public static class GameEventSystem
{
    public static event Action<JSONEvent> OnJSONEvent;
    public static event Action<JSONEventFromNetwork> OnJSONNetworkEvent;

    public static void Call(JSONEvent e) 
    { 
        OnJSONEvent?.Invoke(e); 
    }
    public static void CallFromNetwork(JSONEventFromNetwork e)
    {
        OnJSONNetworkEvent?.Invoke(e);
    }
}
