# -*- coding: utf-8 -*-
"""NEXUM — arka plan C (hibrit) ve Metal logo uretimi.

ARKA PLAN C: fotografin derinligi KORUNUR, uzerine prosedurel cihaz paneli
seridi ve olcu izgarasi bindirilir. Secenek A (doygunluk dusurme) fotografin
karakterini zayiflatiyordu, B (tamamen prosedurel) elle cizilmis laboratuvar
kimligini tamamen atiyordu; olculen kontrastlarla birlikte C secildi.

Uretim sahneye yeni OBJE eklemeden uygulanabilsin diye katmanlar dogrudan
goruntuye PISIRILIR; sahnede yalnizca sprite referansi degisir.

LOGO: GEOMETRI DEGISMEZ (element kutusu, N, 14.007, kose centigi, EXUM);
yalnizca malzeme Metal kitine tasinir. Varyant B (vurgulu) temel alinmis,
kutunun ic parlakligi mevcut logoya yaklastirilmistir.

Cikti:
    Assets/Art/Figures/background_c_main.png      (ana menu / oyun / defter)
    Assets/Art/Figures/background_c_level.png     (bolum haritasi)
    Assets/Art/Figures/background_c_register.png  (kayit)
    Assets/Art/Figures/NEXUM_logo_metal.png       (genis marka)
    Assets/Art/Figures/NEXUM_mark_metal.png       (kare isaret)

Kullanim (proje kokunden):
    python Tools/generate_background_logo.py
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from generate_ui_kit import PAL, ROOT, mix  # noqa: E402

FIG = os.path.join(ROOT, "Assets", "Art", "Figures")
FONTS = os.path.join(ROOT, "Assets", "Fonts")
CLEAR = (0, 0, 0, 0)

# Hedef cozunurluk: kaynak fotograflar 941x1672 (oran 0.5628); 1080x1920
# orani 0.5625 — pratikte ayni, AspectRatioFitter Envelope ile kirpiliyor.
W, H = 1080, 1920


# ------------------------------------------------------------- arka plan

def _izgara(w, h, adim=72, alfa=18):
    kat = Image.new("RGBA", (w, h), CLEAR)
    d = ImageDraw.Draw(kat)
    c = PAL["primary"]
    for x in range(0, w, adim):
        d.line([(x, 0), (x, h)], fill=c + (alfa,), width=1)
    for y in range(0, h, adim):
        d.line([(0, y), (w, y)], fill=c + (alfa,), width=1)
    for x in range(0, w, adim * 5):
        d.line([(x, 0), (x, h)], fill=c + (alfa * 2,), width=2)
    for y in range(0, h, adim * 5):
        d.line([(0, y), (w, y)], fill=c + (alfa * 2,), width=2)
    return kat


def _vinyet(w, h, guc=0.70):
    kat = Image.new("L", (w, h), 0)
    ImageDraw.Draw(kat).ellipse([-w * 0.35, -h * 0.18, w * 1.35, h * 1.18], fill=255)
    kat = kat.filter(ImageFilter.GaussianBlur(min(w, h) * 0.10))
    kat = ImageOps.invert(kat).point(lambda v: int(v * guc))
    kara = Image.new("RGBA", (w, h), (2, 8, 14, 0))
    kara.putalpha(kat)
    return kara


def arkaplan_c(kaynak):
    foto = Image.open(os.path.join(FIG, kaynak)).convert("RGBA")
    o = max(W / foto.width, H / foto.height)
    foto = foto.resize((int(foto.width * o) + 1, int(foto.height * o) + 1), Image.LANCZOS)
    x = (foto.width - W) // 2
    img = foto.crop((x, 0, x + W, H))

    img = Image.alpha_composite(img, Image.new("RGBA", (W, H), PAL["cardBackground"] + (120,)))
    img = Image.alpha_composite(img, _izgara(W, H))
    img = Image.alpha_composite(img, _vinyet(W, H))

    # cihaz paneli seritleri: ust ve alt
    kat = Image.new("RGBA", (W, H), CLEAR)
    d = ImageDraw.Draw(kat)
    serit = 74
    zemin = mix(PAL["panelBackground"], (0, 0, 0), 0.25) + (205,)
    d.rectangle([0, 0, W, serit], fill=zemin)
    d.rectangle([0, H - serit, W, H], fill=zemin)
    d.line([(0, serit), (W, serit)], fill=PAL["primary"] + (150,), width=3)
    d.line([(0, H - serit), (W, H - serit)], fill=PAL["primary"] + (150,), width=3)
    for x in range(40, W - 20, 48):
        d.line([(x, serit - 16), (x, serit - 6)], fill=PAL["primaryBright"] + (90,), width=2)
        d.line([(x, H - serit + 6), (x, H - serit + 16)], fill=PAL["primaryBright"] + (90,), width=2)

    return Image.alpha_composite(img, kat)


# ------------------------------------------------------------------ logo

def _font(ad, punto):
    return ImageFont.truetype(os.path.join(FONTS, ad), punto)


def _kutu(boy, ic_parlaklik):
    """Element kutusu: kazinmis metal plaka + bezel + kose centigi + N + 14.007."""
    SS = 3
    S = boy * SS
    img = Image.new("RGBA", (S, S), CLEAR)
    d = ImageDraw.Draw(img)
    r = int(S * 0.17)

    # plaka: ic parlaklik mevcut logoya yaklastirildi (duz koyu mavi zemin)
    ust = mix(PAL["cardBackground"], (255, 255, 255), ic_parlaklik + 0.10)
    alt = mix(PAL["cardBackground"], (0, 0, 0), 0.18)
    plaka = Image.new("RGBA", (S, S), CLEAR)
    pd = ImageDraw.Draw(plaka)
    for y in range(S):
        pd.line([(0, y), (S, y)], fill=mix(ust, alt, y / max(1, S - 1)) + (255,))
    maske = Image.new("L", (S, S), 0)
    ImageDraw.Draw(maske).rounded_rectangle([0, 0, S - 1, S - 1], radius=r, fill=255)
    plaka.putalpha(maske)
    img.alpha_composite(plaka)

    bw = max(3, int(S * 0.050))
    d.rounded_rectangle([0, 0, S - 1, S - 1], radius=r, outline=PAL["primary"] + (255,), width=bw)

    # Ust kenar vurgusu: elips YAYI DEGIL, yuvarlatilmis dikdortgenin UST
    # YARISI. Yay kullanilinca plakanin uzerine kabarcik gibi bir yarim daire
    # biniyordu.
    vurgu = Image.new("RGBA", (S, S), CLEAR)
    ImageDraw.Draw(vurgu).rounded_rectangle(
        [bw // 2, bw // 2, S - 1 - bw // 2, S - 1 - bw // 2], radius=max(1, r - bw // 2),
        outline=mix(PAL["primaryBright"], (255, 255, 255), 0.5) + (190,),
        width=max(2, bw // 2))
    ust_yari = Image.new("L", (S, S), 0)
    ImageDraw.Draw(ust_yari).rectangle([0, 0, S, int(S * 0.42)], fill=255)
    vurgu.putalpha(Image.composite(vurgu.split()[3], Image.new("L", (S, S), 0), ust_yari))
    img.alpha_composite(vurgu)
    d = ImageDraw.Draw(img)

    uz = int(S * 0.17)
    kal = max(2, bw // 2)
    cc = PAL["primaryBright"] + (225,)
    d.line([(bw * 2, bw * 2 + uz), (bw * 2, bw * 2)], fill=cc, width=kal)
    d.line([(bw * 2, bw * 2), (bw * 2 + uz, bw * 2)], fill=cc, width=kal)

    fN = _font("Oxanium-SemiBold.ttf", int(S * 0.60))
    k = d.textbbox((0, 0), "N", font=fN)
    nx = (S - (k[2] - k[0])) / 2 - k[0]
    ny = (S - (k[3] - k[1])) / 2 - k[1] - S * 0.05
    d.text((nx, ny + bw * 0.5), "N", font=fN, fill=(0, 0, 0, 150))
    d.text((nx, ny), "N", font=fN, fill=PAL["textPrimary"] + (255,))

    fA = _font("Oxanium-Medium.ttf", int(S * 0.145))
    k = d.textbbox((0, 0), "14.007", font=fA)
    d.text(((S - (k[2] - k[0])) / 2 - k[0], S * 0.755), "14.007", font=fA,
           fill=PAL["primaryBright"] + (235,))

    return img.resize((boy, boy), Image.LANCZOS)


def logo_genis(w=1366, h=320, ic_parlaklik=0.16):
    img = Image.new("RGBA", (w, h), CLEAR)
    kutu = int(h * 0.92)
    img.alpha_composite(_kutu(kutu, ic_parlaklik), (int(h * 0.06), int(h * 0.04)))

    d = ImageDraw.Draw(img)
    fE = _font("Oxanium-SemiBold.ttf", int(h * 0.70))
    ex = int(h * 0.06) + kutu + int(h * 0.10)
    k = d.textbbox((0, 0), "EXUM", font=fE)
    ey = int(h * 0.04) + (kutu - (k[3] - k[1])) / 2 - k[1]
    d.text((ex, ey + h * 0.012), "EXUM", font=fE, fill=(0, 0, 0, 140))
    d.text((ex, ey), "EXUM", font=fE, fill=PAL["textPrimary"] + (255,))
    return img


def logo_kare(boy=512, ic_parlaklik=0.16):
    img = Image.new("RGBA", (boy, boy), CLEAR)
    img.alpha_composite(_kutu(int(boy * 0.94), ic_parlaklik),
                        (int(boy * 0.03), int(boy * 0.03)))
    return img


# ---------------------------------------------------------------- olcum

def _lum(c):
    def f(v):
        v /= 255.0
        return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    return 0.2126 * f(c[0]) + 0.7152 * f(c[1]) + 0.0722 * f(c[2])


def _kontrast(a, b):
    la, lb = _lum(a), _lum(b)
    if la < lb:
        la, lb = lb, la
    return (la + 0.05) / (lb + 0.05)


def okunabilirlik(img, boy):
    """Kucultulmus isarette N glifi hala ayirt ediliyor mu?

    Olcum: merkez bolgedeki en acik piksel (N) ile cevresindeki plaka
    ortalamasi arasindaki WCAG kontrasti ve N'nin kapladigi alan orani.
    """
    k = img.resize((boy, boy), Image.LANCZOS).convert("RGB")
    px = k.load()
    x0, x1 = int(boy * 0.28), int(boy * 0.72)
    y0, y1 = int(boy * 0.20), int(boy * 0.62)

    parlak = []
    zemin = []
    for y in range(y0, y1):
        for x in range(x0, x1):
            parlak.append(px[x, y])
    parlak.sort(key=_lum)
    n_pikselleri = parlak[-max(1, len(parlak) // 6):]      # en acik %16
    plaka = parlak[:max(1, len(parlak) // 3)]              # en koyu %33

    def ort(liste):
        return tuple(sum(c[i] for c in liste) // len(liste) for i in range(3))

    n_renk, plaka_renk = ort(n_pikselleri), ort(plaka)
    esik = (_lum(n_renk) + _lum(plaka_renk)) / 2
    alan = sum(1 for c in parlak if _lum(c) > esik) / len(parlak)

    return {
        "boy": boy,
        "kontrast": _kontrast(n_renk, plaka_renk),
        "alan": alan,
        "n": n_renk,
        "plaka": plaka_renk,
    }


def main():
    kaynaklar = [
        ("arkaplan.png", "background_c_main.png"),
        ("level_selection_arkaplan.png", "background_c_level.png"),
        ("Registration_arkaplan.png", "background_c_register.png"),
    ]
    for kaynak, hedef in kaynaklar:
        img = arkaplan_c(kaynak)
        yol = os.path.join(FIG, hedef)
        img.convert("RGB").save(yol, optimize=True)
        print("%-28s <- %-32s %dx%d" % (hedef, kaynak, img.width, img.height))

    genis = logo_genis()
    genis.save(os.path.join(FIG, "NEXUM_logo_metal.png"))
    print("NEXUM_logo_metal.png        %dx%d" % genis.size)

    kare = logo_kare()
    kare.save(os.path.join(FIG, "NEXUM_mark_metal.png"))
    print("NEXUM_mark_metal.png        %dx%d" % kare.size)

    print()
    print("LOGO OKUNABILIRLIK (kare isaret, WCAG kontrasti N / plaka)")
    mevcut = Image.open(os.path.join(FIG, "NEXUM_logo-.png")).convert("RGBA")
    for boy in (96, 48):
        y = okunabilirlik(kare, boy)
        m = okunabilirlik(mevcut, boy)
        print("  %3d px  YENI kontrast %5.2f:1  alan %%%2.0f   |   MEVCUT %5.2f:1  alan %%%2.0f"
              % (boy, y["kontrast"], y["alan"] * 100, m["kontrast"], m["alan"] * 100))


if __name__ == "__main__":
    main()
