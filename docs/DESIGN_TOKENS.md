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
