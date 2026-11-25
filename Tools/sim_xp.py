# -*- coding: utf-8 -*-
"""NEXUM XP olcum araci (analiz, oyunu DEGISTIRMEZ).

Soru: bugunku icerikle (7 bolum, 21 tarif, 36 soru) bir oyuncu ne kadar
KAZANILAN puan toplar? Rutbe esikleri bu veriden turetilir, uydurulmaz.

Tarifler, spawn havuzlari ve hedefler `sim_core_loop` uzerinden Game.unity
icinden okunur; bu arac yalnizca puan muhasebesi ekler.

NEDEN AYRI ARAC: sim_core_loop "bolum cozuluyor mu" sorusunu olcuyor ve
cikti bicimi o soruya gore. Puan muhasebesini oraya eklemek o aracin
ciktisini degistirirdi (parmak izi kiyaslari bozulur).

MODEL SINIRLARI (rapora yazilir):
- Yapay oyuncu sezgiseldir; insan oyuncu daha iyi/kotu oynar.
- Undo, ipucu, Joker ve quiz YOK: olculen tutar LevelRun.EarnedScore'un
  karsiligidir (harcama dusulmez), yani tam olarak XP'nin beslendigi deger.
- Sinav modu (x2 puan) ve serbest mod ayri olculur.

Kullanim (proje kokunden):
    python Tools/sim_xp.py [oyun_sayisi] [derinlik]
"""
import os
import random
import sys
from collections import defaultdict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import sim_core_loop as sim


def oyna_puanli(level, recipes, rng, depth=2, max_moves=400,
                entropy_limit=5, mode_multiplier=1):
    """Bir oyun oynar; (kazandi, hamle, kazanilan_puan) dondurur.

    Kazanilan puan = her birlesmenin recipe.scoreReward toplami (sinav
    modunda x2). Oyundaki `GameManager.AddScore` -> `LevelRun.AddScore`
    zinciriyle ayni muhasebe.
    """
    rmap = {}
    skor = {}
    rmap_by_result = defaultdict(list)
    for a, b, res, sc in recipes:
        key = (a, b) if a < b else (b, a)
        if key not in rmap:
            rmap[key] = res
            skor[key] = sc
        rmap_by_result[res].append((a, b, res))

    game = sim.Game(level, rmap, rng, "hybrid", "full", entropy_limit)
    wanted = sim.needed_chain(level, rmap_by_result)
    kazanilan = 0

    for _ in range(max_moves):
        if game.goals_met():
            return True, game.moves, kazanilan

        best, _ = sim.search(game, wanted, depth)
        if best is None:
            return False, game.moves, kazanilan

        # Hamleyi puanlariyla uygula: shift'in urettigi `gained` sozlugu
        # hangi urunden kac tane dogdugunu veriyor; her urun kendi
        # tarifinin puanini getirir.
        once = dict(game.produced)
        changed, merged, gained = game.shift(best)
        if not changed:
            break
        game.moves += 1
        for k, v in gained.items():
            game.produced[k] += v
            kazanilan += v * _urun_puani(skor, rmap, k) * mode_multiplier

        # spawn / entropi kurallari: sim_core_loop.play ile ayni
        if merged > 0:
            game.spawn()
            game.empty_shifts = 0
            game.moves_since_spawn = 0
        else:
            game.moves_since_spawn += 1
            if game.moves_since_spawn >= 2:
                game.spawn()
                game.moves_since_spawn = 0
            if entropy_limit:
                game.empty_shifts += 1
                if game.empty_shifts >= entropy_limit:
                    game.spawn()
                    game.empty_shifts = 0
                    game.moves_since_spawn = 0

        del once
        if not game.empty_indices() and not game.has_any_move():
            return game.goals_met(), game.moves, kazanilan

    return game.goals_met(), game.moves, kazanilan


_PUAN_CACHE = {}


def _urun_puani(skor, rmap, urun):
    """Urunu ureten tarifin puani (ilk tanim gecerli; GridManager da oyle)."""
    if urun in _PUAN_CACHE:
        return _PUAN_CACHE[urun]
    for key, res in rmap.items():
        if res == urun:
            _PUAN_CACHE[urun] = skor[key]
            return skor[key]
    _PUAN_CACHE[urun] = 0
    return 0


def main():
    runs = int(sys.argv[1]) if len(sys.argv) > 1 else 120
    depth = int(sys.argv[2]) if len(sys.argv) > 2 else 2
    recipes, levels = sim.read_scene()

    print("== KAZANILAN PUAN (LevelRun.EarnedScore karsiligi), %d oyun, derinlik-%d ==" %
          (runs, depth))
    print("%-6s %8s %10s %10s %10s %10s" %
          ("bolum", "kazanma", "ort.puan", "kaz.ort", "kaz.min", "kaz.max"))

    toplam_kazanan_ort = 0
    satirlar = []
    for li, level in enumerate(levels):
        rng = random.Random(4242 + li)
        puanlar, kazanan_puanlar, wins = [], [], 0
        for _ in range(runs):
            won, moves, puan = oyna_puanli(level, recipes, rng, depth=depth)
            puanlar.append(puan)
            if won:
                wins += 1
                kazanan_puanlar.append(puan)

        ort = sum(puanlar) / len(puanlar)
        kort = (sum(kazanan_puanlar) / len(kazanan_puanlar)) if kazanan_puanlar else 0
        kmin = min(kazanan_puanlar) if kazanan_puanlar else 0
        kmax = max(kazanan_puanlar) if kazanan_puanlar else 0
        print("L%-5d %7.0f%% %10.0f %10.0f %10d %10d" %
              (li + 1, wins * 100.0 / runs, ort, kort, kmin, kmax))
        satirlar.append((li + 1, kort if kazanan_puanlar else ort))
        toplam_kazanan_ort += kort if kazanan_puanlar else ort

    print()
    print("Kazanilan puan toplami (her bolum bir kez, kazanan oyun ortalamasi): %.0f"
          % toplam_kazanan_ort)
    print()
    print("Not: yapay oyuncu sezgiseldir. L4-L7 kazanma orani dusuk oldugu icin")
    print("     o bolumlerin 'kazanan oyun ortalamasi' az ornekten gelir.")
    print("Not: undo/ipucu/Joker harcamasi YOK; olculen tutar kazanilan puandir.")


if __name__ == "__main__":
    main()
