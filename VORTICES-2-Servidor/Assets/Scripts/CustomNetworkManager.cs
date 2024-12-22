using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{

    public GameObject chatCanvasPrefab;

    public override void Awake()
    {
        base.Awake();
        Debug.Log("CustomNetworkManager - Awake");
    }

    public override void Start()
    {
        base.Start();
        Debug.Log("CustomNetworkManager - Start");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[OnServerAddPlayer] Añadiendo jugador con connId: {conn.connectionId}...");

        // Instanciar y añadir el jugador
        Transform startPos = GetStartPosition();
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        player.name = $"Player [connId={conn.connectionId}]";
        Debug.Log("[OnServerAddPlayer] Jugador instanciado: " + player.name);

        NetworkServer.AddPlayerForConnection(conn, player);
        Debug.Log("[OnServerAddPlayer] Jugador añadido a la conexión del cliente.");
        
        // Spawnear el ChatCanvas
        if (chatCanvasPrefab != null)
        {
            Debug.Log("[ChatCanvas] Intentando spawnear ChatCanvas...");

            GameObject chatInstance = Instantiate(chatCanvasPrefab);

            if (chatInstance == null)
            {
                Debug.LogError("[ChatCanvas] No se pudo instanciar el ChatCanvasPrefab.");
                return;
            }

            Debug.Log("[ChatCanvas] ChatCanvasPrefab instanciado correctamente.");

            NetworkServer.Spawn(chatInstance, conn);

            Debug.Log("[ChatCanvas] ChatCanvas spawneado exitosamente para connId=" + conn.connectionId);
        }
        else
        {
            Debug.LogError("[ChatCanvas] ChatCanvasPrefab no asignado en el NetworkManager.");
        }
    }


}
