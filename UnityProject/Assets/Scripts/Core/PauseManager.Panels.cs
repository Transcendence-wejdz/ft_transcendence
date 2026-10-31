using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pause panels: slide-in aware show/hide and the context-aware Leave/Quit buttons.
public partial class PauseManager
{
    // Animated when the panel has a PanelSlideIn, instant otherwise.
    private static void SetPanelVisible(GameObject panel, bool show)
    {
        PanelSlideIn slide;

        if (panel == null)
            return;
        slide = panel.GetComponent<PanelSlideIn>();
        if (slide == null)
            panel.SetActive(show);
        else if (show)
            slide.Show();
        else
            slide.Hide();
    }

    // A panel still playing its close animation counts as closed.
    private static bool IsPanelOpen(GameObject panel)
    {
        PanelSlideIn slide;

        if (panel == null || !panel.activeSelf)
            return false;
        slide = panel.GetComponent<PanelSlideIn>();
        return slide == null || slide.IsOpen;
    }

    private void RefreshLeaveButton()
    {
        TMP_Text label;

        leaveButton.gameObject.SetActive(!InLobby || IsOnline);
        label = leaveButton.GetComponentInChildren<TMP_Text>();
        if (label == null)
            return;
        if (LeavesSession)
            label.text = "Leave Session";
        else
            label.text = "Return to Lobby";
    }

    // Shows the leave button when there is somewhere to go; hides Quit on WebGL.
    private void RefreshButtons()
    {
        buttonsShownOnline = IsOnline;
        if (leaveButton != null)
            RefreshLeaveButton();
        if (quitButton != null)
            quitButton.gameObject.SetActive(Application.platform != RuntimePlatform.WebGLPlayer);
    }
}
