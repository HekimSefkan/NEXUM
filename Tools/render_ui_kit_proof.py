# -*- coding: utf-8 -*-
"""NEXUM UI kiti — kanit sayfalari (docs/ui-kit/variant-*.png).

generate_ui_kit.py kiti uretir; bu dosya kiti GERCEK kullanimda gosterir:

- Sayfadaki her buton TEK sprite'tan 9-SLICE ile cizilir. Unity'nin
  Image.Type.Sliced davranisinin aynisi burada Python'da uygulandi
  (nine_slice); yani sayfada gordugunuz sey oyunda gorecegimiz sey.
- Gorsellerde metin YOK: her yazi ayri katman olarak (Unity'de TMP
  etiketi olarak) ustune koyuluyor. Metinler gercek ve EN UZUN Turkce
  varyantlar.
- Bilesenler oyunun GERCEK arka plani uzerine, vinyet katmaniyla
  yerlestiriliyor; okunurluk gercek zeminde degerlendirilsin.

Kullanim (proje kokunden):
    python Tools/generate_ui_kit.py      (kiti uretir, sonra bunu cagirir)
    python Tools/render_ui_kit_proof.py  (yalnizca sayfalari yeniden cizer)
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from generate_ui_kit import (  # noqa: E402
    OUT, PAL, ROOT, VARIANTS, STATES, border_payi,
)

CLEAR = (0, 0, 0, 0)

with open(os.path.join(OUT, "manifest.json"), encoding="utf-8") as _f:
    MANIFEST = json.load(_f)


def sprite_border(vk, dosya):
    """Sprite'in KENDI kenar payi (manifest'ten). Ince cubuklarda kisilmistir."""
    kayit = MANIFEST["varyantlar"][vk]["sprites"].get(dosya)
    if not kayit:
        pay = border_payi(vk)
        return [pay] * 4
    return kayit["border"]
DOCS = os.path.join(ROOT, "docs", "ui-kit")
FONT_DIR = os.path.join(ROOT, "Assets", "Fonts")
BG = os.path.join(ROOT, "Assets", "Art", "Figures", "arkaplan.png")

BASLIK_RENK = PAL["primaryBright"]
NOT_RENK = PAL["textSecondary"]

# Gercek ve EN UZUN Turkce metinler (ceviri ve %85 sigma kurali icin)
ORNEK = {
    "primary": "DENEYİ BAŞLAT",
    "secondary": "NASIL OYNANIR",
    "ghost": "VAZGEÇ",
    "destructive": "İLERLEMEYİ SIFIRLA",
    "chip": "SKOR 12 480",
    "tab": "BİLEŞİKLER",
    "row": "H + H",
    "row_sag": "H2",
    "toast": "YENİ TEPKİME KAYDEDİLDİ",
    "quiz": "C) Sodyum klorür",
    "tooltip": "Bu iki elementi birleştir",
    "catalyst": "BEDAVA PARÇALAMA ×1",
    "book": "TARİF DEFTERİ",
}


def font(ad, punto):
    return ImageFont.truetype(os.path.join(FONT_DIR, ad), punto)


# ------------------------------------------------------------- 9-slice

def nine_slice(img, border, w, h):
    """Unity Image.Type.Sliced ile ayni davranis.

    border = [sol, alt, sag, ust] (Unity spriteBorder sirasi).
    Koseler sabit kalir, kenarlar tek eksende, merkez iki eksende gerilir.
    """
    l, b, r, t = border
    W, H = img.size
    w = max(w, l + r)
    h = max(h, t + b)

    out = Image.new("RGBA", (w, h), CLEAR)
    sx, sy = [0, l, W - r, W], [0, t, H - b, H]
    dx, dy = [0, l, w - r, w], [0, t, h - b, h]

    for i in range(3):
        for j in range(3):
            parca = img.crop((sx[i], sy[j], sx[i + 1], sy[j + 1]))
            gw, gh = dx[i + 1] - dx[i], dy[j + 1] - dy[j]
            if gw <= 0 or gh <= 0:
                continue
            if parca.size != (gw, gh):
                parca = parca.resize((gw, gh), Image.BILINEAR)
            out.paste(parca, (dx[i], dy[j]))
    return out


# -------------------------------------------------------------- cizim

def metin(d, xy, s, f, renk, ortala=False):
    if ortala:
        kutu = d.textbbox((0, 0), s, font=f)
        xy = (xy[0] - (kutu[2] - kutu[0]) / 2, xy[1])
    d.text(xy, s, font=f, fill=renk)


def zemin(w, h, guc=0.62):
    bg = Image.open(BG).convert("RGBA")
    olcek = max(w / bg.width, h / bg.height)
    bg = bg.resize((int(bg.width * olcek) + 1, int(bg.height * olcek) + 1), Image.LANCZOS)
    bg = bg.crop((0, 0, w, h))
    kara = Image.new("RGBA", (w, h), PAL["cardBackground"] + (int(255 * guc),))
    return Image.alpha_composite(bg, kara)


def buton(sayfa, vk, bilesen, durum, x, y, w, h, etiket=None, f=None, renk=None):
    """Kitten TEK sprite alir, 9-slice ile olceklendirir, metni AYRI katman
    olarak ustune koyar."""
    dosya = "%s_%s.png" % (bilesen, durum)
    sp = Image.open(os.path.join(OUT, vk, dosya)).convert("RGBA")
    sayfa.alpha_composite(nine_slice(sp, sprite_border(vk, dosya), int(w), int(h)), (int(x), int(y)))

    if etiket:
        d = ImageDraw.Draw(sayfa)
        r = renk or (PAL["textPrimary"] if durum != "disabled" else PAL["textSecondary"])
        a = 255 if durum != "disabled" else 140
        kutu = d.textbbox((0, 0), etiket, font=f)
        metin(d, (x + w / 2, y + (h - (kutu[3] - kutu[1])) / 2 - kutu[1]), etiket, f, r + (a,), True)


OK_SPRITE = os.path.join(ROOT, "Assets", "Art", "Figures", "arrow_reaction.png")


def tepkime(sayfa, d, x, y, h, f, renk, kilitli=False):
    """"H + H [ok] H2" — ok, oyundaki GERCEK sprite (fontlarda → glifi yok)."""
    sol = "? + ?" if kilitli else ORNEK["row"]
    sag = "?" if kilitli else ORNEK["row_sag"]

    kutu = d.textbbox((0, 0), sol, font=f)
    ty = y + (h - (kutu[3] - kutu[1])) / 2 - kutu[1]
    d.text((x, ty), sol, font=f, fill=renk)
    ox = x + (kutu[2] - kutu[0]) + 14

    ok = Image.open(OK_SPRITE).convert("RGBA")
    boy = max(12, int(f.size * 0.80))             # sayfada okunur kalsin
    ok = ok.resize((int(ok.width * boy / max(1, ok.height)), max(1, boy)), Image.LANCZOS)
    kat = Image.new("RGBA", ok.size, renk[:3] + (0,))
    kat.putalpha(ok.split()[3])
    sayfa.alpha_composite(kat, (int(ox), int(y + (h - boy) / 2)))

    d.text((ox + ok.width + 14, ty), sag, font=f, fill=renk)


def sabit(sayfa, vk, dosya, x, y, olcek=1.0, ton=None):
    """ton verilirse sprite tonlanir (Unity'de Image.color ile ayni is)."""
    im = Image.open(os.path.join(OUT, vk, dosya)).convert("RGBA")
    if olcek != 1.0:
        im = im.resize((max(1, int(im.width * olcek)), max(1, int(im.height * olcek))), Image.LANCZOS)
    if ton:
        kat = Image.new("RGBA", im.size, tuple(ton) + (0,))
        kat.putalpha(im.split()[3])
        im = kat
    sayfa.alpha_composite(im, (int(x), int(y)))
    return im.size


# --------------------------------------------------------------- sayfa

def kanit_sayfasi(vk):
    v = VARIANTS[vk]
    W, H, M = 1800, 2900, 60
    sayfa = zemin(W, H)
    d = ImageDraw.Draw(sayfa)

    f_baslik = font("Oxanium-SemiBold.ttf", 52)
    f_bolum = font("Oxanium-SemiBold.ttf", 30)
    f_not = font("Oxanium-Regular.ttf", 22)
    f_btn = font("Oxanium-SemiBold.ttf", 28)
    f_kucuk = font("Oxanium-Medium.ttf", 24)

    y = 40
    metin(d, (M, y), "NEXUM UI — Varyant %s · %s" % (vk, v["ad"]), f_baslik, BASLIK_RENK + (255,))
    y += 70
    metin(d, (M, y), v["aciklama"], f_not, NOT_RENK + (255,))
    y += 28
    metin(d, (M, y), "Hiçbir görselde metin basılı değil; her yazı ayrı katman (TMP etiketi). "
                     "Sayfadaki her çerçeve tek sprite'tan 9-slice ile çizildi.",
          f_not, NOT_RENK + (255,))
    y += 52

    def bolum(baslik, alt=None):
        nonlocal y
        d.line([(M, y), (W - M, y)], fill=(90, 120, 150, 160), width=2)
        y += 14
        metin(d, (M, y), baslik, f_bolum, BASLIK_RENK + (255,))
        if alt:
            metin(d, (M + 430, y + 8), alt, f_not, NOT_RENK + (255,))
        y += 46

    durumlar = list(STATES.keys())

    # 1 — butonlar
    bolum("1 · BUTONLAR", "dört durum: normal / basılı / devre dışı / seçili")
    for i, dd in enumerate(durumlar):
        metin(d, (M + 240 + i * 380, y), dd.upper(), f_kucuk, NOT_RENK + (255,))
    y += 34

    for ad, bh, etiket in (("btn_primary", 92, ORNEK["primary"]),
                           ("btn_secondary", 84, ORNEK["secondary"]),
                           ("btn_ghost", 84, ORNEK["ghost"]),
                           ("btn_destructive", 84, ORNEK["destructive"])):
        metin(d, (M, y + bh / 2 - 14), ad, f_not, NOT_RENK + (255,))
        for i, dd in enumerate(durumlar):
            buton(sayfa, vk, ad, dd, M + 240 + i * 380, y, 350, bh, etiket, f_btn)
        y += bh + 18
    y += 22

    # 2 — kontroller
    bolum("2 · KONTROLLER", "ikon butonu · çip · anahtar · kaydırıcı · yıldız · düğüm · rozet")
    x = M
    for dd in durumlar:
        sabit(sayfa, vk, "icon_button_%s.png" % dd, x, y)
        x += 116
    x += 30
    for dd in ("normal", "selected"):
        buton(sayfa, vk, "chip", dd, x, y + 18, 250, 58, ORNEK["chip"], f_kucuk)
        x += 270
    x += 20
    for dd, st in (("off", "normal"), ("on", "normal"), ("on", "disabled")):
        sabit(sayfa, vk, "toggle_%s_%s.png" % (dd, st), x, y + 20)
        x += 136
    sabit(sayfa, vk, "ring_progress.png", W - M - 230, y - 14, 0.5, PAL["primary"])
    sabit(sayfa, vk, "lock_overlay.png", W - M - 80, y + 4, 0.62, PAL["textSecondary"])
    y += 118

    x = M
    iz = Image.open(os.path.join(OUT, vk, "slider_track.png")).convert("RGBA")
    dolgu = Image.open(os.path.join(OUT, vk, "slider_fill.png")).convert("RGBA")
    sayfa.alpha_composite(nine_slice(iz, sprite_border(vk, "slider_track.png"), 420, 26), (x, y + 24))
    sayfa.alpha_composite(nine_slice(dolgu, sprite_border(vk, "slider_fill.png"), 250, 26), (x, y + 24))
    sabit(sayfa, vk, "slider_knob.png", x + 222, y + 8)
    x += 480
    for dolu in ("empty", "full", "full"):
        sabit(sayfa, vk, "star_%s_normal.png" % dolu, x, y + 8)
        x += 86
    x += 40
    for durum in ("locked", "open", "done"):
        sabit(sayfa, vk, "level_node_%s.png" % durum, x, y)
        x += 156
    sabit(sayfa, vk, "badge_frame_normal.png", x + 20, y)
    sabit(sayfa, vk, "avatar_frame_normal.png", x + 200, y - 6, 0.72)
    y += 190

    # 3 — yüzeyler
    bolum("3 · YÜZEYLER", "panel · modal · liste satırı · sekme · bildirim · ipucu · sınav şıkkı")
    buton(sayfa, vk, "panel_lg", "normal", M, y, 400, 250)
    metin(d, (M + 200, y + 28), "PANEL", f_bolum, NOT_RENK + (255,), True)
    buton(sayfa, vk, "modal", "normal", M + 440, y, 400, 250)
    metin(d, (M + 640, y + 28), "MODAL", f_bolum, NOT_RENK + (255,), True)

    rx = M + 900
    for i, dd in enumerate(("normal", "selected", "disabled")):
        buton(sayfa, vk, "list_row", dd, rx, y + i * 74, 740, 66)
        tepkime(sayfa, d, rx + 280, y + i * 74, 66, f_kucuk,
                (PAL["textSecondary"] if dd == "disabled" else PAL["textPrimary"]) + (255,),
                kilitli=(dd == "disabled"))
    for i, dd in enumerate(("normal", "selected")):
        buton(sayfa, vk, "tab", dd, rx + i * 250, y + 226, 230, 64, ORNEK["tab"], f_kucuk)
    y += 320

    buton(sayfa, vk, "toast", "normal", M, y, 560, 84, ORNEK["toast"], f_btn)
    buton(sayfa, vk, "tooltip", "normal", M + 600, y, 420, 84, ORNEK["tooltip"], f_kucuk)
    buton(sayfa, vk, "quiz_option", "normal", M + 1060, y, 400, 84, ORNEK["quiz"], f_kucuk)
    y += 100
    buton(sayfa, vk, "input_field", "normal", M, y, 560, 84, "Kimyager adın", f_kucuk,
          renk=PAL["textSecondary"])
    buton(sayfa, vk, "quiz_option", "selected", M + 1060, y, 400, 84, ORNEK["quiz"], f_kucuk,
          renk=PAL["success"])
    y += 126

    # 4 — 9-slice kanıtı
    bolum("4 · 9-SLICE KANITI",
          "üçü de AYNI sprite: btn_primary_normal.png (320×96, kenar payı %d)"
          % sprite_border(vk, "btn_primary_normal.png")[0])
    x = M
    for bw in (220, 430, 760):
        buton(sayfa, vk, "btn_primary", "normal", x, y, bw, 92, ORNEK["book"], f_btn)
        metin(d, (x + bw / 2, y + 98), "%d birim" % bw, f_not, NOT_RENK + (255,), True)
        x += bw + 40
    metin(d, (M, y + 132), "Köşe geometrisi ve kenarlık kalınlığı üç ölçüde de birebir aynı; "
                           "yalnızca orta bölge gerildi.", f_not, NOT_RENK + (255,))
    y += 180

    # 5 — örnek ekran
    bolum("5 · ÖRNEK EKRAN", "ana menü · duraklat kartı · oyun HUD parçası")
    mw, mh = 400, 700
    buton(sayfa, vk, "panel_lg", "normal", M, y, mw, mh)
    metin(d, (M + mw / 2, y + 34), "NEXUM", font("Oxanium-SemiBold.ttf", 44),
          BASLIK_RENK + (255,), True)
    for i, et in enumerate(["DENEYİ BAŞLAT", "NASIL OYNANIR", "ANSİKLOPEDİ",
                            ORNEK["book"], "AYARLAR"]):
        buton(sayfa, vk, "btn_primary" if i == 0 else "btn_secondary",
              "selected" if i == 3 else "normal",
              M + 28, y + 118 + i * 104, mw - 56, 86, et, f_kucuk)

    px, pw = M + 480, 540
    buton(sayfa, vk, "modal", "normal", px, y, pw, mh)
    metin(d, (px + pw / 2, y + 34), "LABORATUVAR MOLASI", f_bolum, BASLIK_RENK + (255,), True)
    metin(d, (px + pw / 2, y + 90), "BİLİYOR MUYDUN?", f_kucuk, NOT_RENK + (255,), True)
    buton(sayfa, vk, "panel_sm", "normal", px + 36, y + 134, pw - 72, 280)
    metin(d, (px + 66, y + 168),
          "İki yanıcı (H) ve bir yakıcı (O)\ngazın birleşmesi, yangını\nsöndüren suyu verir.",
          f_kucuk, NOT_RENK + (255,))
    buton(sayfa, vk, "btn_secondary", "normal", px + 36, y + 448, 220, 76, "DEVAM ET", f_kucuk)
    buton(sayfa, vk, "btn_secondary", "normal", px + 282, y + 448, 220, 76, "ANA MENÜ", f_kucuk)
    buton(sayfa, vk, "btn_ghost", "normal", px + 36, y + 546, pw - 72, 76, ORNEK["book"], f_kucuk)

    hx = M + 1100
    buton(sayfa, vk, "chip", "normal", hx, y, 250, 62, ORNEK["chip"], f_kucuk)
    buton(sayfa, vk, "progress_track", "normal", hx, y + 86, 440, 36)
    buton(sayfa, vk, "progress_fill", "selected", hx, y + 86, 270, 36)
    metin(d, (hx, y + 136), ORNEK["catalyst"], f_kucuk, PAL["success"] + (255,))
    buton(sayfa, vk, "toast", "normal", hx, y + 182, 500, 80, ORNEK["toast"], f_kucuk)
    for i, durum in enumerate(("normal", "selected", "disabled")):
        buton(sayfa, vk, "list_row", durum, hx, y + 286 + i * 78, 500, 70)
        tepkime(sayfa, d, hx + 160, y + 286 + i * 78, 70, f_kucuk,
                (PAL["textSecondary"] if durum == "disabled" else PAL["textPrimary"]) + (255,),
                kilitli=(durum == "disabled"))
    sabit(sayfa, vk, "ring_progress.png", hx + 60, y + 528, 0.68, PAL["primary"])
    metin(d, (hx + 147, y + 598), "7/21", f_bolum, BASLIK_RENK + (255,), True)

    os.makedirs(DOCS, exist_ok=True)
    yol = os.path.join(DOCS, "variant-%s.png" % vk)
    sayfa.convert("RGB").save(yol, optimize=True)
    print("kanit sayfasi: %s  (%dx%d)" % (yol, W, H))
    return yol


def main():
    for vk in VARIANTS:
        kanit_sayfasi(vk)


if __name__ == "__main__":
    main()
