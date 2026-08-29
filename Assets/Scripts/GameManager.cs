using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Mobil Kaydırma (Swipe) Ayarları")]
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private float swipeThreshold = 50f; // Kaydırmanın algılanması için gereken minimum piksel mesafesi

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
                endTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                // Parmak ekrandan kalktığında son konumu kaydet ve hesapla
                endTouchPosition = touch.position;
                DetectSwipe();
            }
        }
    }

    // Parmağın hangi yöne çekildiğini hesaplayan matematiksel fonksiyon
    private void DetectSwipe()
    {
        float xDistance = endTouchPosition.x - startTouchPosition.x;
        float yDistance = endTouchPosition.y - startTouchPosition.y;

        // Parmağın kat ettiği mesafe, belirlediğimiz barajı (threshold) geçti mi? (Yanlışlıkla dokunmaları engeller)
        if (Mathf.Abs(xDistance) > swipeThreshold || Mathf.Abs(yDistance) > swipeThreshold)
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