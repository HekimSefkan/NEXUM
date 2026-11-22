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

1.5'te **yalnızca iki yazım hatası** düzeltildi (`Siktik`→`Sitrik`,
`Proten`→`Proton`) ve boşluklar kırpıldı. Soru gövdesi/şık içeriğini
değiştirmek kullanıcının doğrulama turunun işi; yukarıdaki son iki satır
onun kararına bırakıldı.
