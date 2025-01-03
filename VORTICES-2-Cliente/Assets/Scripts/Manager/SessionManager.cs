using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Rendering.DebugUI;

namespace Vortices
{
    public class SessionManager : MonoBehaviour
    {

        // Static instance
        public static SessionManager instance;

        // Settings
        public string sessionName;
        public int userId;
        public string environmentName;
        public bool isOnlineSession = false;
        // Environment Settings
        public string displayMode;
        public string browsingMode;
        public bool volumetric;
        public Vector3Int dimension;
        public List<string> elementPaths;
        // Session Manager settings
        public float initializeTime = 2.0f;

        // Controllers
        [SerializeField] private SceneTransitionManager actualTransitionManager;
        [SerializeField] public CategoryController categoryController;
        [SerializeField] public ElementCategoryController elementCategoryController;
        [SerializeField] public SpawnController spawnController;
        [SerializeField] public AddonsController addonsController;
        private CategorySelector categorySelector;
        [SerializeField] public LoggingController loggingController;
        public InputController inputController;
        [SerializeField] public LocalizationController localizationController;
        [SerializeField] public ErrorController errorController;
        public RighthandTools righthandTools;

        // Coroutine
        public bool sessionLaunchRunning;

        public GameObject currentlySelected;
        public GameObject lastSelected;
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Escena cargada: {scene.name}");

            // Verificar objetos registrados para spawn
            Debug.Log("Objetos registrados para sincronizaci�n:");
            foreach (var prefab in NetworkClient.prefabs.Values)
            {
                Debug.Log($"Prefab registrado: {prefab.name}");
            }

            // Opcional: verificar objetos de la escena con NetworkIdentity
            var networkObjects = GameObject.FindObjectsOfType<NetworkIdentity>();
            foreach (var obj in networkObjects)
            {
                Debug.Log($"Objeto en escena con NetworkIdentity: {obj.name}, AssetId: {obj.assetId}");
            }

            lastSelected = null; // Reset lastSelected when a new scene is loaded
        }

        private void Start()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            categoryController = GameObject.FindObjectOfType<CategoryController>(true);
            elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
            inputController = GameObject.FindObjectOfType<InputController>(true);

            errorController.Initialize();
            inputController.Initialize();
            addonsController.Initialize();
            localizationController.Initialize();

            // Registrar handlers en el cliente
            NetworkClient.RegisterHandler<SessionCreatedMessage>(HandleSessionCreatedMessage);
            Debug.Log("Handler de SessionCreatedMessage registrado.");
            NetworkClient.RegisterHandler<ActiveSessionResponseMessage>(HandleActiveSessionResponse);
            Debug.Log("Handler de ActiveSessionResponseMessage registrado.");

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void HandleSessionCreatedMessage(SessionCreatedMessage msg)
        {
            if (!msg.success)
            {
                Debug.LogError("Error al crear la sesi�n en el servidor.");
                return;
            }
            Debug.Log("[Client] Recibido mensaje SessionCreatedMessage");
            Debug.Log("Datos de la sesi�n recibidos del servidor:");
            Debug.Log($"- Nombre: {msg.sessionName}");
            Debug.Log($"- Usuario ID: {msg.userId}");
            Debug.Log($"- Entorno: {msg.environmentName}");
            Debug.Log($"- Is Online: {msg.isOnlineSession}");
            Debug.Log($"- Display Mode: {msg.displayMode}");
            Debug.Log($"- Browsing Mode: {msg.browsingMode}");
            Debug.Log($"- Volumetric: {msg.volumetric}");
            Debug.Log($"- Dimension: {msg.dimension}");
            Debug.Log($"- Element Paths: {string.Join(", ", msg.elementPaths)}");
            Debug.Log($"- Categor�as: {string.Join(", ", msg.categories)}");

            // Actualizar SessionManager con los datos recibidos
            sessionName = msg.sessionName;
            userId = msg.userId;
            // Normalizar el nombre del entorno
            if (msg.environmentName == "Circular Environment")
            {
                environmentName = "Circular";
            }
            else if (msg.environmentName == "Museum Environment")
            {
                environmentName = "Museum";
            }
            else
            {
                environmentName = msg.environmentName;
            }
            isOnlineSession = msg.isOnlineSession;
            displayMode = msg.displayMode;
            browsingMode = msg.browsingMode;
            volumetric = msg.volumetric;
            dimension = msg.dimension;
            elementPaths = msg.elementPaths;

            Debug.Log("Session Manager seteado al Crear Sesion");
            // Actualizar categor�as en el controlador
            categoryController.UpdateCategoriesList(msg.categories);
        }


        #region UI Handling

        private void Update()
        {
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                currentlySelected = EventSystem.current.currentSelectedGameObject;
            }
            else
            {
                currentlySelected = null;
            }
            if (EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
            {
                FindAndSetNextSelectable();
            }
            else
            {
                lastSelected = EventSystem.current.currentSelectedGameObject;
            }
        }

        private void FindAndSetNextSelectable()
        {
            GameObject canvasObject = GameObject.Find("Canvas"); // Find the GameObject named "Canvas"

            if (canvasObject == null)
            {
                Debug.LogError("No GameObject named 'Canvas' found in the scene.");
                return;
            }

            Canvas canvasComponent = canvasObject.GetComponent<Canvas>(); // Get the Canvas component from the GameObject

            if (canvasComponent == null)
            {
                Debug.LogError("No Canvas component found on the 'Canvas' GameObject.");
                return;
            }

            Selectable[] selectables = canvasComponent.GetComponentsInChildren<Selectable>();

            if (selectables.Length == 0)
            {
                return; // No selectables in the canvas
            }

            int startIndex = 0;
            if (lastSelected != null)
            {
                int lastIndex = System.Array.FindIndex(selectables, selectable => selectable.gameObject == lastSelected);
                if (lastIndex != -1)
                {
                    startIndex = (lastIndex + 1) % selectables.Length;
                }
            }

            for (int i = 0; i < selectables.Length; i++)
            {
                int index = (startIndex + i) % selectables.Length;
                if (selectables[index].gameObject.activeInHierarchy && selectables[index].interactable)
                {
                    EventSystem.current.SetSelectedGameObject(selectables[index].gameObject);
                    lastSelected = selectables[index].gameObject;
                    break;
                }
            }
        }

        #endregion

        #region Data Operations
        public void LaunchSession()
        {
            if (!sessionLaunchRunning)
            {
                StartCoroutine(LaunchSessionCoroutine(sessionName, userId, environmentName));
            }
        }

        public IEnumerator LaunchSessionCoroutine(string sessionName, int userId, string environmentName)
        {
            sessionLaunchRunning = true;

            GameObject keyboard = GameObject.Find("Keyboard Canvas");
            if (keyboard != null)
            {
                keyboard.GetComponent<HandKeyboard>().RemoveInputField();
            }


            // Switch to environment scene
            actualTransitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
            actualTransitionManager.returnToMain = false;

            if (isOnlineSession)
            {
                if (!NetworkClient.isConnected)
                {
                    Debug.LogError("El cliente no est� conectado al servidor. Intentando conectar...");
                    NetworkManager.singleton.networkAddress = "192.168.31.72"; // Cambia por la IP del servidor si no es local 192.168.31.72
                    NetworkManager.singleton.StartClient();
                    actualTransitionManager = GameObject.FindObjectOfType<SceneTransitionManager>(true);
                    categoryController = GameObject.FindObjectOfType<CategoryController>(true);
                    elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
                    loggingController = GameObject.FindObjectOfType<LoggingController>(true);
                    righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);

                    // Esperar hasta que el cliente se conecte o exceda el tiempo de espera
                    float timeout = 10f;
                    while (!NetworkClient.isConnected && timeout > 0f)
                    {
                        timeout -= Time.deltaTime;
                        yield return null;
                    }

                    if (!NetworkClient.isConnected)
                    {
                        Debug.LogError("No se pudo conectar al servidor.");
                        sessionLaunchRunning = false;
                        yield break;
                    }
                }

                Debug.Log("Conexi�n establecida. Enviando datos de la sesi�n...");

                // Crear datos de la sesi�n
                SessionData sessionData = new SessionData
                {
                    sessionName = sessionName,
                    userId = userId,
                    environmentName = environmentName,
                    elementPaths = elementPaths,
                    categories = categoryController.GetCategories(), // M�todo para obtener las categor�as seleccionadas
                    browsingMode = "Online" // Ignoramos archivos por ahora
                };

                // Enviar datos al servidor
                yield return SendSessionDataToServer(sessionData);

                // Esperar confirmaci�n del servidor (si se implementa)
                Debug.Log("Datos enviados al servidor.");
                yield break;
            }

            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());
            

            yield return new WaitForSeconds(initializeTime);

            // When done, configure controllers of the scene
            actualTransitionManager = GameObject.FindObjectOfType<SceneTransitionManager>(true);
            categoryController = GameObject.FindObjectOfType<CategoryController>(true);
            elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
            loggingController = GameObject.FindObjectOfType<LoggingController>(true);
            spawnController = GameObject.FindObjectOfType<SpawnController>(true);
            righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);

            elementCategoryController.Initialize();
            loggingController.Initialize();
            spawnController.Initialize();

            inputController.RestartInputs();


            sessionLaunchRunning = false;
        }

        public IEnumerator SendSessionDataToServer(SessionData sessionData)
        {
            if (!NetworkClient.isConnected)
            {
                Debug.LogError("El cliente no est� conectado al servidor. No se puede enviar la sesi�n.");
                yield break;
            }

            Debug.Log("Enviando datos de sesi�n al servidor...");
            NetworkClient.Send(new CreateSessionMessage
            {
                sessionName = sessionName,
                userId = userId,
                environmentName = environmentName,
                isOnlineSession = isOnlineSession,
                displayMode = displayMode,
                browsingMode = browsingMode,
                volumetric = volumetric,
                dimension = dimension,
                categories = categoryController.GetCategories(),
                elementPaths = elementPaths
            });

            yield return new WaitForSeconds(1.0f);
        }


        public IEnumerator StopSessionCoroutine()
        {
            sessionLaunchRunning = true;

            if (NetworkClient.isConnected)
            {
                Debug.Log("El cliente est� conectado a una sesi�n. Desconectando...");
                NetworkClient.Disconnect();

                // Esperar a que el cliente se desconecte
                float timeout = 5f; // Tiempo m�ximo para desconectar
                while (NetworkClient.isConnected && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (NetworkClient.isConnected)
                {
                    Debug.LogError("No se pudo desconectar del servidor dentro del tiempo de espera.");
                    yield break;
                }
                else
                {
                    Debug.Log("Desconectado del servidor con �xito.");
                }
            }
            else
            {
                Debug.Log("El cliente no est� conectado a ninguna sesi�n.");
            }

            actualTransitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
            righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);

            if (actualTransitionManager == null)
            {
                Debug.LogError("No se encontro el actualTransitionManager");
            }
            else
            {
                Debug.Log("Se encontro el actualTransitionManager");
            }

            if (righthandTools == null)
            {
                Debug.LogError("righthandTools no est� asignado. Verifica su inicializaci�n.");
                yield break; // Salimos del coroutine para evitar m�s errores
            }
            else
            {
                Debug.Log("righthandTools est� asignado correctamente.");
            }

            Fade toolsFader = righthandTools.GetComponent<Fade>();
            yield return StartCoroutine(toolsFader.FadeOutCoroutine());

            actualTransitionManager.returnToMain = true;
            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());

            yield return new WaitForSeconds(initializeTime);

            categorySelector = GameObject.FindObjectOfType<CategorySelector>(true);

            if(categorySelector == null)
            {
                Debug.LogError("No se encontro el categorySelector");
            }
            else
            {
                Debug.Log("categoriSelector encontrado");
            }

            ResetSessionManager();

            sessionLaunchRunning = false;
        }

        private void ResetSessionManager()
        {
            Debug.Log("Reiniciando SessionManager...");

            // Restablecer valores del SessionManager
            sessionName = string.Empty;
            userId = 0;
            environmentName = string.Empty;
            displayMode = null;
            volumetric = false;
            dimension = Vector3Int.zero;
            elementPaths?.Clear();
            categoryController.UpdateCategoriesList(null);
            isOnlineSession = false;
            browsingMode = string.Empty;

            // Reiniciar controladores auxiliares si es necesario
            categoryController?.Initialize();
            elementCategoryController?.Initialize();
            loggingController?.Initialize();

            Debug.Log("SessionManager reiniciado con �xito.");
        }

        public void JoinSession(string ipAddress)
        {
            if (!string.IsNullOrEmpty(ipAddress))
            {
                NetworkManager.singleton.networkAddress = ipAddress;
                NetworkManager.singleton.StartClient();
                actualTransitionManager = GameObject.FindObjectOfType<SceneTransitionManager>(true);
                categoryController = GameObject.FindObjectOfType<CategoryController>(true);
                elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
                loggingController = GameObject.FindObjectOfType<LoggingController>(true);
                righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);
                
                // Esperar conexi�n y solicitar sesiones activas
                StartCoroutine(WaitForConnectionAndJoinSession());
            }
            else
            {
                Debug.LogWarning("La direcci�n IP est� vac�a.");
            }
        }

        private IEnumerator WaitForConnectionAndJoinSession()
        {
            float timeout = 10f;
            while (!NetworkClient.isConnected && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (NetworkClient.isConnected)
            {
                Debug.Log("Conexi�n establecida. Verificando sesiones activas...");
                NetworkClient.Send(new RequestActiveSessionMessage());
            }
            else
            {
                Debug.LogError("No se pudo conectar al servidor.");
            }
        }

        private void HandleActiveSessionResponse(ActiveSessionResponseMessage msg)
        {
            Debug.Log("[Client] Recibido mensaje ActiveSessionResponseMessage");
            if (!msg.success)
            {
                Debug.LogError("No se encontraron sesiones activas en el servidor.");
                return;
            }

            if (msg.sessionData.categories == null)
            {
                msg.sessionData.categories = new List<string>();
            }

            if (msg.sessionData.elementPaths == null)
            {
                msg.sessionData.elementPaths = new List<string>(); // Prevenir nulos
            }

            Debug.Log("Sesi�n activa encontrada. Configurando datos:");
            Debug.Log($"- Nombre: {msg.sessionData.sessionName}");
            Debug.Log($"- Usuario ID: {msg.sessionData.userId}");
            Debug.Log($"- Entorno: {msg.sessionData.environmentName}");
            Debug.Log($"- Categor�as: {string.Join(", ", msg.sessionData.categories)}");
            Debug.Log($"- Elementos: {string.Join(", ", msg.sessionData.elementPaths)}");

            // Configurar datos en el SessionManager
            sessionName = msg.sessionData.sessionName;
            userId = msg.sessionData.userId;
            // Normalizar el nombre del entorno
            if (msg.sessionData.environmentName == "Circular Environment")
            {
                environmentName = "Circular";
            }
            else if (msg.sessionData.environmentName == "Museum Environment")
            {
                environmentName = "Museum";
            }
            else
            {
                environmentName = msg.sessionData.environmentName;
            }
            browsingMode = msg.sessionData.browsingMode;
            elementPaths = msg.sessionData.elementPaths;
            displayMode = msg.sessionData.displayMode;
            volumetric = msg.sessionData.volumetric;
            dimension = msg.sessionData.dimension;

            Debug.Log("Session Manager seteado al Unirse a una Sesion");

        }





        #endregion
    }
}

