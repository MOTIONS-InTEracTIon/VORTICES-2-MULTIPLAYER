using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class ServerSessionManager : NetworkBehaviour
{
    // Diccionario para almacenar las sesiones activas
    private Dictionary<string, SessionData> activeSessions = new Dictionary<string, SessionData>();

    private static ServerSessionManager _instance;

    public static ServerSessionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("ServerSessionManager no está en la escena.");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("ServerSessionManager inicializado.");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Servidor iniciado y listo para recibir conexiones.");

        // Registrar handlers
        NetworkServer.RegisterHandler<CreateSessionMessage>(HandleCreateSessionMessage);
        NetworkServer.RegisterHandler<RequestActiveSessionMessage>(HandleRequestActiveSessionMessage);
    }


    #region Manejo de Sesiones

    private void HandleCreateSessionMessage(NetworkConnectionToClient conn, CreateSessionMessage msg)
    {
        Debug.Log($"Recibido mensaje para crear sesión: {msg.sessionName}");

        // Log de los datos iniciales del mensaje
        Debug.Log($"Datos iniciales del mensaje:\n" +
                  $"- ElementPaths: {string.Join(", ", msg.elementPaths ?? new List<string>())}\n" +
                  $"- Categorías: {string.Join(", ", msg.categories ?? new List<string>())}");

        // Log de los datos de la sesión recibidos
        Debug.Log($"Datos de la sesión recibidos:\n" +
                  $"- Nombre: {msg.sessionName}\n" +
                  $"- Usuario ID: {msg.userId}\n" +
                  $"- Entorno: {msg.environmentName}\n" +
                  $"- DisplayMode: {msg.displayMode}\n" +
                  $"- Volumetric: {msg.volumetric}\n" +
                  $"- Dimension: {msg.dimension}\n" +
                  $"- ElementPaths: {string.Join(", ", msg.elementPaths)}\n" +
                  $"- Categorías: {string.Join(", ", msg.categories)}\n" +
                  $"- Modo de navegación: {msg.browsingMode}");

        // Verificar si ya existe la sesión
        if (activeSessions.ContainsKey(msg.sessionName))
        {
            Debug.LogWarning($"Sesión '{msg.sessionName}' ya existe.");
            Debug.Log("[Server] Enviando mensaje: SessionCreatedMessage");


            conn.Send(new SessionCreatedMessage { success = false });
            return;
        }

        // Ajustar el nombre de la escena si es necesario
        if (msg.environmentName == "Museum")
        {
            msg.environmentName = "Museum Environment"; // Nombre exacto de la escena en el proyecto
        }
        else if (msg.environmentName == "Circular")
        {
            msg.environmentName = "Circular Environment";
        }
        else
        {
            Debug.LogError($"Nombre de escena desconocido: {msg.environmentName}");
            Debug.Log("[Server] Enviando mensaje: SessionCreatedMessage");


            conn.Send(new SessionCreatedMessage { success = false });
            return;
        }

        // Crear y guardar la sesión
        var sessionData = new SessionData
        {
            sessionName = msg.sessionName,
            userId = msg.userId,
            environmentName = msg.environmentName,
            isOnlineSession = msg.isOnlineSession,
            displayMode = msg.displayMode,
            browsingMode = msg.browsingMode,
            volumetric = msg.volumetric,
            dimension = msg.dimension,
            categories = msg.categories ?? new List<string>(),
            elementPaths = msg.elementPaths ?? new List<string>()
        };
        activeSessions[msg.sessionName] = sessionData;

        Debug.Log($"Sesión '{msg.sessionName}' creada con éxito.");

        Debug.Log($"SessionData creado:\n" +
                  $"- Nombre: {sessionData.sessionName}\n" +
                  $"- Usuario ID: {sessionData.userId}\n" +
                  $"- Entorno: {sessionData.environmentName}\n" +
                  $"- DisplayMode: {sessionData.displayMode}\n" +
                  $"- Volumetric: {sessionData.volumetric}\n" +
                  $"- Dimension: {sessionData.dimension}\n" +
                  $"- Categorías: {string.Join(", ", sessionData.categories)}\n" +
                  $"- ElementPaths: {string.Join(", ", sessionData.elementPaths)}");

        Debug.Log("[Server] Enviando mensaje: SessionCreatedMessage");


        // Enviar datos al cliente
        conn.Send(new SessionCreatedMessage
        {
            success = true,
            sessionName = sessionData.sessionName,
            userId = sessionData.userId,
            environmentName = sessionData.environmentName,
            isOnlineSession = sessionData.isOnlineSession,
            displayMode = sessionData.displayMode,
            browsingMode = sessionData.browsingMode,
            volumetric = sessionData.volumetric,
            dimension = sessionData.dimension,
            categories = sessionData.categories,
            elementPaths = sessionData.elementPaths
        });

        // Cambiar a la escena correspondiente
        NetworkManager.singleton.ServerChangeScene(msg.environmentName);
    }



    private void HandleRequestActiveSessionMessage(NetworkConnectionToClient conn, RequestActiveSessionMessage msg)
    {
        Debug.Log($"Servidor recibió RequestActiveSessionMessage del cliente {conn.connectionId}.");

        if (activeSessions.Count == 0)
        {
            Debug.LogWarning("No hay sesiones activas.");
            Debug.Log("[Server] Enviando mensaje: ActiveSessionResponseMessage");


            conn.Send(new ActiveSessionResponseMessage { success = false });
            return;
        }

        var sessionData = activeSessions.Values.First();
        Debug.Log($"Enviando datos de la sesión activa al cliente {conn.connectionId}: {sessionData.sessionName}");
        Debug.Log("[Server] Enviando mensaje: ActiveSessionResponseMessage");


        conn.Send(new ActiveSessionResponseMessage
        {
            success = true,
            sessionData = sessionData
        });
        Debug.Log($"Mensaje enviado al cliente {conn.connectionId}: {sessionData.sessionName}, {sessionData.environmentName}, {string.Join(", ", sessionData.categories)}, {sessionData.dimension}");
    }


    // Comando para unirse a una sesión existente
    [Command]
    public void CmdJoinSession(NetworkConnectionToClient conn)
    {
        if (activeSessions.Count == 0)
        {
            Debug.LogWarning("No hay sesiones activas en este momento.");
            Debug.Log("[Server] Enviando mensaje: SessionCreatedMessage");


            conn.Send(new SessionCreatedMessage { success = false });
            return;
        }

        // Selecciona la primera sesión activa (puedes cambiar esto si necesitas algo más específico)
        var sessionData = activeSessions.Values.First();

        Debug.Log($"Cliente {conn.connectionId} unido a la sesión '{sessionData.sessionName}'.");

        Debug.Log("[Server] Enviando mensaje: SessionCreatedMessage");


        // Enviar los datos de la sesión al cliente
        conn.Send(new SessionCreatedMessage
        {
            success = true,
            sessionName = sessionData.sessionName,
            userId = sessionData.userId,
            environmentName = sessionData.environmentName,
            isOnlineSession = sessionData.isOnlineSession,
            displayMode = sessionData.displayMode,
            browsingMode = sessionData.browsingMode,
            volumetric = sessionData.volumetric,
            dimension = sessionData.dimension,
            categories = sessionData.categories,
            elementPaths = sessionData.elementPaths
        });
    }



    #endregion

    #region Sincronización con el Cliente

    // Notificar al cliente que la sesión fue creada exitosamente
    [TargetRpc]
    private void TargetNotifySessionCreated(NetworkConnection target, SessionData sessionData)
    {
        if (sessionData.categories == null)
        {
            sessionData.categories = new List<string>(); // Asegurarse de que no sea nulo
        }

        Debug.Log($"Sesión '{sessionData.sessionName}' creada en el cliente.");
        Debug.Log($"Datos enviados: Nombre: {sessionData.sessionName}, Usuario ID: {sessionData.userId}, Entorno: {sessionData.environmentName}, Categorías: {string.Join(", ", sessionData.categories)}");
    }


    // Notificar al cliente que se unió exitosamente a una sesión
    [TargetRpc]
    private void TargetNotifySessionJoined(NetworkConnection target, SessionData sessionData)
    {
        Debug.Log($"Cliente unido a la sesión '{sessionData.sessionName}'.");
        // Aquí puedes sincronizar datos de la sesión con el cliente.
    }

    // Notificar al cliente de un error
    [TargetRpc]
    private void TargetNotifyError(NetworkConnection target, string errorMessage)
    {
        Debug.LogError($"Error enviado al cliente: {errorMessage}");
        // Aquí puedes mostrar un mensaje de error en el cliente.
    }

    #endregion
}
