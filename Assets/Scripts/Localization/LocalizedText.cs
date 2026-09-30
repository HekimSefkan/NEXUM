using TMPro;
using UnityEngine;

/// <summary>
/// Sahnedeki bir TMP metnini sözlüğe bağlar. Dil değişince metin anında
/// güncellenir (<see cref="Loc.LanguageChanged"/> olayı).
///
/// Bileşen sahnelere elle değil, idempotent Editor scriptiyle eklenir:
/// <c>NEXUM → Metinleri Sözlüğe Bağla</c>.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Sözlük anahtarı (Assets/Data/Localization/tr.json)")]
    public string key;

    private TMP_Text target;

    private void Awake()
    {
        target = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Loc.LanguageChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        Loc.LanguageChanged -= Apply;
    }

    /// <summary>Metni geçerli dile göre yazar.</summary>
    public void Apply()
    {
        if (string.IsNullOrEmpty(key)) return;
        if (target == null) target = GetComponent<TMP_Text>();
        if (target == null) return;

        target.text = Loc.Get(key);
    }
}
