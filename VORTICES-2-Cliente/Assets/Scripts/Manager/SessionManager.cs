using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Threading.Tasks;

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
            NetworkClient.RegisterHandler<ActiveSessionResponseMessage>(HandleActiveSessionResponse);

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void HandleSessionCreatedMessage(SessionCreatedMessage msg)
        {
            if (!msg.success)
            {
                return;
            }

            sessionName = msg.sessionName;
            userId = msg.userId;

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
                    NetworkManager.singleton.networkAddress = "134.65.228.226"; // Cambia por la IP del servidor si no es local 192.168.31.72(maquina virtual) 192.168.31.117(notebook)
                    NetworkManager.singleton.StartClient();
                    actualTransitionManager = GameObject.FindObjectOfType<SceneTransitionManager>(true);
                    categoryController = GameObject.FindObjectOfType<CategoryController>(true);
                    elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
                    loggingController = GameObject.FindObjectOfType<LoggingController>(true);
                    righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);

                    float timeout = 10f;
                    while (!NetworkClient.isConnected && timeout > 0f)
                    {
                        timeout -= Time.deltaTime;
                        yield return null;
                    }

                    if (!NetworkClient.isConnected)
                    {
                        sessionLaunchRunning = false;
                        yield break;
                    }
                }

                SessionData sessionData = new SessionData
                {
                    sessionName = sessionName,
                    userId = userId,
                    environmentName = environmentName,
                    elementPaths = elementPaths,
                    categories = categoryController.GetCategories(),
                    browsingMode = "Online" 
                };

                StartCoroutine(ConnectToVoiceChatCoroutine(userId));

                yield return SendSessionDataToServer(sessionData);

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
                
                // Conectar al canal de voz
                StartCoroutine(ConnectToVoiceChatCoroutine(userId));
    
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
                msg.sessionData.elementPaths = new List<string>(); 
            }

            sessionName = msg.sessionData.sessionName;
            userId = msg.sessionData.userId;

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
        }

        private IEnumerator ConnectToVoiceChatCoroutine(int userId)
        {
            bool loginSuccess = false;
            bool channelJoinSuccess = false;

            // Intentar iniciar sesión en Vivox
            VivoxVoiceManager.Instance.LoginAsync(userId.ToString()).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    loginSuccess = true;
                    Debug.Log($"[VoiceChat] Usuario {userId} conectado a Vivox.");
                }
                else
                {
                    Debug.LogError($"[VoiceChat] Error al iniciar sesión en Vivox: {task.Exception?.Message}");
                }
            });

            // Esperar que el login termine
            yield return new WaitUntil(() => loginSuccess);

            // Intentar unirse al canal de voz
            VivoxVoiceManager.Instance.JoinChannelAsync("VoRTIcESVoiceChat").ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    channelJoinSuccess = true;
                    Debug.Log("[VoiceChat] Usuario unido al canal de voz: VoRTIcESVoiceChat");
                }
                else
                {
                    Debug.LogError($"[VoiceChat] Error al unirse al canal de voz: {task.Exception?.Message}");
                }
            });

            // Esperar que se conecte al canal
            yield return new WaitUntil(() => channelJoinSuccess);

            if (loginSuccess && channelJoinSuccess)
            {
                Debug.Log("[VoiceChat] Usuario conectado exitosamente al chat de voz.");
            }
        }

        #endregion
    }
}

