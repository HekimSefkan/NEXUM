using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// BAĞLI BUTONLAR SESSİZCE GEÇMEZ (Çalışma anlaşması → Panel erişimi).
///
/// Bir <c>OnClick</c>, serileştirilmiş ya da çalışma zamanında bulunan bir
/// referansa bağlıysa ve o referans null ise, butona basmak hiçbir şey
/// yapmaz — ve hiçbir yerde hata görünmez. Tarif defteri satırları oyunda
/// tam olarak böyle çalışmıyordu: <c>RecipeBookPanel.encyclopedia</c>
/// null'dı, <c>OpenCard</c> sessizce <c>return</c> ediyordu.
///
/// Bu test iki şeyi ölçer:
///   1) her <c>Button</c>'ın kalıcı çağrı hedefi YAŞIYOR mu (null/Missing değil),
///   2) o hedef bileşenin serileştirilmiş referansları dolu mu.
///
/// Bilerek boş bırakılan alanlar <see cref="Muaf"/> listesinde gerekçesiyle
/// durur; liste <c>NexumValidation.AllowedNullFields</c> ile aynı mantıkta
/// sahneye özel anahtar kabul eder.
/// </summary>
public class WiredButtonTests
{
    private const string Tag = "NEXUM_BUTONBAG";

    /// <summary>"Sahne::Tip.alan" ya da "Tip.alan". Gerekçeler yanlarında.</summary>
    private static readonly HashSet<string> Muaf = new HashSet<string>
    {
        // Oyun sahnesindeki ansiklopedi YALNIZCA ayrıntı kartı için kurulu;
        // kart listesinin alanları orada kullanılmıyor.
        "Game::EncyclopediaManager.cardPrefab",
        "Game::EncyclopediaManager.cardParent",
        "Game::EncyclopediaManager.contentArea",
        "Game::EncyclopediaManager.detailFormulaText",

        // Overlay canvas'ta particle görünmüyor; efekt bilerek kapalı.
        "GridManager.mergeParticlePrefab",
    };

    [UnityTest]
    public IEnumerator MenuButonlariBagli()
    {
        yield return Olc("MainMenu");
    }

    [UnityTest]
    public IEnumerator OyunButonlariBagli()
    {
        yield return Olc("Game");
    }

    private IEnumerator Olc(string sahne)
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene(sahne);
        yield return null;
        yield return new WaitForSeconds(1f);

        Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.transform).FirstOrDefault(t => t.name == "Canvas");
        Assert.IsNotNull(canvas, "Canvas yok");

        List<string> hatalar = new List<string>();
        HashSet<Object> hedefler = new HashSet<Object>();
        int buton = 0, cagri = 0;

        foreach (Button b in canvas.GetComponentsInChildren<Button>(true))
        {
            buton++;
            int n = b.onClick.GetPersistentEventCount();
            for (int i = 0; i < n; i++)
            {
                cagri++;
                Object hedef = b.onClick.GetPersistentTarget(i);
                string metot = b.onClick.GetPersistentMethodName(i);

                if (hedef == null)
                {
                    hatalar.Add($"{PixelProbe.Path(b.transform)} -> {metot}: hedef null/Missing");
                    continue;
                }
                hedefler.Add(hedef);
            }
        }

        // Butonları ÇALIŞMA ZAMANINDA bağlayan bileşenler kalıcı çağrı
        // listesinde görünmez (tarif defteri satırları böyle). Bu yüzden
        // tarama OnClick hedefleriyle sınırlı kalmaz: sahnedeki bütün proje
        // scriptlerinin bağımlılıkları ölçülür.
        foreach (MonoBehaviour mb in canvas.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null) hedefler.Add(mb);
        }

        // Hedef bileşenlerin bağımlılıkları
        foreach (Object hedef in hedefler)
        {
            Component c = hedef as Component;
            if (c == null) continue;
            // Yalnızca proje scriptleri (Assembly-CSharp). Test asmdef'i o
            // derlemeyi referans almaz, bu yüzden tip adla çözülür.
            if (c.GetType().Assembly.GetName().Name != "Assembly-CSharp") continue;

            foreach (FieldInfo f in c.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!typeof(Object).IsAssignableFrom(f.FieldType)) continue;

                string anahtar = $"{c.GetType().Name}.{f.Name}";
                if (Muaf.Contains(anahtar) || Muaf.Contains($"{sahne}::{anahtar}")) continue;

                Object deger = f.GetValue(c) as Object;
                if (deger == null)
                {
                    hatalar.Add($"{c.GetType().Name} ({c.name}) alanı {f.Name} NULL — " +
                                "bu bileşeni çağıran buton sessizce işlevsiz");
                }
            }
        }

        Debug.Log($"{Tag}_SONUC: {sahne} buton {buton}, kalıcı çağrı {cagri}, " +
                  $"hedef bileşen {hedefler.Count}, sorun {hatalar.Count}");
        foreach (string h in hatalar.Distinct().Take(25)) Debug.Log($"{Tag}_SORUN: {h}");

        Assert.IsEmpty(hatalar.Distinct().ToList(),
            string.Join(" | ", hatalar.Distinct().Take(6)));
    }
}
