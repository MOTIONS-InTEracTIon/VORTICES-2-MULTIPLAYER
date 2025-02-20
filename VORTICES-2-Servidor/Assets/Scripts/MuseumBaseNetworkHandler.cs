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
        Debug.Log($"[Cliente] Intentando actualizar categoría '{categoryName}' en '{elementUrl}' (Agregar: {isAdding})");

        // Obtener el controlador de categorías
        ElementCategoryController elementCategoryController = FindObjectOfType<ElementCategoryController>();
        if (elementCategoryController == null)
        {
            Debug.LogError("[Cliente] No se encontró ElementCategoryController en la escena.");
            return;
        }

        // Obtener el elemento afectado
        Element affectedElement = elementCategoryController.GetElementByUrl(elementUrl);
        if (affectedElement == null)
        {
            Debug.LogWarning($"[Cliente] No se encontró un elemento con la URL '{elementUrl}'.");
            return;
        }

        // Obtener la categoría del elemento
        var elementCategory = elementCategoryController.GetSelectedCategories(elementUrl);

        // Verificar si la categoría ya está para evitar duplicados
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

        // Agregar o eliminar la categoría
        if (isAdding)
        {
            elementCategory.elementCategories.Add(categoryName);
            elementCategory.elementCategories.Sort();  // Ordenar alfabéticamente
        }
        else
        {
            elementCategory.elementCategories.Remove(categoryName);
        }

        // Actualizar la lista de categorías del elemento
        elementCategoryController.UpdateElementCategoriesList(elementUrl, elementCategory);

        // Actualizar estado visual del elemento
        affectedElement.SetCategorized(isAdding);

        // Obtener referencia a RightHandTools y actualizar UI
        RighthandTools rightHandTools = FindObjectOfType<RighthandTools>();
        if (rightHandTools != null)
        {
            rightHandTools.AddUISortingCategories();
        }
        else
        {
            Debug.LogWarning("[Cliente] No se encontró RightHandTools para actualizar la UI.");
        }

        UIElementCategory[] categoryElements = Resources.FindObjectsOfTypeAll<UIElementCategory>();

        foreach (UIElementCategory categoryElement in categoryElements)
        {
            if (categoryElement.categoryName == categoryName)
            {
                Debug.Log($"[Cliente] Sincronizando UI para categoría '{categoryName}' en '{elementUrl}'.");

                // 🔹 Buscar el `Select Toggle` dentro del `UIElementCategory`
                Transform toggleTransform = categoryElement.transform.Find("Select Toggle");

                if (toggleTransform != null)
                {
                    Toggle toggle = toggleTransform.GetComponent<Toggle>();

                    if (toggle != null)
                    {
                        // 🔹 Evitar que el cambio de `isOn` dispare `SelectedToggle()`
                        toggle.onValueChanged.RemoveAllListeners();
                        toggle.isOn = isAdding;
                        toggle.onValueChanged.AddListener((value) => categoryElement.SelectedToggle());

                        Debug.Log($"[Cliente] Toggle actualizado para '{categoryName}', isOn: {isAdding}");
                    }
                    else
                    {
                        Debug.LogError($"[Cliente] No se encontró un componente Toggle en 'Select Toggle' para '{categoryName}'");
                    }
                }
                else
                {
                    Debug.LogError($"[Cliente] No se encontró el objeto 'Select Toggle' en '{categoryElement.name}'");
                }

                break;
            }
        }
    }


}
