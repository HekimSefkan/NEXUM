#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AutoAnchorTools : EditorWindow
{
    // Üst menüye tek tıkla çalışan bir buton ekler
    [MenuItem("NEXUM/Seçili Objeleri Tam Bulunduğu Yere Çıpala")]
    static void AnchorToCorners()
    {
        int changedCount = 0;

        // Hiyerarşide seçili olan tüm objeleri tarar
        foreach (Transform transform in Selection.transforms)
        {
            RectTransform t = transform as RectTransform;
            RectTransform pt = transform.parent as RectTransform;

            if (t == null || pt == null) continue;

            // Ctrl+Z (Geri al) yapabilmen için işlemi hafızaya kaydeder
            Undo.RecordObject(t, "Auto Anchor");

            // Objenin ekrandaki mevcut pozisyonuna göre yeni çapaları matematiksel olarak hesaplar
            Vector2 newAnchorsMin = new Vector2(t.anchorMin.x + t.offsetMin.x / pt.rect.width,
                                                t.anchorMin.y + t.offsetMin.y / pt.rect.height);
            Vector2 newAnchorsMax = new Vector2(t.anchorMax.x + t.offsetMax.x / pt.rect.width,
                                                t.anchorMax.y + t.offsetMax.y / pt.rect.height);

            // Çapaları objenin tam 4 köşesine sabitler
            t.anchorMin = newAnchorsMin;
            t.anchorMax = newAnchorsMax;
            
            // Kenar boşluklarını sıfırlar (Artık obje çapalara %100 bağımlıdır)
            t.offsetMin = t.offsetMax = new Vector2(0, 0);

            changedCount++;
        }

        Debug.Log($"<color=#00B4D8><b>NEXUM:</b></color> <b>{changedCount}</b> adet arayüz objesi tam bulundukları konuma kusursuzca çıpalandı!");
    }
}
#endif