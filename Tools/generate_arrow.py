# -*- coding: utf-8 -*-
"""NEXUM tepkime oku — TMP sprite'i icin gorsel ureteci.

Neden gerekli: tepkime oku "→" (U+2192) projenin HICBIR fontunda yok
(Oxanium, Orbitron, LiberationSans) ve atlaslarin hepsi statik. Bu yuzden ok
bir TMP sprite'i olarak gomuluyor.

Tasarim kurallari:
- TEK RENK BEYAZ (alfa ile sekil): TMP rengi tonlayabilsin diye. Denklemde
  girdiler sonuk, urun vurgulu renkte; ok da metnin rengini alabilmeli.
- Metin satir yuksekligiyle oranti: govde yuksekligi CANVAS'in %18'i,
  boylece 36 punto metinde ~13 piksellik bir ok ciziliyor.
- Optik denge: okun govdesi tam ortada degil, x-height merkezine denk gelecek
  sekilde YUKARI kaydirilmis (BODY_CENTER). Taban hizasi Editor tarafinda
  GlyphMetrics.bearingY ile verilir; buradaki kaydirma yalnizca gorselin
  kendi icindeki dengeyi saglar.

Cikti: Assets/Art/Figures/arrow_reaction.png (kare, seffaf, 4x superornekleme)

Kullanim (proje kokunden):
    python Tools/generate_arrow.py
"""
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Art", "Figures", "arrow_reaction.png")

PARAMS = {
    "size": 128,          # cikti kenari (piksel)
    "ss": 4,              # superornekleme carpani
    "margin": 0.10,       # soldan/sagdan bos pay (kenar orani)
    "shaft_h": 0.065,     # govde kalinligi (kenar orani)
    "head_w": 0.20,       # ok ucu genisligi (kenar orani)
    "head_h": 0.22,       # ok ucu yuksekligi (kenar orani)
    "body_center": 0.46,  # dikey merkez: 0.5 tam orta, kucuk deger = yukari
}


def draw_arrow(p):
    s = p["size"] * p["ss"]
    img = Image.new("RGBA", (s, s), (255, 255, 255, 0))
    d = ImageDraw.Draw(img)

    cy = s * p["body_center"]
    left = s * p["margin"]
    right = s * (1.0 - p["margin"])

    shaft_h = s * p["shaft_h"]
    head_w = s * p["head_w"]
    head_h = s * p["head_h"]

    # Govde: ok ucunun biraz icine girsin ki birlesme yerinde centik olmasin
    shaft_right = right - head_w * 0.55
    d.rectangle(
        [left, cy - shaft_h / 2.0, shaft_right, cy + shaft_h / 2.0],
        fill=(255, 255, 255, 255),
    )

    # Ok ucu: ucgen
    d.polygon(
        [
            (right, cy),
            (right - head_w, cy - head_h / 2.0),
            (right - head_w, cy + head_h / 2.0),
        ],
        fill=(255, 255, 255, 255),
    )

    return img.resize((p["size"], p["size"]), Image.LANCZOS)


def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img = draw_arrow(PARAMS)
    img.save(OUT)

    # Opak sinirlari raporla: Editor tarafi GlyphRect'i buna gore kurar
    bbox = img.split()[3].getbbox()
    print("yazildi: %s" % OUT)
    print("boyut  : %dx%d" % img.size)
    print("opak   : x %d..%d  y %d..%d  (genislik %d, yukseklik %d)"
          % (bbox[0], bbox[2], bbox[1], bbox[3], bbox[2] - bbox[0], bbox[3] - bbox[1]))
    print("not    : y degerleri USTTEN; TMP GlyphRect alttan sayar.")


if __name__ == "__main__":
    main()
