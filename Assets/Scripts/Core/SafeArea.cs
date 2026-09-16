using UnityEngine;

// RectTransform'u cihazın güvenli alanına (Screen.safeArea: çentik, kamera deliği, sistem çubukları) göre anchor'lar.
// Canvas altında tam ekran (stretch) bir kök objeye eklenmeli; HUD bu objenin çocukları olmalı.
// Güncelleme yalnızca güvenli alan, ekran boyutu veya yön değiştiğinde yapılır.
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScreenOrientation lastOrientation;
    private bool hasApplied = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplyIfChanged();
    }

    private void Update()
    {
        ApplyIfChanged();
    }

    private void ApplyIfChanged()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation orientation = Screen.orientation;

        if (hasApplied && safeArea == lastSafeArea && screenSize == lastScreenSize && orientation == lastOrientation) return;
        if (screenSize.x <= 0 || screenSize.y <= 0) return;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        lastOrientation = orientation;
        hasApplied = true;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= screenSize.x;
        anchorMin.y /= screenSize.y;
        anchorMax.x /= screenSize.x;
        anchorMax.y /= screenSize.y;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
