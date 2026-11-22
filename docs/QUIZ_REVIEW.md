# Quiz bankası — inceleme föyü (Faz 1 / 1.5)

Bu dosya iki iş görür:
1. **Mevcut quiz akışının belgesi** (§1) — nerede tetiklenir, veri nerede durur.
2. **Soru bankasının doğrulama tablosu** — her sorunun kimya doğruluğu
   kullanıcı tarafından yayından önce tek tek onaylanır.

---

## 1. Mevcut akış (1.5 öncesi hâl)

### Nerede tetikleniyor
Quiz **tek bir yerden** açılır: oyun sonu ekranındaki **ikinci şans butonu**
(`Canvas/GameOverPanel/ReviveButton`). Butonun `OnClick` listesinde
`UIManager.ShowQuizPanel` çağrısı serileştirilmiştir
(`Assets/Scenes/Game.unity`). Koddan başka çağrısı yok.

Hak bir oyunda **bir kez**tir: `ShowQuizPanel` ilk satırda
`GridManager.Instance.hasUsedRevive = true` yazar, `ShowGameOver` da butonu
`!hasUsedRevive` koşuluyla gösterir. Bayrak sahne örneğinde durur, sahne
yeniden yüklenince sıfırlanır.

### Soru havuzu nerede
- Veri tipi: `ChemistryQuiz` (ScriptableObject) — `questionText`,
  `options[4]`, `correctAnswerIndex`.
- Asset'ler: `Assets/Data/Quizzes/Soru_1..Soru_16.asset` → **16 soru**.
- Çalışma zamanı listesi: `UIManager.allQuizzes` (Game sahnesinde
  serileştirilmiş `List<ChemistryQuiz>`). Sahne dışında bir katalog yok.
- Seçim: `Random.Range(0, allQuizzes.Count)` — **düz rastgele**, hafızasız.
  Aynı soru üst üste gelebilir, yanlış cevaplanan soru özel bir öncelik
  kazanmaz.

### Doğru cevap paneli nasıl çalışıyor
`CheckAnswer(int)` → `ProvideFeedbackRoutine(int)` (coroutine):
1. Şık butonları gizlenir (`optionButtonObjects`).
2. Soru metni kutusu geri bildirim düzenine alınır: 800×300 @ y=330 yerine
   **800×700 @ y=0** (panel ortası). Tasarımdaki yer `quizQuestionPos` /
   `quizQuestionSize` içinde bir kez yakalanır, panel tekrar açılınca geri
   yazılır.
3. `quizAttempts` her cevapta artar; doğruysa `quizCorrect` da artar,
   `SaveService.SaveNow()`.
4. **Doğru:** `quizCorrectClip` sesi + `Loc.Get("quiz.correct")` metni →
   2 sn bekle → panel kapanır → `GridManager.ApplyReviveBonus()`.
5. **Yanlış:** `quizWrongClip` + `Loc.Get("quiz.wrong")` → 2 sn bekle →
   panel kapanır → `ShowGameOver()` (buton artık görünmez).

Yani "doğru cevap paneli" ayrı bir panel değil: **aynı QuizPanel**, şıkları
gizlenmiş ve soru metni kutusu büyütülmüş hâlidir. 1.5'te açıklama bu
kutuya girer.

### Panel yerleşimi
`QuizPanel` tam ekran karartma (stretch); içindeki çerçeve 900×1100
merkezde, soru 800×300 @ y=330, şıklar y = 60 / −80 / −220 / −360.

### 1.5 öncesi durumda görülen içerik kusurları
| Nerede | Kusur |
|---|---|
| `Soru_8` C şıkkı | "Siktik Asit" — "Sitrik Asit" yazım hatası (ekranda çirkin) |
| `Soru_12` C şıkkı | "Proten ve Nötron" — "Proton" yazım hatası |
| `Soru_16` şıkları | Gazlar `N` ve `H` olarak yazılmış; doğrusu `N<sub>2</sub>` / `H<sub>2</sub>` |
| `Soru_13` gövdesi | "kimyasal bir karışım" terimi müfredatta yok; kastedilen "saf madde olmayan" |
| `Soru_8` A/C şıkları | sonda/başta gereksiz boşluk (`'Laktik Asit '`) |

İlk turda **yalnızca iki yazım hatası** düzeltildi (`Siktik`→`Sitrik`,
`Proten`→`Proton`) ve boşluklar kırpıldı; `Soru_16` ve `Soru_13` kullanıcı
kararına bırakıldı. **PR #27 düzeltme turunda ikisi de onaylanıp uygulandı**
(bkz. §5). Beş kusurun hepsi kapandı.

---

## 2. 1.5'te ne değişti

| Ne | Nerede |
|---|---|
| Soru sayısı 16 → **36** | `Assets/Data/Quizzes/Soru_1..Soru_36.asset` |
| Her soruya **açıklama** | sözlükte, anahtar `quiz.<asset>.explain` (`QuizStrings.cs` tohumlar) |
| Her soruya **etiket** | `ChemistryQuiz.topic` · `difficulty` (1-3) · `relatedCompound` |
| **Yanlış cevap havuzu** | `QuizSelector` + `SaveData.quizWrongPool` |
| **Bilgi Kartları ayarı** bağlandı | `UIManager.TogglePause` → duraklat kartı |
| Soru puntosu **pişirildi** | Game sahnesi: auto-size kapandı, punto 46 |
| Cevap paneli **dokunmadan kapanmıyor** | `UIManager.ReadHold` — en az 2 sn, sonra dokunuş |

### Soru seçimi artık nasıl çalışıyor
`UIManager.ShowQuizPanel` içindeki `Random.Range` kaldırıldı; seçim
`QuizSelector.Pick` üzerinden:

1. Havuz boş değilse **1/3 olasılıkla** soru havuzdan gelir.
2. Kalan 2/3'te soru **havuzun dışından** gelir.
3. En son sorulan soru hiçbir turda seçilemez.
4. Havuz **yalnızca kesinleşmiş cevapla** değişir (`RecordAnswer`): yanlışta
   girer, doğruda çıkar. Panelin açılıp kapanması havuza dokunmaz.
5. Bankada karşılığı olmayan havuz girdisi ilk seçimde ayıklanır.

### Havuz oranı neden 1/3
- **Neden sabit oran, neden ağırlıklı çekiliş değil:** "her havuz sorusuna 3
  ağırlık" denediğimizde pay havuz büyüklüğüne bağlı çıkıyor — 1 soruluk
  havuzda %8, 10 soruluk havuzda %54. Yani tavan ölçülemez hâle geliyor.
  Sabit oranla pay **havuz büyüklüğünden bağımsız**.
- **Neden iki küme ayrık:** "havuz turu değilse bankanın tamamı" kuralında
  havuz dışı turda da havuz sorusu gelebiliyor; 12/36 havuzda toplam pay
  %56'ya çıkıyor, yani havuz baskın oluyor. Kümeler ayrılınca pay tam 1/3.
- **Neden 1/3 ve örneğin 1/2 değil:** quiz oyun başına en fazla bir kez
  çıkıyor (ikinci şans hakkı). 1/2'de oyuncu kaybettiği her iki oyundan
  birinde eski hatasını görür, bu tekrar hissi verir; 1/3'te üç oyundan
  birinde görür, geri kalan ikisinde banka genişler. Ölçüm:
  `QuizPoolTests.HavuzunPayiTavandaKaliyor` → 3000 çekilişte **%32,3**
  (beklenen %33,3), havuz dışı 32 ayrı soru geldi.

### Göç kararı
`saveVersion` **2 → 3**. Göç adımı yalnızca alanları kurar;
**geriye dönük doldurma YAPILMAZ.** Gerekçe: hangi soruların yanlış
cevaplandığı eskiden hiç kaydedilmiyordu. Uydurmak (ör. "quizAttempts >
quizCorrect ise rastgele soruları havuza at") oyuncuya hiç görmediği
soruları öncelikli hâle getirirdi. Boş havuz, "henüz yanlış yok" ile aynı
davranır: düz rastgele seçim. Ölçüm:
`QuizPoolTests.EskiKayitSurum3eGocEder`.

Bozuk veri onarımı `SaveService.Repair` içinde: null liste, boş girdi,
yineleme ve akıl dışı uzunluk (400 girdi → 256) temizlenir.

### Punto neden pişirildi
Soru metninde çalışma zamanı auto-size açıktı (34-50). CLAUDE.md bunu
yasaklıyor ve ölçümü de anlamsız kılıyordu: TMP metni kutuya sığdırdığı için
doluluk HER metin için %100 çıkıyordu. Auto-size kapatılıp punto merdiveni
ölçüldü (`QuizFitTests.PuntoMerdiveniOlculuyor`):

| Punto | En kötü soru | En kötü cevap |
|---|---|---|
| 50 | %100 (taşıyor) | %77 |
| **46** | **%73** | **%70** |
| 42 | %67 | %59 |
| 36 | %42 | %50 |

46, %85 sınırını geçen en büyük punto. Eski hâlde kısa sorular 50'de, uzun
sorular 34'e kadar küçülüyordu; artık hepsi 46'da.

### Açıklama 2 saniyede okunmuyordu
Eski akışta doğru cevap yazısı 2 sn durup kapanıyordu. 1-2 cümlelik açıklama
o sürede okunamaz. Kural: **en az 2 sn bekler, sonra YALNIZCA ekrana
dokununca kapanır.** Kendiliğinden kapanma yok.

İlk denemede 10 saniyelik bir üst sınır vardı; kaldırıldı, çünkü metni yavaş
okuyanın açıklaması ortasından kesiliyordu ve "okumaya yeten süre" diye
evrensel bir değer yok. Panel açıkken oyun zaten duruyor (quiz modal, girdi
kilitli). Kapandıktan sonra akış aynı: `ApplyReviveBonus`. Ekranda bunu
söyleyen satır: `quiz.continue_hint`. Ölçüm: `QuizAnswerFlowTests`
(30 sn dokunulmadan panel açık + kodda üst sınır sabiti yok).

---

## 3. Doğrulama tablosu — 1.5'te eklenen 20 soru

> ⚠️ **Hepsi "doğrulanmadı".** Kimya doğruluğu kullanıcı tarafından yayından
> önce tek tek onaylanacak. Tablo asset'lerden ve sözlükten **üretilmiştir**
> (`scratch/tablo.py`), elle yazılmadı — yani burada yazan, oyunun
> gösterdiğidir.

| Asset | Soru | Şıklar | Doğru cevap | Açıklama | Etiketler | Durum |
|---|---|---|---|---|---|---|
| `Soru_17` | Ozon tabakası (O<sub>3</sub>) Dünya'yı Güneş'ten gelen hangi ışınlardan korur? | A) Kızılötesi ışınlar<br>B) Ultraviyole ışınlar<br>C) Radyo dalgaları<br>D) Mikrodalgalar | **B) Ultraviyole ışınlar** | Stratosferdeki ozon tabakası Güneş'ten gelen ultraviyole ışınların büyük bölümünü tutar. | Atmosfer kimyası · zorluk 1 · O3 | doğrulanmadı |
| `Soru_18` | Kireç taşı ve mermerin yapısındaki ana bileşik hangisidir? | A) NaCl<br>B) CaO<br>C) CaCO<sub>3</sub><br>D) Fe<sub>2</sub>O<sub>3</sub> | **C) CaCO<sub>3</sub>** | Kireç taşı, mermer ve yumurta kabuğu ağırlıkla kalsiyum karbonattır (CaCO<sub>3</sub>). | Bileşikler · zorluk 1 · CaCO3 | doğrulanmadı |
| `Soru_19` | Halk arasında \"sönmemiş kireç\" denilen madde hangisidir? | A) CaO<br>B) CaCO<sub>3</sub><br>C) NaCl<br>D) H<sub>2</sub>CO<sub>3</sub> | **A) CaO** | Kalsiyum oksite (CaO) sönmemiş kireç denir; suyla tepkimesinde ısı açığa çıkar ve sönmüş kireç oluşur. | Günlük kimya · zorluk 2 · CaO | doğrulanmadı |
| `Soru_20` | Kireç taşı (CaCO<sub>3</sub>) fırında yüksek sıcaklıkta ısıtıldığında açığa çıkan gaz hangisidir? | A) O<sub>2</sub><br>B) H<sub>2</sub><br>C) CO<br>D) CO<sub>2</sub> | **D) CO<sub>2</sub>** | Kireç taşı ısıtılınca CaO ve CO<sub>2</sub>'ye ayrışır; kireç fırınları bu tepkimeyle çalışır. | Tepkime türleri · zorluk 2 · CaCO3 | doğrulanmadı |
| `Soru_21` | Karbondioksitin suda çözünmesiyle oluşan zayıf asit hangisidir? | A) Nitrik asit<br>B) Sülfürik asit<br>C) Karbonik asit<br>D) Asetik asit | **C) Karbonik asit** | Karbondioksit suda çözününce karbonik asit (H<sub>2</sub>CO<sub>3</sub>) oluşur; gazlı içeceklerdeki hafif ekşi tadın bir kısmı buradan gelir. | Asit-baz · zorluk 2 · H2CO3 | düzeltildi — kullanıcı onayı bekliyor (açıklama: "hafif ekşi tadın bir kısmı") |
| `Soru_22` | Fe<sub>2</sub>O<sub>3</sub> bileşiğinde demirin yükseltgenme basamağı kaçtır? | A) +1<br>B) +2<br>C) +3<br>D) +4 | **C) +3** | Oksijenin yükseltgenme basamağı -2'dir; üç oksijen -6 verince iki demirin toplamı +6 olmalıdır, yani demir başına +3 düşer. | Yükseltgenme basamağı · zorluk 3 · Fe2O3 | düzeltildi — kullanıcı onayı bekliyor (açıklama: "yükseltgenme basamağı -2") |
| `Soru_23` | Katı sofra tuzu elektriği iletmez, ama suda çözündüğünde iletir. Bunun nedeni nedir? | A) Suyun kendisinin iletken olması<br>B) İyonların çözeltide serbest hareket etmesi<br>C) Tuzun suda moleküllere ayrılması<br>D) Çözünme sırasında klor gazı çıkması | **B) İyonların çözeltide serbest hareket etmesi** | Katıda iyonlar örgüde sabittir; su içinde Na+ ve Cl- serbest kalıp yükü taşır ve çözelti iletken olur. | İyonik bileşikler · zorluk 3 · NaCl | doğrulanmadı |
| `Soru_24` | Amonyağın (NH<sub>3</sub>) sulu çözeltisi için aşağıdakilerden hangisi doğrudur? | A) Asidiktir<br>B) Baziktir<br>C) Nötrdür<br>D) pH değeri sıfırdır | **B) Baziktir** | Amonyak suda hidroksit iyonu oluşturur, yani çözeltisi baziktir; temizlik ürünlerinde bu yüzden kullanılır. | Asit-baz · zorluk 1 · NH3 | doğrulanmadı |
| `Soru_25` | Azot ve hidrojen gazlarından amonyak üretilen sanayi yöntemi hangisidir? | A) Haber-Bosch yöntemi<br>B) Solvay yöntemi<br>C) Kontakt yöntemi<br>D) Elektroliz yöntemi | **A) Haber-Bosch yöntemi** | Haber-Bosch yönteminde azot ve hidrojen yüksek basınçta katalizörle amonyağa dönüşür; gübre üretiminin temelidir. | Sanayi kimyası · zorluk 2 · NH3 | doğrulanmadı |
| `Soru_26` | Kuru havanın hacimce yaklaşık yüzde 78'ini oluşturan gaz hangisidir? | A) Oksijen<br>B) Karbondioksit<br>C) Azot<br>D) Argon | **C) Azot** | Kuru havanın hacimce yaklaşık yüzde 78'i azot, yüzde 21'i oksijendir. | Atmosfer kimyası · zorluk 1 · N2 | doğrulanmadı |
| `Soru_27` | Azot molekülünü (N<sub>2</sub>) oda koşullarında çok kararlı yapan bağ türü nedir? | A) Tek kovalent bağ<br>B) İkili kovalent bağ<br>C) Üçlü kovalent bağ<br>D) İyonik bağ | **C) Üçlü kovalent bağ** | İki azot atomu arasında üçlü bağ vardır; bu güçlü bağ azot gazını oda koşullarında çok tepkimesiz kılar. | Bağlar · zorluk 2 · N2 | doğrulanmadı |
| `Soru_28` | Taşıt motorlarında yüksek sıcaklıkta havanın azotu ile oksijeninden oluşan kirletici gaz hangisidir? | A) CO<sub>2</sub><br>B) SO<sub>2</sub><br>C) CH<sub>4</sub><br>D) NO | **D) NO** | Motor içindeki yüksek sıcaklıkta havanın azotu ve oksijeni birleşip azot monoksit verir; bu gaz havada yükseltgenerek kirliliğe yol açar. | Çevre kimyası · zorluk 3 · NO | doğrulanmadı |
| `Soru_29` | Karbonmonoksit (CO) solunduğunda neden ölümcül olabilir? | A) Hemoglobine oksijenden daha güçlü bağlanır<br>B) Kanı asitleştirir<br>C) Akciğer dokusunu yakar<br>D) Suda çözünüp baz oluşturur | **A) Hemoglobine oksijenden daha güçlü bağlanır** | Karbonmonoksit hemoglobine oksijenden çok daha güçlü bağlanır; kan artık oksijen taşıyamaz. | Sağlık ve kimya · zorluk 2 · CO | doğrulanmadı |
| `Soru_30` | Karbon içeren bir yakıt yeterli oksijen bulamadan yandığında hangi gaz oluşur? | A) CO<sub>2</sub><br>B) CO<br>C) O<sub>3</sub><br>D) H<sub>2</sub> | **B) CO** | Oksijen yeterliyse karbondioksit, yetersizse zehirli karbonmonoksit oluşur; sobalı ortamlar bu yüzden havalandırılır. | Yanma · zorluk 2 · CO | doğrulanmadı |
| `Soru_31` | Suyun, benzer büyüklükteki moleküllere göre kaynama noktasının çok yüksek olmasının nedeni nedir? | A) İyonik bağ<br>B) Metalik bağ<br>C) Hidrojen bağı<br>D) London kuvvetleri | **C) Hidrojen bağı** | Su molekülleri arasındaki hidrojen bağları güçlüdür; onları koparmak için yüksek sıcaklık gerekir. | Moleküller arası etkileşim · zorluk 2 · H2O | doğrulanmadı |
| `Soru_32` | Suyun elektrolizinde açığa çıkan hidrojen ve oksijen gazlarının hacim oranı kaçtır? | A) 2'ye 1<br>B) 1'e 1<br>C) 1'e 2<br>D) 3'e 1 | **A) 2'ye 1** | Dengeli denklemde 2H<sub>2</sub>O'dan 2H<sub>2</sub> ve 1O<sub>2</sub> oluşur; aynı sıcaklık ve basınçta gaz hacimleri mol sayısıyla orantılı olduğundan oran 2'ye 1'dir. | Elektrokimya · zorluk 3 · H2O | düzeltildi — kullanıcı onayı bekliyor (açıklama: dengeli denklem + hacim-mol orantısı) |
| `Soru_33` | İçme suyuna ve havuz suyuna klor (Cl<sub>2</sub>) eklenmesinin amacı nedir? | A) Suyu yumuşatmak<br>B) Mikroorganizmaları yok etmek<br>C) Suyun pH değerini yükseltmek<br>D) Suya tat vermek | **B) Mikroorganizmaları yok etmek** | Klor güçlü bir yükseltgendir; sudaki bakteri ve virüsleri etkisiz kıldığı için dezenfeksiyonda kullanılır. | Günlük kimya · zorluk 1 · Cl2 | doğrulanmadı |
| `Soru_34` | Sodyum metali neden gazyağı içinde saklanır? | A) Hava ve nemle hızla tepkime verdiği için<br>B) Çok kolay eridiği için<br>C) Işık altında rengini kaybettiği için<br>D) Kokusunu yitirdiği için | **A) Hava ve nemle hızla tepkime verdiği için** | Sodyum nemle şiddetli tepkime verip hidrojen açığa çıkarır; bu yüzden havayla temas etmeyecek şekilde gazyağında saklanır. | Metaller · zorluk 2 · Na | doğrulanmadı |
| `Soru_35` | O<sub>2</sub> ile O<sub>3</sub> arasındaki ilişkiyi en iyi hangisi anlatır? | A) Aynı elementin allotroplarıdır<br>B) Birbirinin izotopudur<br>C) Birbirinin izomeridir<br>D) Aynı maddenin iki farklı fiziksel durumudur | **A) Aynı elementin allotroplarıdır** | İkisi de yalnızca oksijen atomlarından oluşur ama atom sayıları farklıdır; aynı elementin bu tür yapılarına allotrop denir. | Allotropi · zorluk 2 · O3 | doğrulanmadı |
| `Soru_36` | Kemik ve dişlerin sertliğini sağlayan, vücutta en bol bulunan metal hangisidir? | A) Demir<br>B) Sodyum<br>C) Klor<br>D) Kalsiyum | **D) Kalsiyum** | Kemik ve dişin sertliğini kalsiyum tuzları sağlar; kalsiyum insan vücudunda en bol bulunan metaldir. | Biyokimya · zorluk 1 · Ca | doğrulanmadı |

---

## 4. Doğrulama tablosu — mevcut 16 soru

Bu soruların gövdeleri ve şıkları 1.5'te yazılmadı, devralındı; eklenen
şey **açıklama** ve **etiket**. Kullanıcı onayıyla yapılan içerik
düzeltmeleri Durum sütununda.

| Asset | Soru | Açıklama | Etiketler | Durum |
|---|---|---|---|---|
| `Soru_1` | Aşağıdakilerden hangisi bir Asittir? | HCl suda H+ iyonu verdiği için asittir; NaOH baz, NaCl tuz, H<sub>2</sub>O ise nötrdür. | Asit-baz · zorluk 1 · HCl | doğrulanmadı |
| `Soru_2` | Periyodik tablodaki en elektronegatif element aşağıdakilerden hangisidir? | Elektronu kendine en güçlü çeken element flordur; elektronegatiflik periyodik tabloda sağa ve yukarı doğru artar. | Periyodik sistem · zorluk 2 · F | doğrulanmadı |
| `Soru_3` | İki ametal atomu arasında elektronların ortaklaşa kullanılmasıyla oluşan kimyasal bağ türü nedir? | İki ametal elektronlarını ortaklaşa kullanır. Elektron alışverişi olsaydı iyonik bağ oluşurdu. | Bağlar · zorluk 1 · H2 | doğrulanmadı |
| `Soru_4` | Oda sıcaklığında (yaklaşık 25°C) sıvı halde bulunan tek metal hangisidir? | Cıva oda sıcaklığında sıvı olan tek metaldir; galyum yaklaşık 30 °C'de erir, brom ise metal değildir. | Elementler · zorluk 2 · Hg | doğrulanmadı |
| `Soru_5` | pH değeri 13 olan sulu bir çözelti için aşağıdakilerden hangisi söylenebilir? | pH 7'nin üstü bazik demektir; 14'e yaklaştıkça bazlık artar, yani 13 kuvvetli bazdır. | Asit-baz · zorluk 2 · NaOH | doğrulanmadı |
| `Soru_6` | Aşağıdaki bileşiklerden hangisi \"organik\" bir maddedir? | Organik bileşikler karbon-hidrojen iskeletine dayanır; metan (CH<sub>4</sub>) bunun en basit örneğidir. | Organik kimya · zorluk 1 · CH4 | doğrulanmadı |
| `Soru_7` | Karbonun doğada bulunan allotroplarından biri olan ve bilinen en sert doğal madde hangisidir? | Elmasta her karbon dört komşusuna kovalent bağlıdır, bu yüzden çok serttir; grafitte katmanlar birbiri üzerinde kayar. | Allotropi · zorluk 2 · C | doğrulanmadı |
| `Soru_8` | Sirkenin yapısında bulunan ve ona ekşi tadını veren asit hangisidir? | Sirke, suda çözünmüş asetik asittir; ekşi tat asidin verdiği H+ iyonlarından gelir. | Günlük kimya · zorluk 1 · CH3COOH | doğrulanmadı |
| `Soru_9` | Bir kimyasal tepkimenin hızını artıran, ancak tepkime sonunda hiçbir değişikliğe uğramadan çıkan maddelere ne ad verilir? | Katalizör aktifleşme enerjisini düşürür ve tepkimeden değişmeden çıkar; inhibitör ise tepkimeyi yavaşlatır. | Tepkime hızı · zorluk 2 · — | düzeltildi — kullanıcı onayı bekliyor (ilgili bileşik etiketi kaldırıldı: kavram sorusu) |
| `Soru_10` | Suyun (H2O) molekül geometrisi aşağıdakilerden hangisidir? | Oksijenin bağ yapmayan elektron çiftleri bağları iter; açı 104,5 dereceye düşer ve molekül açısal olur. | Molekül geometrisi · zorluk 3 · H2O | doğrulanmadı |
| `Soru_11` | Demir çivinin açık havada paslanması olayı kimyasal olarak ne tür bir tepkimedir? | Paslanma için oksijen ve nem birlikte gerekir; oluşan pas sulu demir(III) oksittir. | Tepkime türleri · zorluk 2 · Fe2O3 | düzeltildi — kullanıcı onayı bekliyor (açıklama: oksijen + nem; sulu demir(III) oksit) |
| `Soru_12` | Nötr bir atomun çekirdeğinde hangi atom altı parçacıklar bulunur? | Çekirdekte proton ve nötron bulunur; elektronlar çekirdeğin çevresindeki katmanlarda yer alır. | Atomun yapısı · zorluk 1 · H | doğrulanmadı |
| `Soru_13` | Aşağıdakilerden hangisi bir karışım değildir? | Saf su tek tür molekülden oluşur, yani saf maddedir; hava, çelik ve deniz suyu karışımdır. | Saf madde ve karışım · zorluk 2 · H2O | düzeltildi — kullanıcı onayı bekliyor (gövde: "kimyasal bir karışım" → "karışım") |
| `Soru_14` | Halk arasında \"Kezzap\" olarak bilinen tehlikeli kimyasal maddenin formülü nedir? | Kezzap, derişik nitrik asittir (HNO<sub>3</sub>); sülfürik asit ise zaç yağı adıyla bilinir. | Günlük kimya · zorluk 2 · HNO3 | doğrulanmadı |
| `Soru_15` | Aşağıdaki elementlerden hangisi bir \"soygaz\" (asal gaz) değildir? | Soygazların son katmanı tamdır ve kolay tepkime vermezler; klor ise bir elektron alıp tuz oluşturur. | Periyodik sistem · zorluk 1 · Cl | doğrulanmadı |
| `Soru_16` | Hangi gaz oksijenle temas edip bir kıvılcımla karşılaştığında patlayarak yanar? | Hidrojen oksijenle patlayarak yanar ve su oluşur; karbondioksit, helyum ve azot bu koşulda yanmaz. | Yanma · zorluk 1 · H2 | düzeltildi — kullanıcı onayı bekliyor (şıklar: N → N<sub>2</sub>, H → H<sub>2</sub>) |

---

## 5. Kullanıcı kararları

### Uygulananlar (PR #27 düzeltme turu)

| Konu | Ne yapıldı |
|---|---|
| `Soru_16` şıkları `N` ve `H` | `N<sub>2</sub>` / `H<sub>2</sub>` oldu |
| `Soru_13` "kimyasal bir karışım" | "bir karışım" oldu |
| `Soru_9` ilgili bileşik etiketi | kaldırıldı (kavram sorusu); `topic` = "Tepkime hızı" |
| `Soru_11` açıklaması | paslanma için oksijen **ve nem**; pas sulu demir(III) oksit |
| `Soru_21` açıklaması | "ekşiliği buradan gelir" → "hafif ekşi tadın bir kısmı buradan gelir" |
| `Soru_22` açıklaması | "değerlikli" → "yükseltgenme basamağı -2" |
| `Soru_32` açıklaması | atom oranı yerine dengeli denklem + aynı koşulda hacim-mol orantısı |

Düzeltilen her satır tablolarda **"düzeltildi — kullanıcı onayı bekliyor"**
olarak işaretli.

> `Soru_32`'nin açıklamasında ok karakteri (→) **kullanılmadı**: proje
> fontlarının hiçbirinde yok, atlaslar statik ve ok yalnızca `ReactionText`
> üzerinden gelen bir TMP sprite'ı (CLAUDE.md). Denklem bu yüzden
> "2H<sub>2</sub>O'dan 2H<sub>2</sub> ve 1O<sub>2</sub> oluşur" biçiminde,
> alt simgelerle yazıldı; istenen mantık (dengeli denklem + hacimlerin mol
> sayısıyla orantılı olması) korundu.

### Kalan

- **20 yeni sorunun kimya doğruluğu** — §3 tablosunda satır satır onay.
- Onaylanan satırların `Durum` sütunu "doğrulandı" olarak güncellenir.
