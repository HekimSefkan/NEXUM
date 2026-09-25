using UnityEngine;
using UnityEngine.EventSystems; // UI (Arayüz) tıklamaları için BÜYÜK SIR bu!
using DG.Tweening;

// IPointerDownHandler: "Eğer ben bir UI objesiysem ve bana tıklanırsa bunu algıla" demektir.
public class TileInteraction : MonoBehaviour, IPointerDownHandler
{
    [Header("Element Verisi")]
    public ElementData myElementData; // Şüpheli 2'deki aradığımız kutu bu!

    // Taşlar sık yaratıldığı için referans statik ve tembel olarak tutulur;
    // sahne değişince eski referans Unity tarafından null sayılır ve yeniden aranır.
    private static GameManager cachedManager;
    private static GameManager Manager
    {
        get
        {
            if (cachedManager == null) cachedManager = FindObjectOfType<GameManager>();
            return cachedManager;
        }
    }

    // OnMouseDown YERİNE artık UI sisteminin kendi tıklama algılayıcısını kullanıyoruz
    public void OnPointerDown(PointerEventData eventData)
    {
        GameManager manager = Manager;

        // Eğer GameManager varsa ve Joker Modu açıksa
        if (manager != null && manager.isJokerModeActive)
        {
            if (myElementData == null)
            {
                Debug.LogError("Bu taşa Element Data atanmamış! Inspector'dan atamalısın.");
                return;
            }

            int cost = myElementData.jokerCost;

            // Paramız yetiyorsa taşı parçala
            if (manager.SpendScore(cost))
            {
                Debug.Log($"{myElementData.elementName} parçalandı! -{cost} Puan.");

                transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    Destroy(gameObject);
                });

                manager.ResetJokerMode(); // Şalteri kapat
            }
        }
    }
}