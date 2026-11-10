using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class InteractableZone : MonoBehaviour
{
    [SerializeField] private string promptText = "Enter";
    [SerializeField] private InteractPromptUI promptUI;
    [SerializeField] private UnityEvent onInteract;

    public Vector3 Position => transform.position;

    // Filters out remote players - the zone registry is shared, so their movement must not affect it.
    private static bool IsLocalPlayer(Collider2D other)
    {
        NetworkObject networkObject;

        if (!other.CompareTag("Player"))
            return false;
        networkObject = other.GetComponentInParent<NetworkObject>();
        return networkObject == null || !networkObject.IsSpawned || networkObject.IsOwner;
    }

    public void ShowPrompt()
    {
        if (promptUI != null)
            promptUI.Show(promptText);
    }

    public void HidePrompt()
    {
        if (promptUI != null)
            promptUI.Hide();
    }

    public void Interact()
    {
        onInteract?.Invoke();
    }

    private void Start()
    {
        if (promptUI != null)
            promptUI.SetLabel(promptText);
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsLocalPlayer(other))
            InteractionManager.Register(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsLocalPlayer(other))
            InteractionManager.Unregister(this);
    }

    // SetActive(false) on this object or a parent never fires OnTriggerExit2D, which would otherwise leave this zone stuck in the registry forever.
    private void OnDisable()
    {
        InteractionManager.Unregister(this);
    }
}
