# Üçüncü taraf bileşenler ve lisansları

Projeye dışarıdan giren her şey burada. Yeni bir paket/varlık eklenmeden
önce lisansı doğrulanır, sürümü sabitlenir ve bu tabloya yazılır.

| Bileşen | Sürüm | Lisans | Nereden | APK'ya etkisi |
|---|---|---|---|---|
| DOTween (Demigiant) | depoda gelen sürüm | [DOTween lisansı](http://dotween.demigiant.com/license.php) | `Assets/Plugins/Demigiant` | projenin başından beri var |
| NativeShare (yasirkula) | **v1.5.5** | **MIT** | GitHub sürüm dosyası | **~30 KB** (ayrıntı aşağıda) |

---

## NativeShare — v1.5.5

**Ne işe yarıyor:** bölüm sonu sonuç kartının paylaşım görselini cihazın
kendi paylaşım menüsüne verir (1.6b). Ağ erişimi yoktur; dosya yalnızca
cihazda üretilir ve işletim sistemine teslim edilir.

### Lisans
MIT. Doğrulandı:
`https://raw.githubusercontent.com/yasirkula/UnityNativeShare/master/LICENSE.txt`

- İlk satır: `MIT License`
- Telif satırı: `Copyright (c) 2017 Süleyman Yasir KULA`

Paketin `.unitypackage`'i lisans dosyası GETİRMİYOR. Metin yukarıdaki
adresten doğrulandı ve **depoya elle kondu**:
`Assets/Plugins/NativeShare/LICENSE.txt`. Üst kaynak arşivlenmiş olduğu
için lisansın yalnızca GitHub'da durması yeterli değil.

### Sürüm sabitleme
- Etiket: **v1.5.5** (deponun en son sürümü)
- Dosya: `NativeShare.unitypackage`
- Adres:
  `https://github.com/yasirkula/UnityNativeShare/releases/download/v1.5.5/NativeShare.unitypackage`
- Boyut: 38.912 bayt
- SHA-256: `64db169d8c60a7c31f07c0a3c6bf13ea037421d4b497bc696070f3726ee42191`

### Dış bağımlılık YOK (doğrulandı)
Paket depoya **açılmış hâliyle** girdi (vendor): 10 kaynak dosyanın hepsi
`Assets/Plugins/NativeShare` altında ve git'te izleniyor.
`Packages/manifest.json` içinde NativeShare girdisi, git URL'si, yerel yol
ya da `scopedRegistries` **yok** (41 bağımlılığın hepsi sürüm numaralı UPM
paketi). Yani build hiçbir dış depoya bağlı değil; üst kaynak yarın
kaybolsa da proje derlenir.

`.unitypackage` dosyası depoda tutulmuyor. Yeniden indirilmesi gerekirse
yukarıdaki adres ve özet değeri kullanılır.

### ⚠️ Depo ARŞİVLENMİŞ
Üst kaynak 10 Mayıs 2026'da salt okunur yapıldı; README "This asset is no
longer maintained!" diyor. Yani:
- Yeni Unity sürümlerinde kırılırsa **üstten düzeltme gelmeyecek**.
- MIT olduğu için gerektiğinde çatallanıp (fork) içeride düzeltilebilir;
  taşınan kod 12 KB C# + 30 KB `.aar`, yani yönetilebilir büyüklükte.
- Alternatif, küçük bir Android eklentisi yazmaktı; lisans ve boyut
  koşulları sağlandığı için yazılmadı (1.6b kararı).

### APK'ya eklediği boyut
**Ölçülen: 29.716 bayt (~29 KB)** — `Android/NativeShare.aar`, pakette
Android tarafında APK'ya giren tek ikili.

Paketin tamamı (açılmış, meta'sız) 52.166 bayt. Bunun APK'ya girmeyen
kısmı: iOS kaynakları (`NativeShare.mm`, `NSShareResultCallbackiOS.cs` =
6.558 bayt) ve editör scripti (`NSPostProcessBuild.cs` + asmdef = 3.251
bayt). Geriye kalan ~12 KB C# (`NativeShare.cs`, `NSCallbackHelper.cs`,
`NSShareResultCallbackAndroid.cs`) IL2CPP ile derlenip `libil2cpp`'e
birkaç KB olarak ekleniyor.

Yani toplam etki **~30–40 KB**, 500 KB sınırının çok altında.

> **Ölçümün sınırı — kullanıcı doğrulaması gerekiyor.** Uçtan uca APK
> karşılaştırması (öncesi/sonrası paket boyutu) **yapılamadı**: batch
> mode'da imzalama şifresi olmadığı için Android build'i
> `UnityException: Can not sign the application` ile düşüyor. Bu bilinçli
> bir kural (`CLAUDE.md` — şifreler depoya girmez, `NexumBuild` paketleri
> yalnızca ölçüm içindir ve imzalı paket Editor'de alınır). Yukarıdaki
> sayı paketin kendi yüküdür, gerçek fark Editor'de alınacak imzalı
> build'de doğrulanmalı.

### Kurulum notları
- Android'de **elle kurulum gerekmiyor**: eski sürümlerin istediği
  `<provider .../>` kaydı artık gerekmiyor, `<queries>` ögesini paket
  kendisi ekliyor.
- **INTERNET izni istemiyor.** Uygulamanın "hiçbir veriyi dışarı
  göndermez" kuralı bozulmadı; üretilen manifest kontrol edilmeli
  (`docs/RELEASE_CHECKLIST.md`).
