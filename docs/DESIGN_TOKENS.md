# NEXUM – Tasarım Tokenları

**Asset:** `Assets/Data/Theme/NexumTheme.asset` · **Sınıf:** `Assets/Scripts/Theme/NexumTheme.cs`
**Kurucular:** `Assets/Editor/UIBuilder/UITokens.cs` tema asset'inden okur.

> Değerler **uydurulmadı**: mevcut iki sahne ve 30 prefab sayılarak çıkarıldı.
> Her token'ın yanında kaç bileşende geçtiği yazıyor.

---

## 1. Envanter — bu tokenlar nereden geldi

### Renk (TMP yazı rengi, bileşen sayısı)

| Renk | Kullanım | Token |
|---|---|---|
| `#FFFFFF` | 30 | **TextPrimary** |
| `#323232` | 19 | **TextOnLight** |
| `#F8F9FA` | 10 | — (beyaza çok yakın, aykırı) |
| `#00B4D8` | 8 | **Primary** |
| `#FFD700` | 8 | **Warning** |
| `#C0C0C0` | 8 | — (aykırı) |
| `#00FF00` | 7 | — (aykırı, saf yeşil) |
| `#515961` | 6 | — (aykırı) |
| `#ADB5BD` | 6 (+7 etiket) | **TextSecondary** |
| `#000000` | 6 | — (gölge/kontur) |
| `#C4FFB9` | 5 | — (aykırı) |
| `#007DD8` | 4 | — (aykırı mavi) |
| `#90E0EF` | 3 | **Secondary** |
| `#2E3240` | 3 | **PanelBackground** |
| `#60E8F5` | 3 | — (aykırı camgöbeği) |
| `#8FD8FF` | 2 | **PrimaryBright** |
| `#E63946` | 1 | **Danger** |
| `#0B2A3D` | 1 | **CardBackground** |

### Zengin metin etiketleri (kod + sözlük)

| Etiket | Kullanım | Not |
|---|---|---|
| `red` | 11 | Adlandırılmış renk — token değil |
| `#ADB5BD` | 7 | TextSecondary ile aynı ✔ |
| `#2ECC71` | 6 | **Success** |
| `#F1C40F` | 4 | Warning'den farklı sarı (aykırı) |
| `#48CAE4` | 2 | Aykırı camgöbeği |
| `green` | 2 | Adlandırılmış renk — token değil |

### Punto (TMP bileşen sayısı)

| Punto | Kullanım | Token | Örnek |
|---|---|---|---|
| 80 | 28 | **Display** | Taş etiketi (`H₂O`, `NaCl`) |
| 36 | 22 | **Body** | Gövde metni, satır etiketi |
| 40 | 19 | **Subtitle** | Giriş alanı, alt başlık |
| 50 | 16 | **Title** | `ÖĞRENME TERCİHLERİ`, `SES AYARLARI` |
| 60 | 10 | **Value** | Bileşik adı, sayısal değer |
| 30 | 8 | **Label** | `NORMAL MOD`, `Kimlik No` |
| 45 | 8 | — | aykırı |
| 42 | 6 | — | aykırı |
| 35 / 64 / 100 / 28 / 24 / 14 / 70 / 55 | 1–4 | — | aykırı |

### Font asset

| Font | Bileşen | Not |
|---|---|---|
| **LiberationSans SDF** | **72** | TMP'nin **varsayılanı** — marka fontu atanmamış |
| Oxanium (Regular/SemiBold/Medium) | 43 | Gövde ailesi |
| Orbitron (Bold/Medium/SemiBold) | 21 | Başlık ailesi |

---

## 2. Tokenlar

### Renk paleti

| Token | Değer | Ne zaman |
|---|---|---|
| `primary` | `#00B4D8` | Marka camgöbeği: birincil butonlar, etkin durum |
| `primaryBright` | `#8FD8FF` | HUD vurgusu, bölüm başlığı, katalizör etiketi |
| `secondary` | `#90E0EF` | İkinci düzey vurgu, pasif ikon |
| `danger` | `#E63946` | Hata, kayıp, geri alınamaz işlem |
| `success` | `#2ECC71` | Başarı, doğru cevap, hazır durum |
| `warning` | `#FFD700` | Bedel, sayaç, dikkat |
| `textPrimary` | `#FFFFFF` | Koyu zeminde ana metin |
| `textSecondary` | `#ADB5BD` | Koyu zeminde açıklama, ipucu |
| `textOnLight` | `#323232` | Beyaz/açık kart üzerinde metin |
| `panelBackground` | `#2E3240` | Panel zemini |
| `cardBackground` | `#0B2A3D` | Kart zemini |

### Tipografi ölçeği

| Rol | Punto | Font | Ne zaman |
|---|---|---|---|
| `display` | 80 | Orbitron-Bold | Taş ve bileşik etiketi |
| `title` | 50 | Orbitron-SemiBold | Bölüm başlığı |
| `subtitle` | 40 | Oxanium-SemiBold | Alt başlık, giriş alanı |
| `body` | 36 | Oxanium-Regular | Paragraf, açıklama |
| `label` | 30 | Oxanium-Medium | Buton metni, küçük etiket |
| `value` | 60 | Orbitron-Bold | Sayısal değer, bileşik adı |

> Senin istediğin beş rolün (başlık / alt başlık / gövde / etiket / değer) üstüne
> **display** eklendi: 80 punto tek başına en çok kullanılan boyut (28 bileşen,
> taş etiketleri) ve beş rolün hiçbirine girmiyordu.

### Boşluk ölçeği

`xs 8` · `sm 16` · `md 24` · `lg 40` · `xl 64`

### Çerçeve

| Token | Değer |
|---|---|
| `radiusSmall` | 12 |
| `radiusMedium` | 24 |
| `radiusLarge` | 40 |
| `borderThin` | 2 |
| `borderThick` | 4 |

> Köşeler şu an sprite'ların **içinde çizili**; bu değerler yeni çizilecek
> yüzeyler için referanstır, mevcut görsellere uygulanmıyor.

### Hareket

| Token | Değer | Nerede |
|---|---|---|
| `durationShort` | 0.15 | `DOPunchScale`, dokunma geri bildirimi |
| `durationMedium` | 0.30 | `DOScale`, panel açılışı |
| `durationLong` | 0.50 | Kazanma ekranı, vurgu döngüsü |
| `easeAppear` | `OutBack` | Belirme |
| `easeMove` | `OutQuad` | Kayma |
| `easeDisappear` | `InBack` | Kaybolma |
| `easeFill` | `OutBounce` | Doluluk göstergesi |

---

## 3. Aykırı değerler — **ŞİMDİ DOKUNULMAYACAK**

Faz 6 / Adım 2'de ele alınacak. Hiçbiri bu turda değiştirilmedi.

### Renk sapmaları

| Aykırı | Yakın token | Nerede |
|---|---|---|
| `#FFA300` turuncu | yok | **PARÇALA (Joker) butonu** — Game.unity |
| `#4EE145`, `#4EE045`, `#6DE847`, `#44E453`, `#5CFF56`, `#87CF5D`, `#269501` | `success` | **GERİ AL butonu** ve çeşitli yeşiller — 7 ayrı ton |
| `#00FF00` (7), `#C4FFB9` (5), `#C3FFB8` | `success` | Saf/pastel yeşil |
| `#90E0EF`, `#60E8F5`, `#48CAE4`, `#007DD8` | `primary` / `secondary` | Camgöbeği ailesi 4 ayrı tona dağılmış |
| `#F1C40F` | `warning` (`#FFD700`) | İki farklı sarı |
| `#F8F9FA`, `#DADADA`, `#D0D0D3`, `#C0C0C0`, `#E4E1E1`, `#D9D7D7` | `textPrimary` | Beyaza yakın 6 ton |
| `#515961`, `#5B5B5B`, `#3F3F3F`, `#2E3240`, `#0D0C0C` | `panelBackground` | Gri/koyu aile |
| `#085D73`, `#00B4FF` | `primary` | Prefab içi camgöbeği |
| `red`, `green` (adlandırılmış) | `danger`, `success` | Kodda 13 çağrı |

### Punto sapmaları

| Punto | Bileşen | Yakın token |
|---|---|---|
| 45 | 8 | `subtitle` (40) |
| 42 | 6 | `subtitle` (40) |
| 35 | 4 | `body` (36) |
| 64 | 4 | `value` (60) |
| 100 | 4 | `display` (80) |
| 28 | 2 | `label` (30) |
| 24 | 2 | `label` (30) |
| 70, 55, 14 | 1'er | — |

### Font sapması

**72 TMP bileşeni hâlâ `LiberationSans SDF` (TMP varsayılanı) kullanıyor** —
yani sahnedeki metinlerin yarısından fazlasına marka fontu hiç atanmamış.
Tek kalem en büyük görsel borç.

---

## 4. Kurallar

1. **Kurucular temadan okur.** `UITokens` tek kapı; kurucuların içine renk kodu
   veya punto yazılmaz. Değer değişikliği asset'te yapılır, kod değişmez.
2. **Validator kuralı (WARN):** `managed.json`'da kayıtlı — yani UIBuilder ile
   kurulan — bir objede tema dışı renk veya punto varsa uyarı verilir.
   Elle yapılmış eski objeler **kapsam dışı**: borçları yukarıdaki listede.
   Doğrulandı: tema dışı punto taşıyan bir obje kayda eklendiğinde kural
   `tema disi punto 45` uyarısını üretiyor.
3. **Geometri temaya ait değil.** Cam kartın oranı ve iç kenarları belirli bir
   sprite'ın ölçüleri; `UITokens` içinde sabit kalır.
4. **Yeni renk/punto eklemek** istiyorsan önce bu belgedeki envantere bak:
   zaten yakın bir token varsa onu kullan, yoksa token olarak ekle.

---

# 5. Bileşen dili (Faz 1 / Adım 2d-1)

> Bu bölüm **tasarım dilinin yazılı tanımıdır**. Sayısal değerler
> `Tools/generate_ui_kit.py` içindeki `VARIANTS` sözlüğünden gelir; kanıt
> sayfaları `docs/ui-kit/variant-*.png`. Renkler **yalnızca** yukarıdaki
> paletten çıkar — üreteç paleti `NexumTheme.asset`'ten okur, uydurmaz.

## 5.1 Hedef

Temiz **analitik laboratuvar cihazı arayüzü**. İşlemeli/fantezi değil; ölçülü,
keskin, okunabilir. Çerçeve bilgiyi taşır, bilginin önüne geçmez.

## 5.2 Köşe geometrisi

| Varyant | Köşe | Değer |
|---|---|---|
| A · Çizgi | pah (chamfer) | 10 birim |
| B · Cam | yarıçap (radius) | 20 birim |
| C · Metal | pah (chamfer) | 14 birim |

Kural: **bir varyant içinde tek bir köşe dili** kullanılır; pah ve yarıçap
karıştırılmaz. Yuvarlak kontroller (ikon butonu, anahtar topuzu, halka) bu
kuralın dışındadır — onlar daire.

## 5.3 Çerçeve kalınlığı ve parıltı

| Varyant | Kenarlık | Dış parıltı (yarıçap / güç) | İç parıltı şeridi |
|---|---|---|---|
| A | 2 birim | yok | yok, yerine köşe işaretleri |
| B | 3 birim | 11 / 1.0 | var (üst kenarda, kenarlığın 1.1 katı) |
| C | 8 birim | 4 / 0.18 | yok, yerine açık–koyu bezel |

- **Dış parıltı yalnızca gövdenin DIŞINDA** çizilir. Gövdenin üstüne de
  düşseydi hayalet (ghost) buton dolu görünürdü.
- **İç parıltı şeridi üst kenar şeridinde kalır** — 9-slice'ta üst kenar
  yalnızca yatayda gerilir, böylece şerit her boyda aynı kalınlıkta görünür.
- **Köşe işaretleri (A)** köşe karolarında durur; gerilmezler.

## 5.4 Yükseklik kademeleri

| Kademe | Yükseklik | Kullanım |
|---|---|---|
| Büyük | **96 birim** | birincil eylem (DENEYİ BAŞLAT) |
| Orta | **80 birim** | ikincil menü butonları, sınav şıkkı |
| Küçük | **56 birim** | çip, etiket, satır içi eylem |
| Satır | **92 birim** | liste satırı (tarif defteri) |
| Çubuk | **36 birim** | ilerleme çubuğu |

Kural: bir ekranda **en çok iki kademe** yan yana kullanılır.

## 5.5 Dokunma hedefi

Android erişilebilirlik tavsiyesi **48dp**. NEXUM'un tasarım alanı 1080×1920
ve referans genişlik 1080 birim; tipik bir telefonda 1080 birim ≈ 360dp, yani
**1dp ≈ 3 birim**.

> **48dp ≈ 144 birim.**

- Dokunulabilir her öğenin **dokunma alanı en az 144×144 birim** olmalıdır.
- Görsel öğe daha küçük olabilir (ör. 56 birimlik çip): bu durumda butonun
  `RectTransform`'u 144 birime tamamlanır, görsel ortalanır.
- İkon butonu bu yüzden **96 birim çizilir ama 144 birimlik kutuda** durur.

## 5.6 Durum renkleri

Dört durum zorunludur. Renk **rolden** gelir (primary / secondary / danger …),
durum yalnızca o rolü **değiştirir**:

| Durum | Zemin parlaklığı | Kenarlık | Genel alfa |
|---|---|---|---|
| normal | ×1.00 | rolün rengi | 1.00 |
| pressed | ×0.72 (içe çöker) | rol + PrimaryBright karışımı | 1.00 |
| disabled | ×0.85 | TextSecondary | **0.45** |
| selected | ×1.28 (öne çıkar) | rol + PrimaryBright karışımı | 1.00 |

Rol → kenarlık rengi: `primary→Primary`, `secondary→Secondary`,
`ghost→TextSecondary`, `destructive→Danger`, `success→Success`,
`warning→Warning`.

## 5.7 Panel zemini ve saydamlık

| Yüzey | Zemin rolü | Opaklık |
|---|---|---|
| Buton | CardBackground | A %92 · B %52 · C %100 |
| Panel / modal | PanelBackground | aynı |
| Hayalet buton | yok | %0 (yalnızca kenarlık) |
| Modal arkası karartma | CardBackground | **%62** |

**Bulanıklık (blur) KULLANILMAZ.** Gerekçe: uGUI'de gerçek arka plan bulanıklığı
ek bir kamera/RenderTexture ister; 60 FPS hedefi ve mobil doldurma oranı
maliyeti buna değmez. Derinlik hissi **karartma + kenar parıltısı** ile verilir.

## 5.8 9-slice sözleşmesi

- **Her çerçeve tek sprite'tır.** Buton başına ayrı PNG üretmek yasak; bugünkü
  dağınıklığın sebebi buydu (522×220, 300×64, 236×55 … hepsi ayrı görsel).
- Kenar payı = `köşe + kenarlık + parıltı yarıçapı + 2`, ve sprite'ın
  yarısını geçemez (ince çubuklarda otomatik kısılır). Paylar
  `Tools/ui-kit/manifest.json` içinde sprite başına yazılıdır.
- **Merkez düz olmalı.** Detay (parıltı şeridi, köşe işareti, bezel) kenar ve
  köşe karolarında durur; merkeze konan desen gerilince bozulur.
- Kanıt: her varyant sayfasının **4. bölümü** aynı sprite'tan 220 / 430 / 760
  birimlik üç buton çizer; köşe ve kenarlık üçünde de birebir aynıdır.

## 5.9 Metin kuralı

**Hiçbir görselde metin basılı olmaz.** Bugün `oyna.png`, `nasıloynanır.png`,
`ansiklopedi.png`, `ayarlar.png` metni görselin içinde taşıyor; bu yüzden
İngilizce sürüm imkânsız ve "TARİF DEFTERİ" için aynı dili kullanan bir buton
üretilemedi. Tüm yazılar TMP etiketidir ve `%85 sığma kuralına` tabidir.

---

# 6. Font kararı (ölçümle)

## 6.1 Ölçüm

Üç aile iki ayrı yerde ölçüldü: **TTF'in kendi `cmap` tablosu** (karakter var mı)
ve **TMP SDF atlası** (çalışma zamanında çizilebiliyor mu). Araç:
`Tools/` altındaki envanter betiği, harici kütüphane kullanmadan `cmap`
okuyor.

### TTF kapsamı — `ş Ş ğ Ğ İ ı ç Ç ö Ö ü Ü`

| TTF | Glif | Türkçe | `→` |
|---|---|---|---|
| Orbitron (Bold/Medium/SemiBold) | 207 | **ş Ş ğ Ğ İ EKSİK** | yok |
| Oxanium (Regular/Medium/SemiBold) | 357 | **tam** | yok |
| LiberationSans | 2294 | **tam** | **var** |

### TMP SDF atlası (çalışma zamanında gerçekten çizilebilen)

| Asset | Karakter | Türkçe eksik | Atlas |
|---|---|---|---|
| Orbitron × 3 | 96 | **12 harfin hepsi** | Static |
| Oxanium × 3 | 97 | **12 harfin hepsi** | Static |
| LiberationSans SDF | 250 | ş Ş ğ Ğ İ ı | Static |
| LiberationSans SDF - Fallback | 6 | — (ç Ç ö Ö ü Ü var) | **Dynamic** |

## 6.2 Bulgular

1. **Hiçbir statik atlasta Türkçe harfler yok.** Bugün ekranda görünen
   ş/ğ/İ/ı harfleri **dinamik fallback** üzerinden LiberationSans ile
   çiziliyor; yani her Türkçe kelimede **iki font karışıyor**. Depoda
   `LiberationSans SDF - Fallback.asset` dosyasının sürekli değişmesinin
   sebebi de bu (çalışma zamanında atlasa glif ekleniyor).
2. **Orbitron Türkçe için elenir.** Sorun atlasta değil **TTF'in kendisinde**:
   `ş Ş ğ Ğ İ` karakterleri font dosyasında yok. Atlası yeniden üretmek
   çözmez. Orbitron bugün 22 bileşende kullanılıyor ve "TARİF DEFTERİ"
   gibi bir metinde **İ** harfini kaybeder.
3. **Oxanium yeterli.** TTF'te 12 Türkçe harfin hepsi var; eksik olan tek şey
   **atlasın hangi karakter kümesiyle üretildiği**. Yeni font indirmeye
   gerek yok.
4. `→` (U+2192) üç ailede de yok — bu yüzden ok zaten TMP sprite'ı
   (`Tools/generate_arrow.py`). Ölçüm bu kararı doğruluyor.

## 6.3 Karar

| Rol | Font | Gerekçe |
|---|---|---|
| Başlık / display | **Oxanium SemiBold** | Türkçe tam, teknik-geniş karakter, mevcut dille uyumlu |
| Gövde / etiket | **Oxanium Regular** (gerekirse Medium) | aynı aile, ikinci ağırlık |
| Fallback | LiberationSans | yalnızca beklenmedik glif için; normalde devreye girmemeli |

**Tek aile, iki ağırlık.** Orbitron metin rollerinden çekilir (logo zaten
görsel). **Yeni font indirilmedi** — gerek kalmadı.

### Yapılacak (2d-2)
Oxanium SDF atlasları **Türkçe + İngilizce tam Latin** karakter kümesiyle
yeniden üretilecek (en az: ASCII + `ÇĞİÖŞÜçğıöşü` + `×–—’…`), atlas **Static**
kalacak. Ardından dinamik fallback devreye girmemeli; bu, atlas dosyasının
git'te sürekli değişmesini de bitirir.

> **Lisans:** Oxanium ve Orbitron Google Fonts üzerinden **SIL OFL 1.1**,
> Liberation ailesi de OFL ile dağıtılıyor; üçü de ticari kullanıma açık.
> Depoda lisans metni **yok** — yayından önce `Assets/Fonts/OFL.txt` olarak
> eklenmeli (2d-2 iş listesinde).
