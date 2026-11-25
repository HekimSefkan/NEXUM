# -*- coding: utf-8 -*-
"""NEXUM yildiz kalibrasyon araci (analiz, oyunu DEGISTIRMEZ).

Soru: her bolumde "hamle sinirinin altinda bitirme" (ikinci yildiz) icin
sinir KAC olmali? Oyunda rastgele dogma var, yani tek bir "optimal hamle
sayisi" YOK; bu yuzden sinir tek bir kosudan degil DAGILIMDAN turetilir.

YONTEM
------
- Her bolum N FARKLI TOHUMLA oynanir (varsayilan 300, brif en az 200
  istiyor). Her tohum ayri bir `random.Random` ornegi, yani bolumler
  arasi karsilastirma ayni tohum kumesiyle yapilir.
- Yalnizca KAZANILAN oyunlarin hamle dagilimi olculur: medyan, %75, %90.
- Sinir onerisi = kazanan oyunlarin %75'inin altinda kalabildigi deger
  (yukari yuvarlanir). Alternatifler (medyan x1,3 ve %90) tabloya girer.
- Ardindan oranlar olculur: bolum basina ikinci ve ucuncu yildizi alma
  orani.

BOTUN SINIRLARI (rapora yazilir, gizlenmez)
-------------------------------------------
- Bot IPUCU, JOKER ve GERI ALMA KULLANMAZ. Ucuncu yildizin kosulu zaten
  "ipucusuz ve ucretli Jokersiz bitirme" oldugu icin, botun kazandigi her
  oyun ucuncu yildizi da alir: **ucuncu yildiz orani = kazanma orani**.
  Bu bir kisitlama degil, kosulun tanimi.
- Derinlik-2 bot kullanilir. Daha guclu bir cozucu DAHA AZ hamleyle
  bitirir, yani siniri DARALTIR; insan oyuncuya haksiz bir cubuk kurardi.
  Derinlik farki `--derinlik-karsilastir` ile olculup rapora konur.
- Mutlak oranlar degil, BOLUMLER ARASI ve SENARYOLAR ARASI fark
  anlamlidir (sim_core_loop ile ayni uyari).

Kullanim (proje kokunden):
    python Tools/sim_stars.py [tohum_sayisi] [derinlik]
    python Tools/sim_stars.py 300 2 --derinlik-karsilastir
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import sim_core_loop as sim


def yuzdelik(sirali, oran):
    """Dogrusal interpolasyonsuz yuzdelik (kucuk orneklemde abartmasin)."""
    if not sirali:
        return 0
    i = int(round((len(sirali) - 1) * oran))
    return sirali[max(0, min(len(sirali) - 1, i))]


def bolum_olc(level, recipes, tohum_sayisi, depth, taban=9000):
    """Bir bolumu N tohumla oynar; (kazanan_hamleler, kazanma_sayisi) doner."""
    kazanan_hamleler = []
    for t in range(tohum_sayisi):
        rng = random.Random(taban + t)
        won, moves = sim.play_game(level, recipes, rng, "hybrid", "full", depth=depth)
        if won:
            kazanan_hamleler.append(moves)
    kazanan_hamleler.sort()
    return kazanan_hamleler


def main():
    tohum = int(sys.argv[1]) if len(sys.argv) > 1 and sys.argv[1].isdigit() else 300
    depth = int(sys.argv[2]) if len(sys.argv) > 2 and sys.argv[2].isdigit() else 2
    karsilastir = "--derinlik-karsilastir" in sys.argv

    recipes, levels = sim.read_scene()

    print("== YILDIZ KALIBRASYONU: %d tohum / bolum, derinlik-%d bot ==" % (tohum, depth))
    print()
    print("%-5s %8s %7s %8s %8s %8s %9s %9s %9s" %
          ("bolum", "kazanma", "orneklem", "medyan", "%75", "%90",
           "SINIR", "2.yildiz", "3.yildiz"))

    satirlar = []
    for li, level in enumerate(levels):
        hamleler = bolum_olc(level, recipes, tohum, depth)
        kazanma = len(hamleler)
        kazanma_orani = kazanma * 100.0 / tohum

        medyan = yuzdelik(hamleler, 0.50)
        p75 = yuzdelik(hamleler, 0.75)
        p90 = yuzdelik(hamleler, 0.90)

        # Sinir onerisi: kazanan oyunlarin %75'inin altinda kalabildigi deger.
        sinir = p75

        # Oranlar: ikinci yildiz hem kazanan oyunlar icinde hem tum denemelerde
        iki_kazanan = sum(1 for m in hamleler if m <= sinir)
        iki_orani_kazanan = (iki_kazanan * 100.0 / kazanma) if kazanma else 0.0
        iki_orani_toplam = iki_kazanan * 100.0 / tohum

        # Ucuncu yildiz: bot ipucu/Joker kullanmiyor -> kazanan her oyun alir.
        uc_orani = kazanma_orani

        print("L%-4d %7.0f%% %8d %8d %8d %8d %9d %8.0f%% %8.0f%%" %
              (li + 1, kazanma_orani, kazanma, medyan, p75, p90,
               sinir, iki_orani_kazanan, uc_orani))

        satirlar.append(dict(bolum=li + 1, kazanma=kazanma_orani, orneklem=kazanma,
                             medyan=medyan, p75=p75, p90=p90, sinir=sinir,
                             iki_kazanan=iki_orani_kazanan, iki_toplam=iki_orani_toplam,
                             uc=uc_orani))

    print()
    print("2.yildiz orani = KAZANAN oyunlar icinde; tum denemelerde:")
    print("   " + "  ".join("L%d %.0f%%" % (r["bolum"], r["iki_toplam"]) for r in satirlar))
    print()

    print("-- Alternatif sinirlar --")
    print("%-5s %9s %9s %9s %12s" % ("bolum", "medyan", "medyanx1,3", "%75", "%90"))
    for r in satirlar:
        print("L%-4d %9d %9d %9d %12d" %
              (r["bolum"], r["medyan"], int(round(r["medyan"] * 1.3)), r["p75"], r["p90"]))
    print()

    az = [r for r in satirlar if r["orneklem"] < 30]
    if az:
        print("UYARI: su bolumlerde kazanan oyun ornegi 30'un altinda, dagilim")
        print("       gurultulu: " + ", ".join("L%d (%d)" % (r["bolum"], r["orneklem"]) for r in az))
        print()

    if karsilastir:
        print("-- Derinlik karsilastirmasi (ayni tohumlar) --")
        print("%-5s %14s %14s %10s" % ("bolum", "derinlik-1 %75", "derinlik-2 %75", "fark"))
        for li, level in enumerate(levels):
            az_tohum = max(60, tohum // 4)
            h1 = bolum_olc(level, recipes, az_tohum, 1)
            h2 = bolum_olc(level, recipes, az_tohum, 2)
            a, b = yuzdelik(h1, 0.75), yuzdelik(h2, 0.75)
            print("L%-4d %14d %14d %10d" % (li + 1, a, b, b - a))
        print()
        print("Not: daha guclu cozucu daha AZ hamleyle bitirir, yani siniri")
        print("     daraltir. Sinir derinlik-2 ile kalibre edildi.")

    print("Not: bot ipucu/Joker/geri alma KULLANMAZ; ucuncu yildiz orani")
    print("     kazanma oranina esittir (kosulun tanimi geregi).")
    print("Not: yapay oyuncu sezgiseldir; bolumler arasi FARK anlamlidir.")


if __name__ == "__main__":
    main()
