# NEXUM — Araştırma Puanı önerisi (Faz 2 / 2.1 eki)

**Durum: ÖNERİ. Kod yazılmadı, hiçbir bedel değiştirilmedi.**
Kullanıcı onayından sonra ayrı bir adımda uygulanacak.

Bu belge üç şeyi yapar:
1. Bugünkü ekonomiyi **ölçer** (ne, neyle, kaça ödeniyor).
2. Araştırma Puanı'nın kazanım ve harcama yerlerini önerir.
3. Enflasyon analizini ve öncesi/sonrası tabloyu verir.

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

## 2. Araştırma Puanı (ÖNERİ)

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
Faz 3'ün günlük bulmacası gelene kadar arz **sonlu**; bu bilinçli.

### 2.3 Harcama yerleri (kalıcı yükseltmeler)

Her yükseltme **tek seferlik ya da basamaklı**, hepsi kalıcı. Hiçbiri
bölüm içinde tüketilmiyor, yani "harcadım, kayboldu" duygusu yok.

| yükseltme | etki | basamaklar | bedel | toplam |
|---|---|---|---|---|
| **Asistan ödeneği** | ipucu taban bedeli 25 → 20 → 15 | 2 | 60 / 90 | 150 |
| **Yedek numune** | bölüm başına +1 geri alma hakkı | 2 | 70 / 110 | 180 |
| **Katalizör verimi** | bedava Joker eşiği 5 → 4 sentez | 1 | 120 | 120 |
| **Fazladan reaktif** | bölüm 1 fazla taşla başlar | 1 | 90 | 90 |
| **Defter imzası** (kozmetik) | tarif defterinde oyuncu adı ve rütbesi | 1 | 40 | 40 |
| **Mentor portresi** (kozmetik) | ikinci portre seti | 1 | 60 | 60 |
| | | | **toplam** | **640** |

Kozmetikler bilerek listede: oyuncunun RP'yi oyun hissini değiştirmeden
harcayabileceği bir yer olmalı, yoksa "her şeyi aldım" noktasından sonra
kaynak anlamsızlaşır.

### 2.4 Enflasyon analizi

| | RP |
|---|---|
| bugünkü arz tavanı (%100 tamamlama) | **491** |
| yükseltme listesinin toplamı | **640** |
| **fark** | **−149** |

Arz talebin **%77'si**. Yani bugünkü içerikle oyuncu listeyi
bitiremez — kasten. Üç sonucu var:

1. **Seçim gerçek olur.** Oyuncu 491 RP ile hangi yükseltmeyi alacağına
   karar vermek zorunda; hepsini alabiliyorsa karar yoktur.
2. **Faz 3 bir anlam kazanır.** Günlük bulmaca (10 RP/gün) tek
   yenilenebilir kaynak: 15 günde kalan 149 RP kapanır. Günlük bulmacanın
   "neden her gün gireyim" sorusu böyle cevaplanır.
3. **Enflasyon riski tek bir yerde toplanır.** Diğer kaynakların hepsi
   sonlu (bölüm, tarif, soru, rozet, ansiklopedi); süresiz akan tek
   musluk günlük bulmaca. Dengeyi bozacak bir şey olursa nerede
   arayacağımız bellidir.

**Kaçınılan iki hata:**

- *Sentez başına RP vermek.* Serbest mod sınırsız oynanıyor; sentez başına
  ödeme sınırsız RP demekti. Ölçüm: L7 ortalama 32 hamlede ~430 puan, yani
  sentez başına 1 RP bile saatte dört haneli üretirdi.
- *Tekrar tamamlamaya tam RP vermek.* L1 ortalama **6 hamlede** bitiyor
  (ölçüm). Tam ödeme olsaydı dakikada ~2 tamamlama × 15 RP = saatte
  ~1800 RP; bütün liste 20 dakikada alınırdı. XP'deki "en iyi sonuç"
  defteri aynen uygulanıyor.

### 2.5 Öncesi / sonrası

| | bugün | öneri sonrası |
|---|---|---|
| bölüm içi cüzdan | bölüm skoru | **değişmedi** |
| ipucu bedeli | 25 / 50 / 75 | **değişmedi** (yükseltmeyle 20 / 40 / 60, sonra 15 / 30 / 45) |
| geri alma bedeli | 50 | **değişmedi** |
| geri alma hakkı | 2 + bölüm/2 | aynı + yükseltmeyle en fazla +2 |
| Joker bedeli | taşın `jokerCost`'u | **değişmedi** |
| katalizör eşiği | 5 sentez | aynı, yükseltmeyle 4 |
| kalıcı para birimi | yok (`totalScore` ölü kayıt) | **Araştırma Puanı** |
| kalıcı harcama yeri | yok | 6 yükseltme (4 oynanış + 2 kozmetik) |
| `totalScore` | ölü kayıt | **kaldırılır** — anlamı yanlış (kalan skorların toplamı), yerini RP alır |

---

## 3. Uygulanırsa yapılacaklar (bu turda YAPILMADI)

1. `SaveData.researchPoints` + yükseltme basamakları (`List<int>`),
   `saveVersion` 5 ve göç. Geriye dönük RP, XP'nin geriye dönük hesabıyla
   **aynı veriden** türetilir (uydurma yok).
2. `ProgressionService` ile aynı dosyada bir `ResearchPoints` muhasebesi:
   kazanç olayları ortak, böylece iki sayaç ayrışamaz.
3. Yükseltmelerin oyun tarafına bağlanması: `baseHintCost`,
   `currentUndoLimit`, `CatalystChargeLimit`, `startingTileCount` artık
   sabit değil, yükseltme basamağından okunur. **Bu, oyun hissini
   değiştiren bir değişikliktir**: ayrı analiz + onay turu ister.
4. Yükseltme ekranı (yeni panel) — hangi sahnelerden açılacağı baştan
   kararlaştırılır ve o sahnelerin HEPSİNE kurulur (CLAUDE.md panel
   erişimi kuralı).
5. `Tools/sim_core_loop.py`'ye yükseltme senaryoları: her basamağın
   L4–L7 kazanma oranına etkisi ölçülür, tahmin edilmez.

## 4. Açık sorular (kullanıcıya)

- Kozmetik yükseltmeler kapsamda mı, yoksa RP yalnızca oynanışa mı
  harcanmalı?
- Yükseltmeler **geri alınabilir** olsun mu (RP iadesi)? Bugünkü öneri:
  hayır — iade, "dene-geri al" ile denge ölçümünü imkânsız kılar.
- Faz 3 gelene kadar arzın sonlu kalması kabul mü, yoksa bugünden
  yenilenebilir bir kaynak (ör. günde bir kez bölüm tekrarına küçük RP)
  gerekli mi?
