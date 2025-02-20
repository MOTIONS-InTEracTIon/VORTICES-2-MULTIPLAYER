using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Vortices;
using System.Linq;
using Vuplex.WebView;

public class CircularNetworkHandler : NetworkBehaviour
{
    public static CircularNetworkHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        Debug.Log("[Cliente] Buscando CircularBase local...");
        
        DontDestroyOnLoad(gameObject);

        StartCoroutine(WaitForCircularBase());
    }

    //  Sincronización de URLs
    public void OnElementUrlChanged(Element element, string newUrl)
    {
        Debug.Log($"[Cliente] Notificando cambio de URL en Circular para índice {element.circularIndex}: {newUrl}");

        if (isServer)
        {
            RpcUpdateUrl(element.circularIndex, newUrl);
        }
        else
        {
            CmdUpdateUrl(element.circularIndex, newUrl);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateUrl(int circularIndex, string newUrl)
    {
        Debug.Log($"[Servidor] Recibido cambio de URL en Circular para índice {circularIndex}: {newUrl}");
        RpcUpdateUrl(circularIndex, newUrl);
    }

    [ClientRpc]
    void RpcUpdateUrl(int circularIndex, string newUrl)
    {
        Debug.Log($"[Cliente] Actualizando URL en todos los clientes para índice {circularIndex}: {newUrl}");

        Element element = GetElementByIndex(circularIndex);
        if (element != null)
        {
            element.url = newUrl;
            element.GetComponentInChildren<CanvasWebViewPrefab>().WebView.LoadUrl(newUrl);
            Debug.Log($"[Cliente] URL del Element {circularIndex} actualizada correctamente.");
        }
        else
        {
            Debug.LogError($"[Cliente] No se encontró un elemento con índice {circularIndex}.");
        }
    }

    //  Sincronización de categorías
    [Command(requiresAuthority = false)]
    public void CmdUpdateCategory(string elementUrl, string categoryName, bool isAdding)
    {
        Debug.Log($"[Servidor] Recibida actualización de categorización para {elementUrl}, categoría: {categoryName}, agregar: {isAdding}");
        RpcUpdateCategory(elementUrl, categoryName, isAdding);
    }

    [ClientRpc]
    void RpcUpdateCategory(string elementUrl, string categoryName, bool isAdding)
    {
        Debug.Log($"[Cliente] Intentando actualizar categoría '{categoryName}' en '{elementUrl}' (Agregar: {isAdding})");

        ElementCategoryController elementCategoryController = FindObjectOfType<ElementCategoryController>();
        if (elementCategoryController == null)
        {
            Debug.LogError("[Cliente] No se encontró ElementCategoryController en la escena.");
            return;
        }

        Element affectedElement = elementCategoryController.GetElementByUrl(elementUrl);
        if (affectedElement == null)
        {
            Debug.LogWarning($"[Cliente] No se encontró un elemento con la URL '{elementUrl}'.");
            return;
        }

        var elementCategory = elementCategoryController.GetSelectedCategories(elementUrl);

        if (isAdding && elementCategory.elementCategories.Contains(categoryName))
        {
            Debug.Log($"[Cliente] La categoría '{categoryName}' ya existe en '{elementUrl}', no se vuelve a agregar.");
            return;
        }
        else if (!isAdding && !elementCategory.elementCategories.Contains(categoryName))
        {
            Debug.Log($"[Cliente] La categoría '{categoryName}' no existe en '{elementUrl}', no se puede eliminar.");
            return;
        }

        if (isAdding)
        {
            elementCategory.elementCategories.Add(categoryName);
            elementCategory.elementCategories.Sort();
        }
        else
        {
            elementCategory.elementCategories.Remove(categoryName);
        }

        elementCategoryController.UpdateElementCategoriesList(elementUrl, elementCategory);

        affectedElement.SetCategorized(isAdding);

        RighthandTools rightHandTools = FindObjectOfType<RighthandTools>();
        if (rightHandTools != null)
        {
            rightHandTools.AddUISortingCategories();
        }
        else
        {
            Debug.LogWarning("[Cliente] No se encontró RightHandTools para actualizar la UI.");
        }
    }

    //  Método para obtener un `Element` por su `circularIndex`
    Element GetElementByIndex(int circularIndex)
    {
        Element[] elements = FindObjectsOfType<Element>();
        foreach (Element element in elements)
        {
            if (element.circularIndex == circularIndex)
            {
                return element;
            }
        }
        return null;
    }

    IEnumerator WaitForCircularBase()
    {
        Debug.Log("[Cliente] Esperando a que CircularBase cargue...");
        CircularSpawnBase localCircularBase = null;
        while (localCircularBase == null)
        {
            yield return null;
            localCircularBase = FindObjectOfType<CircularSpawnBase>();
        }
        Debug.Log("[Cliente] CircularBase encontrado. Sincronización lista.");
    }
}
