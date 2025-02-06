using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Vortices;
using Vuplex.WebView;
using UnityEngine.SceneManagement;

public class MuseumBaseNetworkHandler : NetworkBehaviour
{
    
    public static MuseumBaseNetworkHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        Debug.Log("[Cliente] Buscando MuseumBase local...");
        
            // Asegurar que este objeto no se destruya al cambiar de escena
        DontDestroyOnLoad(gameObject);

        // Iniciar corrutina para esperar a que MuseumBase cargue
        StartCoroutine(WaitForMuseumBase());
    }

    //  Recibe cambios de URL desde Element y lo notifica al servidor
    public void OnElementUrlChanged(int globalIndex, string newUrl)
    {
        Debug.Log($"[Cliente] Notificando cambio de URL para GlobalIndex {globalIndex}: {newUrl}");

        if (isServer)
        {
            RpcUpdateUrl(globalIndex, newUrl);
        }
        else
        {
            CmdUpdateUrl(globalIndex, newUrl);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateUrl(int globalIndex, string newUrl)
    {
        Debug.Log($"[Servidor] Recibido cambio de URL para GlobalIndex {globalIndex}: {newUrl}");

        var element = GetMuseumElementByIndex(globalIndex);
        if (element != null)
        {
            element.url = newUrl; // Se actualiza la SyncVar
        }

        RpcUpdateUrl(globalIndex, newUrl);
    }

    [ClientRpc]
    void RpcUpdateUrl(int globalIndex, string newUrl)
    {
        Debug.Log($"[Cliente] Actualizando URL en todos los clientes para GlobalIndex {globalIndex}: {newUrl}");

        // Encuentra el MuseumBase local
        MuseumBase localMuseumBase = FindObjectOfType<MuseumBase>();
        if (localMuseumBase != null)
        {
            var element = localMuseumBase.GetMuseumElementByIndex(globalIndex);
            if (element != null)
            {
                element.url = newUrl;
                Debug.Log($"[Cliente] URL de MuseumElement {globalIndex} actualizada correctamente.");

                //  Ahora llamamos a OnUrlChanged para actualizar el navegador web
                element.OnUrlChanged(newUrl);
            }
            else
            {
                Debug.LogError($"[Cliente] No se encontró MuseumElement con GlobalIndex {globalIndex}.");
            }
        }
        else
        {
            Debug.LogError("[Cliente] No se encontró MuseumBase en la escena.");
        }
    }


    MuseumElement GetMuseumElementByIndex(int globalIndex)
    {
        foreach (Transform child in transform)
        {
            var museumElement = child.GetComponent<MuseumElement>();
            if (museumElement != null && museumElement.globalIndex == globalIndex)
            {
                return museumElement;
            }
        }
        return null;
    }

    IEnumerator WaitForMuseumBase()
    {
        Debug.Log("[Cliente] Esperando a que MuseumBase cargue...");

        MuseumBase localMuseumBase = null;
        while (localMuseumBase == null)
        {
            yield return null; // Espera un frame antes de volver a buscar
            localMuseumBase = FindObjectOfType<MuseumBase>();
        }

        Debug.Log("[Cliente] MuseumBase encontrado. Sincronización lista.");
    }
}
