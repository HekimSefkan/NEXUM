# 2d-3a teşhis — ölçüm neden cihazı yakalamadı?

**Dal:** `metal-kit-materials` · **Kapsam:** yalnızca teşhis, **düzeltme yok**.

Soru şuydu: *bütün testler yeşilken cihaz nasıl bozuk olabilir?*
Cevap tek cümleyle: **projedeki her yerleşim testi dikdörtgen karşılaştırır;
bugüne kadar hiçbir test tek bir piksele bakmadı.** Metal kiti geçişi
dikdörtgenleri korudu, pikselleri değiştirdi — yani kusur, aygıtın
göremeyeceği bir yerden girdi.

---

## 1. Gerçek ekran görüntüleri

`Assets/Editor/NexumDeviceShot.cs` (bu adımda yazıldı) sahneyi açar,
panelleri açıp kapatır, tahtayı dört seviyeden taşla doldurur (bir hücre
bilerek boş), Canvas'ı geçici bir ortografik kameraya bağlar ve kamerayı
hedef boyutta bir `RenderTexture`'a çizdirir. **Sahne kaydedilmez.**

Neden bu yol gerekti: batch mode her zaman 640×480 bir yüzeyle açılır ve
`-screen-width` işlemez. `LayoutCheck` bu yüzden hedef boyutu
`SafeAreaRoot`'a *dayatır* — Canvas 640×480 kalır. Kameraya bağlamak
CanvasScaler'ı cihazdaki gibi çalıştırır.

```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -quit -projectPath C:\Projects\NEXUM -executeMethod NexumDeviceShot.Run -logFile $env:TEMP\d_shot.log | Out-Null
```

(`-nographics` **verilmez**; render bağlamı gerekir.)

12 görüntü, iki canvas boyutunda — `docs/device-shots/`:

| Ekran | 1080×1920 | 1440×1920 |
|---|---|---|
| ana menü | `ana-menu_1080x1920.png` | `ana-menu_1440x1920.png` |
| ayarlar | `ayarlar_1080x1920.png` | `ayarlar_1440x1920.png` |
| nasıl oynanır | `nasil-oynanir_1080x1920.png` | `nasil-oynanir_1440x1920.png` |
| tarif defteri | `tarif-defteri_1080x1920.png` | `tarif-defteri_1440x1920.png` |
| oyun (tahta dolu) | `oyun_1080x1920.png` | `oyun_1440x1920.png` |
| duraklat kartı | `duraklat_1080x1920.png` | `duraklat_1440x1920.png` |

Ayrıca üç yakın plan: `detay_taslar.png`, `detay_ayarlar-mod-butonlari.png`,
`detay_tarif-defteri-butonu.png`.

Her kare şunu logladı: `canvas 1080x1920  scaleFactor 1,000  referans 1080x1920`.

### Aygıtın kendi sınırı — raporda bunlar kusur DEĞİL

Render **edit mode**'da alınır; oyun çalışmaz. Bu yüzden görüntülerde:

* **38 etiket boş** (MainMenu 27, Game 11). Bunlar `LocalizedText` ile
  çalışma zamanında doluyor — tarif defteri butonunun yazısı, defterin
  sayaç halkası, denklem satırları. Cihazda yazı **vardır**.
* **Dört anahtar (Müzik / Efektler / Bilgi Kartları / Mentor İpuçları) beyaz
  dikdörtgen.** Serileştirilmiş sprite Unity'nin yerleşik `UISprite`'ı;
  gerçek sprite'ı `SettingsManager.cs:88-91` çalışma zamanında atıyor.
  (`docs/ui-kit/NEXT_3B.md` satır 49 bunu zaten not etmişti.)
* **Profil avatarı ve tarif satırlarının içeriği boş** — aynı sebep.

Bunların dışında kalan her şey gerçek: **render bildirilen üç kusurun
üçünü de birebir üretti.**

---

## 2. Ayarlar ekranı — kök sebep

### Ne oluyor

`detay_ayarlar-mod-butonlari.png`: üç mod butonu 56×56'lık altın lekelere
çöktü, "NORMAL MOD / SINAV MOD / SERBEST MOD" etiketleri satır başına iki
harfe bölünüp üst üste bindi. Panelin kendisi de kartlıktan çıkıp ekranı
baştan başa kaplayan düz bir levhaya döndü.

### Sebep: `Image.Type.Simple → Sliced`, `preferredWidth`'i yeniden tanımlıyor

`NexumSpriteSwap.cs:202-207` sprite'ı değiştirirken, kenarlığı olan her
sprite için `img.type = Image.Type.Sliced` yapıyor — 9-slice köşe
geometrisi bozulmasın diye. Doğru bir satır, ama **bir yan etkisi var**:
`Image` bir `ILayoutElement`'tir ve `preferredWidth` / `preferredHeight`
değerleri çizim tipine göre *farklı* hesaplanır.

Ölçüm (`NEXUM_DIAG_SPRITEOLCU`):

| | sprite | doku | border | type | `preferredWidth` |
|---|---|---|---|---|---|
| ESKİ | `SettingsPanel_modsecimkartı` | 1365×548 | (0,0,0,0) | Simple | **1365,0** |
| YENİ | `btn_secondary_normal` | 280×80 | (28,28,28,28) | **Sliced** | **56,0** |

`Simple`'da `preferredWidth` = dokunun piksel genişliği. `Sliced`'da
= **yatay kenarlıkların toplamı** (28 + 28 = 56). Doku 1365'ten 280'e
inmedi; ölçü 1365'ten **56**'ya indi.

### Niçin bu, butonu küçültüyor

`NEXUM_DIAG_BILESEN`:

```
ModeContainer  VerticalLayoutGroup(ctrlW=True expW=False ctrlH=True spacing=10)
  NormalModeBtn  sizeDelta 480,118   Image(btn_secondary_normal 280x80 type=Sliced prefW=56,0)
    Text (TMP)   anchor 0,0-1,1 sizeDelta 0,0   TMP("NORMAL MOD" punto=30)
```

`childControlWidth = True` + `childForceExpandWidth = False` demek:
**çocuğun genişliğini `preferredWidth` belirler.** Serileştirilmiş
`sizeDelta 480×118` artık sadece eski bir kalıntı; layout yeniden
kurulduğunda üzerine yazılıyor.

Ölçülen sonuç (`NEXUM_DIAG_FARK`):

```
NormalModeBtn   56,0 x 56,0
ExamModeBtn     56,0 x 56,0
FreeModeBtn     56,0 x 56,0
```

TMP etiketi `(0,0)-(1,1)` ile ebeveyne gerildiği için 56 birimlik bir
kutuya düşüyor; "NORMAL MOD" 30 puntoda oraya sığmayınca iki harfe
bölünüp dışarı taşıyor. Yani **harf yığılması bir metin sorunu değil,
çökmenin belirtisi.**

> Not: `ResetProgressButton`'ın sprite'ı *değişmedi* (yerleşik `UISprite`).
> O da 388,8×52,1'e düşüyor ama sebebi farklı: `sizeDelta (0,0)` ile
> ebeveyn layout zincirine bağlı ve o zincir yukarıdan daralmış durumda.

### İkinci, bağımsız mekanizma: çerçeve pikselin içindeydi

Mod butonlarıyla ilgisi yok, ama aynı geçişten geldi ve **duraklat
kartını** da bozan şey bu:

| nesne | eski sprite | yeni sprite |
|---|---|---|
| `SettingsPanel/GlassBackground` (sizeDelta 0,0 → tam ekran) | `SettingsPanel_.png` 796×1280, border 0 | `panel_lg_normal.png` 420×300, border 28 |
| `PausePanel` (1080×1519) | `gamepanel.png` 1024×1536, border 0 | `modal_normal.png` 360×260, border 28 |

Eski görseller **boyalı kartlardı**: kenar payı, iç kart, gölge — hepsi
pikselin içinde pişmişti. 1080×1519'luk bir `Image`'a konduğunda ekranda
daha küçük bir kart görünüyordu. 9-slice çerçevenin iç payı **yok**;
dikdörtgeni kenardan kenara doldurur. Dikdörtgen değişmedi, kartın
görünür sınırı değişti.

`duraklat_1080x1920.png` tam olarak bunu gösteriyor: çerçeve ekranı
kaplıyor, içerik 688×957'lik görünmez `ContainerBox`'ta kalıyor, "DEVAM
ET" / "ANA MENÜ" gri bir denizde yüzüyor.
(O iki butonun arka planının olmaması **eski bir durum**: `alpha 0` ile
serileştirilmişler, bu geçişten gelmiyor.)

### Bunu `LayoutCheck` neden yakalamadı?

Üç ayrı sebep var ve üçü de bağımsız olarak yeterli.

**(a) Test, artık ekranda olmayan bir sprite'ı doğruluyor.**
`SettingsLayoutTests.cs:20-26`:

```csharp
private const float SpriteW = 796f;      // SettingsPanel_.png
private const float SpriteH = 1280f;
private const float InnerLeft  = 100f / SpriteW;
private const float InnerRight = 690f / SpriteW;
```

Bunlar **sabit**. Kartın "iç alanı" `AspectFittedInner` ile bu sabitlerden
türetiliyor; test hiçbir yerde `Image.sprite`'ı okumuyor. Sprite
`panel_lg_normal`'a geçtiğinde test eski görselin boyalı paylarını
ölçmeye devam etti. Hayalet bir çerçeveye göre "içeride" diyor.

**(b) İddialar yalnızca *kapsanma* ve *çakışma*.**
`tasma > 1f` ve `FindOverlaps`. 56×56'ya çöken bir buton kartın **daha
çok** içindedir ve **daha az** çakışır. **Asgari boyut iddiası yok**,
"çizilen boyut tasarlanan boyuta eşit olmalı" iddiası yok, dokunma hedefi
iddiası yok. Çökme, bu iki ölçütün ikisini de *iyileştirir*.

**(c) Çöken nesneler ölçülen listede yok.**
`Content` elle yazılmış 11 isim ve `ModeContainer` var, ama
`NormalModeBtn` / `ExamModeBtn` / `FreeModeBtn` **yok** — onlar torun.
`ModeContainer`'ın kendisi 800,5×343,9'da kaldı (anchor'la boyutlanıyor),
dolayısıyla geçti. Elle yazılmış bir liste, adını anmadığı nesnedeki
kusuru yakalayamaz.

Duraklat kartı için aynı deseni `RecipeBookLayoutTests.cs:286-300`
tekrarlıyor: sınır olarak **`ContainerBox`'ın kendi rect'i** alınıyor
(`LocalRect(kutuRect, kutuRect)`). Oyuncunun "kart" dediği şey ise
`PausePanel`'in `Image`'ı. Test bu ikisini hiç karşılaştırmıyor; önce de
sonra da içerik `ContainerBox` içinde, yani her iki hâlde yeşil.

### Test neyi ölçüyor: amaçlanan rect'i mi, gerçek rect'i mi?

**Gerçek, layout sonrası rect'i.** Bu kanıtlandı: `NexumLayoutDiag` aynı
nesneleri iki rejimde ölçtü.

```
NEXUM_DIAG_REJIM_A: canvas  640x480   scaleFactor 0,250   SafeAreaRoot 1080x1920   (LayoutCheck rejimi)
NEXUM_DIAG_REJIM_B: canvas 1080x1920  scaleFactor 1,000   SafeAreaRoot 1080x1920   (kamera rejimi)

NEXUM_DIAG_FARK: SettingsPanel        A 1080,0x1920,0   B 1080,0x1920,0
NEXUM_DIAG_FARK: GlassBackground      A 1080,0x1920,0   B 1080,0x1920,0
NEXUM_DIAG_FARK: ModeContainer        A  800,5x343,9    B  800,5x343,9
NEXUM_DIAG_FARK: NormalModeBtn        A   56,0x56,0     B   56,0x56,0
NEXUM_DIAG_FARK: ResetProgressButton  A  388,8x52,1     B  388,8x52,1
```

**Tek bir nesnede bile fark yok.** İki rejim arasındaki tek ayrım
`scaleFactor` (0,250 / 1,000) ve bu yalnızca `Screen.*` veya
`canvas.scaleFactor` okuyan koda dokunur — rect hesabına değil.

Yani **ölçüm rejimi masum.** `LayoutCheck` doğru sayıyı okuyor; yanlış
şeyi iddia ediyor. 56×56'yı görüyor ve "sorun yok" diyor.

Bir ek gözlem, aynı ayrımın başka bir yüzü: `NEXUM_DIAG_BILESEN` dökümü
layout yeniden kurulmadan alındığı için `NormalModeBtn`'i 479,7×117,8
(serileştirilmiş değer) gösteriyor; `Canvas.ForceUpdateCanvases()`
sonrası aynı nesne 56×56. Sahne dosyasındaki sayıya bakarak karar vermek
bu yüzden yanıltır.

---

## 3. Taşlar — kök sebep

### Sprite'lar bağlandı mı? Evet.

`detay_taslar.png` dört seviyeyi de gösteriyor: L1 lacivert + gümüş kenar,
L2 turkuaz, L3 açık mavi, L4 altın + köşe çentikleri. Kopma yok;
`NexumTileVisuals` 26 prefab'a doğru sprite'ı yazmış ve `TileVisual.level`
tarif grafiğinden türeyen değeri taşıyor.

Yani "düz koyu kare" şikâyeti sprite'ın eksikliğinden değil,
**ayırt edilemezlikten** geliyor. Render pikselinden ölçüm (çerçeve
rengi kontrastı):

| çift | kontrast |
|---|---|
| L1 ↔ L2 | 1,89:1 |
| L2 ↔ L3 | 1,72:1 |
| L3 ↔ L4 | 1,32:1 |
| **L1 ↔ L3** | **1,10:1** |
| dolu L1 ↔ **boş hücre** (iç alan) | 2,56:1 |
| dolu L1 ↔ **boş hücre** (çerçeve) | **1,51:1** |

Dört taş yan yana konduğunda ayırt edilebilmeliydi (2d-2b kararı). L1 ile
L3 arasında 1,10:1 var — pratikte aynı renk. Bunu ölçen bir test yok.

### Harf rengi neden sarı? Çünkü hiç kimse dokunmadı.

26 prefab'ın harf renkleri, geçişten **önce ne idiyse o**:

| renk | adet | taşlar |
|---|---|---|
| `#FFD700` | 8 | C, Ca, Cl, Fe, H, N, Na, O |
| `#C0C0C0` | 8 | CO2, CaCO3, CaO, Fe2O3, H2CO3, H2O, NH3, NaCl |
| `#00FF00` | 7 | C2, Cl2, Fe2, H2, N2, O2, O3 |
| `#60E8F5` | 3 | CO, NH, NO |

Tasarımda beyazdı: `docs/ui-kit/tiles-sheet-v2.png`'de en sık açık piksel
saf beyaz (25 631 piksel, formül metni); altın yalnızca L4 çerçeve
vurgusunda (~12 600 piksel).

Zincir iki yerden kopuyor, ikisi de sessiz:

1. **`NexumTileVisuals.cs`** `img.color`'ı ve `tmp.fontSize`'ı yazıyor ama
   **`tmp.color`'a hiç dokunmuyor.**
2. **`NexumColorTokens.cs`**'teki 18 satırlık eşleme tablosunda bu dört
   rengin **hiçbiri yok** — oysa `docs/DESIGN_TOKENS.md:145-148` üçü için
   hedefi zaten yazmıştı (`#00FF00 → success`, `#C0C0C0 → textPrimary`,
   `#60E8F5 → primary/secondary`). Karar belgede var, tabloda yok.
   Belgedeki eşleme ile koddaki tabloyu karşılaştıran bir test olmadığı
   için fark hiç görünmedi.

### Üçüncü kusur: formül taşmıyor sanıldı, taşıyor

Render pikselinden ölçüm (çerçeve kenarını dışlayan orta bantta):

| taş | çizilen harf genişliği | 200 birimlik taşın |
|---|---|---|
| `Fe₂O₃` | 181 birim | **%90** |
| `CaCO₃` 1. satır ("CaCO") | 176 birim | **%88** |
| `CaCO₃` 2. satır ("₃") | 16 birim | %8 |

**`CaCO₃` iki satıra kırılıyor.** `Fe₂O₃` %90'da — projenin kendi %85
kuralının üstünde. `TileTextFitTests` ise yeşil ve "en kötü doluluk %79"
diyor. Sebebi 5. başlıkta.

Buna bir de şu ekleniyor: taşın TMP kutusu her seviyede sabit 200×200
(`anchor 0,0-1,1`, `sizeDelta 0,0`), ama çerçevenin **düz iç alanı**
seviyeyle daralıyor —

| seviye | düz iç alan (512 px kaynakta) | 200 birimlik taşta |
|---|---|---|
| L1 | 486×486 (%95) | 190 birim |
| L2 | 440×440 (%86) | 172 birim |
| L3 | 258×258 (%50) | 101 birim |
| L4 | 180×180 (%35) | **70 birim** |

Metin 200'e göre ölçülüyor, göze 70'e göre görünüyor. En uzun formüller
(`Fe₂O₃`, `CaCO₃`) tam da en dar çerçeveli L4'te.

---

## 4. Tarif defteri butonu

`detay_tarif-defteri-butonu.png` · ana menüdeki tek "kit çerçevesi +
ayrı TMP etiketi" butonu bu. Diğer dördü (OYNA, NASIL OYNANIR,
ANSİKLOPEDİ, AYARLAR) metni görselin içinde taşıdığı için swap tablosuna
bilerek alınmamıştı — onlar sağlam görünüyor.

Ölçüm:

* etiket rengi: `#323232` = `textOnLight` (`NEXUM_DIAG_DEFTERBTN`)
* yüzey: `btn_secondary_normal`'ın koyu dolgusu, render'dan
  örneklendi — etiket bandı boyunca `#274150` civarı
* **kontrast: 1,09:1 – 1,15:1 (ortalama 1,12:1)**

Aynı yüzeyde alternatifler: `textPrimary` 10,72:1 · `textSecondary` 5,17:1
· `textMuted` 1,51:1.

### `#515961` / `textMuted` tartışmasındaki 1,65:1 bu mu? **Hayır.**

O ölçüm `HowToPlayPanel`'in altı kategori butonuna aitti ve
`NexumColorTokens.cs:39-45`'te yorum olarak duruyor:

```
eski yüzey #A2E6F6 (açık): textMuted 5.15:1 ✓ · textSecondary 1.50:1 ✗
yeni yüzey #213B4A (koyu): textMuted 1.65:1 ✗ · textSecondary 5.66:1 ✓
```

O altı bileşen bu yüzden `textSecondary`'de **bırakıldı** ve
`nasil-oynanir_1080x1920.png`'de hepsi okunuyor.

Ama **arıza sınıfı aynı**: sprite geçişi yüzeyi açıktan koyuya çevirdi,
metin rengi gözden geçirilmedi. Orada elle yakalandı, burada yakalanmadı
— çünkü `RecipeBookButton` o incelemenin konusu olan `#515961` listesinde
değildi; rengi `#323232`.

Not: render'da butonun **tamamen boş** görünmesi ayrı bir şey — etiket
`recipebook.button` anahtarıyla çalışma zamanında doluyor
(`CodeStrings.cs:41,116` → "TARİF DEFTERİ"). Cihazda yazı var, sadece
görünmüyor.

---

## 5. Ölçüm aygıtının kendisi

### Farkın tam sebebi

> **Her yerleşim testi `RectTransform` karşılaştırır. Oyuncunun gördüğü
> çerçeveyi ise bir sprite çizer. Hiçbir test tek bir piksel okumaz.**

Metal kiti geçişi tanım gereği bu boşluktan geçti: dikdörtgenleri korudu
(öyle tasarlanmıştı — "konum ve boyut değişmeyecek"), yalnızca pikselleri
değiştirdi. Aygıt, değişen şeyi ölçmüyor.

Bu genel ifade dört somut delikte toplanıyor:

**D1 — Sprite ölçüleri test içinde sabit yazılı.**
`SettingsLayoutTests` `796×1280` ve boyalı payları sabit tutuyor; sprite
`420×300` + border 28 oldu. Test, ekranda olmayan bir görseli doğruluyor.

**D2 — İddia kümesi "içeride mi / çakışıyor mu" ile sınırlı.**
Çökme bu iki ölçütü de iyileştirdiği için **çökme ödüllendiriliyor.**
Asgari boyut, tasarlanan boyuta eşitlik, dokunma hedefi — hiçbiri yok.

**D3 — Ölçülen nesneler elle yazılmış listelerden geliyor.**
`Content[11]`, `HudOgeleri[]`, `PanelParts[]`… Çöken üç buton hiçbirinde
yok. Liste, adını anmadığı kusuru yakalayamaz.

**D4 — Sığma kuralı yalnızca YÜKSEKLİK ölçüyor.** En sinsi olan bu.

`LayoutCheck.cs:156-162`:

```csharp
public static float FillRatio(TMP_Text tmp, string content)
{
    float boxH = tmp.rectTransform.rect.height;
    float boxW = tmp.rectTransform.rect.width;
    if (boxH <= 0f) return 0f;
    return tmp.GetPreferredValues(content, boxW, 0f).y / boxH;   // <-- .y / boxH
}
```

`boxW` bir **ölçü** değil, bir **satır kırma kısıtı** olarak geçiliyor.
Dönen değer yükseklik oranı. Genişlik hiçbir zaman ölçülmüyor.

`LayoutCheck.cs:126-151`, `IsClipped`:

```csharp
if (tmp.enableWordWrapping)
{
    needW = drawn.x;    // <-- TMP zaten kutuya sigdirmak icin kirdi
    needH = Mathf.Max(tmp.GetPreferredValues(tmp.text, boxW, 0f).y, drawn.y);
}
```

Satır kırma açıkken `needW` = **çizilmiş** genişlik; TMP metni kutuya
sığacak şekilde kırdığı için bu değer tanımı gereği `boxW`'yi aşamaz.
Yani **kırma açıkken `IsClipped` genişlik sorununu bildiremez.**
Taşların `enableWordWrapping = 1`.

İkisi birleşince `CaCO₃`:

1. 70 puntoda 200 birime sığmıyor → TMP `<sub>` sınırından **kırıyor**
2. `IsClipped`: `needW = drawn.x ≤ 200` → kırpık değil
3. `FillRatio`: iki satır ≈ 158 / 200 = **%79** → %85 sınırının altında
4. "Tek satır olmalı" diye bir iddia yok

→ Test yeşil, ekranda `CaCO` / `₃`. Testin kaydettiği "en kötü doluluk
%79" ile ölçülen %88 genişlik aynı kusurun iki yüzü.

Bir not daha: `FillRatio` aynı yardımcıdan hem ipucu kutusuna (807×350,
paragraf — yükseklik doğru ölçüt) hem taşa (200×200, tek sembol —
genişlik doğru ölçüt) uygulanıyor. Tek metrik, iki farklı iş.

### Bu farkı kapatmak için ne gerekir — ÖNERİ (uygulanmadı)

Önem sırasına göre; her biri bu adımda bulunan bir kusuru tekrar edilemez
kılar.

| # | Öneri | Hangi kusuru yakalar | Maliyet |
|---|---|---|---|
| **Ö1** | **Piksel regresyonu.** `NexumDeviceShot` rutine alınır; `docs/device-shots/` referans olur, her PR'da yeniden üretilip piksel farkı eşiğin üstündeyse test düşer. | **Beşini birden.** Beş kusurun beşi de gözle görünür; hiçbiri rect karşılaştırmasıyla ifade edilemez. | Orta — gürültü eşiği kalibrasyonu gerekir (font atlası, sıkıştırma) |
| **Ö2** | **Genişlik birinci sınıf sığma kuralı olur.** `FillRatioW = GetPreferredValues(text, ∞, 0).x / boxW ≤ %85`, kırma **kapalı** ölçülür. Taş ve tek satırlık etiketlerde ayrıca `textInfo.lineCount == 1` iddiası. | `CaCO₃` kırılması, `Fe₂O₃` %90 | Düşük |
| **Ö3** | **Test sprite'ı okur, sabit tutmaz.** `SpriteW/SpriteH/Inner*` yerine canlı `Image.sprite.rect`, `.border` ve `image.type`'tan türetilir. | Ayarlar kartı, duraklat kartı — ve bundan sonraki her sprite değişimi | Düşük |
| **Ö4** | **Çökme iddiası.** Layout zincirindeki her nesne için `rect.size ≥ tasarlanan asgari` (ve butonlarda ≥ 44×44 dp dokunma hedefi). | Üç mod butonu, `ResetProgressButton` | Düşük |
| **Ö5** | **Ölçülen küme elle yazılmaz.** Panelin tüm alt ağacı gezilir; muafiyet açıkça listelenir (bugünün tersi). | Torun nesnelerdeki her kusur | Düşük |
| **Ö6** | **Kontrast iddiası, gerçek yüzeyin pikselinden.** Metnin arkasındaki piksel render'dan örneklenir, ≥ 4,5:1 istenir. | Tarif defteri butonu 1,12:1 | Orta — Ö1'in altyapısını kullanır |
| **Ö7** | **Metin, çerçevenin görünür iç alanına göre ölçülür**, TMP rect'ine göre değil. Seviye başına iç alan sprite'tan türetilir. | L4'te 200 kutu / 70 görünür iç alan | Orta |
| **Ö8** | **Belge ile tablo karşılaştırılır.** `docs/DESIGN_TOKENS.md`'deki eşleme ile `NexumColorTokens.Tablo` ayrışırsa test düşer. | Dört taş harf rengi | Düşük |
| **Ö9** | **Seviye ayırt edilebilirliği ölçülür.** Komşu seviye çiftleri arasında asgari kontrast; gri tonlamada da. | L1↔L3 1,10:1, dolu↔boş 1,51:1 | Orta |

**Ö1 tek başına beşini de yakalardı; Ö2–Ö9 ise kusuru *isimlendirir*** —
piksel farkı "bir şey değişti" der, diğerleri "ne, nerede, ne kadar" der.
İkisi birlikte gerekli.

### Uygulanmayanın kaydı

Bu adımda **hiçbir düzeltme yapılmadı** (istenen buydu). Bulunan ve açık
kalan kusurlar:

1. Ayarlar: üç mod butonu 56×56'ya çöktü
2. Ayarlar / duraklat: 9-slice çerçeve kartın görünür sınırını yok etti
3. Taşlar: harf renkleri (4 aile, 26 prefab) temaya bağlanmadı
4. Taşlar: `CaCO₃` iki satıra kırılıyor, `Fe₂O₃` %90 genişlikte
5. Taşlar: L1↔L3 1,10:1, dolu↔boş 1,51:1 — ayırt edilemiyor
6. Tarif defteri butonu: etiket 1,12:1 kontrastta
7. Tarif defteri satırları: `panelBackground` `#2E3240`, kit dilinin dışında
8. Ölçüm aygıtı: Ö1–Ö9

**Silinmesi gereken dosya yok.** (Silme yetkim yok; olsaydı burada
listelenirdi.)
