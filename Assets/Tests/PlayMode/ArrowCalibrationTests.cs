using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Tepkime okunun (TMP sprite'ı) ölçümlü kalibrasyonu.
///
/// Ok "→" (U+2192) proje fontlarının hiçbirinde yok ve atlaslar statik; bu
/// yüzden ok gömülü bir sprite. Sprite'ın boyu ve taban hizası GlyphRect +
/// scale + bearingY ile elle ayarlanıyor (<c>NexumSymbolSprites</c>), yani
/// ÖLÇÜMLE korunması gerekir: bu test sayı tutmazsa ok fontun yanında
/// küçük/büyük ya da kaymış demektir.
///
/// Hedef (kabul kriteri):
///   - ok yüksekliği = punto'nun %40'ı (±%5 pay)
///   - okun dikey merkezi, komşu harflerin optik merkezinden punto'nun
///     %3'ünden fazla sapmaz
/// Ölçüm dört canvas boyutunda ve iki dilde tekrarlanır.
/// </summary>
public class ArrowCalibrationTests
{
    private const string Tag = "NEXUM_ARROW";
    private const string TextType = "ReactionText";

    /// <summary>Okun hedef yüksekliği (punto oranı) ve payı.</summary>
    private const float TargetHeight = 0.40f;
    private const float HeightTolerance = 0.05f;

    /// <summary>Dikey merkez sapması en çok punto'nun %3'ü.</summary>
    private const float MaxCenterDrift = 0.03f;

    private Loc.Language onceki;
    private TMP_Text olcum;
    private RectTransform safeRoot;
    private MonoBehaviour display;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        onceki = Loc.Current;

        SaveService.Data.selectedLevel = 0;
        SaveService.Data.showReactionEquations = 1;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        display = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "ReactionEquationDisplay");
        Assert.IsNotNull(display, "ReactionEquationDisplay yok (NEXUM -> Tepkime Denklemini Kur)");

        olcum = display.GetComponentsInChildren<TMP_Text>(true).First();
        olcum.gameObject.SetActive(true);

        safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");
    }

    [TearDown]
    public void TearDown()
    {
        if (olcum != null)
        {
            olcum.text = "";
            olcum.gameObject.SetActive(false);
        }
        Reflect.Call(TextType, "ResetArrowCheck");
        Loc.SetLanguage(onceki);
    }

    // ------------------------------------------------------------- yardımcılar

    private static string Denklem(string a, string b, string c)
    {
        return Reflect.Call<string>(TextType, "Equation", a, b, c);
    }

    private struct Olcum
    {
        public float punto;
        public float okYukseklikOran;
        public float merkezSapmaOran;
        public bool okBulundu;
    }

    /// <summary>Ok ile komşu harfleri aynı satırda ölçer.</summary>
    private Olcum Olc()
    {
        // Metin tarif defteri / denklem gösterimi ile AYNI kaynaktan kurulur
        olcum.text = Denklem("H", "H", "H<sub>2</sub>");
        olcum.ForceMeshUpdate();

        TMP_TextInfo info = olcum.textInfo;
        float harfToplam = 0f, okMerkez = 0f, okYukseklik = 0f;
        int harf = 0;
        bool okVar = false;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) continue;

            float merkez = (c.topLeft.y + c.bottomLeft.y) * 0.5f;
            if (c.elementType == TMP_TextElementType.Sprite)
            {
                okVar = true;
                okMerkez = merkez;
                okYukseklik = c.topLeft.y - c.bottomLeft.y;
            }
            else
            {
                // Alt simge (<sub>) kendi hizasında; optik merkez için sayılmaz
                if (c.pointSize < olcum.fontSize * 0.9f) continue;
                harfToplam += merkez;
                harf++;
            }
        }

        Assert.Greater(harf, 0, "harf bulunamadı");

        float punto = olcum.fontSize;
        return new Olcum
        {
            punto = punto,
            okBulundu = okVar,
            okYukseklikOran = okYukseklik / punto,
            merkezSapmaOran = (okMerkez - harfToplam / harf) / punto,
        };
    }

    // ----------------------------------------------------------------- testler

    // Ok sprite'ı yüklenmeli (yoksa fallback devreye girer; o ayrı test).
    [UnityTest]
    public IEnumerator OkSpriteYuklenir()
    {
        Reflect.Call(TextType, "ResetArrowCheck");
        bool var_ = Reflect.Prop<bool>(TextType, "ArrowAvailable");
        string metin = Denklem("H", "H", "H<sub>2</sub>");

        Debug.Log($"{Tag}_YUKLEME: sprite={var_} metin=\"{metin}\"");

        Assert.IsTrue(var_, "ok sprite'ı yüklenmeli (NEXUM -> Sembol Sprite'larını Kur)");
        StringAssert.Contains("name=\"arrow\">", metin, "denklem sprite oku kullanmalı");
        yield return null;
    }

    // Dört canvas boyutu × iki dil: boy %40 ± %5, merkez sapması <= %3.
    [UnityTest]
    public IEnumerator OkDortBoyutIkiDildeHizali()
    {
        List<string> hatalar = new List<string>();

        foreach (Loc.Language dil in new[] { Loc.Language.Turkish, Loc.Language.English })
        {
            Loc.SetLanguage(dil);

            foreach (Vector2 boyut in LayoutCheck.CanvasSizes)
            {
                LayoutCheck.SetCanvasSize(safeRoot, boyut);
                yield return null;

                Olcum o = Olc();
                string ekran = $"{boyut.x:F0}x{boyut.y:F0}";

                Debug.Log($"{Tag}_HIZA: {dil} {ekran} punto={o.punto:F0} " +
                          $"yukseklik={o.okYukseklikOran * 100f:F1}% " +
                          $"merkez sapmasi={o.merkezSapmaOran * 100f:F1}%");

                if (!o.okBulundu) hatalar.Add($"{dil} {ekran}: ok sprite'ı çizilmedi");

                if (Mathf.Abs(o.okYukseklikOran - TargetHeight) > HeightTolerance)
                {
                    hatalar.Add($"{dil} {ekran}: ok boyu {o.okYukseklikOran * 100f:F1}% " +
                                $"(hedef {TargetHeight * 100f:F0}% ± {HeightTolerance * 100f:F0})");
                }

                if (Mathf.Abs(o.merkezSapmaOran) > MaxCenterDrift)
                {
                    hatalar.Add($"{dil} {ekran}: merkez sapması {o.merkezSapmaOran * 100f:F1}% " +
                                $"(sınır ±{MaxCenterDrift * 100f:F0}%)");
                }
            }
        }

        Debug.Log($"{Tag}_SONUC: sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    // Sprite yüklenemezse sessiz kalmaz: "->" kalıbına düşer.
    [UnityTest]
    public IEnumerator SpriteYoksaDuzMetneDuser()
    {
        // Yüklü asset boşaltılıp "denendi" bayrağı açık bırakılıyor; böylece
        // Resources'tan yeniden okumadan sprite yokmuş gibi davranır.
        Reflect.SetField(TextType, "arrowAsset", null);
        Reflect.SetField(TextType, "arrowChecked", true);

        bool var_ = Reflect.Prop<bool>(TextType, "ArrowAvailable");
        string metin = Denklem("H", "H", "H<sub>2</sub>");

        Debug.Log($"{Tag}_FALLBACK: sprite={var_} metin=\"{metin}\"");

        Assert.IsFalse(var_, "sprite yok sayılmalı");
        StringAssert.Contains("->", metin, "sprite yoksa düz metin oka düşülmeli");
        StringAssert.DoesNotContain("<sprite", metin, "sprite etiketi kalmamalı");

        // Gerçek duruma dön
        Reflect.Call(TextType, "ResetArrowCheck");
        Assert.IsTrue(Reflect.Prop<bool>(TextType, "ArrowAvailable"), "sprite geri yüklenmeli");
        yield return null;
    }

    // Üç gösterim de AYNI kalıptan geçer: denklem, tarif defteri, ansiklopedi.
    [UnityTest]
    public IEnumerator UcGosterimAyniKaynaktan()
    {
        Reflect.Call(TextType, "ResetArrowCheck");

        const string ok = "<sprite=\"NexumSymbols\" name=\"arrow\">";

        // 1) Denklem gösterimi (gerçek tarif üzerinden)
        MonoBehaviour grid = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        var recipes = (System.Collections.IList)Reflect.Get(grid, "recipes");
        string denklem = Reflect.Call<string>(display, "BuildEquation", recipes[0]);

        // 2) Tarif defteri satırı
        object katalog = Reflect.Call("RecipeCatalog", "Load");
        Assert.IsNotNull(katalog, "RecipeCatalog yok");
        var girdiler = (System.Collections.IList)Reflect.Get(katalog, "entries");
        object ilk = girdiler[0];
        string defter = Reflect.Call<string>(TextType, "Colored",
            Reflect.Get<string>(ilk, "inputASymbol"),
            Reflect.Get<string>(ilk, "inputBSymbol"),
            Reflect.Get<string>(ilk, "productSymbol"),
            Color.gray, Color.cyan);

        // 3) Ansiklopedi satırı (girdiler birleşik gelir)
        string ansiklopedi = Reflect.Call<string>(TextType, "Equation", "H + H", "H<sub>2</sub>");

        Debug.Log($"{Tag}_TEKKAYNAK: denklem=\"{denklem}\"");
        Debug.Log($"{Tag}_TEKKAYNAK: defter=\"{defter}\"");
        Debug.Log($"{Tag}_TEKKAYNAK: ansiklopedi=\"{ansiklopedi}\"");

        StringAssert.Contains(ok, denklem, "denklem gösterimi sprite okunu kullanmalı");
        StringAssert.Contains(ok, defter, "tarif defteri sprite okunu kullanmalı");
        StringAssert.Contains(ok, ansiklopedi, "ansiklopedi sprite okunu kullanmalı");
        yield return null;
    }
}
