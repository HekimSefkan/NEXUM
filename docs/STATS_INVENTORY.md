# İstatistik envanteri (Faz 1 / 1.4)

`SaveData`'daki her alan ve ekranda gösterilen her sayı tarandı. Tarama
yöntemi: `Assets/Scripts` altında `SaveService.Data.<alan>` yazma/okuma
noktaları + iki sahnedeki TMP metinlerinin hangi script tarafından
doldurulduğu.

Durum sınıfları: **CANLI** (yazılıyor + gösteriliyor, doğru) ·
**ÖLÜ-EKRAN** (gösteriliyor, hiç yazılmıyor) · **ÖLÜ-KAYIT** (yazılıyor,
hiç gösterilmiyor) · **YANLIŞ** (ikisi de var, değer hatalı).

## İstatistikler

| Alan | Nerede YAZILIYOR | Nerede GÖSTERİLİYOR | Durum |
|---|---|---|---|
| `totalSynthesis` | `GridManager.Shift` (her birleşme); geri almada eski değere döner | Profil → **TOPLAM SENTEZ** | CANLI |
| `totalAccidents` | `UIManager.ShowGameOver` (oyun başına bir kez, `accidentCounted`) | Profil → **LABORATUVAR KAZASI** | CANLI |
| `quizAttempts` | `UIManager.ProvideFeedbackRoutine` (her cevap) | Profil → **QUİZ BAŞARISI** (payda) | CANLI |
| `quizCorrect` | `UIManager.ProvideFeedbackRoutine` (doğru cevap) | Profil → **QUİZ BAŞARISI** (pay) | CANLI |
| `maxLevelUnlocked` | `UIManager.ShowWinScreen`, `RegistrationManager` | Profil → **AÇILAN BÖLÜM**; `RecipeBook` geriye dönük doldurma | **YANLIŞ → onarıldı** |
| `unlockedLevel` | `UIManager.ShowWinScreen` | Bölüm haritası kilitleri (`LevelMenuManager`) | **YANLIŞ → onarıldı** |
| `discoveredRecipes` | `RecipeBook.Discover` (ilk birleşmede) | Tarif defteri: satırlar + sayaç + ilerleme halkası | CANLI |
| `totalScore` | `SaveService.AddLevelScore` (`GridManager`, bölüm kazanılınca) | **hiçbir yerde** | ÖLÜ-KAYIT |

### `maxLevelUnlocked` / `unlockedLevel` neden YANLIŞ'tı

`ShowWinScreen` kazanılan bölümün indeksine 1 ve 2 ekliyordu, üst sınır
yoktu. 7 bölümlük oyunda son bölüm kazanılınca `unlockedLevel = 7` ve
`maxLevelUnlocked = 8` oluyordu; profil kartı **"AÇILAN BÖLÜM 8"** yazıyordu
— var olmayan bir bölüm. Bölüm haritasında da hiçbir düğme "şu an oynanacak"
mavisinde kalmıyordu.

Onarım iki yerde:
- **Yazarken:** `ShowWinScreen` artık `LevelManager.levels.Count` ile kırpıyor.
- **Eski kayıtlarda:** `StatsIntegrity.Repair` açılışta kırpıyor
  (`RecipeCatalog.levels.Count` üzerinden; bu asset iki sahneden de okunur).

### `totalScore` neden bağlanmadı

Gösterilecek yeri yok: bölüm sonu paneli yalnızca başlık + iki buton,
profil kartlarının dördü de dolu. Yeni kart eklemek profil ekranını yeniden
tasarlamak olurdu — bu Faz 2'nin işi. **Faz 2 için not:** değer
`GameManager.currentScore`'un kazanma anındaki hâli, yani *kalan* skor;
geri alma ve Joker'e harcanan puanlar sayılmıyor. Gösterilmeye başlanacaksa
önce "kazanılan toplam" anlamına çevrilmeli.

## Ayarlar (istatistik değil, ama tarandı)

| Alan | Nerede YAZILIYOR | Nerede OKUNUYOR | Durum |
|---|---|---|---|
| `musicOn`, `sfxOn` | `SettingsManager` | `AudioManager` | CANLI |
| `gameMode` | `SettingsManager` | `GridManager`, `UIManager` | CANLI |
| `language` | `Loc` | `Loc` | CANLI |
| `showReactionEquations` | **hiçbir yerde** | `ReactionEquationDisplay.Enabled` | ÖLÜ-KAYIT (ayar) |
| `mentorHintsOn` | `SettingsManager` | `InvalidMergeFeedback.Enabled` (geçersiz birleştirme geri bildirimi) | CANLI |
| `flashcardsOn` | `SettingsManager` | yalnızca kendi anahtarının görünümü | ÖLÜ-KAYIT (ayar) |

- `showReactionEquations` okunuyor ama **ayar ekranında karşılığı yok**:
  oyuncu tepkime denklemlerini kapatamıyor, değer kalıcı olarak varsayılan
  (açık). Düzeltmesi yeni bir anahtar satırı eklemek, yani yeni UI → Faz 2.
- `mentorHintsOn` **CANLI**: 1.3'te geçersiz birleştirme geri bildirimine
  bağlandı (`InvalidMergeFeedback.Enabled`) — anahtar kapalıyken mesaj
  çıkmıyor. 1.3 main'e birleşince bu satır CANLI'ya döndü.
- `flashcardsOn` kaydediliyor ama **hiçbir davranışı değiştirmiyor**:
  oyuncu anahtarı çeviriyor, oyunda bir şey olmuyor. Bağlamak yeni bir
  özellik (bilgi kartları) demek — CLAUDE.md'nin onay kuralına tabi, bu
  adımın kapsamı dışı.

## Ayrılmış alanlar (Faz 2–3, dokunulmadı)

`xp`, `rankIndex`, `badges`, `streakDays`, `lastPlayedDate`, `collection`,
`researchPoints`, `encyclopediaRead`, `dailyPuzzleHistory` — `SaveData`
içinde yorum olarak duruyor, henüz alan değiller.

## Diğer bulgular (kullanıcıya görünmeyen)

- `ProfileManager.mainMenuAvatarImage` ile `profileAvatarImage` **aynı**
  objeye bağlı (profil panelindeki avatar). Ana menü düğmesinin avatarını
  zaten `MainMenuManager.profileButtonAvatarImage` dolduruyor, yani ekranda
  bir eksiklik yok; alan adı yanıltıcı, davranış doğru. Dokunulmadı.
- `ProfileManager` "Kimlik No: " metnini sabit yazıyor (Loc dışı). Çeviri
  turunun işi.
