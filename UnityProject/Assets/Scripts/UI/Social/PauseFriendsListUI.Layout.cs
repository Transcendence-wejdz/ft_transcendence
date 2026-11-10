using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Section heights: each list asks for its content height and scrolls on its own when space runs out.
public partial class PauseFriendsListUI
{
    // Each section asks for its content height; when both don't fit, the layout shrinks them toward minScrollHeight and they scroll on their own.
    private void FitSection(LayoutElement scroll, RectTransform content, bool collapsed, int count)
    {
        bool visible = !collapsed && count > 0;
        float height;

        scroll.gameObject.SetActive(visible);
        if (!visible)
            return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        height = LayoutUtility.GetPreferredHeight(content);
        scroll.preferredHeight = height;
        scroll.minHeight = Mathf.Min(height, minScrollHeight);
        scroll.flexibleHeight = 0f;
    }

    private void FitSections()
    {
        FitSection(requestScroll, requestContent, requestsCollapsed, requestCount);
        FitSection(onlineScroll, onlineContent, onlineCollapsed, onlineCount);
        FitSection(offlineScroll, offlineContent, offlineCollapsed, offlineCount);
    }

    private static void ScrollToTop(LayoutElement scroll)
    {
        ScrollRect rect = scroll.GetComponent<ScrollRect>();

        if (rect == null || !rect.isActiveAndEnabled)
            return;
        rect.StopMovement();
        rect.verticalNormalizedPosition = 1f;
    }

    // Rows built in the frame the pause opens can be laid out against a 0-tall viewport; refit and snap each list back to its top once settled.
    private IEnumerator RefitNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        FitSections();
        ScrollToTop(requestScroll);
        ScrollToTop(onlineScroll);
        ScrollToTop(offlineScroll);
    }
}
