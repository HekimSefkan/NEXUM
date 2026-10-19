using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Tepkime denklemi gösterimi: metin, yaşam süresi, havuza dönüş ve çakışma.
public class ReactionEquationTests
{
    private const string Tag = "NEXUM_REACTION";

    // Belir 0.15 + dur 0.5 + sön 0.35 = 1.0 sn; üstüne pay
    private const float Lifetime = 1.3f;

    private MonoBehaviour grid;
    private List<Transform> cells;
    private MonoBehaviour display;   // ReactionEquationDisplay (yansimayla)

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.Data.showReactionEquations = 1;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        grid = Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        FieldInfo cellsField = grid.GetType().GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance);
        cells = (List<Transform>)cellsField.GetValue(grid);

        display = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "ReactionEquationDisplay");
        Assert.IsNotNull(display, "ReactionEquationDisplay yok (NEXUM -> Tepkime Denklemini Kur)");
    }

    // ------------------------------------------------------------- yardımcılar

    private int AktifSayisi()
    {
        return (int)display.GetType()
            .GetProperty("ActiveCount", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(display);
    }

    private List<Rect> AktifKutular()
    {
        var liste = (System.Collections.IList)display.GetType()
            .GetProperty("ActiveScreenRects", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(display);

        List<Rect> sonuc = new List<Rect>();
        foreach (object o in liste) sonuc.Add((Rect)o);
        return sonuc;
    }

    private GameObject BasicElement(string name)
    {
        var basics = (GameObject[])grid.GetType()
            .GetField("basicElements", BindingFlags.Public | BindingFlags.Instance).GetValue(grid);
        return basics.FirstOrDefault(b => b.name == name);
    }

    private void SetBoard(string[] layout)
    {
        foreach (Transform cell in cells)
        {
            for (int i = cell.childCount - 1; i >= 0; i--)
            {
                Transform child = cell.GetChild(i);
                child.SetParent(null);
                Object.DestroyImmediate(child.gameObject);
            }
        }

        for (int i = 0; i < 16; i++)
        {
            if (string.IsNullOrEmpty(layout[i])) continue;
            GameObject prefab = BasicElement("Tile_" + layout[i]);
            Assert.IsNotNull(prefab, "temel element yok: " + layout[i]);
            GameObject tile = Object.Instantiate(prefab, cells[i]);
            tile.name = prefab.name;
            tile.transform.localScale = Vector3.one;
            tile.transform.localPosition = Vector3.zero;
        }
    }

    private void Shift(string direction)
    {
        Vector2 v = direction == "up" ? Vector2.up
                  : direction == "down" ? Vector2.down
                  : direction == "left" ? Vector2.left : Vector2.right;
        grid.GetType().GetMethod("Shift", BindingFlags.Public | BindingFlags.Instance)
            .Invoke(grid, new object[] { v });
    }

    private void SetSingleGoal(string tileName, int amount)
    {
        MonoBehaviour levels = Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "LevelManager");
        var list = (System.Collections.IList)levels.GetType()
            .GetField("levels", BindingFlags.Public | BindingFlags.Instance).GetValue(levels);
        int index = (int)levels.GetType()
            .GetField("currentLevelIndex", BindingFlags.Public | BindingFlags.Instance).GetValue(levels);
        var goals = (System.Collections.IList)list[index].GetType()
            .GetField("levelGoals").GetValue(list[index]);

        var recipes = (System.Collections.IList)grid.GetType()
            .GetField("recipes", BindingFlags.Public | BindingFlags.Instance).GetValue(grid);
        GameObject target = null;
        foreach (object r in recipes)
        {
            var p = (GameObject)r.GetType().GetField("resultPrefab").GetValue(r);
            if (p != null && p.name == tileName) { target = p; break; }
        }

        for (int i = 0; i < goals.Count; i++)
        {
            object g = goals[i];
            g.GetType().GetField("targetPrefab").SetValue(g, i == 0 ? target : target);
            g.GetType().GetField("targetAmount").SetValue(g, i == 0 ? amount : 1);
            g.GetType().GetField("currentAmount").SetValue(g, i == 0 ? 0 : 1);
        }
    }

    // ------------------------------------------------------------------ testler

    // Birleşmede denklem doğru metinle oluşur, süresi dolunca havuza döner.
    [UnityTest]
    public IEnumerator BirlesmedeDenklemOlusurVeHavuzaDoner()
    {
        SetSingleGoal("Tile_H2", 99);
        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });

        int oncekiAktif = AktifSayisi();
        Shift("left");                                     // H + H -> H2
        yield return null;

        int aktif = AktifSayisi();
        TMP_Text gosterim = display.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(t => t.gameObject.activeSelf);

        string metin = gosterim != null ? gosterim.text : "";
        Debug.Log($"{Tag}_METIN: aktif={aktif} (once {oncekiAktif}) \"{metin}\"");

        Assert.AreEqual(1, aktif, "birleşmeden sonra tek denklem görünmeli");
        Assert.IsNotNull(gosterim, "görünen denklem yok");
        // Ok ya sprite olarak ya da (sprite yoksa) duz metin olarak gelmeli
        bool okVarMi = metin.Contains("name=\"arrow\">") || metin.Contains("->");
        Assert.IsTrue(okVarMi, "denklem oku bulunmalı (sprite ya da düz metin)");
        StringAssert.Contains("H<sub>2</sub>", metin, "ürün sembolü ElementData'dan gelmeli");

        yield return new WaitForSeconds(Lifetime);

        Debug.Log($"{Tag}_OMUR: {Lifetime:F1} sn sonra aktif={AktifSayisi()}");
        Assert.AreEqual(0, AktifSayisi(), "süresi dolunca havuza dönmeli");
    }

    // Aynı hamlede iki birleşme: gösterimler üst üste binmemeli.
    [UnityTest]
    public IEnumerator AyniHamledekiIkiDenklemCakismaz()
    {
        SetSingleGoal("Tile_H2", 99);
        SetBoard(new[]
        {
            "H", "H", "", "",
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });

        Shift("left");                                     // iki ayrı H + H -> H2
        yield return null;

        List<Rect> rects = AktifKutular();
        Debug.Log($"{Tag}_CAKISMA: gosterim={rects.Count}");
        foreach (Rect r in rects) Debug.Log($"{Tag}_KUTU: x {r.xMin:F0}..{r.xMax:F0} y {r.yMin:F0}..{r.yMax:F0}");

        Assert.AreEqual(2, rects.Count, "iki birleşme iki denklem üretmeli");

        for (int i = 0; i < rects.Count; i++)
        {
            for (int j = i + 1; j < rects.Count; j++)
            {
                bool cakisiyor = rects[i].Overlaps(rects[j]);
                Debug.Log($"{Tag}_CIFT: {i}x{j} cakisma={cakisiyor}");
                Assert.IsFalse(cakisiyor, $"{i}. ve {j}. denklem çakışıyor");
            }
        }
        yield return null;
    }

    // Denklem kombo yazısının bandına girmemeli ve grid'in yatay sınırında kalmalı.
    [UnityTest]
    public IEnumerator DenklemKomboBandinaVeHudaGirmez()
    {
        SetSingleGoal("Tile_H2", 99);

        RectTransform safeRoot = LayoutCheck.PrepareSafeRoot();
        Assert.IsNotNull(safeRoot, "SafeAreaRoot yok");

        // Kombo yazısı 328 merkezli bantta (UIManager.ComboAnchorY); denklem
        // tavanı 270 -> ikisi arasında en az 58 birim pay var.
        const float KomboBandAlt = 290f;

        RectTransform gridBoard = LayoutCheck.Find("SafeAreaRoot/GridBoard") as RectTransform;
        Assert.IsNotNull(gridBoard, "GridBoard yok");
        Vector4 gridRect = LayoutCheck.LocalRect(safeRoot, gridBoard);

        List<string> hatalar = new List<string>();

        // Dört farklı hücrede birleşme: üst sıra, alt sıra, sol ve sağ sütun
        string[][] tahtalar =
        {
            new[] { "H", "H", "", "",  "", "", "", "",  "", "", "", "",  "", "", "", "" },
            new[] { "", "", "", "",  "", "", "", "",  "", "", "", "",  "H", "H", "", "" },
            new[] { "", "", "H", "H",  "", "", "", "",  "", "", "", "",  "", "", "", "" },
        };

        foreach (string[] tahta in tahtalar)
        {
            SetBoard(tahta);
            Shift("left");
            yield return null;

            foreach (Rect ekran in AktifKutular())
            {
                Vector3 merkez = safeRoot.InverseTransformPoint(new Vector3(ekran.center.x, ekran.center.y, 0f));
                float yariYukseklik = ekran.height / Mathf.Max(0.0001f, safeRoot.lossyScale.y) * 0.5f;
                float yariGenislik = ekran.width / Mathf.Max(0.0001f, safeRoot.lossyScale.x) * 0.5f;
                float ust = merkez.y + yariYukseklik;

                Debug.Log($"{Tag}_SINIR: merkez y {merkez.y:F0} ust {ust:F0} " +
                          $"(kombo bandi {KomboBandAlt:F0}) x {merkez.x - yariGenislik:F0}..{merkez.x + yariGenislik:F0} " +
                          $"(grid {gridRect.x:F0}..{gridRect.z:F0})");

                if (ust > KomboBandAlt) hatalar.Add($"kombo bandina girdi: ust {ust:F0} > {KomboBandAlt:F0}");

                float sol = merkez.x - yariGenislik;
                float sag = merkez.x + yariGenislik;
                if (sol < gridRect.x - 1f) hatalar.Add($"grid solundan tasti: {sol:F0} < {gridRect.x:F0}");
                if (sag > gridRect.z + 1f) hatalar.Add($"grid sagindan tasti: {sag:F0} > {gridRect.z:F0}");
            }

            yield return new WaitForSeconds(Lifetime);
        }

        Debug.Log($"{Tag}_SINIR_SONUC: sorun {hatalar.Count}");
        foreach (string h in hatalar) Debug.Log($"{Tag}_SORUN: {h}");
        Assert.IsEmpty(hatalar, string.Join(" | ", hatalar));
    }

    // Ok sprite'ı: yüklenebiliyor mu ve metnin taban hizasında mı?
    [UnityTest]
    public IEnumerator OkSpriteHizali()
    {
        System.Type tip = System.Type.GetType("ReactionText, Assembly-CSharp");
        Assert.IsNotNull(tip, "ReactionText tipi yok");
        bool okVar = (bool)tip.GetProperty("ArrowAvailable",
            BindingFlags.Public | BindingFlags.Static).GetValue(null);

        Debug.Log($"{Tag}_OK: sprite yuklendi={okVar}");
        Assert.IsTrue(okVar, "ok sprite'ı yüklenmeli (NEXUM -> Sembol Sprite'larını Kur)");

        TMP_Text olcum = display.GetComponentsInChildren<TMP_Text>(true).First();
        olcum.gameObject.SetActive(true);

        // Ok ile komşu harfleri aynı satırda ölç
        olcum.text = "H <sprite=\"NexumSymbols\" name=\"arrow\"> H";
        olcum.ForceMeshUpdate();

        TMP_TextInfo info = olcum.textInfo;
        float harfMerkez = 0f, okMerkez = 0f, okYukseklik = 0f;
        int harfSayisi = 0;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) continue;

            float merkez = (c.topLeft.y + c.bottomLeft.y) * 0.5f;
            if (c.elementType == TMP_TextElementType.Sprite)
            {
                okMerkez = merkez;
                okYukseklik = c.topLeft.y - c.bottomLeft.y;
            }
            else
            {
                harfMerkez += merkez;
                harfSayisi++;
            }
        }

        Assert.Greater(harfSayisi, 0, "harf bulunamadı");
        harfMerkez /= harfSayisi;
        float fark = okMerkez - harfMerkez;
        float punto = olcum.fontSize;

        Debug.Log($"{Tag}_OK_HIZA: punto={punto:F0} harf merkezi={harfMerkez:F2} " +
                  $"ok merkezi={okMerkez:F2} fark={fark:F2} ({fark / punto * 100f:F0}% punto) " +
                  $"ok yuksekligi={okYukseklik:F2} ({okYukseklik / punto * 100f:F0}% punto)");

        olcum.text = "";
        olcum.gameObject.SetActive(false);

        // Ok, harflerin optik ortasından punto'nun %10'undan fazla kaymasın
        Assert.Less(Mathf.Abs(fark), punto * 0.10f,
            $"ok taban hizasından kaymış: {fark:F2} birim ({fark / punto * 100f:F0}% punto)");
        // Ok, satırı şişirmeyecek kadar küçük ama görünür olmalı
        Assert.Greater(okYukseklik, punto * 0.15f, "ok fazla küçük");
        Assert.Less(okYukseklik, punto * 0.75f, "ok fazla büyük");
        yield return null;
    }

    // Ayardan kapatılınca hiç gösterilmemeli.
    [UnityTest]
    public IEnumerator AyarKapaliykenGosterilmez()
    {
        SaveService.Data.showReactionEquations = 0;
        SetSingleGoal("Tile_H2", 99);

        SetBoard(new[]
        {
            "H", "H", "", "",
            "",  "",  "", "",
            "",  "",  "", "",
            "",  "",  "", ""
        });

        Shift("left");
        yield return null;

        Debug.Log($"{Tag}_AYAR: kapaliyken aktif={AktifSayisi()}");
        Assert.AreEqual(0, AktifSayisi(), "ayar kapalıyken denklem gösterilmemeli");

        SaveService.Data.showReactionEquations = 1;
    }
}
