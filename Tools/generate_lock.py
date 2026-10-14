# -*- coding: utf-8 -*-
"""NEXUM kilit ikonu — tarif defterindeki kesfedilmemis satirlar icin.

Neden uretiliyor: projede kucuk bir kilit ikonu yok. Var olan
`level_selection_kilitlibutton.png` tam bir BUTON gorseli; 40 birimlik bir
ikon yuvasina kucultulunce okunmuyor. Bu yuzden ikon, tepkime okuyla ayni
yontemle (vektor cizim + superornekleme) uretiliyor; hicbir gorsel
buyutulmuyor.

Tasarim kurallari:
- TEK RENK BEYAZ (alfa ile sekil): rengi NexumTheme'den TMP/Image tonlasin.
- Govde yuvarlatilmis dikdortgen, uzerinde U seklinde kemer (halka).
- Govdenin ortasinda anahtar deligi: sekil kucukken de "kilit" okunsun.

Cikti: Assets/Art/Figures/lock_icon.png (kare, seffaf, 4x superornekleme)

Kullanim (proje kokunden):
    python Tools/generate_lock.py
"""
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Art", "Figures", "lock_icon.png")

PARAMS = {
    "size": 128,           # cikti kenari (piksel)
    "ss": 4,               # superornekleme carpani
    "body_w": 0.62,        # govde genisligi (kenar orani)
    "body_h": 0.44,        # govde yuksekligi
    "body_bottom": 0.90,   # govdenin alt kenari
    "corner": 0.08,        # govde kose yarikapi
    "shackle_w": 0.38,     # kemer dis genisligi
    "shackle_h": 0.34,     # kemer yuksekligi (govdenin ustunde)
    "shackle_t": 0.085,    # kemer kalinligi
    "hole_r": 0.055,       # anahtar deligi yarikapi
    "hole_cy": 0.70,       # anahtar deligi merkezi (dikey)
}

WHITE = (255, 255, 255, 255)
CLEAR = (255, 255, 255, 0)


def draw_lock(p):
    s = p["size"] * p["ss"]
    img = Image.new("RGBA", (s, s), CLEAR)
    d = ImageDraw.Draw(img)

    cx = s * 0.5
    body_w = s * p["body_w"]
    body_h = s * p["body_h"]
    body_bottom = s * p["body_bottom"]
    body_top = body_bottom - body_h

    # --- kemer (U): dis daire cizilip ic daire ve alt yari silinir
    sw = s * p["shackle_w"]
    sh = s * p["shackle_h"]
    st = s * p["shackle_t"]
    arc_bottom = body_top + st * 0.5
    arc_top = arc_bottom - sh

    shackle = Image.new("RGBA", (s, s), CLEAR)
    sd = ImageDraw.Draw(shackle)
    sd.ellipse([cx - sw / 2.0, arc_top, cx + sw / 2.0, arc_top + sw], fill=WHITE)
    sd.ellipse([cx - sw / 2.0 + st, arc_top + st,
                cx + sw / 2.0 - st, arc_top + sw - st], fill=CLEAR)
    # Kemerin govdeye giren alt yarisi atilir: U kalir
    sd.rectangle([0, arc_bottom, s, s], fill=CLEAR)
    # Kemerin iki bacagi govdeye kadar uzatilir
    sd.rectangle([cx - sw / 2.0, arc_top + sw / 2.0, cx - sw / 2.0 + st, arc_bottom], fill=WHITE)
    sd.rectangle([cx + sw / 2.0 - st, arc_top + sw / 2.0, cx + sw / 2.0, arc_bottom], fill=WHITE)

    img = Image.alpha_composite(img, shackle)
    d = ImageDraw.Draw(img)

    # --- govde
    r = s * p["corner"]
    d.rounded_rectangle([cx - body_w / 2.0, body_top, cx + body_w / 2.0, body_bottom],
                        radius=r, fill=WHITE)

    # --- anahtar deligi (govdeden silinir)
    hr = s * p["hole_r"]
    hcy = s * p["hole_cy"]
    d.ellipse([cx - hr, hcy - hr, cx + hr, hcy + hr], fill=CLEAR)
    d.polygon([(cx - hr * 0.55, hcy), (cx + hr * 0.55, hcy),
               (cx + hr * 0.30, hcy + hr * 2.3), (cx - hr * 0.30, hcy + hr * 2.3)], fill=CLEAR)

    return img.resize((p["size"], p["size"]), Image.LANCZOS)


def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img = draw_lock(PARAMS)
    img.save(OUT)

    bbox = img.split()[3].getbbox()
    print("yazildi: %s" % OUT)
    print("boyut  : %dx%d" % img.size)
    print("opak   : x %d..%d  y %d..%d  (genislik %d, yukseklik %d)"
          % (bbox[0], bbox[2], bbox[1], bbox[3], bbox[2] - bbox[0], bbox[3] - bbox[1]))


if __name__ == "__main__":
    main()
