# -*- coding: utf-8 -*-
"""NEXUM — arka plan işleme katmanının sprite'ları (3c/5).

Arka plan TEK görseldir; üzerindeki işleme (karartma, vinyet, ince ölçü
ızgarası, panel şeridi) görselin İÇİNE PİŞİRİLMEZ, sahnede ayrı katman
olarak durur. Bu script o katmanların iki dokusunu üretir:

  bg_vignette  Kenarları koyulaştıran eliptik alfa rampası. Beyaz çizilir,
               rengi sahnede palet rolünden gelir.
  bg_grid      192 birimlik ölçü ızgarası karosu (Image.Type.Tiled). Kaynak
               görselin kendi ızgarası perspektiflidir ve ekran boyutuna
               göre ölçeklenir; bu karo her ekranda AYNI adımda kalır.

Karartma katmanının dokusu yoktur (düz renk), panel şeridi kitin kendi
9-slice panelini kullanır — yeni görsel üretilmez.

Çıktı: Tools/ui-kit/C/  (NexumKitImport buradan Assets/Art/UI'ye alır)

Kullanım:  python Tools/generate_bg_layers.py
"""

import math
import os

from PIL import Image

CIKTI = os.path.join("Tools", "ui-kit", "C")

VIGNETTE_BOYUT = (512, 1024)
# Merkezin kaçta kaçı tamamen açık kalır (içerik alanı kararmasın)
VIGNETTE_ICERIK = 0.45
VIGNETTE_GUC = 1.6

GRID_BOYUT = 192
GRID_KALIN = 2      # ana çizgi
GRID_INCE = 1       # ara işaret
GRID_ANA_ALFA = 210
GRID_ARA_ALFA = 110


def vignette():
    w, h = VIGNETTE_BOYUT
    im = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    px = im.load()
    cx, cy = (w - 1) / 2.0, (h - 1) / 2.0
    for y in range(h):
        dy = (y - cy) / cy
        for x in range(w):
            dx = (x - cx) / cx
            r = math.sqrt(dx * dx + dy * dy) / math.sqrt(2.0)
            t = (r - VIGNETTE_ICERIK) / (1.0 - VIGNETTE_ICERIK)
            t = 0.0 if t < 0.0 else (1.0 if t > 1.0 else t)
            px[x, y] = (255, 255, 255, int(round(255 * (t ** VIGNETTE_GUC))))
    return im


def grid():
    n = GRID_BOYUT
    im = Image.new("RGBA", (n, n), (255, 255, 255, 0))
    px = im.load()

    for i in range(n):
        for k in range(GRID_KALIN):
            px[i, k] = (255, 255, 255, GRID_ANA_ALFA)
            px[k, i] = (255, 255, 255, GRID_ANA_ALFA)

    orta = n // 2
    for i in range(n):
        for k in range(GRID_INCE):
            if px[i, orta + k][3] == 0:
                px[i, orta + k] = (255, 255, 255, GRID_ARA_ALFA)
            if px[orta + k, i][3] == 0:
                px[orta + k, i] = (255, 255, 255, GRID_ARA_ALFA)
    return im


def yaz(ad, im):
    if not os.path.isdir(CIKTI):
        os.makedirs(CIKTI)
    yol = os.path.join(CIKTI, ad + ".png")
    im.save(yol, "PNG", optimize=True)
    print("%s  %dx%d  (%.1f KB)" % (yol, im.width, im.height, os.path.getsize(yol) / 1024.0))


def main():
    yaz("bg_vignette", vignette())
    yaz("bg_grid", grid())


if __name__ == "__main__":
    main()
