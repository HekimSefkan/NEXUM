using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public struct GuideCategory
{
    public string title;
    [TextArea(4, 10)]
    public string description;
}

public class HowToPlayManager : MonoBehaviour
{
    [Header("Rehber Veritabanı")]
    public GuideCategory[] guideCategories; // Unity'den dolduracağımız içerikler

    [Header("Pop-up Arayüz Bağlantıları")]
    public GameObject guidePopupPanel;
    public TextMeshProUGUI popupTitleText;
    public TextMeshProUGUI popupDescText;
    public CanvasGroup popupCanvasGroup;

    void Start()
    {
        // Başlangıçta pop-up gizli olmalı
        if (guidePopupPanel != null)
        {
            guidePopupPanel.SetActive(false);
        }
    }

    // Butonlara tıklandığında çalışacak fonksiyon (İndeks 0, 1, 2... alacak)
    public void OpenGuideCategory(int index)
    {
        if (index < 0 || index >= guideCategories.Length) return;

        // Tıklama sesi (Eğer AudioManager sahnedeyse)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonSound();
        }

        // Verileri pop-up'a bas
        popupTitleText.text = guideCategories[index].title;
        popupDescText.text = guideCategories[index].description;

        // Pop-up'ı DOTween ile holografik şekilde aç
        guidePopupPanel.SetActive(true);
        popupCanvasGroup.alpha = 0f;
        guidePopupPanel.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        popupCanvasGroup.DOFade(1f, 0.3f);
        guidePopupPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    // KAPAT butonuna basıldığında çalışacak fonksiyon
    public void CloseGuidePopup()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonSound();
        }

        popupCanvasGroup.DOFade(0f, 0.2f);
        guidePopupPanel.transform.DOScale(new Vector3(0.8f, 0.8f, 1f), 0.2f).SetEase(Ease.InBack).OnComplete(() =>
        {
            guidePopupPanel.SetActive(false);
        });
    }
}