# -*- coding: utf-8 -*-
"""NEXUM — logo kilidinin (N kutusu + EXUM) ortalanması (3c/4).

Sorun ÖLÇÜLDÜ: ``NEXUM_logo_metal.png`` 1366x320 tuval, ama boyalı içerik
yalnızca x=19..955 arasında. İçeriğin merkezi 487, tuvalin merkezi 683 —
yani kilit, kendi kutusunun içinde %14 SOLA kaymış duruyor. Bir Image'ı
ortalamak bu yüzden logoyu ortalamıyordu: ortalanan şey boşluğu da içeren
tuvaldi.

Bu script şeffaf payı kırpar; tuval = içerik olur, böylece logo nereye
konursa konsun BİRLEŞİK SINIRLARINA göre ortalanır. Görselin kendisi
değişmez (tek piksel yeniden örneklenmez), yalnızca boş pay atılır.

IDEMPOTENT: pay kalmadıysa dosya yazılmaz.

Kullanım:  python Tools/trim_logo.py
"""

import os

from PIL import Image

HEDEFLER = [
    os.path.join("Assets", "Art", "Figures", "NEXUM_logo_metal.png"),
    os.path.join("Assets", "Art", "Figures", "NEXUM_mark_metal.png"),
]


def kirp(yol):
    if not os.path.exists(yol):
        print("yok: %s" % yol)
        return

    im = Image.open(yol)
    if im.mode != "RGBA":
        im = im.convert("RGBA")

    kutu = im.split()[-1].getbbox()
    if kutu is None:
        print("%s: tamamen saydam, atlandi" % yol)
        return

    if kutu == (0, 0, im.width, im.height):
        print("%s: pay yok (%dx%d), dokunulmadi" % (yol, im.width, im.height))
        return

    out = im.crop(kutu)
    out.save(yol, "PNG", optimize=True)

    eski_merkez = (im.width / 2.0, im.height / 2.0)
    icerik_merkez = ((kutu[0] + kutu[2]) / 2.0, (kutu[1] + kutu[3]) / 2.0)
    print("%s: %dx%d -> %dx%d  (merkez kaymasi %.1f, %.1f piksel = %%%.1f genislik)"
          % (yol, im.width, im.height, out.width, out.height,
             eski_merkez[0] - icerik_merkez[0], eski_merkez[1] - icerik_merkez[1],
             100.0 * abs(eski_merkez[0] - icerik_merkez[0]) / im.width))
    print("    yeni en-boy orani = %.4f" % (out.width / float(out.height)))


def main():
    for yol in HEDEFLER:
        kirp(yol)


if __name__ == "__main__":
    main()
