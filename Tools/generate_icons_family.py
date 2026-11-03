# -*- coding: utf-8 -*-
"""NEXUM simge ailesi — tek ureteç, Metal kiti dili.

NEDEN: geri tusu, duraklat (II), kapatma (X) ve ipucu birbirinden kopuktu;
bazilari Unity'nin yerlesik gorseliydi. Ayni cizgi kalinligi, ayni kose
geometrisi, ayni malzeme tek bir yerden gelsin.

TASARIM KURALI
- Kimya temasi SIMGENIN KENDISINDE degil CERCEVESINDE yasar: yuvarlak metal
  pano + ince olcu izgarasi. Glif sade ve okunakli kalir.
- Dort durum: normal / pressed / disabled / selected.
- Sabit kare, 9-slice DEGIL. Uc cozunurluk.
- Renk uydurulmaz: palet Assets/Data/Theme/NexumTheme.asset'ten okunur.

CIKTI
    Tools/ui-kit/icons/icon_<ad>_<durum>_<boy>.png
    manifest.json icindeki "simgeler" bolumu

Kullanim (proje kokunden):
    python Tools/generate_icons_family.py
"""
import json
import math
import os
import re

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Tools", "ui-kit", "icons")
THEME = os.path.join(ROOT, "Assets", "Data", "Theme", "NexumTheme.asset")
MANIFEST = os.path.join(ROOT, "Tools", "ui-kit", "manifest.json")

SS = 4                      # superornekleme
CLEAR = (0, 0, 0, 0)
SIZES = [96, 144, 192]      # dokunma hedefi 144; ustu ve alti da uretilir

# Ansiklopedi bilesik cemberi AYNI cerceveden cikar: ayni cizgi kalinligi,
# ayni metal, ayni olcu izgarasi — yalnizca glif yoktur. Kit klasorune
# yazilir, cunku kullanimi simge degil YUZEY (250 birim kart).
KIT = os.path.join(ROOT, "Tools", "ui-kit", "C")
CEMBER_BOY = 512


THEME_CS = os.path.join(ROOT, "Assets", "Scripts", "Theme", "NexumTheme.cs")


def load_palette():
    """Palet asset'ten okunur; asset'te OLMAYAN rol icin kod varsayilani.

    Bir rol koda sonradan eklendiginde (ornek: textMuted) Unity alani
    varsayilaniyla dolduruyor ama YAML'da satir OLUSMUYOR. Ureteç yalnizca
    YAML'a bakarsa o rol yokmus gibi davranir.
    """
    s = open(THEME, encoding="utf-8").read()
    pal = {}
    blok = s[s.find("palette:"):s.find("typography:")]
    for m in re.finditer(r"(\w+): \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+), a: ([\d.e-]+)\}", blok):
        pal[m.group(1)] = tuple(int(round(float(x) * 255)) for x in m.groups()[1:4])

    kod = open(THEME_CS, encoding="utf-8").read()
    for m in re.finditer(r'public Color (\w+) = Hex\("#([0-9A-Fa-f]{6})"\)', kod):
        ad, hx = m.group(1), m.group(2)
        if ad in pal:
            continue
        pal[ad] = tuple(int(hx[i:i + 2], 16) for i in (0, 2, 4))
        print("palet: %s asset'te yok, kod varsayilani #%s kullanildi" % (ad, hx.upper()))

    return pal


PAL = load_palette()


def mix(c1, c2, t):
    return tuple(int(round(a + (b - a) * t)) for a, b in zip(c1, c2))


# --------------------------------------------------------------- durumlar
# Tek yerden: cerceve parlakligi, glif rengi, hale.
STATES = {
    "normal":   dict(frame=1.00, glyph="textPrimary", glow=0.00, fill=0.00),
    "pressed":  dict(frame=1.18, glyph="textPrimary", glow=0.35, fill=0.14),
    "disabled": dict(frame=0.55, glyph="textMuted",   glow=0.00, fill=0.00),
    "selected": dict(frame=1.10, glyph="textPrimary", glow=0.55, fill=0.10),
}


def disk(size, inset=0):
    m = Image.new("L", (size, size), 0)
    ImageDraw.Draw(m).ellipse([inset, inset, size - 1 - inset, size - 1 - inset], fill=255)
    return m


def halka(size, kalinlik, inset=0):
    dis = disk(size, inset)
    ic = disk(size, inset + kalinlik)
    return Image.composite(Image.new("L", (size, size), 0), dis, ic)


def izgara(size, adim, kalinlik):
    """Cerceve icindeki ince olcu izgarasi — kimya/laboratuvar imzasi."""
    m = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(m)
    for x in range(0, size, adim):
        d.line([(x, 0), (x, size)], fill=255, width=kalinlik)
    for y in range(0, size, adim):
        d.line([(0, y), (size, y)], fill=255, width=kalinlik)
    return m


# ----------------------------------------------------------------- glifler
# Hepsi AYNI cizgi kalinligi (kal) ve ayni uc geometrisi ile cizilir.

def g_geri(d, S, kal, c):
    cx, cy = S * 0.54, S * 0.5
    r = S * 0.17
    d.line([(cx + r, cy), (cx - r, cy)], fill=c, width=kal)
    d.line([(cx - r, cy), (cx - r + r * 0.62, cy - r * 0.62)], fill=c, width=kal)
    d.line([(cx - r, cy), (cx - r + r * 0.62, cy + r * 0.62)], fill=c, width=kal)


def g_ileri(d, S, kal, c):
    cx, cy = S * 0.46, S * 0.5
    r = S * 0.17
    d.line([(cx - r, cy), (cx + r, cy)], fill=c, width=kal)
    d.line([(cx + r, cy), (cx + r - r * 0.62, cy - r * 0.62)], fill=c, width=kal)
    d.line([(cx + r, cy), (cx + r - r * 0.62, cy + r * 0.62)], fill=c, width=kal)


def g_duraklat(d, S, kal, c):
    cx, cy = S * 0.5, S * 0.5
    h = S * 0.16
    ara = S * 0.075
    d.line([(cx - ara, cy - h), (cx - ara, cy + h)], fill=c, width=kal)
    d.line([(cx + ara, cy - h), (cx + ara, cy + h)], fill=c, width=kal)


def g_kapat(d, S, kal, c):
    cx, cy = S * 0.5, S * 0.5
    r = S * 0.15
    d.line([(cx - r, cy - r), (cx + r, cy + r)], fill=c, width=kal)
    d.line([(cx - r, cy + r), (cx + r, cy - r)], fill=c, width=kal)


def g_onay(d, S, kal, c):
    cx, cy = S * 0.5, S * 0.52
    d.line([(cx - S * 0.16, cy), (cx - S * 0.04, cy + S * 0.12)], fill=c, width=kal)
    d.line([(cx - S * 0.04, cy + S * 0.12), (cx + S * 0.17, cy - S * 0.14)], fill=c, width=kal)


def g_ipucu(d, S, kal, c):
    """Ampul: kure + taban cizgileri. Kimya degil, evrensel 'ipucu'."""
    cx, cy = S * 0.5, S * 0.43
    r = S * 0.155
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=c, width=kal)
    # Ampulun boynu + iki tabani: cizgiler kurenin genisligini asmaz
    d.line([(cx - r * 0.45, cy + r * 0.92), (cx - r * 0.45, cy + r * 1.35)], fill=c, width=kal)
    d.line([(cx + r * 0.45, cy + r * 0.92), (cx + r * 0.45, cy + r * 1.35)], fill=c, width=kal)
    for i in range(2):
        y = cy + r * (1.55 + i * 0.52)
        d.line([(cx - r * 0.5, y), (cx + r * 0.5, y)], fill=c, width=kal)


def g_ses_acik(d, S, kal, c):
    cx, cy = S * 0.44, S * 0.5
    g = S * 0.09
    d.polygon([(cx - g, cy - g * 0.55), (cx - g * 0.25, cy - g * 0.55),
               (cx + g * 0.6, cy - g * 1.35), (cx + g * 0.6, cy + g * 1.35),
               (cx - g * 0.25, cy + g * 0.55), (cx - g, cy + g * 0.55)], outline=c, width=kal)
    for i, r in enumerate((S * 0.10, S * 0.155)):
        d.arc([cx + S * 0.07 - r, cy - r, cx + S * 0.07 + r, cy + r], -55, 55, fill=c, width=kal)


def g_ses_kapali(d, S, kal, c):
    cx, cy = S * 0.46, S * 0.5
    g = S * 0.09
    d.polygon([(cx - g, cy - g * 0.55), (cx - g * 0.25, cy - g * 0.55),
               (cx + g * 0.6, cy - g * 1.35), (cx + g * 0.6, cy + g * 1.35),
               (cx - g * 0.25, cy + g * 0.55), (cx - g, cy + g * 0.55)], outline=c, width=kal)
    x0, y0 = cx + S * 0.10, cy - S * 0.07
    d.line([(x0, y0), (x0 + S * 0.14, y0 + S * 0.14)], fill=c, width=kal)
    d.line([(x0, y0 + S * 0.14), (x0 + S * 0.14, y0)], fill=c, width=kal)


def g_ayarlar(d, S, kal, c):
    """Disli: kimya degil arayuz simgesi, cerceveyle temaya baglaniyor."""
    cx, cy = S * 0.5, S * 0.5
    r1, r2 = S * 0.095, S * 0.165
    d.ellipse([cx - r1, cy - r1, cx + r1, cy + r1], outline=c, width=kal)
    for i in range(8):
        a = math.radians(i * 45)
        d.line([(cx + math.cos(a) * r1 * 1.25, cy + math.sin(a) * r1 * 1.25),
                (cx + math.cos(a) * r2, cy + math.sin(a) * r2)], fill=c, width=kal)


def g_kisi(d, S, kal, c):
    cx = S * 0.5
    r = S * 0.072
    d.ellipse([cx - r, S * 0.36 - r, cx + r, S * 0.36 + r], outline=c, width=kal)
    d.arc([cx - S * 0.15, S * 0.47, cx + S * 0.15, S * 0.70], 180, 360, fill=c, width=kal)


def g_bilgi(d, S, kal, c):
    cx = S * 0.5
    d.ellipse([cx - kal * 0.7, S * 0.35, cx + kal * 0.7, S * 0.35 + kal * 1.4], fill=c)
    d.line([(cx, S * 0.44), (cx, S * 0.64)], fill=c, width=kal)


def g_kilit(d, S, kal, c):
    cx, cy = S * 0.5, S * 0.56
    w, h = S * 0.13, S * 0.10
    d.rectangle([cx - w, cy - h, cx + w, cy + h * 1.5], outline=c, width=kal)
    d.arc([cx - w * 0.68, cy - h * 2.6, cx + w * 0.68, cy + h * 0.2], 180, 360, fill=c, width=kal)


def g_yildiz_dolu(d, S, kal, c):
    d.polygon(_yildiz(S, 1.0), fill=c)


def g_yildiz_bos(d, S, kal, c):
    """Dolu yildiz eksi ic yildiz = temiz halka.

    PIL'in polygon(outline=...) secenegi yildiz gibi ickin kosesi olan bir
    yolda ic kenarlari da ciziyor ve glif bozuluyordu.
    """
    d.polygon(_yildiz(S, 1.0), fill=c)
    d.polygon(_yildiz(S, 1.0 - kal * 2.3 / (S * 0.175)), fill=(0, 0, 0, 0))


def _yildiz(S, olcek=1.0):
    cx, cy = S * 0.5, S * 0.52
    R, r = S * 0.175 * olcek, S * 0.088 * olcek
    p = []
    for i in range(10):
        a = math.radians(-90 + i * 36)
        rad = R if i % 2 == 0 else r
        p.append((cx + math.cos(a) * rad, cy + math.sin(a) * rad))
    return p


ICONS = {
    "back": g_geri,
    "forward": g_ileri,
    "pause": g_duraklat,
    "close": g_kapat,
    "confirm": g_onay,
    "hint": g_ipucu,
    "sound_on": g_ses_acik,
    "sound_off": g_ses_kapali,
    "settings": g_ayarlar,
    "person": g_kisi,
    "info": g_bilgi,
    "lock": g_kilit,
    "star_full": g_yildiz_dolu,
    "star_empty": g_yildiz_bos,
}


def icon(size, ad, durum):
    """Tek simge. Cerceve = yuvarlak metal pano + ince izgara."""
    st = STATES[durum]
    S = size * SS
    img = Image.new("RGBA", (S, S), CLEAR)

    kenar = max(2, int(S * 0.055))
    cerceve_renk = tuple(min(255, int(c * st["frame"])) for c in PAL["textSecondary"])
    zemin = mix(PAL["cardBackground"], (0, 0, 0), 0.15)

    # --- dis hale (durum)
    if st["glow"] > 0:
        g = disk(S, kenar // 2).filter(ImageFilter.GaussianBlur(int(S * 0.045)))
        kat = Image.new("RGBA", (S, S), PAL["primaryBright"] + (0,))
        kat.putalpha(g.point(lambda v: int(v * st["glow"])))
        img = Image.alpha_composite(img, kat)

    # --- pano zemini
    govde = disk(S, kenar)
    kat = Image.new("RGBA", (S, S), zemin + (0,))
    kat.putalpha(govde)
    img = Image.alpha_composite(img, kat)

    # --- OLCU IZGARASI: kimya/laboratuvar imzasi cercevede yasiyor
    izg = Image.composite(izgara(S, max(4, int(S * 0.085)), max(1, int(S * 0.008))),
                          Image.new("L", (S, S), 0), govde)
    kat = Image.new("RGBA", (S, S), PAL["primary"] + (0,))
    kat.putalpha(izg.point(lambda v: int(v * 0.22 * st["frame"])))
    img = Image.alpha_composite(img, kat)

    # --- durum dolgusu
    if st["fill"] > 0:
        kat = Image.new("RGBA", (S, S), PAL["primary"] + (0,))
        kat.putalpha(govde.point(lambda v: int(v * st["fill"])))
        img = Image.alpha_composite(img, kat)

    # --- metal halka: ust yarida acik, altta koyu (kit bezeliyle ayni dil)
    ring = halka(S, kenar, kenar // 2)
    kat = Image.new("RGBA", (S, S), cerceve_renk + (0,))
    kat.putalpha(ring)
    img = Image.alpha_composite(img, kat)

    yari = Image.new("L", (S, S), 0)
    ImageDraw.Draw(yari).rectangle([0, 0, S, S // 2], fill=255)
    kat = Image.new("RGBA", (S, S), mix(cerceve_renk, (255, 255, 255), 0.55) + (0,))
    kat.putalpha(Image.composite(ring, Image.new("L", (S, S), 0), yari).point(lambda v: int(v * 0.55)))
    img = Image.alpha_composite(img, kat)
    kat = Image.new("RGBA", (S, S), mix(cerceve_renk, (0, 0, 0), 0.45) + (0,))
    kat.putalpha(Image.composite(Image.new("L", (S, S), 0), ring, yari).point(lambda v: int(v * 0.5)))
    img = Image.alpha_composite(img, kat)

    # --- GLIF: hepsi ayni cizgi kalinligi
    kal = max(2, int(S * 0.052))
    glif = Image.new("RGBA", (S, S), CLEAR)
    ICONS[ad](ImageDraw.Draw(glif), S, kal, PAL[st["glyph"]] + (255,))
    img = Image.alpha_composite(img, glif)

    return img.resize((size, size), Image.LANCZOS)


def uret_cember():
    """Bilesik cemberi: simge cercevesinin glifsiz hali (ansiklopedi karti)."""
    os.makedirs(KIT, exist_ok=True)
    ICONS["_cember"] = lambda d, S, kal, c: None
    kayit = {}
    for durum in ("normal", "selected"):
        dosya = "compound_ring_%s.png" % durum
        icon(CEMBER_BOY, "_cember", durum).save(os.path.join(KIT, dosya))
        kayit[dosya] = {"tip": "sabit", "boyut": [CEMBER_BOY, CEMBER_BOY],
                        "simge": "cember", "durum": durum, "border": [0, 0, 0, 0]}
    del ICONS["_cember"]
    print("bilesik cemberi: %d sprite -> %s" % (len(kayit), KIT))
    return kayit


def uret():
    os.makedirs(OUT, exist_ok=True)
    kayit = {}

    for ad in ICONS:
        for durum in STATES:
            for boy in SIZES:
                dosya = "icon_%s_%s_%d.png" % (ad, durum, boy)
                icon(boy, ad, durum).save(os.path.join(OUT, dosya))
                kayit[dosya] = {"tip": "sabit", "boyut": [boy, boy],
                                "simge": ad, "durum": durum, "border": [0, 0, 0, 0]}

    print("simgeler: %d sprite -> %s" % (len(kayit), OUT))

    kayit.update(uret_cember())

    if os.path.exists(MANIFEST):
        man = json.load(open(MANIFEST, encoding="utf-8"))
    else:
        man = {}
    man["simgeler"] = {
        "aciklama": "Tek ureteçten simge ailesi. Kimya temasi glifte DEGIL "
                    "cercevede: yuvarlak metal pano + ince olcu izgarasi. "
                    "Ayni cizgi kalinligi, ayni kose geometrisi, ayni malzeme. "
                    "Sabit kare, 9-slice degil.",
        "durumlar": list(STATES.keys()),
        "cozunurlukler": SIZES,
        "sprites": kayit,
    }
    with open(MANIFEST, "w", encoding="utf-8") as f:
        json.dump(man, f, ensure_ascii=False, indent=2)
    print("manifest: %s" % MANIFEST)


uret()
