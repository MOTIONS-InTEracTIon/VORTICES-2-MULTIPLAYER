using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Vortices;

public class HandController : MonoBehaviour
{
    // Other references
    public Element selectElement;

    // Input
    [SerializeField] InputActionProperty aPress;


    private void Start()
    {
        aPress.action.started += SelectElement;

    }

    private void OnDisable()
    {
        aPress.action.started -= SelectElement;

    }

    #region Controller Actions

    // Select Actions
    public void SelectElement(InputAction.CallbackContext context)
    {
        if(selectElement != null)
        {
            selectElement.GetComponent<Element>().SelectElement();
        }
    }

    #endregion

}
