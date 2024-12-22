using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class NewChatManager : NetworkBehaviour
{
    [Header("Chat UI Components")]
    public GameObject chatCanvas;
    public TMP_InputField chatInputField;
    public TMP_Text chatDisplay;
    public Button sendButton;

    private void Start()
    {
        // Ocultar el canvas al inicio
        chatCanvas.SetActive(false);

        Debug.Log("NewChatManager inicializado en: " + gameObject.name);

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
        if (string.IsNullOrEmpty(chatInputField.text)) return;

        CmdSendMessage(chatInputField.text);
        chatInputField.text = ""; // Limpiar el campo de texto
    }

    [Command]
    private void CmdSendMessage(string message)
    {
        Debug.Log($"[NewChatManager] Recibido mensaje del cliente {connectionToClient.connectionId}: {message}");

        // Propagar el mensaje a todos los clientes conectados
        RpcReceiveMessage(connectionToClient.connectionId, message);
    }

    [ClientRpc]
    private void RpcReceiveMessage(int senderId, string message)
    {
        if (chatDisplay != null)
        {
            chatDisplay.text += $"{senderId}: {message}\n";
        }

        Debug.Log($"[NewChatManager] Mensaje recibido: {senderId}: {message}");
    }
}

