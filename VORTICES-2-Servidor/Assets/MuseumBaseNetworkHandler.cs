using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MuseumBaseNetworkHandler : NetworkBehaviour
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"Museum Base inicializado en el servidor con ID de red: {GetComponent<NetworkIdentity>().netId}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"Museum Base sincronizado en el cliente con ID de red: {GetComponent<NetworkIdentity>().netId}");
    }
    
}
