using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Geri tuşu davranışını tuşa basmadan doğrular: BackButtonHandler.HandleBackPress()
// karar mantığının tamamını içerdiği için test onu çağırır.
public class BackButtonTests
{
    private const string Tag = "NEXUM_BACK_TEST";

    private MethodInfo handleBackPress;
    private MethodInfo resetExitWindow;
    private MonoBehaviour menu;

    private bool Back()
    {
        return (bool)handleBackPress.Invoke(null, null);
    }

    private void Call(MonoBehaviour target, string method)
    {
        MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, method + " yok");
        m.Invoke(target, null);
    }

    private object Prop(MonoBehaviour target, string name)
    {
        PropertyInfo p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(p, name + " yok");
        return p.GetValue(target);
    }

    private string savedName;
    private bool hadName;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Kayıt ekranı açıkken geri tuşu bilerek yutuluyor (kaydı iptal etmesin diye).
        // Bu testler "kayıtlı oyuncu" durumunu ölçtüğü için kaydı önceden kuruyoruz.
        hadName = PlayerPrefs.HasKey("PlayerName");
        savedName = PlayerPrefs.GetString("PlayerName", "");
        PlayerPrefs.SetString("PlayerName", "Test Kimyager");
        PlayerPrefs.SetInt("PlayerAvatarIndex", 0);

        System.Type type = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == "Nexum.Core.BackButtonHandler");
        Assert.IsNotNull(type, "BackButtonHandler tipi yok");

        handleBackPress = type.GetMethod("HandleBackPress", BindingFlags.Public | BindingFlags.Static);
        resetExitWindow = type.GetMethod("ResetExitWindow", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(handleBackPress, "HandleBackPress yok");

        SceneManager.LoadScene("MainMenu");
        yield return null;
        yield return new WaitForSeconds(1f);

        menu = Object.FindObjectsOfType<MonoBehaviour>().FirstOrDefault(m => m.GetType().Name == "MainMenuManager");
        Assert.IsNotNull(menu, "MainMenuManager yok");
        resetExitWindow.Invoke(null, null);
    }

    [TearDown]
    public void TearDown()
    {
        if (hadName) PlayerPrefs.SetString("PlayerName", savedName);
        else PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.Save();
    }

    [UnityTest]
    public IEnumerator AcikPanelGeriTusuylaKapanir()
    {
        Call(menu, "OpenSettingsPanel");
        yield return new WaitForSeconds(1f);
        Assert.IsFalse((bool)Prop(menu, "IsAtMainMenuRoot"), "Ayarlar paneli açılmadı");

        bool quit = Back();
        yield return new WaitForSeconds(1f);

        Debug.Log($"{Tag}: ayarlar acik -> geri -> kokte={Prop(menu, "IsAtMainMenuRoot")} cikis={quit}");
        Assert.IsFalse(quit, "Panel kapanırken çıkış istenmemeli");
        Assert.IsTrue((bool)Prop(menu, "IsAtMainMenuRoot"), "Geri tuşu paneli kapatmadı");
    }

    [UnityTest]
    public IEnumerator GecisSirasindaGeriTusuYokSayilir()
    {
        Call(menu, "OpenEncyclopedia");
        yield return null;   // tween başladı, bitmedi

        Assert.IsTrue((bool)Prop(menu, "IsSwitching"), "IsSwitching bayrağı kalkmadı");
        bool quit = Back();
        Debug.Log($"{Tag}: gecis sirasinda geri -> cikis={quit} (yok sayilmali)");
        Assert.IsFalse(quit, "Geçiş sırasında çıkış istenmemeli");

        yield return new WaitForSeconds(1.2f);
        Assert.IsFalse((bool)Prop(menu, "IsSwitching"), "Geçiş bitince bayrak sıfırlanmalı");

        Call(menu, "BackToMainMenu");
        yield return new WaitForSeconds(1f);
    }

    [UnityTest]
    public IEnumerator KokteCiftBasisCikisIster()
    {
        Assert.IsTrue((bool)Prop(menu, "IsAtMainMenuRoot"), "Test ana menü kökünde başlamalı");
        resetExitWindow.Invoke(null, null);

        bool first = Back();
        bool second = Back();

        Debug.Log($"{Tag}: kokte ilk basis cikis={first}, ikinci basis cikis={second}");
        Assert.IsFalse(first, "İlk basış uygulamayı kapatmamalı");
        Assert.IsTrue(second, "İkinci basış çıkış istemeli");

        yield return null;
    }
}
