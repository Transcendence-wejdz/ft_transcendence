using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runtime build of the loading screen: black backdrop, label and progress bar.
public partial class LoadingScreenManager
{
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void PlaceCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = Center;
        rect.anchorMax = Center;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));

        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private GameObject BuildCanvas()
    {
        GameObject canvasGO = new GameObject("LoadingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();

        canvasGO.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGroup = canvasGO.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        return canvasGO;
    }

    private void BuildLabel(Transform parent)
    {
        GameObject labelGO = new GameObject("LoadingLabel", typeof(RectTransform), typeof(TextMeshProUGUI));

        labelGO.transform.SetParent(parent, false);
        PlaceCentered(labelGO.GetComponent<RectTransform>(), new Vector2(0f, 40f), new Vector2(400f, 60f));
        loadingLabel = labelGO.GetComponent<TextMeshProUGUI>();
        loadingLabel.text = DefaultLabel;
        loadingLabel.fontSize = 36f;
        loadingLabel.alignment = TextAlignmentOptions.Center;
        loadingLabel.color = Color.white;
    }

    private void BuildProgressBar(Transform parent)
    {
        GameObject barBackground = CreateImage("ProgressBarBackground", parent, new Color(1f, 1f, 1f, 0.15f));
        GameObject barFill = CreateImage("ProgressBarFill", barBackground.transform, new Color(0.85f, 0.2f, 0.2f));

        PlaceCentered(barBackground.GetComponent<RectTransform>(), new Vector2(0f, -20f), new Vector2(400f, 24f));
        progressBarRoot = barBackground;
        Stretch(barFill.GetComponent<RectTransform>());
        progressFill = barFill.GetComponent<Image>();
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;
    }

    private void BuildUI()
    {
        GameObject canvasGO = BuildCanvas();
        GameObject background = CreateImage("Background", canvasGO.transform, Color.black);

        Stretch(background.GetComponent<RectTransform>());
        BuildLabel(canvasGO.transform);
        BuildProgressBar(canvasGO.transform);
    }
}
