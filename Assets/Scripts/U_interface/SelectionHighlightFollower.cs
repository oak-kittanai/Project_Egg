using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectionHighlightFollower : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.4f, 0.35f);
    [SerializeField] private float padding = 6f;

    private RectTransform highlightRect;
    private Image highlightImage;
    private GameObject currentSelected;

    private void LateUpdate()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        if (selected != currentSelected)
        {
            currentSelected = selected;
            HandleSelectionChanged();
        }

        UpdateHighlightTransform();
    }

    private void HandleSelectionChanged()
    {
        if (currentSelected == null)
        {
            if (highlightImage != null) highlightImage.gameObject.SetActive(false);
            return;
        }

        var targetRect = currentSelected.GetComponent<RectTransform>();
        if (targetRect == null)
        {
            if (highlightImage != null) highlightImage.gameObject.SetActive(false);
            return;
        }

        var targetCanvas = targetRect.GetComponentInParent<Canvas>();
        if (targetCanvas == null) return;

        EnsureHighlightExists();
        if (highlightRect.parent != targetCanvas.transform)
            highlightRect.SetParent(targetCanvas.transform, false);

        highlightRect.SetAsLastSibling();
        highlightImage.gameObject.SetActive(true);
    }

    private void EnsureHighlightExists()
    {
        if (highlightImage != null) return;

        var go = new GameObject("SelectionHighlight", typeof(RectTransform), typeof(Image));
        highlightRect = go.GetComponent<RectTransform>();
        highlightImage = go.GetComponent<Image>();
        highlightImage.color = highlightColor;
        highlightImage.raycastTarget = false;
    }

    private void UpdateHighlightTransform()
    {
        if (highlightImage == null || !highlightImage.gameObject.activeSelf || currentSelected == null) return;

        var targetRect = currentSelected.GetComponent<RectTransform>();
        if (targetRect == null) return;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        highlightRect.position = (corners[0] + corners[2]) / 2f;
        highlightRect.rotation = targetRect.rotation;

        float width = Vector3.Distance(corners[0], corners[3]);
        float height = Vector3.Distance(corners[0], corners[1]);
        float localScale = Mathf.Max(highlightRect.lossyScale.x, 0.0001f);

        highlightRect.sizeDelta = new Vector2(width, height) / localScale + new Vector2(padding, padding) * 2f;
    }
}
