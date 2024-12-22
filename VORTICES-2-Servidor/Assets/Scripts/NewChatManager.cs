using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.UI;


public class NewChatManager : NetworkBehaviour
{
    [Header("Chat UI Components")]
    public GameObject chatCanvas;
    public TMP_InputField chatInputField;
    public TMP_Text chatDisplay;
    public Button sendButton;

    private void Start()
    {
        // Solo desactivar el ChatCanvas si no es el servidor
        if (!isServer)
        {
            chatCanvas.SetActive(false);
        }

        Debug.Log($"[NewChatManager] Inicializado en: {gameObject.name}. Es servidor: {isServer}");

        // Vincular el botón de enviar
        sendButton.onClick.AddListener(OnSendButtonPressed);
    }

    public void ToggleChat()
    {
        // Alternar visibilidad del chat
        chatCanvas.SetActive(!chatCanvas.activeSelf);
        Debug.Log($"Chat {(chatCanvas.activeSelf ? "activado" : "desactivado")}");
    }

    // Llamado cuando se presiona el botón de enviar
    public void OnSendButtonPressed()
    {
        if (chatInputField == null || string.IsNullOrEmpty(chatInputField.text))
        {
            Debug.LogWarning("[NewChatManager] No se puede enviar un mensaje vacío.");
            return;
        }

        string message = chatInputField.text;

        // Obtener el jugador local con autoridad
        GameObject playerObject = NetworkClient.localPlayer?.gameObject;
        if (playerObject == null)
        {
            Debug.LogError("[NewChatManager] Objeto jugador local no encontrado.");
            return;
        }

        // Obtener el controlador de chat del jugador
        PlayerChatController playerChatController = playerObject.GetComponent<PlayerChatController>();
        if (playerChatController == null)
        {
            Debug.LogError("[NewChatManager] PlayerChatController no encontrado en el jugador.");
            return;
        }

        // Enviar el mensaje al servidor
        Debug.Log($"[NewChatManager] Mensaje preparado para enviar: {message}");
        playerChatController.CmdSendMessageToChat(NetworkClient.localPlayer.connectionToServer.connectionId, message);

        // Limpiar el campo de texto
        chatInputField.text = "";
    }


    [ClientRpc]
    public void RpcReceiveMessage(int senderId, string message)
    {
        Debug.Log($"[NewChatManager] Mensaje recibido de {senderId}: {message}");

        if (chatDisplay != null)
        {
            chatDisplay.text += $"{senderId}: {message}\n";
        }
        else
        {
            Debug.LogError("[NewChatManager] chatDisplay no está asignado.");
        }
    }
}
