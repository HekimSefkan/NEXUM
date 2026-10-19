# -*- coding: utf-8 -*-
"""NEXUM — 1080x1920 cihaz boyutunda ekran kanitlari (docs/ui-kit/screen-*.png).

Metal kiti (varyant C) ve yeni taslarla gercek ekranlar. Her cerceve kitten
TEK sprite alinip 9-slice ile cizilir; her yazi ayri katman (Unity'de TMP
etiketi). Arka planlar oyunun GERCEK gorselleri.

Alt simge NOTU: literal ₂ ₃ karakterleri kullanilamaz (Oxanium'da ₅₆₇₈₉ yok,
statik atlaslarda hicbiri yok). Oyunda TMP <sub> etiketi kullaniliyor; bu
sayfada da ayni gorunum kucuk punto + alcaltilmis taban cizgisiyle uretiliyor.

Kullanim (proje kokunden):
    python Tools/render_screens.py
"""
import os
import re
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from generate_ui_kit import OUT, PAL, ROOT, TILE_LEVELS  # noqa: E402
from render_ui_kit_proof import DOCS, font, nine_slice, sprite_border  # noqa: E402

VK = "C"                      # secilen varyant: Metal
W, H = 1080, 1920             # tasarim alani
CLEAR = (0, 0, 0, 0)
FIG = os.path.join(ROOT, "Assets", "Art", "Figures")

# Gercek tas verileri: ad -> (sembol, seviye)  — seviye TileLevels turetimi
TILES = {
    "H": ("H", 1), "O": ("O", 1), "C": ("C", 1), "N": ("N", 1),
    "Na": ("Na", 1), "Cl": ("Cl", 1), "Fe": ("Fe", 1), "Ca": ("Ca", 1),
    "H2": ("H<sub>2</sub>", 2), "O2": ("O<sub>2</sub>", 2), "N2": ("N<sub>2</sub>", 2),
    "Cl2": ("Cl<sub>2</sub>", 2), "C2": ("C<sub>2</sub>", 2), "Fe2": ("Fe<sub>2</sub>", 2),
    "O3": ("O<sub>3</sub>", 2), "NH": ("NH", 2), "CO": ("CO", 2), "CaO": ("CaO", 2),
    "NaCl": ("NaCl", 2), "H2O": ("H<sub>2</sub>O", 2), "CO2": ("CO<sub>2</sub>", 2),
    "NH3": ("NH<sub>3</sub>", 2),
    "Fe2O3": ("Fe<sub>2</sub>O<sub>3</sub>", 3), "CaCO3": ("CaCO<sub>3</sub>", 3),
    "H2CO3": ("H<sub>2</sub>CO<sub>3</sub>", 3), "NO": ("NO", 3),
}


# ------------------------------------------------------------------ zemin

def arkaplan(dosya="arkaplan.png", karartma=0.45):
    bg = Image.open(os.path.join(FIG, dosya)).convert("RGBA")
    olcek = max(W / bg.width, H / bg.height)
    bg = bg.resize((int(bg.width * olcek) + 1, int(bg.height * olcek) + 1), Image.LANCZOS)
    x = (bg.width - W) // 2
    bg = bg.crop((x, 0, x + W, H))
    kat = Image.new("RGBA", (W, H), PAL["cardBackground"] + (int(255 * karartma),))
    return Image.alpha_composite(bg, kat)


# ------------------------------------------------------------------ metin

SUB_RE = re.compile(r"<sub>(.*?)</sub>")


def formul_genislik(d, metin, f, f_alt):
    gen = 0
    for parca, alt in _parcala(metin):
        ff = f_alt if alt else f
        k = d.textbbox((0, 0), parca, font=ff)
        gen += k[2] - k[0]
    return gen


def _parcala(metin):
    """"Fe<sub>2</sub>O<sub>3</sub>" -> [("Fe",False),("2",True),("O",False),("3",True)]"""
    out = []
    i = 0
    for m in SUB_RE.finditer(metin):
        if m.start() > i:
            out.append((metin[i:m.start()], False))
        out.append((m.group(1), True))
        i = m.end()
    if i < len(metin):
        out.append((metin[i:], False))
    return out


def formul(sayfa, d, cx, cy, metin, f, f_alt, renk, max_gen=None):
    """<sub> etiketli formulu ortalayarak cizer (TMP <sub> ile ayni gorunum).

    max_gen verilirse ve formul sigmiyorsa punto kucultulur — 2d-3'te tas
    prefab'larinda yapilacak duzeltmenin (punto 80 -> 74) sayfadaki karsiligi.
    """
    gen = formul_genislik(d, metin, f, f_alt)
    if max_gen and gen > max_gen:
        olcek = max_gen / gen
        f = ImageFont.truetype(f.path, max(10, int(f.size * olcek)))
        f_alt = ImageFont.truetype(f_alt.path, max(8, int(f_alt.size * olcek)))
        gen = formul_genislik(d, metin, f, f_alt)
    k = d.textbbox((0, 0), "H", font=f)
    x = cx - gen / 2
    y = cy - (k[3] - k[1]) / 2 - k[1]
    alcalt = (k[3] - k[1]) * 0.30
    for parca, alt in _parcala(metin):
        ff = f_alt if alt else f
        d.text((x, y + (alcalt if alt else 0)), parca, font=ff, fill=renk)
        kk = d.textbbox((0, 0), parca, font=ff)
        x += kk[2] - kk[0]


def yaz(d, xy, s, f, renk, ortala=False, sagda=False):
    if ortala or sagda:
        k = d.textbbox((0, 0), s, font=f)
        gen = k[2] - k[0]
        xy = (xy[0] - (gen / 2 if ortala else gen), xy[1])
    d.text(xy, s, font=f, fill=renk)


# ----------------------------------------------------------------- parcalar

def cerceve(sayfa, bilesen, durum, x, y, w, h):
    dosya = "%s_%s.png" % (bilesen, durum)
    sp = Image.open(os.path.join(OUT, VK, dosya)).convert("RGBA")
    sayfa.alpha_composite(nine_slice(sp, sprite_border(VK, dosya), int(w), int(h)), (int(x), int(y)))


def buton(sayfa, d, bilesen, durum, x, y, w, h, etiket, f, renk=None):
    cerceve(sayfa, bilesen, durum, x, y, w, h)
    if etiket:
        r = renk or (PAL["textPrimary"] if durum != "disabled" else PAL["textSecondary"])
        a = 255 if durum != "disabled" else 150
        k = d.textbbox((0, 0), etiket, font=f)
        yaz(d, (x + w / 2, y + (h - (k[3] - k[1])) / 2 - k[1]), etiket, f, r + (a,), True)


def tas(sayfa, d, ad, durum, x, y, boy, f, f_alt):
    sembol, seviye = TILES[ad]
    sp = Image.open(os.path.join(OUT, "tiles", "tile_L%d_%s_256.png" % (seviye, durum))).convert("RGBA")
    sp = sp.resize((boy, boy), Image.LANCZOS)
    sayfa.alpha_composite(sp, (int(x), int(y)))
    # Kullanilabilir genislik seviyeye gore daralir: ust seviyelerde ic halka
    # ve kose braketi yer kapliyor.
    kullanilabilir = {1: 0.76, 2: 0.70, 3: 0.58}[seviye]
    formul(sayfa, d, x + boy / 2, y + boy / 2, sembol, f, f_alt,
           PAL["textPrimary"] + (255,), max_gen=boy * kullanilabilir)


def ikon(sayfa, d, durum, x, y, boy, isaret, f=None):
    """Ikon butonu. Isaret FONT GLIFI DEGIL, cizim: Oxanium'da ok/duraklat
    glifleri yok (→ glifinin olmamasiyla ayni tuzak)."""
    sp = Image.open(os.path.join(OUT, VK, "icon_button_%s.png" % durum)).convert("RGBA")
    sp = sp.resize((boy, boy), Image.LANCZOS)
    sayfa.alpha_composite(sp, (int(x), int(y)))

    cx, cy = x + boy / 2, y + boy / 2
    renk = PAL["textPrimary"] + (255,)
    if isaret == "geri":
        r = boy * 0.17
        d.polygon([(cx + r * 0.6, cy - r), (cx + r * 0.6, cy + r), (cx - r * 0.8, cy)], fill=renk)
    elif isaret == "duraklat":
        gw, gh, ara = boy * 0.08, boy * 0.30, boy * 0.09
        d.rectangle([cx - ara - gw, cy - gh / 2, cx - ara, cy + gh / 2], fill=renk)
        d.rectangle([cx + ara, cy - gh / 2, cx + ara + gw, cy + gh / 2], fill=renk)


def logo(sayfa, x, y, w):
    im = Image.open(os.path.join(FIG, "NEXUM_logo.png")).convert("RGBA")
    h = int(im.height * w / im.width)
    sayfa.alpha_composite(im.resize((w, h), Image.LANCZOS), (int(x), int(y)))
    return h


# ------------------------------------------------------------------ ekranlar

def ekran_menu():
    s = arkaplan()
    d = ImageDraw.Draw(s)
    f_btn = font("Oxanium-SemiBold.ttf", 46)
    f_kucuk = font("Oxanium-Regular.ttf", 30)

    logo(s, (W - 620) // 2, 150, 620)

    etiketler = [("DENEYİ BAŞLAT", "btn_primary"), ("NASIL OYNANIR", "btn_secondary"),
                 ("ANSİKLOPEDİ", "btn_secondary"), ("TARİF DEFTERİ", "btn_secondary"),
                 ("AYARLAR", "btn_secondary")]
    y = 620
    for et, bil in etiketler:
        buton(s, d, bil, "normal", 150, y, 780, 150, et, f_btn)
        y += 190

    ikon(s, d, "normal", 70, 120, 140, "geri")
    yaz(d, (W / 2, 1810), "Baş Kimyager · 7/21 tepkime", f_kucuk, PAL["textSecondary"] + (255,), True)
    return s


def grid_ciz(s, d, tiles_4x4, f, f_alt, x0=90, y0=560, hucre=210, bosluk=16):
    cerceve(s, "panel_lg", "normal", x0 - 24, y0 - 24,
            4 * hucre + 3 * bosluk + 48, 4 * hucre + 3 * bosluk + 48)
    for r in range(4):
        for c in range(4):
            ad = tiles_4x4[r * 4 + c]
            if not ad:
                continue
            durum = "normal"
            if ad.startswith("*"):
                ad, durum = ad[1:], "selected"
            elif ad.startswith("+"):
                ad, durum = ad[1:], "newborn"
            tas(s, d, ad, durum, x0 + c * (hucre + bosluk), y0 + r * (hucre + bosluk),
                hucre, f, f_alt)


def ekran_oyun():
    s = arkaplan("arkaplan.png", 0.50)
    d = ImageDraw.Draw(s)
    f_t = font("Oxanium-SemiBold.ttf", 58)
    f_ta = font("Oxanium-SemiBold.ttf", 34)
    f_hud = font("Oxanium-Medium.ttf", 32)
    f_kucuk = font("Oxanium-Regular.ttf", 26)

    logo(s, 380, 40, 320)
    ikon(s, d, "normal", 920, 50, 110, "duraklat")

    # hedef beherleri + skor
    for i, (ad, sayi) in enumerate([("H2O", "2/4"), ("CO2", "1/3")]):
        cerceve(s, "chip", "normal", 60 + i * 230, 190, 210, 90)
        sembol = TILES[ad][0]
        formul(s, d, 60 + i * 230 + 70, 235, sembol, f_hud, font("Oxanium-Medium.ttf", 20),
               PAL["primaryBright"] + (255,))
        yaz(d, (60 + i * 230 + 160, 218), sayi, f_hud, PAL["textPrimary"] + (255,), True)

    buton(s, d, "chip", "normal", 700, 190, 320, 90, "SKOR 12 480", f_hud)
    buton(s, d, "btn_secondary", "normal", 700, 300, 320, 110, "PARÇALA", f_hud)
    buton(s, d, "btn_ghost", "normal", 60, 300, 300, 110, "GERİ AL", f_hud)

    tahta = ["H", "O", "*H2O", "C",
             "Na", "Cl", "O2", "+N",
             "Fe2O3", "CaO", "H", "O",
             "CO2", "N2", "Ca", "CaCO3"]
    grid_ciz(s, d, tahta, f_t, f_ta)

    # katalizor gostergesi
    cerceve(s, "progress_track", "normal", 240, 1540, 600, 44)
    cerceve(s, "progress_fill", "selected", 240, 1540, 360, 44)
    yaz(d, (W / 2, 1600), "BEDAVA PARÇALAMA ×1", f_hud, PAL["success"] + (255,), True)

    buton(s, d, "toast", "normal", 90, 1700, 900, 110, "YENİ TEPKİME KAYDEDİLDİ", f_hud,
          renk=PAL["primaryBright"])
    yaz(d, (W / 2, 1840), "tepkime denklemi: ok ve alt simge TMP sprite/<sub> ile gelir", f_kucuk,
        PAL["textSecondary"] + (255,), True)
    return s


def ekran_oyun_oncesi():
    """Mevcut hal: bugunku grid.png + bugunku seviye taslari + bugunku butonlar.
    Karsilastirma icin, mevcut varliklardan yeniden kuruldu."""
    s = arkaplan("arkaplan.png", 0.50)
    d = ImageDraw.Draw(s)
    f_t = font("Oxanium-SemiBold.ttf", 58)
    f_ta = font("Oxanium-SemiBold.ttf", 34)
    f_hud = font("Oxanium-Medium.ttf", 32)

    logo(s, 380, 40, 320)

    grid = Image.open(os.path.join(FIG, "grid.png")).convert("RGBA").resize((900, 900), Image.LANCZOS)
    s.alpha_composite(grid, (90, 560))

    # bugunku seviye taslari (1/2/3.seviyetas.png) — ELLE atanmis seviyelerle
    elle = {"H": 1, "O": 1, "C": 1, "Na": 1, "Cl": 1, "Ca": 1,
            "H2O": 3, "O2": 2, "N": 1, "Fe2O3": 3, "CaO": 2, "CO2": 3,
            "N2": 2, "CaCO3": 3}
    tahta = ["H", "O", "H2O", "C", "Na", "Cl", "O2", "N",
             "Fe2O3", "CaO", "H", "O", "CO2", "N2", "Ca", "CaCO3"]
    hucre, bosluk, x0, y0 = 210, 16, 90, 560
    for i, ad in enumerate(tahta):
        sv = elle.get(ad, 1)
        sp = Image.open(os.path.join(FIG, "%d.seviyetas.png" % sv)).convert("RGBA")
        sp = sp.resize((hucre, hucre), Image.LANCZOS)
        x = x0 + (i % 4) * (hucre + bosluk)
        y = y0 + (i // 4) * (hucre + bosluk)
        s.alpha_composite(sp, (x, y))
        formul(s, d, x + hucre / 2, y + hucre / 2, TILES[ad][0], f_t, f_ta,
               PAL["textPrimary"] + (255,))

    for ad, x in (("game_button.png", 60), ("game_button1.png", 700)):
        sp = Image.open(os.path.join(FIG, ad)).convert("RGBA").resize((320, 110), Image.LANCZOS)
        s.alpha_composite(sp, (x, 300))
    yaz(d, (220, 340), "GERİ AL", f_hud, PAL["textOnLight"] + (255,), True)
    yaz(d, (860, 340), "PARÇALA", f_hud, PAL["textOnLight"] + (255,), True)

    tup = Image.open(os.path.join(FIG, "Entropitup.png")).convert("RGBA").resize((600, 400), Image.LANCZOS)
    s.alpha_composite(tup, (240, 1480))
    yaz(d, (W / 2, 1860), "MEVCUT HÂL (bugünkü varlıklardan yeniden kuruldu)",
        font("Oxanium-Regular.ttf", 26), PAL["warning"] + (255,), True)
    return s


def ekran_duraklat():
    s = ekran_oyun()
    kat = Image.new("RGBA", (W, H), PAL["cardBackground"] + (int(255 * 0.62),))
    s = Image.alpha_composite(s, kat)
    d = ImageDraw.Draw(s)
    f_b = font("Oxanium-SemiBold.ttf", 44)
    f_o = font("Oxanium-SemiBold.ttf", 34)
    f_g = font("Oxanium-Regular.ttf", 32)

    cerceve(s, "modal", "normal", 90, 380, 900, 1160)
    yaz(d, (W / 2, 440), "LABORATUVAR MOLASI", f_b, PAL["primaryBright"] + (255,), True)
    yaz(d, (W / 2, 520), "BİLİYOR MUYDUN?", f_o, PAL["textSecondary"] + (255,), True)

    cerceve(s, "panel_sm", "normal", 150, 600, 780, 520)
    for i, satir in enumerate(["İki yanıcı (H) ve bir yakıcı (O)",
                               "gazın birleşmesi, yangını",
                               "söndüren suyu verir."]):
        yaz(d, (W / 2, 660 + i * 56), satir, f_g, PAL["textSecondary"] + (255,), True)

    # Panelde BOYALI YUVA YOK: butonlar serbest yerlesiyor
    buton(s, d, "btn_primary", "normal", 150, 1180, 370, 130, "DEVAM ET", f_o)
    buton(s, d, "btn_secondary", "normal", 560, 1180, 370, 130, "ANA MENÜ", f_o)
    buton(s, d, "btn_ghost", "normal", 150, 1340, 780, 130, "TARİF DEFTERİ", f_o)
    return s


def ekran_tarif_defteri():
    s = arkaplan()
    d = ImageDraw.Draw(s)
    f_b = font("Oxanium-SemiBold.ttf", 46)
    f_s = font("Oxanium-Medium.ttf", 32)
    f_sa = font("Oxanium-Medium.ttf", 20)
    f_g = font("Oxanium-Regular.ttf", 28)

    logo(s, 290, 60, 500)
    ikon(s, d, "normal", 60, 80, 120, "geri")
    yaz(d, (W / 2, 250), "TARİF DEFTERİ", f_b, PAL["primaryBright"] + (255,), True)

    halka = Image.open(os.path.join(OUT, VK, "ring_progress.png")).convert("RGBA").resize((200, 200), Image.LANCZOS)
    kat = Image.new("RGBA", halka.size, PAL["primary"] + (0,))
    kat.putalpha(halka.split()[3])
    s.alpha_composite(kat, ((W - 200) // 2, 330))
    yaz(d, (W / 2, 400), "7/21", font("Oxanium-SemiBold.ttf", 44), PAL["primaryBright"] + (255,), True)
    yaz(d, (W / 2, 560), "7 / 21 tepkime keşfedildi", f_g, PAL["textSecondary"] + (255,), True)

    satirlar = [("H", "H", "H2", True), ("H2", "O", "H2O", True), ("O", "O", "O2", True),
                ("C", "O", "CO", True), ("CO", "O", "CO2", True), ("Ca", "O", "CaO", True),
                ("Fe", "Fe", "Fe2", True), (None, None, None, False), (None, None, None, False),
                (None, None, None, False)]
    y = 640
    ok = Image.open(os.path.join(FIG, "arrow_reaction.png")).convert("RGBA")
    for a, b, c, acik in satirlar:
        cerceve(s, "list_row", "normal" if acik else "disabled", 70, y, 940, 110)
        renk = (PAL["textPrimary"] if acik else PAL["textSecondary"]) + (255,)
        if acik:
            formul(s, d, 300, y + 55, TILES[a][0], f_s, f_sa, renk)
            yaz(d, (360, y + 36), "+", f_s, renk)
            formul(s, d, 430, y + 55, TILES[b][0], f_s, f_sa, renk)
            okk = ok.resize((int(ok.width * 26 / ok.height), 26), Image.LANCZOS)
            kat = Image.new("RGBA", okk.size, renk[:3] + (0,))
            kat.putalpha(okk.split()[3])
            s.alpha_composite(kat, (500, y + 44))
            formul(s, d, 640, y + 55, TILES[c][0], f_s, f_sa, PAL["primaryBright"] + (255,))
        else:
            yaz(d, (470, y + 36), "?  +  ?", f_s, renk)
            kilit = Image.open(os.path.join(OUT, VK, "lock_overlay.png")).convert("RGBA").resize((48, 48), Image.LANCZOS)
            kat = Image.new("RGBA", kilit.size, PAL["textSecondary"] + (0,))
            kat.putalpha(kilit.split()[3])
            s.alpha_composite(kat, (920, y + 30))
        y += 122
    return s


def ekran_quiz():
    s = ekran_oyun()
    s = Image.alpha_composite(s, Image.new("RGBA", (W, H), PAL["cardBackground"] + (int(255 * 0.70),)))
    d = ImageDraw.Draw(s)
    f_b = font("Oxanium-SemiBold.ttf", 40)
    f_o = font("Oxanium-Medium.ttf", 34)

    cerceve(s, "modal", "normal", 90, 420, 900, 1080)
    yaz(d, (W / 2, 480), "HİPOTEZ DOĞRULAMA", f_b, PAL["primaryBright"] + (255,), True)
    for i, satir in enumerate(["Sodyum (Na) ile klorun (Cl)", "birleşmesinden ne oluşur?"]):
        yaz(d, (W / 2, 580 + i * 54), satir, f_o, PAL["textPrimary"] + (255,), True)

    siklar = [("A) Su", "normal"), ("B) Amonyak", "normal"),
              ("C) Sodyum klorür", "selected"), ("D) Karbondioksit", "normal")]
    y = 760
    for et, durum in siklar:
        buton(s, d, "quiz_option", durum, 150, y, 780, 130, et, f_o,
              renk=PAL["success"] if durum == "selected" else None)
        y += 160
    yaz(d, (W / 2, 1420), "İkinci şans hakkın: 1", font("Oxanium-Regular.ttf", 28),
        PAL["textSecondary"] + (255,), True)
    return s


def ekran_ayarlar():
    s = arkaplan()
    d = ImageDraw.Draw(s)
    f_b = font("Oxanium-SemiBold.ttf", 46)
    f_o = font("Oxanium-Medium.ttf", 34)
    f_g = font("Oxanium-Regular.ttf", 30)

    ikon(s, d, "normal", 60, 80, 120, "geri")
    yaz(d, (W / 2, 120), "AYARLAR", f_b, PAL["primaryBright"] + (255,), True)

    cerceve(s, "panel_lg", "normal", 80, 260, 920, 520)
    yaz(d, (130, 300), "OYUN MODU", f_o, PAL["secondary"] + (255,))
    for i, (et, durum) in enumerate([("NORMAL", "selected"), ("SINAV", "normal"), ("SERBEST", "normal")]):
        buton(s, d, "tab", durum, 120 + i * 290, 380, 270, 110, et, f_g)
    yaz(d, (130, 530), "Normal: ipucu ve geri alma açık.", f_g, PAL["textSecondary"] + (255,))
    yaz(d, (130, 590), "Sınav: yardım yok, puan iki kat.", f_g, PAL["textSecondary"] + (255,))
    yaz(d, (130, 650), "Serbest: kaybetme yok.", f_g, PAL["textSecondary"] + (255,))

    cerceve(s, "panel_lg", "normal", 80, 820, 920, 420)
    yaz(d, (130, 860), "SES", f_o, PAL["secondary"] + (255,))
    for i, (et, acik) in enumerate([("Müzik", True), ("Efektler", True)]):
        yaz(d, (130, 950 + i * 120), et, f_g, PAL["textPrimary"] + (255,))
        sp = Image.open(os.path.join(OUT, VK, "toggle_%s_normal.png" % ("on" if acik else "off"))).convert("RGBA")
        sp = sp.resize((180, 84), Image.LANCZOS)
        s.alpha_composite(sp, (780, 935 + i * 120))

    buton(s, d, "btn_destructive", "normal", 150, 1320, 780, 140, "İLERLEMEYİ SIFIRLA", f_o)
    yaz(d, (W / 2, 1500), "Bu işlem geri alınamaz.", f_g, PAL["textSecondary"] + (255,), True)
    return s


def tas_sayfasi():
    """3 seviye x 5 durum, gercek formullerle."""
    sutun = ["normal", "selected", "merging", "newborn", "discovered"]
    boy, pay = 190, 40
    GW = max(1480, pay + 220 + len(sutun) * (boy + pay))
    GH = 200 + 3 * (boy + 80)
    s = Image.new("RGBA", (GW, GH), PAL["cardBackground"] + (255,))
    d = ImageDraw.Draw(s)
    f_b = font("Oxanium-SemiBold.ttf", 38)
    f_e = font("Oxanium-Regular.ttf", 24)
    f_t = font("Oxanium-SemiBold.ttf", 46)
    f_ta = font("Oxanium-SemiBold.ttf", 28)

    yaz(d, (pay, 36), "NEXUM — taş seviyeleri (Metal kiti)", f_b, PAL["primaryBright"] + (255,))
    yaz(d, (pay, 90), "Seviye ÇERÇEVEDEN okunur: halka sayısı 1/2/3, kenarlık 6/7/9 birim, "
                      "en üst seviyede köşe braketi.",
        f_e, PAL["textSecondary"] + (255,))
    yaz(d, (pay, 118), "Renk tek başına ayırt edici değil.", f_e, PAL["textSecondary"] + (255,))

    for c, durum in enumerate(sutun):
        yaz(d, (220 + pay + c * (boy + pay) + boy / 2, 140), durum, f_e,
            PAL["textSecondary"] + (255,), True)

    ornek = {1: "O", 2: "H2O", 3: "Fe2O3"}
    for r, sv in enumerate((1, 2, 3)):
        y = 180 + r * (boy + 80)
        cfg = TILE_LEVELS[sv]
        yaz(d, (pay, y + boy / 2 - 40), "SEVİYE %d" % sv, f_b, PAL["textPrimary"] + (255,))
        yaz(d, (pay, y + boy / 2 + 10), "%d halka · kenarlık %d" % (cfg["rings"], cfg["border"]),
            f_e, PAL["textSecondary"] + (255,))
        for c, durum in enumerate(sutun):
            tas(s, d, ornek[sv], durum, 220 + pay + c * (boy + pay), y, boy, f_t, f_ta)
    return s


def main():
    os.makedirs(DOCS, exist_ok=True)
    ekranlar = [
        ("screen-menu.png", ekran_menu),
        ("screen-game.png", ekran_oyun),
        ("screen-game-before.png", ekran_oyun_oncesi),
        ("screen-pause.png", ekran_duraklat),
        ("screen-recipebook.png", ekran_tarif_defteri),
        ("screen-quiz.png", ekran_quiz),
        ("screen-settings.png", ekran_ayarlar),
        ("tiles-sheet.png", tas_sayfasi),
    ]
    for ad, fn in ekranlar:
        img = fn()
        yol = os.path.join(DOCS, ad)
        img.convert("RGB").save(yol, optimize=True)
        print("%-26s %dx%d" % (ad, img.width, img.height))


if __name__ == "__main__":
    main()
