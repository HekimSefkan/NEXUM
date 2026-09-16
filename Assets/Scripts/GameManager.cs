using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Mobil Kaydırma (Swipe) Ayarları")]
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private float swipeThreshold = 50f; // Kaydırmanın algılanması için gereken minimum piksel mesafesi
    private bool touchStartedOnSelectable = false; // Dokunuş bir butonun/kaydırıcının üzerinde başladıysa swipe sayılmaz
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    [Header("Joker Sistemi")]
    public bool isJokerModeActive = false;
    public Image jokerButtonImage; 

    [Header("Skor Sistemi")]
    public int currentScore = 0; 
    public TextMeshProUGUI scoreText; 

    void Update()
    {
        // 1. BİLGİSAYAR TESTLERİ İÇİN KLAVYE GİRDİLERİ
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) GridManager.Instance.Shift(Vector2.up);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) GridManager.Instance.Shift(Vector2.down);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) GridManager.Instance.Shift(Vector2.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) GridManager.Instance.Shift(Vector2.right);

        // 2. MOBİL CİHAZLAR İÇİN DOKUNMATİK (SWIPE) GİRDİLERİ
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0); // Ekrandaki ilk parmağı algıla

            if (touch.phase == TouchPhase.Began)
            {
                // Parmak ekrana ilk değdiğinde konumu kaydet
                startTouchPosition = touch.position;
                touchStartedOnSelectable = IsTouchOverSelectable(touch.position);
                endTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                // Parmak ekrandan kalktığında son konumu kaydet ve hesapla
                endTouchPosition = touch.position;
                if (!touchStartedOnSelectable) DetectSwipe();
            }
        }
    }

    // Parmağın hangi yöne çekildiğini hesaplayan matematiksel fonksiyon
    private void DetectSwipe()
    {
        float xDistance = endTouchPosition.x - startTouchPosition.x;
        float yDistance = endTouchPosition.y - startTouchPosition.y;

        // Parmağın kat ettiği mesafe, belirlediğimiz barajı (threshold) geçti mi? (Yanlışlıkla dokunmaları engeller)
        float threshold = GetSwipeThreshold();
        if (Mathf.Abs(xDistance) > threshold || Mathf.Abs(yDistance) > threshold)
        {
            // Yatayda mı daha çok kaydırmış, dikeyde mi?
            if (Mathf.Abs(xDistance) > Mathf.Abs(yDistance))
            {
                // Yatay Kaydırma
                if (xDistance > 0) GridManager.Instance.Shift(Vector2.right); // Sağa
                else GridManager.Instance.Shift(Vector2.left); // Sola
            }
            else
            {
                // Dikey Kaydırma
                if (yDistance > 0) GridManager.Instance.Shift(Vector2.up); // Yukarı
                else GridManager.Instance.Shift(Vector2.down); // Aşağı
            }
        }
    }

    // Eşik cihazın fiziksel boyutuna göre ölçeklenir; swipeThreshold alt sınır olarak kalır
    private float GetSwipeThreshold()
    {
        if (Screen.dpi > 0f) return Mathf.Max(swipeThreshold, Screen.dpi * 0.25f);
        return Mathf.Max(swipeThreshold, Screen.width * 0.05f);
    }

    // Dokunuşun en üstteki UI hedefi bir Selectable (Button, Toggle, Slider, Scrollbar, InputField) içinde mi?
    // Grid hücreleri ve taşlar da UI Image olduğu için IsPointerOverGameObject yerine sadece Selectable aranır.
    private bool IsTouchOverSelectable(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        var pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);
        if (raycastResults.Count == 0) return false;

        GameObject topHit = raycastResults[0].gameObject;
        return topHit != null && topHit.GetComponentInParent<Selectable>() != null;
    }

    public void ToggleJokerMode()
    {
        isJokerModeActive = !isJokerModeActive;

        if (isJokerModeActive)
        {
            jokerButtonImage.color = Color.red;
            Debug.Log("Joker Modu AKTİF! Silmek istediğin elemente tıkla.");
        }
        else
        {
            jokerButtonImage.color = Color.white;
            Debug.Log("Joker Modu İPTAL EDİLDİ.");
        }
    }
    
    public void ResetJokerMode()
    {
        isJokerModeActive = false;
        jokerButtonImage.color = Color.white;
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    public bool SpendScore(int amount)
    {
        if (currentScore >= amount)
        { 
            currentScore -= amount; 
            UpdateScoreUI();
            return true; 
        }
        else
        {
            Debug.Log("Yetersiz Puan! Gereken: " + amount + " / Sende olan: " + currentScore);
            return false; 
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "SKOR: " + currentScore.ToString();
        }
    }

    public void SubtractScore(int amount) 
    { 
        currentScore -= amount; 
        UpdateScoreUI(); 
    }
}