# -*- coding: utf-8 -*-
"""NEXUM tepkime sembolleri — TMP sprite'i icin gorsel ureteci.

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

IKI SEMBOL TEK SAYFADA: TMP sprite asset'i tek bir doku kullanir. Sayfa
128x256; ALT yari ok (eski 128x128 goruntunun birebir ayni yeri — okun
olculmus GlyphRect'i DEGISMEZ), UST yari "tepkime yok" carpisi.

Carpi neden sprite: "✕" (U+2715) de projenin fontlarinda yok ve atlaslar
statik. Ayni yontem, ayni kalinlik, ayni optik merkez.

Cikti: Assets/Art/Figures/arrow_reaction.png (128x256, seffaf, 4x superornekleme)

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
    # Carpinin opak kutusu okunkiyle AYNI yukseklikte (33 px) olmali: boylece
    # ayni GlyphMetrics (scale 1.77, bearingY 40.2) iki sembolu de ayni optik
    # merkeze oturtur, yeni kalibrasyon gerekmez. Kutu + cizgi kalinligi = 33.
    "cross_box": 0.193,
    "cross_w": 0.065,     # carpinin cizgi kalinligi = okun govdesi
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


def draw_cross(p):
    """Tepkime yok: carpi. Cizgi kalinligi okun govdesiyle AYNI."""
    s = p["size"] * p["ss"]
    img = Image.new("RGBA", (s, s), (255, 255, 255, 0))
    d = ImageDraw.Draw(img)

    cy = s * p["body_center"]
    cx = s * 0.5
    yari = s * p["cross_box"] / 2.0
    kal = s * p["cross_w"]

    d.line([cx - yari, cy - yari, cx + yari, cy + yari], fill=(255, 255, 255, 255),
           width=int(round(kal)))
    d.line([cx - yari, cy + yari, cx + yari, cy - yari], fill=(255, 255, 255, 255),
           width=int(round(kal)))

    return img.resize((p["size"], p["size"]), Image.LANCZOS)


def rapor(ad, sayfa, ust, yukseklik):
    """Sayfadaki bir yarinin opak sinirlarini TMP GlyphRect dilinde yazar."""
    dilim = sayfa.crop((0, ust, sayfa.width, ust + yukseklik))
    bbox = dilim.split()[3].getbbox()
    if bbox is None:
        print("%-6s: BOS" % ad)
        return
    x0, y0, x1, y1 = bbox
    # Sayfa icindeki mutlak ust koordinatlar
    uy0, uy1 = ust + y0, ust + y1
    # TMP GlyphRect ALTTAN sayar
    gy = sayfa.height - uy1
    print("%-6s: x %d..%d  y(ustten) %d..%d  ->  GlyphRect(%d, %d, %d, %d)"
          % (ad, x0, x1, uy0, uy1, x0, gy, x1 - x0, uy1 - uy0))


def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)

    boy = PARAMS["size"]
    sayfa = Image.new("RGBA", (boy, boy * 2), (255, 255, 255, 0))
    sayfa.paste(draw_cross(PARAMS), (0, 0))       # ust yari: carpi
    sayfa.paste(draw_arrow(PARAMS), (0, boy))     # alt yari: ok (yeri DEGISMEZ)
    sayfa.save(OUT)

    print("yazildi: %s" % OUT)
    print("boyut  : %dx%d" % sayfa.size)
    rapor("carpi", sayfa, 0, boy)
    rapor("ok", sayfa, boy, boy)
    print("not    : okun GlyphRect'i eski 128x128 goruntudekiyle AYNI kalmali.")


if __name__ == "__main__":
    main()
