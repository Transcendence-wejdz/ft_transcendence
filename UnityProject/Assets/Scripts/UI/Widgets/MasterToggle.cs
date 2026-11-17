using UnityEngine;
using UnityEngine.UI;

// Master toggle that quickly toggles children, and can on/off them too
[RequireComponent(typeof(Toggle))]
public class MasterToggle : MonoBehaviour
{
    [SerializeField] private Toggle[] children;

    private Toggle master;

    private void Awake()
    {
        master = GetComponent<Toggle>();
        master.onValueChanged.AddListener(SetChildren);
        foreach (Toggle child in children)
            child.onValueChanged.AddListener(_ => Sync());
    }

    private void SetChildren(bool status)
    {
        foreach (Toggle child in children)
            child.isOn = status;
    }

    public void Sync()
    {
        bool allOn = true;

        foreach (Toggle child in children)
            allOn &= child.isOn;
        master.SetIsOnWithoutNotify(allOn);
    }
}
