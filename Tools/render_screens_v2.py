# -*- coding: utf-8 -*-
"""NEXUM — 2d-2b kanitlari: tas ailesi v2, arka plan secenekleri, logo,
ipucu butonu, kayit ekrani ve hub mimarisi menu.

Her cerceve kitten TEK sprite alinip 9-slice ile cizilir; her yazi ayri
katman (Unity'de TMP etiketi). Alt simge <sub> gorunumu, ok ise gercek
sprite ile ciziliyor; font gliflerine guvenilmiyor.

Kullanim (proje kokunden):
    python Tools/render_screens_v2.py
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from generate_ui_kit import OUT, PAL, ROOT, TILE_LEVELS, mix  # noqa: E402
from render_ui_kit_proof import DOCS, font, nine_slice, sprite_border  # noqa: E402
from render_screens import TILES, formul, yaz  # noqa: E402

VK = "C"
W, H = 1080, 1920
CLEAR = (0, 0, 0, 0)
FIG = os.path.join(ROOT, "Assets", "Art", "Figures")


# ================================================================ arka plan

def _foto(w, h, dosya="arkaplan.png"):
    bg = Image.open(os.path.join(FIG, dosya)).convert("RGBA")
    o = max(w / bg.width, h / bg.height)
    bg = bg.resize((int(bg.width * o) + 1, int(bg.height * o) + 1), Image.LANCZOS)
    x = (bg.width - w) // 2
    return bg.crop((x, 0, x + w, h))


def _izgara(w, h, adim=60, renk=None, alfa=26):
    """Ince olcu izgarasi: cihaz paneli dili."""
    kat = Image.new("RGBA", (w, h), CLEAR)
    d = ImageDraw.Draw(kat)
    c = (renk or PAL["primary"]) + (alfa,)
    for x in range(0, w, adim):
        d.line([(x, 0), (x, h)], fill=c, width=1)
    for y in range(0, h, adim):
        d.line([(0, y), (w, y)], fill=c, width=1)
    # her 5. cizgi daha belirgin: olcu isareti
    c2 = (renk or PAL["primary"]) + (alfa * 2,)
    for x in range(0, w, adim * 5):
        d.line([(x, 0), (x, h)], fill=c2, width=2)
    for y in range(0, h, adim * 5):
        d.line([(0, y), (w, y)], fill=c2, width=2)
    return kat


def _vinyet(w, h, guc=0.75):
    kat = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(kat)
    d.ellipse([-w * 0.35, -h * 0.18, w * 1.35, h * 1.18], fill=255)
    kat = kat.filter(ImageFilter.GaussianBlur(min(w, h) * 0.10))
    kat = ImageOps.invert(kat).point(lambda v: int(v * guc))
    kara = Image.new("RGBA", (w, h), (2, 8, 14, 0))
    kara.putalpha(kat)
    return kara


def _pano(w, h, kenar=26):
    """Prosedurel metal pano: plaka gradyani + kenar cercevesi + perc."""
    kat = Image.new("RGBA", (w, h), CLEAR)
    d = ImageDraw.Draw(kat)
    ust = mix(PAL["panelBackground"], (255, 255, 255), 0.10)
    alt = mix(PAL["cardBackground"], (0, 0, 0), 0.25)
    for y in range(h):
        d.line([(0, y), (w, y)], fill=mix(ust, alt, y / max(1, h - 1)) + (255,))

    # kenar cercevesi
    c = PAL["primary"]
    d.rectangle([kenar, kenar, w - kenar, h - kenar],
                outline=mix(c, (0, 0, 0), 0.35) + (170,), width=3)
    d.rectangle([kenar + 10, kenar + 10, w - kenar - 10, h - kenar - 10],
                outline=mix(c, (255, 255, 255), 0.25) + (70,), width=1)
    # perc izleri
    for x in (kenar + 26, w - kenar - 26):
        for y in (kenar + 26, h - kenar - 26):
            d.ellipse([x - 7, y - 7, x + 7, y + 7], fill=mix(c, (255, 255, 255), 0.4) + (120,))
    return kat


def arkaplan_A(w=W, h=H):
    """A — mevcut foto + guclu kit islemesi (karartma, doygunluk dusurme,
    kenarda metal pano cercevesi)."""
    bg = _foto(w, h)
    gri = ImageOps.grayscale(bg.convert("RGB")).convert("RGBA")
    bg = Image.blend(bg, gri, 0.45)                       # doygunluk dusur
    bg = Image.alpha_composite(bg, Image.new("RGBA", (w, h), PAL["cardBackground"] + (155,)))
    bg = Image.alpha_composite(bg, _vinyet(w, h, 0.55))

    cerc = Image.new("RGBA", (w, h), CLEAR)
    d = ImageDraw.Draw(cerc)
    d.rectangle([18, 18, w - 18, h - 18], outline=PAL["primary"] + (90,), width=4)
    d.rectangle([30, 30, w - 30, h - 30], outline=PAL["primaryBright"] + (45,), width=1)
    return Image.alpha_composite(bg, cerc)


def arkaplan_B(w=W, h=H):
    """B — tamamen prosedurel cihaz paneli."""
    bg = _pano(w, h)
    bg = Image.alpha_composite(bg, _izgara(w, h, 60, PAL["primary"], 22))
    return Image.alpha_composite(bg, _vinyet(w, h, 0.65))


def arkaplan_C(w=W, h=H):
    """C — hibrit: fotografin derinligi + prosedurel panel cercevesi ve izgara."""
    bg = _foto(w, h)
    bg = Image.alpha_composite(bg, Image.new("RGBA", (w, h), PAL["cardBackground"] + (120,)))
    bg = Image.alpha_composite(bg, _izgara(w, h, 72, PAL["primary"], 18))
    bg = Image.alpha_composite(bg, _vinyet(w, h, 0.70))

    cerc = Image.new("RGBA", (w, h), CLEAR)
    d = ImageDraw.Draw(cerc)
    serit = 74
    d.rectangle([0, 0, w, serit], fill=mix(PAL["panelBackground"], (0, 0, 0), 0.25) + (205,))
    d.rectangle([0, h - serit, w, h], fill=mix(PAL["panelBackground"], (0, 0, 0), 0.25) + (205,))
    d.line([(0, serit), (w, serit)], fill=PAL["primary"] + (150,), width=3)
    d.line([(0, h - serit), (w, h - serit)], fill=PAL["primary"] + (150,), width=3)
    for x in range(40, w - 20, 48):
        d.line([(x, serit - 16), (x, serit - 6)], fill=PAL["primaryBright"] + (90,), width=2)
    return Image.alpha_composite(bg, cerc)


ARKAPLANLAR = {"A": arkaplan_A, "B": arkaplan_B, "C": arkaplan_C}
SECILEN_ARKAPLAN = "C"


def zemin(w=W, h=H, secim=None):
    return ARKAPLANLAR[secim or SECILEN_ARKAPLAN](w, h)


# ===================================================================== kit

def cerceve(s, bilesen, durum, x, y, w, h):
    dosya = "%s_%s.png" % (bilesen, durum)
    sp = Image.open(os.path.join(OUT, VK, dosya)).convert("RGBA")
    s.alpha_composite(nine_slice(sp, sprite_border(VK, dosya), int(w), int(h)), (int(x), int(y)))


def buton(s, d, bilesen, durum, x, y, w, h, etiket=None, f=None, renk=None):
    cerceve(s, bilesen, durum, x, y, w, h)
    if etiket:
        r = renk or (PAL["textPrimary"] if durum != "disabled" else PAL["textSecondary"])
        a = 255 if durum != "disabled" else 150
        k = d.textbbox((0, 0), etiket, font=f)
        yaz(d, (x + w / 2, y + (h - (k[3] - k[1])) / 2 - k[1]), etiket, f, r + (a,), True)


def tas(s, d, ad, durum, x, y, boy, f, f_alt):
    sembol, _ = TILES[ad]
    seviye = TAS_SEVIYE[ad]
    sp = Image.open(os.path.join(OUT, "tiles", "tile_L%d_%s_512.png" % (seviye, durum))).convert("RGBA")
    s.alpha_composite(sp.resize((boy, boy), Image.LANCZOS), (int(x), int(y)))
    kullanilabilir = {1: 0.74, 2: 0.72, 3: 0.70, 4: 0.64}[seviye]
    formul(s, d, x + boy / 2, y + boy / 2, sembol, f, f_alt,
           PAL["textPrimary"] + (255,), max_gen=boy * kullanilabilir)


# Turetilen seviye (TileLevels, esikler {0,1,2,3}) — 8 / 10 / 4 / 4
TAS_SEVIYE = {
    "H": 1, "O": 1, "C": 1, "N": 1, "Na": 1, "Cl": 1, "Fe": 1, "Ca": 1,
    "H2": 2, "O2": 2, "N2": 2, "Cl2": 2, "C2": 2, "Fe2": 2,
    "CO": 2, "CaO": 2, "NaCl": 2, "NH": 2,
    "H2O": 3, "CO2": 3, "NH3": 3, "O3": 3,
    "Fe2O3": 4, "CaCO3": 4, "H2CO3": 4, "NO": 4,
}


def ikon_butonu(s, d, durum, x, y, boy, isaret):
    sp = Image.open(os.path.join(OUT, VK, "icon_button_%s.png" % durum)).convert("RGBA")
    s.alpha_composite(sp.resize((boy, boy), Image.LANCZOS), (int(x), int(y)))
    cx, cy = x + boy / 2, y + boy / 2
    r = PAL["textPrimary"] + (255,)
    if isaret == "geri":
        q = boy * 0.17
        d.polygon([(cx + q * 0.6, cy - q), (cx + q * 0.6, cy + q), (cx - q * 0.8, cy)], fill=r)
    elif isaret == "duraklat":
        gw, gh, ara = boy * 0.08, boy * 0.30, boy * 0.09
        d.rectangle([cx - ara - gw, cy - gh / 2, cx - ara, cy + gh / 2], fill=r)
        d.rectangle([cx + ara, cy - gh / 2, cx + ara + gw, cy + gh / 2], fill=r)
    elif isaret == "ses":
        q = boy * 0.16
        d.polygon([(cx - q, cy - q * 0.5), (cx - q * 0.3, cy - q * 0.5), (cx + q * 0.3, cy - q),
                   (cx + q * 0.3, cy + q), (cx - q * 0.3, cy + q * 0.5), (cx - q, cy + q * 0.5)], fill=r)
    elif isaret == "ayar":
        q = boy * 0.20
        for a in range(0, 360, 45):
            rad = math.radians(a)
            d.line([(cx + math.cos(rad) * q * 0.5, cy + math.sin(rad) * q * 0.5),
                    (cx + math.cos(rad) * q, cy + math.sin(rad) * q)], fill=r, width=max(3, int(boy * 0.05)))
        d.ellipse([cx - q * 0.42, cy - q * 0.42, cx + q * 0.42, cy + q * 0.42],
                  outline=r, width=max(3, int(boy * 0.05)))
    elif isaret == "soru":
        yaz(d, (cx, cy - boy * 0.28), "?", font("Oxanium-SemiBold.ttf", int(boy * 0.52)), r, True)
    elif isaret == "mentor":
        q = boy * 0.17
        d.ellipse([cx - q * 0.55, cy - q, cx + q * 0.55, cy + q * 0.1], outline=r, width=max(3, int(boy * 0.05)))
        d.arc([cx - q, cy + q * 0.05, cx + q, cy + q * 1.6], 180, 360, fill=r, width=max(3, int(boy * 0.05)))


# =================================================================== logo

def metal_logo(w=760, style="olculu"):
    """NEXUM markasi, Metal kiti malzemesiyle. GEOMETRI DEGISMEZ:
    element kutusu + N + 14.007 + kose centigi + EXUM."""
    h = int(w * 320 / 1366)
    SSx = 3
    Wp, Hp = w * SSx, h * SSx
    img = Image.new("RGBA", (Wp, Hp), CLEAR)
    d = ImageDraw.Draw(img)

    kutu = int(Hp * 0.92)
    kx, ky = int(Hp * 0.06), int(Hp * 0.04)
    r = int(kutu * 0.17)
    vurgulu = style == "vurgulu"

    # --- plaka gradyani (kazinmis metal)
    plaka = Image.new("RGBA", (kutu, kutu), CLEAR)
    pd = ImageDraw.Draw(plaka)
    ust = mix(PAL["panelBackground"], (255, 255, 255), 0.22 if vurgulu else 0.12)
    alt = mix(PAL["cardBackground"], (0, 0, 0), 0.30)
    for y in range(kutu):
        pd.line([(0, y), (kutu, y)], fill=mix(ust, alt, y / max(1, kutu - 1)) + (255,))
    maske = Image.new("L", (kutu, kutu), 0)
    ImageDraw.Draw(maske).rounded_rectangle([0, 0, kutu - 1, kutu - 1], radius=r, fill=255)
    plaka.putalpha(maske)
    img.alpha_composite(plaka, (kx, ky))

    # --- kenarlik + bezel
    bw = max(3, int(kutu * (0.055 if vurgulu else 0.035)))
    d.rounded_rectangle([kx, ky, kx + kutu, ky + kutu], radius=r,
                        outline=PAL["primary"] + (255,), width=bw)
    d.arc([kx, ky, kx + kutu, ky + kutu], 180, 360,
          fill=mix(PAL["primaryBright"], (255, 255, 255), 0.5) + (190 if vurgulu else 110,),
          width=max(2, bw // 2))

    # --- kose centigi (mevcut logodaki isaret)
    uz = int(kutu * 0.17)
    kal = max(2, bw // 2)
    cc = PAL["primaryBright"] + (220,)
    d.line([(kx + bw * 2, ky + bw * 2 + uz), (kx + bw * 2, ky + bw * 2)], fill=cc, width=kal)
    d.line([(kx + bw * 2, ky + bw * 2), (kx + bw * 2 + uz, ky + bw * 2)], fill=cc, width=kal)

    # --- N (kazinmis: once acik golge, sonra ana harf)
    fN = ImageFont.truetype(os.path.join(ROOT, "Assets", "Fonts", "Oxanium-SemiBold.ttf"),
                            int(kutu * 0.62))
    k = d.textbbox((0, 0), "N", font=fN)
    nx = kx + (kutu - (k[2] - k[0])) / 2 - k[0]
    ny = ky + (kutu - (k[3] - k[1])) / 2 - k[1] - kutu * 0.05
    if vurgulu:
        d.text((nx, ny + bw * 0.6), "N", font=fN, fill=(0, 0, 0, 150))
        d.text((nx, ny), "N", font=fN, fill=PAL["textPrimary"] + (255,))
    else:
        d.text((nx, ny - bw * 0.5), "N", font=fN, fill=mix(PAL["textPrimary"], (255, 255, 255), 1.0) + (70,))
        d.text((nx, ny), "N", font=fN, fill=mix(PAL["secondary"], (255, 255, 255), 0.35) + (255,))

    # --- 14.007
    fA = ImageFont.truetype(os.path.join(ROOT, "Assets", "Fonts", "Oxanium-Medium.ttf"),
                            int(kutu * 0.15))
    yaz(d, (kx + kutu / 2, ky + kutu * 0.76), "14.007", fA,
        PAL["primaryBright"] + (235,), True)

    # --- EXUM
    fE = ImageFont.truetype(os.path.join(ROOT, "Assets", "Fonts", "Oxanium-SemiBold.ttf"),
                            int(Hp * 0.70))
    ex = kx + kutu + int(Hp * 0.10)
    k = d.textbbox((0, 0), "EXUM", font=fE)
    ey = ky + (kutu - (k[3] - k[1])) / 2 - k[1]
    if vurgulu:
        d.text((ex, ey + bw * 0.5), "EXUM", font=fE, fill=(0, 0, 0, 140))
        d.text((ex, ey), "EXUM", font=fE, fill=PAL["textPrimary"] + (255,))
    else:
        d.text((ex, ey), "EXUM", font=fE, fill=mix(PAL["secondary"], (255, 255, 255), 0.45) + (255,))

    return img.resize((w, h), Image.LANCZOS)


# ============================================================== bilesenler

def ipucu_butonu(s, d, x, y, w, h, durum, kalan, f, f_rozet):
    """IPUCU butonu + kalan hak rozeti. Faz 5'te kademeli ipucu gelecegi icin
    rozet kosede ayri bir katman: kademe gostergesi oraya eklenecek."""
    buton(s, d, "btn_secondary" if durum != "disabled" else "btn_ghost",
          durum, x, y, w, h, "İPUCU", f,
          renk=PAL["textPrimary"] if durum != "disabled" else PAL["textSecondary"])
    if durum == "disabled":
        return
    rw, rh = 76, 52
    rx, ry = x + w - rw + 10, y - 14
    cerceve(s, "chip", "selected", rx, ry, rw, rh)
    yaz(d, (rx + rw / 2, ry + 8), "×%d" % kalan, f_rozet, PAL["warning"] + (255,), True)


def tup_gostergesi(s, d, x, y, w, h, dolum, f):
    """Katalizor gostergesi: GERCEK tup gorseli (Entropitup) KORUNUR, dolum
    cubugu kitin progress sprite'larina tasinir ve tupun CAM ICINE oturur.

    Tup gorselinin ic cam alani olculdu: yatayda %12-%88, dikeyde %38-%62.
    """
    # Tup gorseli YATAYDA 3-SLICE edilir: altin kapaklar sabit kalir, ortadaki
    # cam govde gerilir. Boylece gercek gorsel korunur ama her genislige
    # oturur (bugun 922x533 bir gorsel 600x60 bir olcugun icine sikistiriliyor).
    tup = Image.open(os.path.join(FIG, "Entropitup.png")).convert("RGBA")
    th = int(h)
    taban_w = int(tup.width * th / tup.height)
    tup = tup.resize((taban_w, th), Image.LANCZOS)
    kapak = int(taban_w * 0.17)
    s.alpha_composite(nine_slice(tup, [kapak, 0, kapak, 0], int(w), th), (int(x), int(y)))

    ix = int(x + w * 0.11)
    iw = int(w * 0.78)
    ih = max(16, int(th * 0.34))
    iy = int(y + th * 0.50 - ih / 2)
    cerceve(s, "progress_track", "normal", ix, iy, iw, ih)
    if dolum > 0:
        cerceve(s, "progress_fill", "selected", ix, iy, max(8, int(iw * dolum)), ih)


def beher_cipi(s, d, x, y, ad, sayi, f, f_alt):
    """Hedef gostergesi: GERCEK beher gorseli (Beherglas) KORUNUR, sayac
    serbest etiket yerine kitin CIP sprite'ina tasinir.

    Bugunku BeakerGoalPrefab'ta etiket kabi 22 birim asiyordu; cip sabit
    kutulu oldugu icin tasma kaynagi ortadan kalkiyor.
    """
    beher = Image.open(os.path.join(FIG, "Beherglas.png")).convert("RGBA")
    bh = 132
    bw = int(beher.width * bh / beher.height)
    s.alpha_composite(beher.resize((bw, bh), Image.LANCZOS), (int(x), int(y)))

    # formul beherin ICINDE
    formul(s, d, x + bw / 2, y + bh * 0.62, TILES[ad][0], f_alt, f_alt,
           PAL["primaryBright"] + (255,), max_gen=bw * 0.72)

    # sayac cipi beherin SAGINDA
    cw, ch = 150, 64
    cerceve(s, "chip", "normal", x + bw + 8, y + bh * 0.33, cw, ch)
    yaz(d, (x + bw + 8 + cw / 2, y + bh * 0.33 + 14), sayi, f, PAL["textPrimary"] + (255,), True)


def karakter(s, x, y, boy):
    """Ipucu asistani: GERCEK gorsel korunur, dairesel kirpilip kitin avatar
    cercevesine oturtulur. Bugun oyun sahnesinde sag kenardan tasip
    kirpiliyordu; cerceve icinde tasma kaynagi kalmiyor."""
    kar = Image.open(os.path.join(FIG,
                     "vecteezy_detective-in-trench-coat-examining-clue-with-magnifying_65428683 (1).png"))
    kar = kar.convert("RGBA")

    # opak icerigi ortala ve kareye getir
    bb = kar.split()[3].getbbox() or (0, 0, kar.width, kar.height)
    kar = kar.crop(bb)
    kenar = max(kar.width, kar.height)
    kare = Image.new("RGBA", (kenar, kenar), CLEAR)
    kare.alpha_composite(kar, ((kenar - kar.width) // 2, (kenar - kar.height) // 2))
    kare = kare.resize((boy, boy), Image.LANCZOS)

    maske = Image.new("L", (boy, boy), 0)
    ImageDraw.Draw(maske).ellipse([boy * 0.08, boy * 0.08, boy * 0.92, boy * 0.92], fill=255)
    kare.putalpha(Image.composite(kare.split()[3], Image.new("L", (boy, boy), 0), maske))

    # Once cerceve (plakasi opak), sonra karakter: aksi halde cerceve
    # karakteri orter.
    cerc = Image.open(os.path.join(OUT, VK, "avatar_frame_normal.png")).convert("RGBA")
    s.alpha_composite(cerc.resize((boy, boy), Image.LANCZOS), (int(x), int(y)))
    s.alpha_composite(kare, (int(x), int(y)))


# ================================================================ ekranlar

def ekran_oyun_v2(secim=None):
    s = zemin(secim=secim)
    d = ImageDraw.Draw(s)
    f_t = font("Oxanium-SemiBold.ttf", 64)
    f_ta = font("Oxanium-SemiBold.ttf", 38)
    f_hud = font("Oxanium-Medium.ttf", 34)
    f_r = font("Oxanium-SemiBold.ttf", 26)

    s.alpha_composite(metal_logo(340, "olculu"), (370, 34))
    ikon_butonu(s, d, "normal", 930, 40, 110, "duraklat")

    beher_cipi(s, d, 40, 150, "H2O", "2/4", f_hud, font("Oxanium-Medium.ttf", 22))
    beher_cipi(s, d, 430, 150, "CO2", "1/3", f_hud, font("Oxanium-Medium.ttf", 22))
    cerceve(s, "chip", "normal", 820, 178, 220, 72)
    yaz(d, (930, 196), "12 480", f_hud, PAL["primaryBright"] + (255,), True)

    bw3, bh3, by = 300, 112, 310
    buton(s, d, "btn_ghost", "normal", 40, by, bw3, bh3, "GERİ AL", f_hud)
    buton(s, d, "btn_secondary", "normal", 390, by, bw3, bh3, "PARÇALA", f_hud)
    ipucu_butonu(s, d, 740, by, 300, bh3, "normal", 2, f_hud, f_r)

    tahta = ["H", "O", "*H2O", "C", "Na", "Cl", "O2", "+N",
             "Fe2O3", "CaO", "H", "O", "CO2", "N2", "Ca", "CaCO3"]
    x0, y0, hucre, bosluk = 70, 500, 222, 18
    tw = 4 * hucre + 3 * bosluk
    cerceve(s, "panel_lg", "normal", x0 - 26, y0 - 26, tw + 52, tw + 52)
    for i, ad in enumerate(tahta):
        durum = "normal"
        if ad.startswith("*"):
            ad, durum = ad[1:], "selected"
        elif ad.startswith("+"):
            ad, durum = ad[1:], "newborn"
        tas(s, d, ad, durum, x0 + (i % 4) * (hucre + bosluk),
            y0 + (i // 4) * (hucre + bosluk), hucre, f_t, f_ta)

    tup_gostergesi(s, d, 150, 1498, 780, 136, 0.6, f_hud)
    yaz(d, (W / 2, 1636), "BEDAVA PARÇALAMA ×1", f_hud, PAL["success"] + (255,), True)

    karakter(s, 55, 1700, 150)
    buton(s, d, "toast", "normal", 235, 1716, 795, 110, "YENİ TEPKİME KAYDEDİLDİ", f_hud,
          renk=PAL["primaryBright"])
    return s


def tas_sayfasi_v2():
    sutun = ["normal", "selected", "merging", "newborn", "discovered"]
    boy, pay = 190, 36
    GW = 300 + len(sutun) * (boy + pay) + pay
    GH = 210 + 4 * (boy + 76) + 300
    s = Image.new("RGBA", (GW, GH), PAL["cardBackground"] + (255,))
    d = ImageDraw.Draw(s)
    f_b = font("Oxanium-SemiBold.ttf", 38)
    f_e = font("Oxanium-Regular.ttf", 23)
    f_t = font("Oxanium-SemiBold.ttf", 48)
    f_ta = font("Oxanium-SemiBold.ttf", 29)

    yaz(d, (pay, 32), "NEXUM — taş ailesi v2 (iki katmanlı)", f_b, PAL["primaryBright"] + (255,))
    yaz(d, (pay, 84), "DIŞ: metal tutucu (kitin pahı, kenar vurgusu, kabartması) · "
                      "İÇ: ışıyan numune alanı — ışıma KENARDA, merkez koyu (formül okunsun)",
        f_e, PAL["textSecondary"] + (255,))
    yaz(d, (pay, 116), "Seviye üç sinyalle: ışıma yoğunluğu · köşe çentiği (0/1/2/3) · "
                       "çerçeve kalınlığı ve malzemesi (mat → fırçalanmış → parlak)",
        f_e, PAL["textSecondary"] + (255,))

    for c, durum in enumerate(sutun):
        yaz(d, (300 + c * (boy + pay) + boy / 2, 172), durum, f_e,
            PAL["textSecondary"] + (255,), True)

    ornek = {1: "O", 2: "CaO", 3: "H2O", 4: "Fe2O3"}
    malzeme = {1: "mat", 2: "fırçalanmış", 3: "fırçalanmış", 4: "parlak"}
    for r, sv in enumerate((1, 2, 3, 4)):
        y = 210 + r * (boy + 76)
        cfg = TILE_LEVELS[sv]
        yaz(d, (pay, y + boy / 2 - 46), "SEVİYE %d" % sv, f_b, PAL["textPrimary"] + (255,))
        yaz(d, (pay, y + boy / 2 + 4), "%d çentik · kenarlık %d · %s"
            % (cfg["notch"], cfg["border"], malzeme[sv]), f_e, PAL["textSecondary"] + (255,))
        for c, durum in enumerate(sutun):
            tas(s, d, ornek[sv], durum, 300 + c * (boy + pay), y, boy, f_t, f_ta)

    gy = 210 + 4 * (boy + 76) + 20
    d.line([(pay, gy), (GW - pay, gy)], fill=(90, 120, 150, 160), width=2)
    yaz(d, (pay, gy + 14),
        "GRİ TONLAMA AYRIM ŞERİDİ — renk kaldırıldığında da dört seviye ayırt edilir",
        f_b, PAL["warning"] + (255,))
    gb = 170
    for i, sv in enumerate((1, 2, 3, 4)):
        parca = Image.new("RGBA", (gb, gb), CLEAR)
        pd = ImageDraw.Draw(parca)
        tas(parca, pd, ornek[sv], "normal", 0, 0, gb,
            font("Oxanium-SemiBold.ttf", 42), font("Oxanium-SemiBold.ttf", 26))
        gri = ImageOps.grayscale(parca.convert("RGB")).convert("RGBA")
        gri.putalpha(parca.split()[3])
        s.alpha_composite(gri, (300 + i * (gb + pay), gy + 66))
        yaz(d, (300 + i * (gb + pay) + gb / 2, gy + 66 + gb + 10),
            "seviye %d · %d çentik" % (sv, TILE_LEVELS[sv]["notch"]), f_e,
            PAL["textSecondary"] + (255,), True)
    return s


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


def arkaplan_kontrasti(secim):
    bg = ARKAPLANLAR[secim](540, 960).convert("RGB")
    px = bg.load()
    tot = [0, 0, 0]
    n = 0
    for y in range(0, 960, 6):
        for x in range(0, 540, 6):
            r, g, b = px[x, y]
            tot[0] += r
            tot[1] += g
            tot[2] += b
            n += 1
    ort = tuple(v // n for v in tot)
    return {
        "renk": ort,
        "parlaklik": int(sum(ort) / 3),
        "tas": _kontrast(ort, PAL["secondary"]),
        "buton": _kontrast(ort, PAL["primary"]),
        "metin": _kontrast(ort, PAL["textPrimary"]),
    }


def arkaplan_sayfasi(secim):
    s = zemin(secim=secim)
    d = ImageDraw.Draw(s)
    f_e = font("Oxanium-Regular.ttf", 26)
    f_btn = font("Oxanium-SemiBold.ttf", 38)
    f_t = font("Oxanium-SemiBold.ttf", 52)
    f_ta = font("Oxanium-SemiBold.ttf", 31)

    aciklama = {
        "A": "A — mevcut fotoğraf + güçlü kit işlemesi (doygunluk −%45, karartma, metal çerçeve)",
        "B": "B — tamamen prosedürel: metal pano, ölçü ızgarası, perç, vinyet",
        "C": "C — hibrit: fotoğrafın derinliği + prosedürel panel şeridi ve ızgara",
    }[secim]

    s.alpha_composite(metal_logo(460, "olculu"), (310, 96))
    yaz(d, (W / 2, 250), aciklama, f_e, PAL["warning"] + (255,), True)

    for i, (et, bil) in enumerate([("DENEYİ BAŞLAT", "btn_primary"),
                                   ("ANSİKLOPEDİ", "btn_secondary"),
                                   ("AYARLAR", "btn_ghost")]):
        buton(s, d, bil, "normal", 150, 320 + i * 150, 780, 128, et, f_btn)

    yaz(d, (W / 2, 800), "oyun parçası — taşlar ve tahta bu zemin üzerinde", f_e,
        PAL["textSecondary"] + (255,), True)
    hucre, bosluk, x0, y0 = 200, 18, 130, 870
    tw = 4 * hucre + 3 * bosluk
    cerceve(s, "panel_lg", "normal", x0 - 24, y0 - 24, tw + 48, 2 * hucre + bosluk + 48)
    ornek = ["H", "CaO", "H2O", "Fe2O3", "O", "N2", "CO2", "CaCO3"]
    for i, ad in enumerate(ornek):
        tas(s, d, ad, "normal", x0 + (i % 4) * (hucre + bosluk),
            y0 + (i // 4) * (hucre + bosluk), hucre, f_t, f_ta)

    k = arkaplan_kontrasti(secim)
    yaz(d, (W / 2, 1340), "zemin ortalama parlaklık %d" % k["parlaklik"], f_e,
        PAL["primaryBright"] + (255,), True)
    yaz(d, (W / 2, 1380), "kontrast — taş çerçevesi %.2f:1 · buton kenarı %.2f:1 · beyaz metin %.2f:1"
        % (k["tas"], k["buton"], k["metin"]), f_e, PAL["primaryBright"] + (255,), True)

    buton(s, d, "toast", "normal", 90, 1450, 900, 110, "YENİ TEPKİME KAYDEDİLDİ",
          font("Oxanium-Medium.ttf", 34), renk=PAL["primaryBright"])
    cerceve(s, "list_row", "normal", 90, 1600, 900, 100)
    yaz(d, (W / 2, 1632), "liste satırı — okunurluk denetimi",
        font("Oxanium-Medium.ttf", 30), PAL["textPrimary"] + (255,), True)
    return s


def logo_sayfasi():
    GW, GH = 1280, 900
    s = Image.new("RGBA", (GW, GH), PAL["cardBackground"] + (255,))
    s = Image.alpha_composite(s, _izgara(GW, GH, 60, PAL["primary"], 16))
    d = ImageDraw.Draw(s)
    f_b = font("Oxanium-SemiBold.ttf", 36)
    f_e = font("Oxanium-Regular.ttf", 24)

    yaz(d, (50, 36), "NEXUM logosu — Metal kiti malzemesi", f_b, PAL["primaryBright"] + (255,))
    yaz(d, (50, 86), "GEOMETRİ DEĞİŞMEDİ: element kutusu, N, 14.007, köşe çentiği ve EXUM korundu. "
                     "Değişen yalnızca malzeme.", f_e, PAL["textSecondary"] + (255,))

    mevcut = Image.open(os.path.join(FIG, "NEXUM_logo.png")).convert("RGBA")
    gen = 700
    s.alpha_composite(mevcut.resize((gen, int(mevcut.height * gen / mevcut.width)), Image.LANCZOS),
                      (60, 170))
    yaz(d, (60, 320), "MEVCUT — düz dolgu, dış parıltı", f_e, PAL["textSecondary"] + (255,))

    s.alpha_composite(metal_logo(gen, "olculu"), (60, 410))
    yaz(d, (60, 560), "YENİ A — ölçülü: kazınmış plaka, ince kenarlık, mat N",
        f_e, PAL["secondary"] + (255,))

    s.alpha_composite(metal_logo(gen, "vurgulu"), (60, 650))
    yaz(d, (60, 800), "YENİ B — vurgulu: kalın bezel, kabartmalı N, güçlü üst parlama",
        f_e, PAL["warning"] + (255,))
    return s


def ekran_kayit():
    s = zemin()
    d = ImageDraw.Draw(s)
    f_b = font("Oxanium-SemiBold.ttf", 40)
    f_o = font("Oxanium-Medium.ttf", 34)
    f_e = font("Oxanium-Regular.ttf", 28)

    s.alpha_composite(metal_logo(520, "vurgulu"), (280, 90))
    yaz(d, (W / 2, 260), "LABORATUVAR KAYDI", f_b, PAL["primaryBright"] + (255,), True)

    cerceve(s, "panel_lg", "normal", 70, 340, 940, 1180)

    yaz(d, (120, 390), "KİMYAGER ADI", f_e, PAL["secondary"] + (255,))
    buton(s, d, "input_field", "normal", 120, 440, 840, 120, "Marie", f_o)

    # hata durumu
    yaz(d, (120, 590), "E-POSTA (isteğe bağlı)", f_e, PAL["secondary"] + (255,))
    buton(s, d, "input_field", "disabled", 120, 640, 840, 120, "marie@", f_o,
          renk=PAL["textSecondary"])
    cerceve(s, "toast", "normal", 120, 772, 840, 76)
    yaz(d, (W / 2, 792), "Kabul edilmeyen e-posta formatı", f_e, PAL["danger"] + (255,), True)

    yaz(d, (120, 880), "MENTOR SEÇ", f_e, PAL["secondary"] + (255,))
    for i in range(3):
        durum = "selected" if i == 1 else "normal"
        cerceve(s, "avatar_frame", durum, 140 + i * 280, 930, 220, 220)
        yaz(d, (250 + i * 280, 1160), ["Curie", "Mendeleev", "Sancar"][i], f_e,
            (PAL["primaryBright"] if i == 1 else PAL["textSecondary"]) + (255,), True)

    # yukleniyor durumu
    cerceve(s, "progress_track", "normal", 120, 1240, 840, 30)
    cerceve(s, "progress_fill", "selected", 120, 1240, 520, 30)
    yaz(d, (W / 2, 1288), "kimlik kartı hazırlanıyor…", f_e, PAL["textSecondary"] + (255,), True)

    buton(s, d, "btn_primary", "normal", 120, 1350, 840, 130, "DENEYE BAŞLA", f_b)
    buton(s, d, "btn_ghost", "normal", 120, 1560, 840, 110, "GÖZLEMCİ OLARAK DEVAM ET", f_o)
    return s



# ============================================================== ana menu hub

# Hub girisleri. "kilit" = henuz yapilmadi; "gizli" bayragi tek yerden
# kontrol edilir — yeni oyuncuya sekiz kilit gosterilmez, ama duzen
# bugunden oturur.
HUB = [
    # (anahtar, etiket, durum, yeni_oyuncuda_gorunur)
    ("gunluk",   "GÜNLÜK BULMACA",    "kilit",  False),
    ("bolum",    "BÖLÜM SEÇİMİ",      "acik",   True),
    ("sinav",    "SINAV SİMÜLASYONU", "kilit",  False),
    ("sonsuz",   "SONSUZ LAB",        "kilit",  False),
    ("haftanin", "HAFTANIN DENEYİ",   "kilit",  False),
    ("ansiklo",  "ANSİKLOPEDİ",       "acik",   True),
    ("defter",   "TARİF DEFTERİ",     "acik",   True),
]


def _avatar(s, x, y, boy, dosya):
    """Mentor portresi: gercek gorsel, dairesel kirpilip cerceveye oturur."""
    yol = os.path.join(ROOT, "Assets", "Art", "Avatars", dosya)
    im = Image.open(yol).convert("RGBA").resize((boy, boy), Image.LANCZOS)
    maske = Image.new("L", (boy, boy), 0)
    ImageDraw.Draw(maske).ellipse([boy * 0.10, boy * 0.10, boy * 0.90, boy * 0.90], fill=255)
    im.putalpha(Image.composite(im.split()[3], Image.new("L", (boy, boy), 0), maske))
    s.alpha_composite(im, (int(x), int(y)))


def _kilit_rozeti(s, d, x, y, f):
    kilit = Image.open(os.path.join(OUT, VK, "lock_overlay.png")).convert("RGBA")
    kilit = kilit.resize((34, 34), Image.LANCZOS)
    kat = Image.new("RGBA", kilit.size, PAL["textSecondary"] + (0,))
    kat.putalpha(kilit.split()[3])
    s.alpha_composite(kat, (int(x), int(y)))
    yaz(d, (x + 46, y + 2), "YAKINDA", f, PAL["textSecondary"] + (255,))


def ekran_menu_hub(ilerlemis):
    """ilerlemis=False -> yeni oyuncu (kilitliler GIZLI)
       ilerlemis=True  -> hepsi acik"""
    s = zemin()
    d = ImageDraw.Draw(s)
    f_h = font("Oxanium-SemiBold.ttf", 46)
    f_o = font("Oxanium-SemiBold.ttf", 34)
    f_e = font("Oxanium-Regular.ttf", 26)
    f_k = font("Oxanium-Regular.ttf", 22)

    # --- UST SERIT: profil karti (her zaman ilk ekranda)
    cerceve(s, "panel_sm", "normal", 40, 40, 1000, 200)
    cerceve(s, "avatar_frame", "normal", 60, 60, 160, 160)
    _avatar(s, 60, 60, 160, "Marie_Curie.png")
    yaz(d, (240, 70), "Baş Kimyager" if ilerlemis else "Yeni Kimyager", f_o,
        PAL["textPrimary"] + (255,))
    yaz(d, (240, 118), ("Rütbe 7 · Doçent" if ilerlemis else "Rütbe 1 · Stajyer"), f_e,
        PAL["secondary"] + (255,))
    cerceve(s, "progress_track", "normal", 240, 162, 540, 28)
    cerceve(s, "progress_fill", "selected", 240, 162, 410 if ilerlemis else 90, 28)
    yaz(d, (800, 158), "XP", f_k, PAL["textSecondary"] + (255,))
    if ilerlemis:
        cerceve(s, "chip", "selected", 830, 96, 190, 80)
        yaz(d, (925, 108), "12 GÜN SERİ", f_k, PAL["warning"] + (255,), True)
    else:
        cerceve(s, "chip", "normal", 830, 96, 190, 80)
        yaz(d, (925, 108), "1. GÜN", f_k, PAL["textSecondary"] + (255,), True)

    s.alpha_composite(metal_logo(360, "olculu"), (360, 262))

    # --- KAHRAMAN: en baskin
    hy = 400
    cerceve(s, "btn_primary", "selected", 40, hy, 1000, 300)
    yaz(d, (90, hy + 54), "DENEYİ BAŞLAT", f_h, PAL["textPrimary"] + (255,))
    yaz(d, (90, hy + 142), "Bölüm %d · kaldığın yerden" % (5 if ilerlemis else 1), f_e,
        PAL["secondary"] + (255,))
    for i in range(3):
        dolu = i < (2 if ilerlemis else 0)
        yildiz = Image.open(os.path.join(OUT, VK, "star_%s_normal.png" % ("full" if dolu else "empty")))
        s.alpha_composite(yildiz.convert("RGBA").resize((66, 66), Image.LANCZOS),
                          (838 + i * 70, hy + 120))

    # --- IKILI KART
    ky = hy + 350
    gorunur = [g for g in HUB[:2] if ilerlemis or g[3]]
    if len(gorunur) == 2:
        kartlar = [(0, HUB[0]), (1, HUB[1])]
        kw = 490
    else:
        kartlar = [(0, g) for g in gorunur]
        kw = 1000
    for i, (_, g) in enumerate(kartlar):
        anahtar, etiket, durum, _y = g
        acik = durum == "acik" or ilerlemis
        x = 40 + i * 510
        cerceve(s, "panel_md", "normal" if acik else "disabled", x, ky, kw, 250)
        yaz(d, (x + 32, ky + 34), etiket, f_o,
            (PAL["textPrimary"] if acik else PAL["textSecondary"]) + (255,))
        if anahtar == "gunluk" and acik:
            cerceve(s, "chip", "selected", x + 32, ky + 140, 190, 70)
            yaz(d, (x + 127, ky + 160), "SERİ 12", f_k, PAL["warning"] + (255,), True)
        elif anahtar == "bolum":
            yaz(d, (x + 32, ky + 150), "%d / 7 açık" % (5 if ilerlemis else 1), f_e,
                PAL["secondary"] + (255,))
        if not acik:
            _kilit_rozeti(s, d, x + 32, ky + 160, f_k)

    # --- MOD SATIRI: orta agirlik
    my = ky + 300
    modlar = [g for g in HUB[2:5] if ilerlemis or g[3]]
    if modlar:
        mw = (1000 - (len(modlar) - 1) * 20) // len(modlar)
        for i, (anahtar, etiket, durum, _y) in enumerate(modlar):
            acik = durum == "acik" or ilerlemis
            x = 40 + i * (mw + 20)
            cerceve(s, "tab", "normal" if acik else "disabled", x, my, mw, 190)
            for j, satir in enumerate(etiket.split(" ")):
                yaz(d, (x + mw / 2, my + 36 + j * 38), satir, f_k,
                    (PAL["textPrimary"] if acik else PAL["textSecondary"]) + (255,), True)
            if not acik:
                _kilit_rozeti(s, d, x + mw / 2 - 70, my + 134, f_k)
        my += 234

    # --- BILGI
    for i, (anahtar, etiket, durum, _y) in enumerate(HUB[5:]):
        x = 40 + i * 510
        cerceve(s, "btn_secondary", "normal", x, my, 490, 160)
        yaz(d, (x + 245, my + 58), etiket, f_o, PAL["textPrimary"] + (255,), True)
    my += 210

    # --- IKON SERIDI: en sakin
    for i, (isaret, etiket) in enumerate([("ayar", "Ayarlar"), ("soru", "Nasıl Oynanır"),
                                          ("mentor", "Mentor"), ("ses", "Ses")]):
        x = 88 + i * 236
        ikon_butonu(s, d, "normal", x, my, 134, isaret)
        yaz(d, (x + 67, my + 150), etiket, f_k, PAL["textSecondary"] + (255,), True)

    etiket = "YENİ OYUNCU — kilitli girişler gizli (tek bayrak)" if not ilerlemis \
        else "İLERLEMİŞ OYUNCU — tüm girişler açık"
    yaz(d, (W / 2, 1862), etiket, f_k, PAL["warning"] + (255,), True)
    return s


# ====================================================== yerlesim olcumu

HUD_YERLESIM = {
    # ad: (x, y, genislik, yukseklik)  — screen-game-v2 ile AYNI degerler
    "beher H2O":   (40, 150, 88 + 158, 132),
    "beher CO2":   (430, 150, 88 + 158, 132),
    "skor cipi":   (820, 178, 220, 72),
    "GERİ AL":     (40, 310, 300, 112),
    "PARÇALA":     (390, 310, 300, 112),
    "İPUCU":       (740, 310, 300, 112),
    "ipucu rozeti": (740 + 300 - 76 + 10, 310 - 14, 76, 52),
    "tahta":       (44, 474, 4 * 222 + 3 * 18 + 52, 4 * 222 + 3 * 18 + 52),
}


def yerlesim_raporu():
    """HUD ucgeninin (GERI AL / PARCALA / IPUCU) cakismasiz oldugunu ve
    dokunma hedefi kuralini (>= 144 birim) sagladigini olcer."""
    def kesisim(a, b):
        ax, ay, aw, ah = a
        bx, by, bw, bh = b
        w = min(ax + aw, bx + bw) - max(ax, bx)
        h = min(ay + ah, by + bh) - max(ay, by)
        return w * h if (w > 0 and h > 0) else 0

    adlar = list(HUD_YERLESIM)
    cakisma = []
    for i in range(len(adlar)):
        for j in range(i + 1, len(adlar)):
            a, b = adlar[i], adlar[j]
            if a == "İPUCU" and b == "ipucu rozeti":
                continue                      # rozet bilerek butonun uzerinde
            k = kesisim(HUD_YERLESIM[a], HUD_YERLESIM[b])
            if k > 0:
                cakisma.append("%s x %s = %d birim kare" % (a, b, k))

    print("HUD YERLESIM OLCUMU (1080x1920 referans)")
    for ad, (x, y, w, h) in HUD_YERLESIM.items():
        print("  %-14s x %4d..%4d  y %4d..%4d  (%dx%d)" % (ad, x, x + w, y, y + h, w, h))

    ux = [HUD_YERLESIM[k][0] for k in ("GERİ AL", "PARÇALA", "İPUCU")]
    gen = HUD_YERLESIM["GERİ AL"][2]
    print("  ucgen araligi: %d ve %d birim (esit: %s)"
          % (ux[1] - ux[0] - gen, ux[2] - ux[1] - gen, (ux[1] - ux[0]) == (ux[2] - ux[1])))
    print("  sag kenar payi: %d birim" % (1080 - (ux[2] + gen)))
    print("  tahtaya pay: %d birim" % (HUD_YERLESIM["tahta"][1] - (310 + 112)))
    print("  hedef cipine pay: %d birim" % (310 - (150 + 150)))

    # Dokunma hedefi: gorsel 112 birim, DOKUNMA KUTUSU 144 birime tamamlanir
    # (DESIGN_TOKENS 5.5). Genisletilmis kutular da cakismamali.
    DOKUNMA = 144
    genis = {}
    for ad in ("GERİ AL", "PARÇALA", "İPUCU"):
        x, y, w, h = HUD_YERLESIM[ad]
        d = max(0, DOKUNMA - h) / 2
        genis[ad] = (x, y - d, w, h + 2 * d)
        print("  %-9s dokunma kutusu y %.0f..%.0f (gorsel %d..%d)"
              % (ad, y - d, y + h + d, y, y + h))

    cip_alt = max(HUD_YERLESIM["beher H2O"][1] + HUD_YERLESIM["beher H2O"][3],
                  HUD_YERLESIM["skor cipi"][1] + HUD_YERLESIM["skor cipi"][3])
    ust = min(v[1] for v in genis.values())
    alt = max(v[1] + v[3] for v in genis.values())
    print("  dokunma kutusu -> hedef cipi payi: %.0f birim" % (ust - cip_alt))
    print("  dokunma kutusu -> tahta payi: %.0f birim" % (HUD_YERLESIM["tahta"][1] - alt))
    if ust <= cip_alt or alt >= HUD_YERLESIM["tahta"][1]:
        cakisma.append("dokunma kutusu komsusuna giriyor")

    print("  CAKISMA: %s" % (" | ".join(cakisma) or "YOK"))
    return cakisma


def main():
    os.makedirs(DOCS, exist_ok=True)
    cikti = [
        ("tiles-sheet-v2.png", tas_sayfasi_v2),
        ("screen-game-v2.png", lambda: ekran_oyun_v2()),
        ("background-A.png", lambda: arkaplan_sayfasi("A")),
        ("background-B.png", lambda: arkaplan_sayfasi("B")),
        ("background-C.png", lambda: arkaplan_sayfasi("C")),
        ("logo-variants.png", logo_sayfasi),
        ("screen-register.png", ekran_kayit),
        ("screen-menu-new.png", lambda: ekran_menu_hub(False)),
        ("screen-menu-full.png", lambda: ekran_menu_hub(True)),
    ]
    yerlesim_raporu()

    for ad, fn in cikti:
        img = fn()
        yol = os.path.join(DOCS, ad)
        img.convert("RGB").save(yol, optimize=True)
        print("%-26s %dx%d" % (ad, img.width, img.height))


if __name__ == "__main__":
    main()
