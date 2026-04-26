using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkManagerLobby : NetworkManager
{
    public static NetworkManagerLobby Instance => singleton as NetworkManagerLobby;

    public static readonly List<NetworkCarController> GamePlayers = new List<NetworkCarController>();

    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("[Host] Хост запущен, загружаем карту...");
        // Сразу загружаем игровую сцену
        ServerChangeScene(onlineScene);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        if (sceneName.Contains("SoloGameScene"))
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null)
                    SpawnCarForConnection(conn);
            }
        }
    }

    // Когда новый клиент подключается уже во время игры
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        SpawnCarForConnection(conn);
    }

    private void SpawnCarForConnection(NetworkConnectionToClient conn)
    {
        if (conn == null) return;
        if (conn.identity != null) return; // уже заспавнен

        Transform startPos = GetStartPosition();
        Vector3 pos = startPos != null ? startPos.position : Vector3.zero;
        Quaternion rot = startPos != null ? startPos.rotation : Quaternion.identity;

        GameObject carObj = Instantiate(playerPrefab, pos, rot);
        NetworkServer.AddPlayerForConnection(conn, carObj);

        var car = carObj.GetComponent<NetworkCarController>();
        if (car != null) GamePlayers.Add(car);

        Debug.Log($"[Server] Машина заспавнена. Игроков на карте: {GamePlayers.Count}");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            var car = conn.identity.GetComponent<NetworkCarController>();
            if (car != null) GamePlayers.Remove(car);
        }
        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        GamePlayers.Clear();
    }
}