using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;


public class CustomNetworkManager : NetworkManager
{
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);

        Debug.Log($"Cliente {conn.connectionId} desconectado.");

        // Verificar si no quedan clientes conectados
        if (NetworkServer.connections.Values.All(connection => connection == null || !connection.isAuthenticated))
        {
            Debug.Log("Todos los clientes se han desconectado.");

            // Llamar al método de limpieza de sesiones en el ServerSessionManager
            var serverSessionManager = FindObjectOfType<ServerSessionManager>();
            if (serverSessionManager != null)
            {
                serverSessionManager.HandleAllClientsDisconnected();
            }
            else
            {
                Debug.LogError("No se encontró el ServerSessionManager en la escena.");
            }
        }
    }
}
