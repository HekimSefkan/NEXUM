using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Tarif defteri panelinin ve iki erişim butonunun dört canvas boyutunda
/// yerleşim ölçümü: taşma, çakışma, metin kırpılması ve %85 doluluk kuralı.
///
/// Ölçüm kodu burada değil, ortak yardımcıda: <see cref="LayoutCheck"/>.
/// </summary>
public class RecipeBookLayoutTests
{
    private const string Tag = "NEXUM_RECIPEBOOK_LAYOUT";

    private const string PanelPath = "SafeAreaRoot/RecipeBookPanel";

    /// <summary>Bu adımın eklediği obje: ölçüm kuralları buna uygulanır.</summary>
    private const string Yeni = "RecipeBookButton";

    /// <summary>Panelin kendi parçaları (çakışmaması gerekenler).</summary>
    private static readonly string[] PanelParts =
    {
        "TitleText", "ProgressRing", "CounterText", "Scroll View", "BackButton",
    };

    private RectTransform safeRoot;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour menu = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Assert.IsNotNull(menu, "MainMenuManager yok");

        Reflect.Call(menu, "OpenRecipeBook");
        yield return new WaitForSeconds(1f);

        safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");
    }

    // ------------------------------------------------------------- yardımcı

    private Vector4 SafeBounds(Vector2 size)
    {
        return new Vector4(-size.x * 0.5f, -size.y * 0.5f, size.x * 0.5f, size.y * 0.5f);
    }

    private static List<TMP_Text> Metinler(Transform kok)
    {
        return kok.GetComponentsInChildren<TMP_Text>(true)
            .Where(t => t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text))
            .ToList();
    }

    // -------------------------------------------------------------- testler

    // Panel parçaları güvenli alanda kalır ve birbirleriyle çakışmaz.
    [UnityTest]
    public IEnumerator PanelDortBoyuttaTemiz()
    {
        Transform panel = LayoutCheck.Find(PanelPath);
        Assert.IsNotNull(panel, "RecipeBookPanel yok (NEXUM -> Tarif Defterini Kur)");

        List<string> hatalar = new List<string>();

        foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, boyut);
            yield return null;
            yield return null;

            string ekran = $"{boyut.x:F0}x{boyut.y:F0}";
            Vector4 sinir = SafeBounds(boyut);

            List<RectTransform> parcalar = new List<RectTransform>();
            foreach (string ad in PanelParts)
            {
                Transform t = panel.Find(ad);
                Assert.IsNotNull(t, "panel parçası yok: " + ad);
                RectTransform rt = (RectTransform)t;
                parcalar.Add(rt);

                Vector4 r = LayoutCheck.LocalRect(safeRoot, rt);
                float tasma = LayoutCheck.Overflow(r, sinir);
                Debug.Log($"{Tag}: {ekran} {LayoutCheck.Describe(ad, r)} tasma={tasma:F0}");
                if (tasma > 1f) hatalar.Add($"{ekran}: {ad} {tasma:F0} birim taştı");
            }

            foreach (string cakisma in LayoutCheck.FindOverlaps(safeRoot, parcalar))
            {
                hatalar.Add($"{ekran}: cakisma {cakisma}");
            }
        }

        Debug.Log($"{Tag}_SONUC: sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    // Panel metinleri kırpılmaz ve kutularının %85'ini aşmaz.
    [UnityTest]
    public IEnumerator PanelMetinleriSigar()
    {
        Transform panel = LayoutCheck.Find(PanelPath);
        Assert.IsNotNull(panel, "RecipeBookPanel yok");

        List<string> hatalar = new List<string>();
        float enKotu = 0f;
        string enKotuAd = "-";

        foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, boyut);
            yield return null;
            yield return null;

            string ekran = $"{boyut.x:F0}x{boyut.y:F0}";

            foreach (TMP_Text tmp in Metinler(panel))
            {
                string detay;
                bool kirpik = LayoutCheck.IsClipped(tmp, out detay);
                float doluluk = LayoutCheck.FillRatio(tmp, tmp.text);

                string yol = tmp.transform.parent != null
                    ? tmp.transform.parent.name + "/" + tmp.name
                    : tmp.name;

                if (doluluk > enKotu) { enKotu = doluluk; enKotuAd = $"{ekran} {yol}"; }

                if (kirpik) hatalar.Add($"{ekran}: {yol} kırpık ({detay})");
                if (doluluk > LayoutCheck.MaxFillRatio)
                {
                    hatalar.Add($"{ekran}: {yol} doluluk %{doluluk * 100f:F0} " +
                                $"(sınır %{LayoutCheck.MaxFillRatio * 100f:F0})");
                }
            }
        }

        Debug.Log($"{Tag}_DOLULUK: en kotu %{enKotu * 100f:F0} ({enKotuAd}), " +
                  $"sinir %{LayoutCheck.MaxFillRatio * 100f:F0}");
        Debug.Log($"{Tag}_DOLULUK_SONUC: sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    // Ana menüdeki erişim butonu: güvenli alanda, diğer menü butonlarıyla çakışmaz.
    [UnityTest]
    public IEnumerator AnaMenuButonuTemiz()
    {
        MonoBehaviour menu = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Reflect.Call(menu, "BackToMainMenu");
        yield return new WaitForSeconds(1f);

        Transform menuPanel = LayoutCheck.Find("SafeAreaRoot/MainMenuPanel");
        Assert.IsNotNull(menuPanel, "MainMenuPanel yok");

        List<string> hatalar = new List<string>();

        foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safeRoot, boyut);
            yield return null;
            yield return null;

            string ekran = $"{boyut.x:F0}x{boyut.y:F0}";
            Vector4 sinir = SafeBounds(boyut);

            List<RectTransform> butonlar = new List<RectTransform>();
            foreach (Transform t in menuPanel)
            {
                if (!t.gameObject.activeSelf) continue;
                RectTransform rt = t as RectTransform;
                if (rt == null) continue;
                butonlar.Add(rt);

                Vector4 r = LayoutCheck.LocalRect(safeRoot, rt);
                float tasma = LayoutCheck.Overflow(r, sinir);
                if (t.name == "RecipeBookButton")
                {
                    Debug.Log($"{Tag}_MENU: {ekran} {LayoutCheck.Describe(t.name, r)} tasma={tasma:F0}");
                }
                if (tasma > 1f)
                {
                    string satir = $"{ekran}: {t.name} {tasma:F0} birim taştı";
                    if (t.name == Yeni) hatalar.Add(satir);
                    else Debug.Log($"{Tag}_MEVCUT: {satir}");
                }
            }

            foreach (string cakisma in LayoutCheck.FindOverlaps(safeRoot, butonlar))
            {
                string satir = $"{ekran}: cakisma {cakisma}";
                if (cakisma.Contains(Yeni)) hatalar.Add(satir);
                else Debug.Log($"{Tag}_MEVCUT: {satir}");
            }
        }

        Debug.Log($"{Tag}_MENU_SONUC: sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    // Duraklat menüsündeki erişim butonu: kutusunda kalır, kardeşleriyle çakışmaz.
    [UnityTest]
    public IEnumerator DuraklatButonuTemiz()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        Transform kutu = null;
        foreach (GameObject kok in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (kok.name != "Canvas") continue;
            Transform pause = kok.transform.Find("PausePanel");
            Assert.IsNotNull(pause, "PausePanel yok");
            pause.gameObject.SetActive(true);
            kutu = pause.Find("ContainerBox");
        }
        Assert.IsNotNull(kutu, "ContainerBox yok");
        yield return null;
        yield return null;

        RectTransform kutuRect = (RectTransform)kutu;
        List<string> hatalar = new List<string>();

        RectTransform safe = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safe, "SafeAreaRoot yok");

        foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
        {
            LayoutCheck.SetCanvasSize(safe, boyut);
            yield return null;
            yield return null;

            string ekran = $"{boyut.x:F0}x{boyut.y:F0}";
            Vector4 sinir = LayoutCheck.LocalRect(kutuRect, kutuRect);

            List<RectTransform> kardesler = new List<RectTransform>();
            foreach (Transform t in kutu)
            {
                if (!t.gameObject.activeSelf) continue;
                RectTransform rt = t as RectTransform;
                if (rt == null) continue;
                kardesler.Add(rt);

                Vector4 r = LayoutCheck.LocalRect(kutuRect, rt);
                float tasma = LayoutCheck.Overflow(r, sinir);
                if (t.name == "RecipeBookButton")
                {
                    Debug.Log($"{Tag}_PAUSE: {ekran} {LayoutCheck.Describe(t.name, r)} " +
                              $"kutu x {sinir.x:F0}..{sinir.z:F0} y {sinir.y:F0}..{sinir.w:F0} tasma={tasma:F0}");
                }

                // Bu adımın sorumluluğu YENİ buton; duraklat kutusunda önceden
                // var olan taşmalar (ör. TitleText) raporlanır ama testi düşürmez.
                if (tasma > 1f)
                {
                    string satir = $"{ekran}: {t.name} kutudan {tasma:F0} birim taştı";
                    if (t.name == Yeni) hatalar.Add(satir);
                    else Debug.Log($"{Tag}_MEVCUT: {satir}");
                }
            }

            foreach (string cakisma in LayoutCheck.FindOverlaps(kutuRect, kardesler))
            {
                string satir = $"{ekran}: cakisma {cakisma}";
                if (cakisma.Contains(Yeni)) hatalar.Add(satir);
                else Debug.Log($"{Tag}_MEVCUT: {satir}");
            }
        }

        Debug.Log($"{Tag}_PAUSE_SONUC: sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }
}
