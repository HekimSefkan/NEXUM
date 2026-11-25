# NEXUM — Araştırma Puanı (Faz 2 / 2.1 eki)

**Durum: TASARIM ONAYLANDI (25 Kasım 2026). Kod YAZILMADI,
hiçbir bedel değiştirilmedi.** Uygulama ayrı bir adımda yapılacak ve
oyun hissini değiştiren kısımları (bölüm 3, madde 3) kendi analiz +
onay turunu ister.

Bu belge dört şeyi yapar:
1. Bugünkü ekonomiyi **ölçer** (ne, neyle, kaça ödeniyor).
2. Araştırma Puanı'nın kazanım ve harcama yerlerini tanımlar.
3. Arz/talep dengesini ve öncesi/sonrası tabloyu verir.
4. Verilen kararları ve gerekçelerini kaydeder (bölüm 4).

---

## 1. Bugünkü ekonomi (ölçüm)

### 1.1 Tek para birimi var: **bölüm skoru**

`GameManager.currentScore` hem ekranda gösterilen skor hem de yardımların
ödendiği cüzdan. Bölüm başına sıfırlanır (sahne örneğinde durur), yani
bölümler arası taşınmaz.

`SaveData.totalScore` **ölü kayıt**: `GridManager` bölüm kazanılınca
*kalan* skoru ekliyor, hiçbir ekran göstermiyor, hiçbir şey harcamıyor
(bkz. `docs/STATS_INVENTORY.md`).

### 1.2 Kazanç tarafı

Puan yalnızca **sentezden** gelir: `MergeRecipe.scoreReward`.
Sınav modunda **×2**.

| tarif katmanı | örnek | puan |
|---|---|---|
| iki temel element | `H + H → H₂`, `N + N → N₂` | 10 |
| ara bileşik | `H₂ + O → H₂O`, `Cl₂ + Na → NaCl` | 30 |
| hedef bileşik | `CaO + CO₂ → CaCO₃`, `NH₃ + O₂ → NO` | 50 |

Bölüm başına **kazanılan** puan (ölçüm: `Tools/sim_xp.py`, 80 oyun,
derinlik-2 bot, kazanan oyunların ortalaması):

| | L1 | L2 | L3 | L4 | L5 | L6 | L7 | toplam |
|---|---|---|---|---|---|---|---|---|
| kazanılan puan | 58 | 57 | 81 | 251 | 215 | 204 | 430 | **1296** |
| ortalama hamle | 6 | 8 | 10 | 34 | 27 | 31 | 32 | |

> Bot sezgiseldir; mutlak değer değil **büyüklük sırası** anlamlıdır.
> L4–L7'de kazanma oranı düşük olduğu için o satırlar az örnekten gelir.

### 1.3 Harcama tarafı

| yardım | bedel | sınır | kaynak |
|---|---|---|---|
| **İpucu** | `(kullanılan + 1) × 25` → **25 / 50 / 75** | bölüm başına 3 | `GridManager.baseHintCost`, `maxHints` |
| **Geri al** | **50** (sabit) | `2 + bölümIndeksi / 2` → L1–2: 2, L3–4: 3, L5–6: 4, L7: 5 | `GridManager.undoCost`, `currentUndoLimit` |
| **Joker (parçala)** | taşın `jokerCost` değeri | yok (bütçe kadar) | `ElementData.jokerCost` |

Joker bedeli her taşta **sentez puanının iki katı**:

| taş | sentez puanı | joker bedeli |
|---|---|---|
| temel element (H, O, C, N, Na, Cl, Fe, Ca) | 0 | **30** |
| H₂, O₂, CO, NH, N₂, Cl₂, C₂, Fe₂, H₂O, NaCl | 30 | **60** |
| CO₂, O₃ | 40 | **80** |
| CaO, Fe₂O₃, NH₃ | 50 | **100** |
| CaCO₃, H₂CO₃, NO | 75 | **150** |

**Katalizör** tek indirim kanalı: her **5 sentezde** bir bedava Joker hakkı
(`GridManager.CatalystChargeLimit = 5`). Bedava hak bütçeden düşmez.

Mod farkları: Sınav modunda ipucu **kapalı** (bütçe yetse de verilmiyor),
puan ×2. Serbest modda ipucu **bedava ve sınırsız**, entropi cezası yok.

### 1.4 Ölçümün söylediği: yardım, en çok gerektiği yerde en pahalı

| | L1 | L2 | L3 | L4 | L5 | L6 | L7 |
|---|---|---|---|---|---|---|---|
| kazanılan puan | 58 | 57 | 81 | 251 | 215 | 204 | 430 |
| 3 ipucunun tamamı (150) | **%259** | **%263** | **%185** | %60 | %70 | %74 | %35 |
| izin verilen tüm geri almalar | 100 (%172) | 100 (%175) | 150 (%185) | 150 (%60) | 200 (%93) | 200 (%98) | 250 (%58) |
| bot kazanma oranı | %100 | %100 | %100 | %13 | %37 | %15 | %3 |

Satırlar bölüm bütçesinin yüzdesi. İki şey görünüyor:

- **L1–L3'te yardım ödenemez.** Bir geri alma bölümün tüm kazancına eşit
  ya da üstünde. Oysa oyuncu kuralları orada öğreniyor.
- **L4–L7'de yardım ucuz ama yetmiyor.** Bütçe rahat (%35–%98), buna
  karşılık bölümler zor (bot %3–%37). Hak sayısı bütçeden önce bitiyor:
  L7'de 250 puanlık geri alma hakkı 430 puanlık bütçenin içinde kalıyor.

Yani bugün **bedeller değil SINIRLAR bağlayıcı** (L4–L7'de), erken
bölümlerde ise **bütçe** bağlayıcı. Tek para biriminin iki işi birden
yapması bunun nedeni: aynı sayı hem "ne kadar iyi oynadın" hem "ne kadar
yardım alabilirsin" demek.

---

## 2. Araştırma Puanı

### 2.1 Temel karar: iç döngüye DOKUNMA, meta katman ekle

İki yol var:

**(A) Araştırma Puanı yardımların yeni cüzdanı olsun.**
İpucu/geri al/Joker bölüm skoru yerine RP ile ödenir.
→ *Reddediliyor.* Bu, iç döngünün dengesini değiştirir: yardımın bedeli
kalıcı bir cüzdandan çıkınca "biriktir, zor bölümde harca" stratejisi
doğar ve L4–L7'nin zorluğu ölçülemez hâle gelir. Üstelik kullanıcının
bu tur için koyduğu sınır açık: **ipucu/Joker bedelleri değişmeyecek.**

**(B) Bölüm skoru bölüm içi cüzdan olarak KALSIN; Araştırma Puanı
KALICI yükseltmelerin para birimi olsun.** ← **önerilen**

- Bölüm skoru: bölüm içinde kazanılır, bölüm içinde harcanır, bölüm
  sonunda biter. Yenilenebilir, enflasyon riski yok, oyun hissi aynı.
- Araştırma Puanı: kalıcı başarılardan kazanılır, **kalıcı
  yükseltmelere** harcanır. Bölüm içinde harcanmaz, yani bir hamlenin
  sonucunu değiştirmez.

Bu ayrım 1.4'teki iki sorunu da doğru yerden çözer: erken bölümlerdeki
"yardım ödenemez" sorunu **ipucu taban bedelini kalıcı olarak indiren**
bir yükseltmeyle, geç bölümlerdeki "hak yetmiyor" sorunu **hak sayısını
artıran** bir yükseltmeyle çözülür. İkisi de oyuncunun KARARI olur,
tasarımcının bölüm bölüm ayarladığı bir sabit değil.

### 2.2 Kazanım kaynakları

XP ile **aynı olayları** sayar, aynı çiftlik korumalarını kullanır
(`ProgressionService` deseni: bölüm başına "en iyi sonuç" defteri, tarif
ve soru başına bir kez). İkinci bir muhasebe yazılmaz.

| kaynak | RP | bugünkü tavan | not |
|---|---|---|---|
| bölüm ilk tamamlama | 15 | 7 × 15 = **105** | tekrar oynamak yalnızca yeni en iyi sonuçta öder |
| kazanılan puan payı | `kazanılan / 25` | ≈ **50** | L1 2 · L7 17 |
| yeni tarif keşfi | 8 | 21 × 8 = **168** | |
| quiz ilk doğru cevabı | 3 | 36 × 3 = **108** | banka boyu tavanı |
| rozet | 20 | bugün 3 × 20 = **60** | bugün kazanılabilir üç rozet |
| günlük bulmaca (Faz 3) | 10/gün | — | tek YENİLENEBİLİR kaynak |
| ansiklopedi kartı okuma (Faz 4) | 2 | 26 × 2 = 52 | kart başına bir kez |

**Bugünkü tavan: ≈ 491 RP** (105 + 50 + 168 + 108 + 60).

Günlük bulmaca dışındaki **her kaynak sonlu**: 7 bölüm, 21 tarif, 36 soru,
3 rozet. Süresiz akan tek musluk Faz 3'ün günlük bulmacası olacak —
denge bozulursa nerede arayacağımız bu yüzden belli. Harcama listesi bu
tavana göre ölçüldü (2.4) ve **tavanın altında** tutuldu (karar 4.3).

### 2.3 Harcama yerleri (kalıcı yükseltmeler)

Her yükseltme **tek seferlik ya da basamaklı**, hepsi kalıcı. Hiçbiri
bölüm içinde tüketilmiyor, yani "harcadım, kayboldu" duygusu yok.

| yükseltme | etki | basamaklar | bedel | toplam |
|---|---|---|---|---|
| **Asistan ödeneği** | ipucu taban bedeli 25 → 20 → 15 | 2 | 55 / 80 | 135 |
| **Yedek numune** | bölüm başına +1 geri alma hakkı | 2 | 60 / 90 | 150 |
| **Katalizör verimi** | bedava Joker eşiği 5 → 4 sentez | 1 | 80 | 80 |
| **Fazladan reaktif** | bölüm 1 fazla taşla başlar | 1 | 65 | 65 |
| **Defter imzası** (kozmetik) | tarif defterinde oyuncu adı ve rütbesi | 1 | 40 | 40 |
| | | | **toplam** | **470** |

**Kozmetik listede ama yalnızca KİTTEN ÜRETİLEBİLEN kozmetik**
(karar, bkz. 4.1). Defter imzası mevcut çerçeve + bir TMP etiketi, yani
yeni varlık gerektirmiyor. Önceki taslakta duran "mentor portresi" (60 RP)
**çıkarıldı**: dokuz yeni avatar demekti, yani bir çizim turu — üstelik
mevcut avatarlar zaten "kaynak çözünürlüğü yetersiz" listesinde
(`docs/STATUS.md`). Faz 6'nın görsel turuna bırakıldı.

**İlk basamaklar bilerek ucuz** (40–80 RP, arzın %8–16'sı). Gerekçe
4.2'de: iade yok, yani oyuncu ilk seçimini L4–L7 duvarını hiç görmeden
yapıyor. Yanlış ilk seçimin bedeli küçük tutuldu.

### 2.4 Arz / talep dengesi

| | RP |
|---|---|
| bugünkü arz tavanı (%100 tamamlama) | **491** |
| yükseltme listesinin toplamı | **470** |
| **fark** | **+21** |

**Liste bugünkü arza SIĞIYOR** (karar, bkz. 4.3). Üç sonucu var:

1. **Ekranda görünen her madde ULAŞILABİLİR.** Hub'da verilen karar
   ("uygulanmamış giriş tamamen gizli; kilitli kutu, 'yakında' rozeti,
   soluk buton YOK") yükseltme ekranında da geçerli olur. Listenin
   arzdan uzun olduğu bir tasarımda, oyuncuya *neden* erişemediğini
   söyleyemezdik: Faz 3 henüz yok.
2. **Seçim SIRA seçimidir, dışlama değil.** Yolculuğun tamamını
   bitiren oyuncu (7 bölüm + 21 tarif + 36 soru) listenin tamamını
   alabilir, 21 RP artar. Ama oraya varmak uzun: bot ölçümünde L7
   kazanma oranı %3, L4 %13. Yolun büyük kısmında oyuncu "önce hangisi"
   diye karar veriyor, ve bu karar iade edilemediği için gerçek.
3. **%100'de para birimi SUSAR — ve bu Faz 3'e bir ŞART bırakır.**
   Her şeyi bitiren oyuncunun RP'si birikmeye devam eder ama alacak bir
   şey kalmaz. Bu, listeyi kısaltma kararının bedeli ve kabul edildi.
   Karşılığında **Faz 3 arzı ve maddeleri BİRLİKTE getirmek zorunda**:
   günlük bulmaca (10 RP/gün) tek başına eklenirse ölü para birimini
   daha hızlı biriktirmiş oluruz. Günlük bulmaca turunun kabul
   kriterine bu madde yazılmalı.

**Kaçınılan iki hata:**

- *Sentez başına RP vermek.* Serbest mod sınırsız oynanıyor; sentez
  başına ödeme sınırsız RP demekti. Ölçüm: L7 ortalama 32 hamlede ~430
  puan, yani sentez başına 1 RP bile saatte dört haneli üretirdi.
- *Tekrar tamamlamaya tam RP vermek.* L1 ortalama **6 hamlede** bitiyor
  (ölçüm). Tam ödeme olsaydı dakikada ~2 tamamlama × 15 RP = saatte
  ~1800 RP; bütün liste 20 dakikada alınırdı. XP'deki "en iyi sonuç"
  defteri (`levelXpGranted` deseni) aynen uygulanıyor.

### 2.5 Öncesi / sonrası

| | bugün | uygulanınca |
|---|---|---|
| bölüm içi cüzdan | bölüm skoru | **değişmedi** |
| ipucu bedeli | 25 / 50 / 75 | **değişmedi** (yükseltmeyle 20 / 40 / 60, sonra 15 / 30 / 45) |
| geri alma bedeli | 50 | **değişmedi** |
| geri alma hakkı | 2 + bölüm/2 | aynı + yükseltmeyle en fazla +2 |
| Joker bedeli | taşın `jokerCost`'u | **değişmedi** |
| katalizör eşiği | 5 sentez | aynı, yükseltmeyle 4 |
| kalıcı para birimi | yok (`totalScore` ölü kayıt) | **Araştırma Puanı** |
| kalıcı harcama yeri | yok | 5 yükseltme (4 oynanış + 1 kozmetik), 470 RP |
| `totalScore` | ölü kayıt | **kaldırılır** — anlamı yanlış (kalan skorların toplamı), yerini RP alır |

---

## 3. Uygulanırsa yapılacaklar (bu turda YAPILMADI)

1. `SaveData.researchPoints` + yükseltme basamakları (`List<int>`),
   `saveVersion` 5 ve göç. Geriye dönük RP, XP'nin geriye dönük hesabıyla
   **aynı veriden** türetilir (uydurma yok).
2. `ProgressionService` ile aynı derlemede bir `ResearchPoints`
   muhasebesi: kazanç olayları ortak, böylece iki sayaç ayrışamaz.
   Çiftlik korumaları XP ile aynı (bölüm başına "en iyi sonuç" defteri,
   tarif ve soru başına bir kez).
3. Yükseltmelerin oyun tarafına bağlanması: `baseHintCost`,
   `currentUndoLimit`, `CatalystChargeLimit`, `startingTileCount` artık
   sabit değil, yükseltme basamağından okunur. **Bu, oyun hissini
   değiştiren bir değişikliktir**: ayrı analiz + onay turu ister.
4. Yükseltme ekranı (yeni panel). Hangi sahnelerden açılacağı baştan
   kararlaştırılır ve o sahnelerin HEPSİNE kurulur (CLAUDE.md panel
   erişimi kuralı). Ekranda **her yükseltmenin etkisi somut yazılır**
   ("ipucu 25 → 20 puan"), çünkü iade yok ve oyuncunun seçimini bilgiyle
   yapması gerekiyor (karar 4.2).
5. `Tools/sim_core_loop.py`'ye yükseltme senaryoları: her basamağın
   L4–L7 kazanma oranına etkisi ölçülür, tahmin edilmez.
6. Faz 3'ün günlük bulmaca turuna kabul kriteri: **arz ve yeni maddeler
   birlikte** gelir (bkz. 2.4, madde 3).

### 3.1 Yıldızlardan gelen iki şart (2.2'de eklendi)

Üç yıldız sistemi (Faz 2 / 2.2) yükseltmelerin uygulanmasına iki şart
koyuyor. İkisi de **uygulama turunun kabul kriteridir**.

**Şart 1 — Yıldız sınırları yükseltmesiz zeminde kalibre edildi;
yükseltmeler ★3'ü önemsizleştirmemeli.**

`docs/DESIGN_TOKENS.md` 13.3'teki hamle sınırları (8 / 11 / 15 / 33 /
29 / 28 / 37) **yükseltme yokken** ölçüldü. Listedeki dört oynanış
yükseltmesinin üçü doğrudan yıldız ölçütlerine dokunuyor:

| yükseltme | ★2'ye etkisi | ★3'e etkisi |
|---|---|---|
| Asistan ödeneği (ipucu 25 → 15) | dolaylı: ipucu ucuzlayınca daha çok alınır | **doğrudan tehdit**: ★3 "ipucu kullanma" diyor, ucuz ipucu ★3'ü terk ettirir |
| Yedek numune (+1 geri alma) | geri alma hamleyi geri aldığı için **sınırı kolaylaştırır** | yok (geri alma ★3'ü bozmuyor) |
| Katalizör verimi (5 → 4 sentez) | yok | **doğrudan tehdit**: bedava Joker ★3'ü bozmuyor, eşik düşünce ★3 bedavaya yaklaşır |
| Fazladan reaktif (+1 başlangıç taşı) | **sınırı kolaylaştırır** (daha az hamlede zincir kurulur) | yok |

Uygulama turu şunu göstermek zorunda: her yükseltme basamağı açıkken
`Tools/sim_stars.py` yeniden koşturulur ve **★2 oranı kazanan oyunlarda
%90'ı aşmaz**. Aşıyorsa ya sınır ya yükseltme ayarlanır — ikisinden
hangisi değişirse değişsin, 13.3 tablosu güncellenir.

★3 için ayrı bir ölçüm gerekmiyor: ★3 oyuncunun KARARI (yardım
almamak), yükseltme onu kolaylaştırmaz. Tehdit tersinden geliyor —
yardım ucuzlayınca ★3'ü hedeflemek anlamsızlaşır. Bu bir denge değil
**tasarım** sorusu ve uygulama turunda açıkça cevaplanmalı.

**Şart 2 — Sınav modunda (ve Faz 5'teki KAYNAMA NOKTASI'nda) kalıcı
yükseltmeler DEVRE DIŞI.**

Sınav modu "kendi bilgine güven" sözü veriyor: ipucu zaten engelli ve
puan ×2. Kalıcı yükseltmeler orada da çalışsaydı, mod kendi sözünü
tutmazdı — oyuncu dışarıda biriktirdiği avantajla "yardımsız" moda
girerdi. Aynı mantık Faz 5'in KAYNAMA NOKTASI'na da uygulanır (o mod
bir bilgi sınavı; satın alınmış avantaj ölçümü bozar).

Bu, yükseltmelerin okunduğu tek yerde (yükseltme basamağını veren
yardımcı) mod kontrolüyle uygulanır; `baseHintCost` ve arkadaşlarının
her çağrı yerine dağıtılmaz.

**Şart 3 — Uygulama sırası: 2.2'den SONRA.** Yıldızlar yükseltmelerden
önce yerleşti ki sınırlar temiz bir zeminde ölçülebilsin. Tersi olsaydı
"sınır mı yükseltme mi kolaylaştırdı" sorusu ayrıştırılamazdı.

---

## 4. Kararlar (25 Kasım 2026)

### 4.1 Kozmetik yükseltmeler kapsamda — ama yalnızca kitten üretilebilenler

*Soru:* RP yalnızca oynanışa mı harcanmalı?

*Karar:* **Kozmetik kapsamda, ama yeni çizim gerektirenler değil.**

*Gerekçe:* kozmetik olmadan oyuncu oynanış listesini bitirdiği an RP
**ölü para birimi** olur — her yeni tarif keşfi, her doğru quiz cevabı
hiçbir şeye yaramayan bir sayıyı artırır. Kozmetik, para biriminin
ömrünü uzatan tek sink. Ama maliyetleri eşit değil: defter imzası
mevcut çerçeve + TMP etiketi, mentor portre seti dokuz yeni avatar.
İkincisi listeden düştü.

### 4.2 İade YOK

*Soru:* satın alınan yükseltme geri alınabilsin mi (RP iadesi)?

*Karar:* **Hayır.**

*Gerekçe:* iade varsa oyuncu her bölümde yükseltme takas edebilir ve
"şu yükseltmeyle L4–L7 nasıl hissettiriyor" sorusunun kararlı bir
cevabı kalmaz — denge turu ölçemez hâle gelir.

*Bedeli ve karşılığı:* oyuncu ilk satın almasını L4–L7 duvarını hiç
görmeden, yani bilgisizce yapıyor. Bu iadeyle değil **bilgiyle**
çözülür: (a) her yükseltmenin etkisi ekranda somut yazılır
("ipucu 25 → 20 puan"), (b) her yükseltmenin ilk basamağı ucuz tutulur
(40–80 RP, arzın %8–16'sı), böylece yanlış ilk seçim pahalı olmaz.

### 4.3 Liste bugünkü arza sığar

*Soru:* arz (491 RP) talebin altında kalsın mı?

*Karar:* **Hayır — liste arza sığar (470 RP).**

*Gerekçe:* önceki taslak "arz talebin %77'si, bu kasten" diyordu ve bu,
**projenin kendi kuralıyla çelişiyordu**: hub'da "uygulanmamış giriş
tamamen gizli, 'yakında' rozeti yok" kararı var. Yükseltme ekranında
149 RP'lik erişilemez madde göstermek tam olarak o kuralın yasakladığı
şey; üstelik oyuncuya neden erişemediğini söyleyemiyorduk, çünkü Faz 3
henüz yok.

*Bedeli:* %100 tamamlayan oyuncuda para birimi susar (bkz. 2.4,
madde 3). Karşılığında Faz 3 arzı ve maddeleri birlikte getirmek
zorunda — bu, günlük bulmaca turunun kabul kriterine yazıldı.

*Elenen seçenek:* "bugünden yenilenebilir bir kaynak ekle" (ör. günde
bir kez bölüm tekrarına küçük RP). Yeni bir musluk, yeni bir çiftlik
yüzeyi ve Faz 3'ün günlük bulmacasıyla çakışan bir mekanik demekti.
