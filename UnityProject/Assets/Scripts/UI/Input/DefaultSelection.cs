using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Marks the control a menu focuses first with a gamepad (e.g. Resume), instead of its top-left one.
[RequireComponent(typeof(Selectable))]
public class DefaultSelection : MonoBehaviour
{
    private static readonly List<DefaultSelection> Active = new List<DefaultSelection>();

    public Selectable Selectable { get; private set; }

    public static IReadOnlyList<DefaultSelection> All => Active;

    void Awake()
    {
        Selectable = GetComponent<Selectable>();
    }

    void OnEnable()
    {
        Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
    }
}
