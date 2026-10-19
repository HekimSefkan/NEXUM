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

---

# 7. Metal kiti — derinlik (Faz 1 / Adım 2d-2)

Varyant **C (Metal)** seçildi. Bundan sonra üretilen her görsel bu dildedir;
A ve B tabloda tarih olarak kalır, `ACTIVE_VARIANTS` yalnızca C'yi üretir.

## 7.1 Eklenen efektler (öncesi → sonrası)

| Efekt | Önce | Sonra | Neden |
|---|---|---|---|
| Zemin | düz dolgu | **dikey gradyan, `gradient = 0.34`** (üst ×1.19 açık, alt ×1.15 koyu) | metal plaka hissi; düz dolgu "boyanmış kutu" gibi duruyordu |
| İç kabartma | yok | **`emboss = 0.55`, `emboss_px = 2`** — iç kenarın üstünde beyaz %55, altında siyah %70 şerit | kenar kalınlığı tek başına yüzeyi anlatmıyordu |
| Basılı durum | yalnızca zemin ×0.72 | **ışık yer değiştirir**: gradyan ters döner, kabartma şeritleri yer değiştirir, bezel ters döner | gerçek "içeri çökme"; renk koyulaşması tek başına basılma okutmuyor |
| Kenarlık | 7 birim | **8 birim** | kabartma şeridine yer açmak için |
| Dış parıltı | gövdenin üstüne de düşüyordu | **yalnızca gövdenin dışında** | hayalet buton dolu görünüyordu |

## 7.2 İkinci vurgu rengi

Her şey camgöbeği olmasın; hiyerarşi **renkten de** okunsun:

| Rol | Renk | Kullanım |
|---|---|---|
| `primary` | **Primary** `#00B4D8` | birincil eylem (DENEYİ BAŞLAT, DEVAM ET) |
| `secondary` | **Warning** `#FFD700` (altın) | ikincil eylem (ANSİKLOPEDİ, AYARLAR, PARÇALA) |
| `ghost` | TextSecondary `#ADB5BD` | düşük vurgulu eylem (GERİ AL, VAZGEÇ) |
| `destructive` | Danger `#E63946` | geri alınamaz eylem (İLERLEMEYİ SIFIRLA) |
| `info` | Secondary `#90E0EF` | bilgi yüzeyleri |

Altın seçildi çünkü oyunun mevcut kimliğinde zaten var (katalizör tüpü,
yıldızlar) ve camgöbeğiyle en yüksek ayrımı veren palet rengi.

## 7.3 Panel kuralı — boyalı yuva yasak

**Panel çerçevelerinde boyalı buton yuvası olmaz.** Bugünkü `gamepanel.png`
alt kenarında iki boyalı yuva taşıyor; duraklat kartına üçüncü butonun
eklenememesinin ölçülmüş sebebi buydu. Kit panelleri saf 9-slice çerçevedir,
butonlar üstüne serbest yerleşir.

---

# 8. Taş dili (Faz 1 / Adım 2d-2)

## 8.1 Seviye çerçeveden okunur

| Seviye | Halka | Kenarlık | Köşe braketi | Kenar rengi |
|---|---|---|---|---|
| 1 — saf element | 1 | 6 birim | yok | TextSecondary (çelik) |
| 2 — ara bileşik | 2 | 7 birim | yok | Primary (camgöbeği) |
| 3 — ileri bileşik | 3 | 9 birim | **var** | Warning (altın) |

**Renk tek başına ayırt edici değildir** (renk körlüğü): halka sayısı,
kenarlık kalınlığı ve köşe braketi seviyeyi biçimden de okutur.

## 8.2 Merkez sakin

Zemin yalnızca çok hafif dikey gradyan taşır (üst +%16, alt −%18); desen yok.
Bugünkü parçacıklı küre formülü yutuyordu. Ölçüm: merkez ile beyaz metin
arasındaki kontrast **8,05 – 13,68 : 1** (WCAG hedefi 4,5:1).

## 8.3 Durumlar

| Durum | Kenar | Zemin | Hale | Vurgu halkası |
|---|---|---|---|---|
| normal | ×1.00 | ×1.00 | yok | — |
| selected | ×1.00 | ×1.12 | 0.85 | PrimaryBright |
| merging | ×1.00 | ×1.75 | 1.00 | TextPrimary |
| newborn | ×0.70 | ×0.80 | yok | — |
| discovered | ×1.00 | ×1.06 | 0.60 | Warning |

Animasyon **zamanlamaları değişmez**; bunlar yalnızca sprite durumlarıdır.

## 8.4 Alt simge kuralı — ölçümle

Literal alt simge karakterleri (U+2080–2089) **kullanılamaz**:

| Font | ₀₁₂₃₄ | ₅₆₇₈₉ |
|---|---|---|
| Oxanium (TTF) | var | **yok** |
| Orbitron (TTF) | yok | yok |
| LiberationSans (TTF) | yok | yok |
| **Her statik TMP atlası** | **yok** | **yok** |

Yani `₂` bugün hiçbir atlasta yok, `₅`–`₉` fontun kendisinde de yok.
**Karar: TMP `<sub>` etiketi kullanılmaya devam edilir** — `<sub>` normal
rakam glifini küçültüp alçalttığı için ek glif gerektirmez. ElementData
sembolleri zaten böyle yazılmış (13 bileşikte `<sub>`). Bu karar
`TileTextFitTests` ile her koşuda korunur: literal alt simge görülürse test
düşer.

## 8.5 Boyut

Sabit kare sprite (9-slice **değil**), üç çözünürlük: **128 / 192 / 256**.
Taş çalışma zamanında 200 birimde çiziliyor; 256 kaynak, doku import
kuralındaki 1,5 kat şartını sağlar.

---

# 9. Taş ailesi v2 ve hub mimarisi (Faz 1 / Adım 2d-2b)

## 9.1 İki katmanlı taş

Teşhis: tek katmanlı taş butonlarla aynı malzemedendi; tahta tuş takımı gibi
okunuyordu. Taş, arayüz kromundan **ayrı bir malzeme sınıfı** olmalı.

| Katman | Ne yapar | Nasıl |
|---|---|---|
| **DIŞ** — metal tutucu | taşı arayüze **bağlar** | kitin pahı (%13), kenar vurgusu, bezel (mat/fırçalanmış/parlak) |
| **İÇ** — ışıyan numune | taşı arayüzden **ayırır** | radyal gradyan + iç gölge ("kuyuda duran numune") |

**Işıma KENARDA, merkez koyu.** Ölçüm: merkez parlakken L4 kontrastı **1,31:1**'e
düşüyordu (sınır 4,5:1). Işımayı kenara alınca en kötü değer **11,18:1**.
Fiziksel olarak da doğru — ışık kuyunun kenarında birikir.

## 9.2 Dört seviye, üç sinyal

| Seviye | Derinlik | Taş | Çentik | Kenarlık | Malzeme | Işıma |
|---|---|---|---|---|---|---|
| 1 | 0 | 8 | 0 | 7 | mat | 0,00 |
| 2 | 1 | 10 | 1 | 8 | fırçalanmış | 0,24 |
| 3 | 2 | 4 | 2 | 9 | fırçalanmış | 0,46 |
| 4 | 3+ | 4 | 3 | 11 | parlak | 0,66 |

Eşikler `TileLevels.LevelThresholds = {0, 1, 2, 3}` — **parametre**, beşinci
seviye tek değer eklenerek gelir.

**Renk tek başına asla ayırt edici değildir.** Kanıt: `tiles-sheet-v2.png`
içindeki **gri tonlama ayrım şeridi** — renk kaldırıldığında dört seviye
parlaklık merdiveni ve çentik sayısıyla hâlâ ayırt edilir.

## 9.3 Çözünürlük

Taş çalışma zamanında **200 birim**de çizilir. Üretilen boyutlar
**256 / 384 / 512** → 1080 genişlikte 1:1'in üstünde piksel yoğunluğu
(256/200 = 1,28×; 1440 genişlikte 512 kaynak 1,92×).

## 9.4 Arka plan seçenekleri

| Seçenek | Ortalama parlaklık | Taş çerçevesi | Buton kenarı | Beyaz metin |
|---|---|---|---|---|
| A — foto + güçlü işleme | 36 | 10,31:1 | 6,23:1 | 15,36:1 |
| B — tamamen prosedürel | 55 | 7,93:1 | **4,79:1** | 11,81:1 |
| C — hibrit | 37 | 10,25:1 | 6,20:1 | 15,27:1 |

Üçü de 4,5:1 eşiğini geçiyor; B en dar paya sahip.

## 9.5 Hub mimarisi

Düz liste yerine **ağırlık kademeli hub**:

| Kuşak | Bileşen | Görsel ağırlık |
|---|---|---|
| Üst şerit | profil kartı (avatar, rütbe, XP, seri) | orta |
| **Kahraman** | DENEYİ BAŞLAT + bölüm no + yıldızlar | **en baskın** (300 birim, `selected`) |
| İkili kart | Günlük Bulmaca · Bölüm Seçimi | orta (250 birim) |
| Mod satırı | Sınav · Sonsuz Lab · Haftanın Deneyi | orta-düşük (190 birim, `tab`) |
| Bilgi | Ansiklopedi · Tarif Defteri | düşük (160 birim, altın) |
| İkon şeridi | Ayarlar · Nasıl Oynanır · Mentor · Ses | **en sakin** (134 birim ikon) |

**Gizleme bayrağı:** her girişin `yeni_oyuncuda_gorunur` alanı var. Yeni
oyuncuya kilitli girişler **gösterilmez** (sekiz kilit yerine üç açık giriş);
düzen yine de bugünden oturur, giriş açıldığında yeri hazırdır.

## 9.6 İpucu butonu

`GERİ AL · PARÇALA · İPUCU` üçlüsü, her biri 300×112, aralıklar **eşit 50
birim**, sağ kenar payı 40. Kalan hak rozeti butonun sağ üst köşesinde ayrı
katman — Faz 5'te kademeli ipucu geldiğinde kademe göstergesi aynı yere girer.

Dokunma kutusu **144 birime** tamamlanır (görsel 112): y 294..438.
Ölçüm — hedef çipine 12 birim, tahtaya 36 birim pay, **çakışma yok**.
