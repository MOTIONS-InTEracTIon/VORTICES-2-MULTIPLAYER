using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerChatController : NetworkBehaviour
{
    [Command]
    public void CmdSendMessageToChat(int senderId, string message)
    {
        Debug.Log($"[PlayerChatController] Mensaje recibido del cliente {senderId}: {message}");

        // Buscar el ChatCanvas global en el servidor
        GameObject chatCanvas = GameObject.Find("ChatCanvas(Clone)");
        if (chatCanvas == null)
        {
            Debug.LogError("[PlayerChatController] ChatCanvas no encontrado.");
            return;
        }

        // Obtener el NewChatManager del ChatCanvas
        NewChatManager chatManager = chatCanvas.GetComponent<NewChatManager>();
        if (chatManager == null)
        {
            Debug.LogError("[PlayerChatController] NewChatManager no encontrado en ChatCanvas.");
            return;
        }

        // Propagar el mensaje a todos los clientes
        chatManager.RpcReceiveMessage(senderId, message);
    }
}


