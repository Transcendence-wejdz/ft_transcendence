using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[RequireComponent(typeof(PlayerInput))]
public class InteractionManager : MonoBehaviour
{
    private static readonly List<InteractableZone> ActiveZones = new List<InteractableZone>();

    private InteractableZone currentZone;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private Player player;
    private MenuPanel interactionPanel;

    // Every player copy carries this and shares the same actions; only the locally controlled one may react.
    private bool IsLocal => player == null || player.IsLocallyControlled();

    private InteractableZone GetNearestZone()
    {
        InteractableZone nearest = null;
        float bestSqrDist = float.MaxValue;
        float sqrDist;

        foreach (InteractableZone zone in ActiveZones)
        {
            sqrDist = (zone.Position - transform.position).sqrMagnitude;
            if (sqrDist >= bestSqrDist)
                continue;
            bestSqrDist = sqrDist;
            nearest = zone;
        }
        return nearest;
    }

    // Closes the panel this key opened (the pause key still closes it too), otherwise interacts; ignored while typing in a text field.
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsLocal || IsTyping())
            return;
        if (interactionPanel != null && MenuPanel.CurrentOpen == interactionPanel)
        {
            CloseInteractionPanel(ctx.control);
            return;
        }
        if (PauseManager.IsPaused)
            return;
        currentZone?.Interact();
        interactionPanel = MenuPanel.CurrentOpen;
    }

    private void CloseInteractionPanel(InputControl control)
    {
        if (IsUIControl(control))
            return;
        interactionPanel.Close();
        interactionPanel = null;
    }

    private void SetCurrentZone(InteractableZone zone)
    {
        currentZone?.HidePrompt();
        currentZone = zone;
        currentZone?.ShowPrompt();
    }

    private static InputSystemUIInputModule GetUIModule()
    {
        if (EventSystem.current == null)
            return null;
        return EventSystem.current.currentInputModule as InputSystemUIInputModule;
    }

    // A key shared with UI navigation (e.g. Interact rebound to Cross) stays with the UI instead of closing the panel.
    private static bool IsUIControl(InputControl control)
    {
        InputSystemUIInputModule module = GetUIModule();

        if (module == null)
            return false;
        return Uses(module.submit, control) || Uses(module.cancel, control) || Uses(module.move, control);
    }

    private static bool Uses(InputActionReference reference, InputControl control)
    {
        InputAction action;

        if (reference == null || reference.action == null)
            return false;
        action = reference.action;
        foreach (InputControl bound in action.controls)
        {
            if (bound == control)
                return true;
        }
        return false;
    }

    private static bool IsTyping()
    {
        GameObject selected;
        TMP_InputField field;

        if (EventSystem.current == null)
            return false;
        selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return false;
        field = selected.GetComponent<TMP_InputField>();
        return field != null && field.isFocused;
    }

    public static void Register(InteractableZone zone)
    {
        if (!ActiveZones.Contains(zone))
            ActiveZones.Add(zone);
    }

    public static void Unregister(InteractableZone zone)
    {
        ActiveZones.Remove(zone);
    }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        player = GetComponent<Player>();
    }

    private void OnEnable()
    {
        interactAction = playerInput.actions.FindAction("Interact");
        if (interactAction != null)
            interactAction.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;
        currentZone?.HidePrompt();
        currentZone = null;
    }

    // Paused: the prompt hides; otherwise it follows the nearest zone.
    private void Update()
    {
        InteractableZone nearest;

        if (!IsLocal)
            return;
        if (PauseManager.IsPaused)
        {
            if (currentZone != null)
            {
                currentZone.HidePrompt();
                currentZone = null;
            }
            return;
        }
        nearest = GetNearestZone();
        if (nearest != currentZone)
            SetCurrentZone(nearest);
    }
}
