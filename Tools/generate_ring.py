# -*- coding: utf-8 -*-
"""NEXUM ilerleme halkasi — tarif defteri sayaci icin.

Neden uretiliyor: elde yalnizca DOLU daire (`Cember.png`) vardi. Dolu dairenin
uzerine Image.Filled/Radial360 uygulaninca halka degil PASTA DILIMI ciziliyor;
ekranda rastgele bir mavi leke gibi duruyordu. Dogru gorsel ortasi delik bir
halka (annulus).

Tasarim kurallari (ok ve kilit ikonuyla ayni yontem):
- TEK RENK BEYAZ (alfa ile sekil): rengi NexumTheme uzerinden Image tonlar.
  Ayni gorsel hem dolu kisim hem de soluk "track" halkasi olarak kullanilir.
- Kalinlik yarikapa oranli; 4x superornekleme + LANCZOS ile kenar yumusak.
- Kare tuval: Radial360 dolgusunun merkezi tam ortada olmali.

Cikti: Assets/Art/Figures/ring_progress.png

Kullanim (proje kokunden):
    python Tools/generate_ring.py
"""
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Art", "Figures", "ring_progress.png")

PARAMS = {
    "size": 256,        # cikti kenari (piksel)
    "ss": 4,            # superornekleme carpani
    "outer": 0.47,      # dis yarikap (kenar orani) — kenarda pay birakir
    "thickness": 0.105, # halka kalinligi (kenar orani)
}

WHITE = (255, 255, 255, 255)
CLEAR = (255, 255, 255, 0)


def draw_ring(p):
    s = p["size"] * p["ss"]
    img = Image.new("RGBA", (s, s), CLEAR)
    d = ImageDraw.Draw(img)

    c = s / 2.0
    outer = s * p["outer"]
    inner = outer - s * p["thickness"]

    d.ellipse([c - outer, c - outer, c + outer, c + outer], fill=WHITE)
    d.ellipse([c - inner, c - inner, c + inner, c + inner], fill=CLEAR)

    return img.resize((p["size"], p["size"]), Image.LANCZOS)


def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img = draw_ring(PARAMS)
    img.save(OUT)

    bbox = img.split()[3].getbbox()
    px = img.load()
    merkez_alfa = px[img.size[0] // 2, img.size[1] // 2][3]

    print("yazildi: %s" % OUT)
    print("boyut  : %dx%d" % img.size)
    print("opak   : x %d..%d  y %d..%d" % (bbox[0], bbox[2], bbox[1], bbox[3]))
    print("merkez alfa: %d  (0 olmali: halka, dilim degil)" % merkez_alfa)


if __name__ == "__main__":
    main()
