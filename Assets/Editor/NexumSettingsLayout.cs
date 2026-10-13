/// <summary>
/// ESKİ KURUCU — yerini <see cref="SettingsCardBuilder"/> aldı (Faz 0 / Adım 5).
///
/// Ayarlar panelinin yerleşimi artık <c>Assets/Editor/UIBuilder</c> altındaki
/// ortak çatı üzerinden kuruluyor: idempotans, kayıt defteri (managed.json),
/// isim çakışması koruması, rapor ve dry-run oradan geliyor.
///
/// Bu sınıf yalnızca eski batch komutlarının (<c>-executeMethod
/// NexumSettingsLayout.Run</c>) kırılmaması için duruyor ve çağrıyı iletiyor.
/// Menü girdisi burada YOK; tek menü girdisi SettingsCardBuilder'da.
/// </summary>
public static class NexumSettingsLayout
{
    public static void Run()
    {
        SettingsCardBuilder.Run();
    }
}
