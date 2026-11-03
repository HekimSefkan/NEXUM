# -*- coding: utf-8 -*-
"""NEXUM — tek arka plan kaynağının üretimi (3c/5).

Oyunun bütün ekranları TEK arka plan görselini kullanır. Kaynak dosya
``Tools/ui-kit/source/nexum_background_source.png``; bu script onu en-boy
oranını BOZMADAN hedef yüksekliğe büyütür ve ``Assets/Art/Backgrounds``
altına yazar.

Neden büyütülüyor: 1440 genişlikteki bir cihazda arka plan 1440x2560
piksel kaplar; kaynak 941 piksel geniş, yani oyun onu zaten her karede
1.5x büyütüyordu. Burada bir kez LANCZOS ile büyütmek çalışma zamanındaki
çift örneklemeyi (önce bilinear büyütme, sonra ekrana örnekleme) ortadan
kaldırır. YENİ AYRINTI ÜRETMEZ — büyütme ara değerlerdir, keskinlik
kaynağın kendi keskinliğidir.

İşleme katmanı (karartma, vinyet, ızgara, panel şeridi) BURAYA PİŞMEZ:
o katman sahnede, NexumBackgroundBuilder tarafından kurulur ve
parametreleri koddan değiştirilebilir.

Kullanım:  python Tools/upscale_background.py [hedef_yukseklik]
"""

import os
import sys

from PIL import Image, ImageStat

KAYNAK = os.path.join("Tools", "ui-kit", "source", "nexum_background_source.png")
HEDEF = os.path.join("Assets", "Art", "Backgrounds", "nexum_background.png")
HEDEF_YUKSEKLIK = 3840


def parlaklik(im):
    return round(ImageStat.Stat(im.convert("L")).mean[0], 2)


def main():
    yukseklik = int(sys.argv[1]) if len(sys.argv) > 1 else HEDEF_YUKSEKLIK

    if not os.path.exists(KAYNAK):
        raise SystemExit("kaynak yok: %s" % KAYNAK)

    src = Image.open(KAYNAK).convert("RGB")
    oran = src.width / float(src.height)
    genislik = int(round(yukseklik * oran))

    out = src.resize((genislik, yukseklik), Image.LANCZOS)

    klasor = os.path.dirname(HEDEF)
    if not os.path.isdir(klasor):
        os.makedirs(klasor)
    out.save(HEDEF, "PNG", optimize=True)

    print("kaynak : %dx%d  oran=%.6f  parlaklik=%s" % (src.width, src.height, oran, parlaklik(src)))
    print("cikti  : %dx%d  oran=%.6f  parlaklik=%s" % (out.width, out.height,
                                                       out.width / float(out.height), parlaklik(out)))
    print("dosya  : %s  (%.2f MB)" % (HEDEF, os.path.getsize(HEDEF) / 1048576.0))
    print("oran sapmasi: %.6f" % abs(oran - out.width / float(out.height)))


if __name__ == "__main__":
    main()
