using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Duraklat → tarif defteri → geri akışı OYUN SAHNESİNİ TERK ETMEZ (2d-3c/1).
///
/// Hata: <c>UIManager.OpenRecipeBook</c> <c>SceneManager.LoadScene("MainMenu")</c>
/// çağırıyordu, yani defter açmak oyundan çıkmak demekti — tahta, skor ve
/// duraklatılmış durum yok oluyordu. Defter paneli artık Game sahnesinde de
/// var; panel açılıp kapanıyor, sahne yüklenmiyor.
///
/// Doğrulanan: sahne aynı · tahta aynı · skor aynı · oyun hâlâ duraklatılmış ·
/// geri dönünce duraklat menüsü açık. Android geri tuşu da aynı zincirden
/// geçiyor (<c>BackButtonHandler.HandleBackPress</c>).
/// </summary>
public class PauseRecipeBookTests
{
    private const string Tag = "NEXUM_DEFTERAKIS";

    [UnityTest]
    public IEnumerator DuraklattanDefterOyundanCikmaz()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        string sahne = SceneManager.GetActiveScene().name;
        Assert.AreEqual("Game", sahne, "oyun sahnesi yüklenmedi");

        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "UIManager");
        Assert.IsNotNull(ui, "UIManager yok");

        MonoBehaviour grid = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "GridManager");
        Assert.IsNotNull(grid, "GridManager yok");

        GameObject defter = Reflect.Get<GameObject>(ui, "recipeBookPanel");
        Assert.IsNotNull(defter, "Game sahnesinde recipeBookPanel bağlı değil");

        GameObject duraklat = Reflect.Get<GameObject>(ui, "pausePanel");
        Assert.IsNotNull(duraklat, "pausePanel yok");

        // --- duraklat
        Reflect.Call(ui, "TogglePause", true);
        yield return null;
        Assert.IsTrue(duraklat.activeInHierarchy, "duraklat açılmadı");
        Assert.AreEqual(0f, Time.timeScale, "duraklatta timeScale 0 olmalı");

        int tasSayisi = TahtadakiTas(grid);
        int skor = Skor();
        Debug.Log($"{Tag}_ONCE: sahne={sahne} taş={tasSayisi} skor={skor} " +
                  $"timeScale={Time.timeScale}");

        // --- defteri aç
        Reflect.Call(ui, "OpenRecipeBook");
        yield return null;
        yield return null;

        Assert.AreEqual("Game", SceneManager.GetActiveScene().name,
            "defter açılınca OYUN SAHNESİ TERK EDİLDİ");
        Assert.IsTrue(defter.activeInHierarchy, "defter paneli açılmadı");
        Assert.IsFalse(duraklat.activeInHierarchy, "duraklat kartı kapanmalıydı");
        Assert.AreEqual(0f, Time.timeScale, "defterdeyken oyun duraklatılmış kalmalı");

        // --- geri dön
        Reflect.Call(ui, "CloseRecipeBook");
        yield return null;
        yield return null;

        Assert.AreEqual("Game", SceneManager.GetActiveScene().name, "sahne değişti");
        Assert.IsFalse(defter.activeInHierarchy, "defter kapanmadı");
        Assert.IsTrue(duraklat.activeInHierarchy, "duraklat menüsüne dönülmedi");
        Assert.AreEqual(0f, Time.timeScale, "oyun hâlâ duraklatılmış olmalı");
        Assert.AreEqual(tasSayisi, TahtadakiTas(grid), "tahta değişti");
        Assert.AreEqual(skor, Skor(), "skor değişti");

        Debug.Log($"{Tag}_SONRA: sahne={SceneManager.GetActiveScene().name} " +
                  $"taş={TahtadakiTas(grid)} skor={Skor()} timeScale={Time.timeScale}");
    }

    [UnityTest]
    public IEnumerator GeriTusuDefterdenDuraklataDoner()
    {
        SaveService.Data.selectedLevel = 0;
        SaveService.SetTutorialRead(0);

        SceneManager.LoadScene("Game");
        yield return null;
        yield return new WaitForSeconds(1f);

        MonoBehaviour ui = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "UIManager");
        GameObject defter = Reflect.Get<GameObject>(ui, "recipeBookPanel");
        GameObject duraklat = Reflect.Get<GameObject>(ui, "pausePanel");

        Reflect.Call(ui, "TogglePause", true);
        yield return null;
        Reflect.Call(ui, "OpenRecipeBook");
        yield return null;

        Assert.IsTrue(defter.activeInHierarchy, "defter açık olmalıydı");

        // BackButtonHandler Nexum.Core ad alanında; Reflect kısa ada bakar.
        System.Type handlerType = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == "Nexum.Core.BackButtonHandler");
        Assert.IsNotNull(handlerType, "BackButtonHandler tipi yok");

        bool cikis = (bool)handlerType
            .GetMethod("HandleBackPress",
                       System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Invoke(null, null);
        yield return null;

        Assert.IsFalse(cikis, "geri tuşu uygulamadan çıkmamalı");
        Assert.IsFalse(defter.activeInHierarchy, "geri tuşu defteri kapatmalı");
        Assert.IsTrue(duraklat.activeInHierarchy, "duraklat menüsüne dönülmeli");
        Assert.AreEqual("Game", SceneManager.GetActiveScene().name, "sahne değişti");

        Debug.Log($"{Tag}_GERITUSU: defter kapandı, duraklat açık, sahne Game");
    }

    private static int TahtadakiTas(MonoBehaviour grid)
    {
        var cells = Reflect.Get<System.Collections.Generic.List<Transform>>(grid, "cells");
        return cells == null ? -1 : cells.Count(c => c != null && c.childCount > 0);
    }

    private static int Skor()
    {
        MonoBehaviour gm = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(m => m.GetType().Name == "GameManager");
        return gm == null ? -1 : Reflect.Get<int>(gm, "currentScore");
    }
}
