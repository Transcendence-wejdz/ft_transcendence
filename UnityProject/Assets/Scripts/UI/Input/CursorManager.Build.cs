using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Runtime build of the cursor canvas, pointer, arrow and crosshair images.
public partial class CursorManager
{
    private static Image CreateImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Image image = go.GetComponent<Image>();

        go.transform.SetParent(parent, false);
        image.raycastTarget = false;
        image.rectTransform.anchorMin = Center;
        image.rectTransform.anchorMax = Center;
        image.rectTransform.anchoredPosition = Vector2.zero;
        return image;
    }

    private bool LoadSprites()
    {
        defaultSprite = Resources.Load<Sprite>(DefaultCursorResourcePath);
        hoverSprite = Resources.Load<Sprite>(HoverCursorResourcePath);
        if (defaultSprite == null)
        {
            Debug.LogWarning($"CursorManager: could not load cursor sprite at Resources/{DefaultCursorResourcePath}");
            return false;
        }
        if (hoverSprite == null)
            hoverSprite = defaultSprite;
        return true;
    }

    private Transform BuildCanvas()
    {
        GameObject canvasGO = new GameObject("CursorCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();

        canvasGO.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CursorSortingOrder;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        canvasRect = canvasGO.GetComponent<RectTransform>();
        return canvasGO.transform;
    }

    private void BuildPointer(Transform canvas)
    {
        GameObject pointerGO = new GameObject("Pointer", typeof(RectTransform));

        pointerGO.transform.SetParent(canvas, false);
        pointerRect = pointerGO.GetComponent<RectTransform>();
        pointerRect.anchorMin = Center;
        pointerRect.anchorMax = Center;
        pointerRect.sizeDelta = Vector2.zero;
        arrowImage = CreateImage("Arrow", pointerRect);
        arrowRect = arrowImage.rectTransform;
        crosshairRect = new GameObject("Crosshair", typeof(RectTransform)).GetComponent<RectTransform>();
        crosshairRect.SetParent(pointerRect, false);
        crosshairOutline = CreateImage("Outline", crosshairRect);
        crosshairFill = CreateImage("Fill", crosshairRect);
        crosshairRect.gameObject.SetActive(false);
    }
}
