using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[RequireComponent(typeof(PlayerInput))]
public class InteractionManager : MonoBehaviour
{
    private static readonly List<InteractableZone> ActiveZones = new();

    private InteractableZone currentZone;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private Player player;
    private MenuPanel interactionPanel;

    // Every player copy carries this and shares the same actions; only the locally controlled one may react.
    private bool IsLocal => player == null || player.IsLocallyControlled();

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

    private void Update()
    {
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

        InteractableZone nearest = GetNearestZone();
        if (nearest != currentZone)
        {
            currentZone?.HidePrompt();
            currentZone = nearest;
            currentZone?.ShowPrompt();
        }
    }

    private InteractableZone GetNearestZone()
    {
        InteractableZone nearest = null;
        float bestSqrDist = float.MaxValue;

        foreach (InteractableZone zone in ActiveZones)
        {
            float sqrDist = (zone.Position - transform.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                nearest = zone;
            }
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
            if (IsUIControl(ctx.control))
                return;

            interactionPanel.Close();
            interactionPanel = null;
            return;
        }

        if (PauseManager.IsPaused)
            return;

        currentZone?.Interact();
        interactionPanel = MenuPanel.CurrentOpen;
    }

    // A key shared with UI navigation (e.g. Interact rebound to Cross) stays with the UI instead of closing the panel.
    private static bool IsUIControl(InputControl control)
    {
        InputSystemUIInputModule module = EventSystem.current != null ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
        return module != null && (Uses(module.submit, control) || Uses(module.cancel, control) || Uses(module.move, control));
    }

    private static bool Uses(InputActionReference reference, InputControl control)
    {
        InputAction action = reference != null ? reference.action : null;
        if (action == null)
            return false;

        foreach (InputControl bound in action.controls)
        {
            if (bound == control)
                return true;
        }
        return false;
    }

    private static bool IsTyping()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        TMP_InputField field = selected != null ? selected.GetComponent<TMP_InputField>() : null;
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
}
