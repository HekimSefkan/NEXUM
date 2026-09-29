"""NEXUM çekirdek döngü simülatörü (analiz aracı, oyunu değiştirmez).

Amaç: spawn kuralı ve kayma davranışı seçeneklerinin 7 bölümün çözülebilirliğine
ve zorluğuna etkisini ölçmek. Tarifler, spawn havuzları ve hedefler Game.unity
içinden okunur, elle kopyalanmaz.

Kullanım (proje kökünden):
    python Tools/sim_core_loop.py [oyun_sayisi]

Model sınırları (rapora yazılır):
- Oyuncu yerine sezgisel bir yapay oyuncu var; insan oyuncu daha iyi/kötü oynar.
- Animasyon, undo, joker, ipucu ve quiz yok; yalnızca kaydırma-birleşme döngüsü.
- Sınav/serbest mod farkları yalnızca entropi kuralıyla modellenir.
"""
import os
import random
import re
import sys
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCENE = os.path.join(ROOT, "Assets", "Scenes", "Game.unity")

GRID = 4
CELLS = GRID * GRID


# ---------------------------------------------------------------- sahne okuma

def read_scene():
    guid_name = {}
    prefab_dir = os.path.join(ROOT, "Assets", "Prefabs")
    for f in os.listdir(prefab_dir):
        if f.endswith(".prefab.meta"):
            txt = open(os.path.join(prefab_dir, f), encoding="utf-8", errors="ignore").read(400)
            guid = re.search(r"guid: ([0-9a-f]{32})", txt).group(1)
            guid_name[guid] = f[:-12]

    text = open(SCENE, encoding="utf-8", errors="ignore").read()

    def name_of(guid):
        return guid_name.get(guid, "?" + guid[:6])

    # --- tarifler
    block = re.search(r"\n  recipes:\n(.*?)\n  hasUsedRevive:", text, re.S).group(1)
    recipes = []
    for m in re.finditer(
            r"- element1: \{fileID: -?\d+, guid: ([0-9a-f]{32}).*?\n"
            r"\s*element2: \{fileID: -?\d+, guid: ([0-9a-f]{32}).*?\n"
            r"\s*resultPrefab: \{fileID: -?\d+, guid: ([0-9a-f]{32}).*?\n"
            r"\s*scoreReward: (\d+)", block):
        recipes.append((name_of(m.group(1)), name_of(m.group(2)), name_of(m.group(3)), int(m.group(4))))

    # --- bölümler
    levels_block = "\n" + re.search(r"\n  levels:\n(.*?)\n  currentLevelIndex:", text, re.S).group(1)
    levels = []
    for chunk in re.split(r"\n  - levelName: ", levels_block)[1:]:
        name = chunk.split("\n")[0].strip()
        start = int(re.search(r"startingTileCount: (\d+)", chunk).group(1))
        pool = [(name_of(g), int(w)) for g, w in re.findall(
            r"- elementPrefab: \{fileID: -?\d+, guid: ([0-9a-f]{32}).*?\n\s*spawnWeight: (\d+)", chunk)]
        goals = [(name_of(g), int(a)) for g, a in re.findall(
            r"- targetPrefab: \{fileID: -?\d+, guid: ([0-9a-f]{32}).*?\n\s*targetAmount: (\d+)", chunk)]
        levels.append(dict(name=name, start=start, pool=pool, goals=goals))

    return recipes, levels


# ---------------------------------------------------------------- oyun modeli

class Game:
    def __init__(self, level, recipe_map, rng, spawn_mode, slide_mode, entropy_limit=5):
        self.level = level
        self.rmap = recipe_map
        self.rng = rng
        self.spawn_mode = spawn_mode      # "merge_only" | "any_move" | "hybrid"
        self.slide_mode = slide_mode      # "one_step" | "full"
        self.entropy_limit = entropy_limit
        self.cells = [None] * CELLS
        self.produced = defaultdict(int)
        self.empty_shifts = 0
        self.moves = 0
        self.score = 0
        for _ in range(level["start"]):
            self.spawn()

    # --- yardımcılar
    def empty_indices(self):
        return [i for i, v in enumerate(self.cells) if v is None]

    def spawn(self):
        free = self.empty_indices()
        if not free:
            return False
        names, weights = zip(*self.level["pool"])
        self.cells[self.rng.choice(free)] = self.rng.choices(names, weights=weights)[0]
        return True

    def merge_result(self, a, b):
        return self.rmap.get((a, b) if a < b else (b, a))

    def lines(self, direction):
        """Hareket yönüne göre hücre index dizileri (ilk eleman hedefe en yakın)."""
        out = []
        for k in range(GRID):
            if direction == "up":
                out.append([k + GRID * r for r in range(GRID)])
            elif direction == "down":
                out.append([k + GRID * r for r in reversed(range(GRID))])
            elif direction == "left":
                out.append([k * GRID + c for c in range(GRID)])
            else:
                out.append([k * GRID + c for c in reversed(range(GRID))])
        return out

    # --- kaydırma
    def shift(self, direction, apply=True):
        cells = self.cells[:] if apply else self.cells[:]
        moved = merged = 0
        gained = defaultdict(int)

        for line in self.lines(direction):
            values = [cells[i] for i in line]
            if self.slide_mode == "full":
                new, m, g = self._line_full(values)
            else:
                new, m, g = self._line_one_step(values)
            for i, v in zip(line, new):
                if cells[i] != v:
                    moved += 1
                cells[i] = v
            merged += m
            for k, v in g.items():
                gained[k] += v

        changed = cells != self.cells
        if apply and changed:
            self.cells = cells
        return changed, merged, gained

    def _line_full(self, values):
        """2048 standardı: sıkıştır, bir kez birleştir, tekrar sıkıştır."""
        tiles = [v for v in values if v is not None]
        out, merged, gained = [], 0, defaultdict(int)
        i = 0
        while i < len(tiles):
            if i + 1 < len(tiles):
                res = self.merge_result(tiles[i], tiles[i + 1])
                if res:
                    out.append(res)
                    gained[res] += 1
                    merged += 1
                    i += 2
                    continue
            out.append(tiles[i])
            i += 1
        out += [None] * (GRID - len(out))
        return out, merged, gained

    def _line_one_step(self, values):
        """Mevcut davranış: her taş en fazla bir hücre ilerler; hedef doluysa
        ve tarif varsa birleşir. Hedefe en yakın taştan başlanır."""
        out = values[:]
        merged, gained = 0, defaultdict(int)
        for pos in range(1, GRID):
            src = out[pos]
            if src is None:
                continue
            dst = out[pos - 1]
            if dst is None:
                out[pos - 1] = src
                out[pos] = None
            else:
                res = self.merge_result(src, dst)
                if res:
                    out[pos - 1] = res
                    out[pos] = None
                    gained[res] += 1
                    merged += 1
        return out, merged, gained

    def has_any_move(self):
        for d in ("up", "down", "left", "right"):
            snapshot = self.cells[:]
            changed, _, _ = self.shift(d, apply=True)
            self.cells = snapshot
            if changed:
                return True
        return False

    def goals_met(self):
        return all(self.produced[name] >= amount for name, amount in self.level["goals"])

    # --- bir hamle
    def play(self, direction):
        changed, merged, gained = self.shift(direction)
        if not changed:
            return False
        self.moves += 1
        for k, v in gained.items():
            self.produced[k] += v

        if merged > 0:
            self.spawn()
            self.empty_shifts = 0
        else:
            if self.spawn_mode == "any_move":
                self.spawn()
                self.empty_shifts = 0
            elif self.spawn_mode == "hybrid":
                self.empty_shifts += 1
                if self.empty_shifts >= 2:
                    self.spawn()
                    self.empty_shifts = 0
            else:  # merge_only (mevcut)
                self.empty_shifts += 1
                if self.entropy_limit and self.empty_shifts >= self.entropy_limit:
                    self.spawn()
                    self.empty_shifts = 0
        return True


# ---------------------------------------------------------------- yapay oyuncu

def needed_chain(level, rmap_by_result):
    """Hedefe giden ara ürünleri toplar; yapay oyuncu bunları önceliklendirir."""
    wanted = set(name for name, _ in level["goals"])
    frontier = list(wanted)
    while frontier:
        cur = frontier.pop()
        for a, b, res in rmap_by_result.get(cur, []):
            for x in (a, b):
                if x not in wanted:
                    wanted.add(x)
                    frontier.append(x)
    return wanted


def evaluate(game, wanted):
    """Basit sezgisel: hedef zincirindeki ürünler + boş hücre + birleşebilirlik."""
    score = 0
    for name, amount in game.level["goals"]:
        score += min(game.produced[name], amount) * 1000
    goal_names = set(n for n, _ in game.level["goals"])
    for v in game.cells:
        if v in goal_names:
            score += 60          # hedefe bir adım kalmış ürünler
        elif v in wanted:
            score += 10          # zincirdeki ara ürünler
    score += len(game.empty_indices()) * 12
    # komşu birleşebilirlik
    for i in range(CELLS):
        if game.cells[i] is None:
            continue
        for j in (i + 1, i + GRID):
            if j < CELLS and (j % GRID != 0 or j == i + GRID) and game.cells[j] is not None:
                if game.merge_result(game.cells[i], game.cells[j]):
                    score += 5
    return score


def play_game(level, recipes, rng, spawn_mode, slide_mode, max_moves=400):
    rmap = {}
    rmap_by_result = defaultdict(list)
    for a, b, res, _ in recipes:
        key = (a, b) if a < b else (b, a)
        rmap.setdefault(key, res)
        rmap_by_result[res].append((a, b, res))

    game = Game(level, rmap, rng, spawn_mode, slide_mode)
    wanted = needed_chain(level, rmap_by_result)

    for _ in range(max_moves):
        if game.goals_met():
            return True, game.moves
        best, best_score = None, -1e18
        for d in ("up", "down", "left", "right"):
            snapshot = game.cells[:]
            produced = dict(game.produced)
            changed, merged, gained = game.shift(d)
            if changed:
                for k, v in gained.items():
                    game.produced[k] += v
                value = evaluate(game, wanted) + merged * 25
                if value > best_score:
                    best_score, best = value, d
            game.cells = snapshot
            game.produced = defaultdict(int, produced)
        if best is None:
            return False, game.moves          # hamle yok = oyun sonu
        game.play(best)
        if not game.empty_indices() and not game.has_any_move():
            return game.goals_met(), game.moves
    return game.goals_met(), game.moves


# ---------------------------------------------------------------- çalıştırma

def main():
    runs = int(sys.argv[1]) if len(sys.argv) > 1 else 200
    recipes, levels = read_scene()

    print("Tarifler (%d):" % len(recipes))
    for a, b, res, sc in recipes:
        print("   %-10s + %-10s -> %-12s (%d puan)" % (a, b, res, sc))
    print()

    combos = [
        ("MEVCUT (tek hücre, yalnız birleşmede spawn)", "merge_only", "one_step"),
        ("A: her hamlede spawn (tek hücre)", "any_move", "one_step"),
        ("B: duvara kadar kayma (mevcut spawn)", "merge_only", "full"),
        ("A+B: her hamlede spawn + duvara kadar kayma", "any_move", "full"),
        ("C: melez spawn (2 boş hamlede bir) + duvara kadar", "hybrid", "full"),
    ]

    print("%-52s %s" % ("senaryo", "  ".join("L%d" % (i + 1) for i in range(len(levels)))))
    for label, spawn_mode, slide_mode in combos:
        rates, moves_avg = [], []
        for li, level in enumerate(levels):
            rng = random.Random(1234 + li)
            wins, total_moves = 0, 0
            for _ in range(runs):
                won, moves = play_game(level, recipes, rng, spawn_mode, slide_mode)
                wins += 1 if won else 0
                total_moves += moves
            rates.append(wins * 100.0 / runs)
            moves_avg.append(total_moves / runs)
        print("%-52s %s" % (label, "  ".join("%3.0f%%" % r for r in rates)))
        print("%-52s %s" % ("   ortalama hamle", "  ".join("%4.0f" % m for m in moves_avg)))
    print("\nNot: yapay oyuncu sezgiseldir; mutlak kazanma oranı değil, senaryolar arası FARK anlamlıdır.")


if __name__ == "__main__":
    main()
