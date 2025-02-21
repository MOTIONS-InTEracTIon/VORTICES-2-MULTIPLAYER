using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Vortices;
using Vuplex.WebView;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Command(requiresAuthority = false)]
    public void CmdUpdateCategory(string elementUrl, string categoryName, bool isAdding)
    {
        Debug.Log($"[Servidor] Recibida actualización de categorización para {elementUrl}, categoría: {categoryName}, agregar: {isAdding}");

        // Llamar al Rpc para propagar la actualización a todos los clientes
        RpcUpdateCategory(elementUrl, categoryName, isAdding);
    }

    [ClientRpc]
    void RpcUpdateCategory(string elementUrl, string categoryName, bool isAdding)
    {
        Debug.Log($"[Cliente] Sincronizando Toggle para categoría '{categoryName}' en '{elementUrl}', isAdding: {isAdding}");

        UIElementCategory[] categoryElements = Resources.FindObjectsOfTypeAll<UIElementCategory>();

        foreach (UIElementCategory categoryElement in categoryElements)
        {
            if (categoryElement.categoryName == categoryName)
            {
                Toggle toggle = categoryElement.GetComponentInChildren<Toggle>();

                if (toggle != null)
                {
                    bool shouldBeOn = isAdding;

                    // *** Evita el bucle infinito: Solo cambia si es necesario ***
                    if (toggle.isOn != shouldBeOn)
                    {
                        toggle.isOn = shouldBeOn;
                        Debug.Log($"[Cliente] Toggle actualizado correctamente para '{categoryName}', isOn: {toggle.isOn}");
                    }
                    else
                    {
                        Debug.Log($"[Cliente] Toggle ya estaba en el estado correcto para '{categoryName}', isOn: {toggle.isOn}");
                    }
                }
                else
                {
                    Debug.LogError($"[Cliente] No se encontró el Toggle en UIElementCategory para '{categoryName}'.");
                }

                break; // No es necesario seguir buscando
            }
        }
    }

}
