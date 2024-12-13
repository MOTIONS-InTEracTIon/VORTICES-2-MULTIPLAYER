using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct RequestActiveSessionMessage : NetworkMessage
{
}

public struct ActiveSessionResponseMessage : NetworkMessage
{
    public bool success;
    public SessionData sessionData;
}

public struct SessionEndedMessage : NetworkMessage
{
    public string reason; // Razón de la finalización de la sesión
}