# -*- coding: utf-8 -*-
"""NEXUM rozet kanit sayfasi (docs/ui-kit/badges.png).

NE GOSTERIR: dokuz rozetin ONERILEN gorseli, oyundaki haliyle. Rozet
gorseli = kitteki MADALYA CERCEVESI (badge_frame_normal, 9-slice) + simge
ailesinden bir glif. Yeni bir gorsel dili ACILMADI.

Sayfadaki cerceve Unity'nin Image.Type.Sliced davranisiyla cizilir
(render_ui_kit_proof.nine_slice), yani sayfada gorulen sey oyunda
gorulecek sey.

IKI DURUM yan yana: KAZANILMIS (parlak glif) ve KILITLI (sonuk glif).
Bugun kazanilamayan rozetler (gun serisi, ansiklopedi, yildiz, gunluk
bulmaca) sayfada isaretlidir; tabloda TANIMLI ama kazanilamaz.

Sayfadaki metinler kanit icindir; OYUN gorsellerinin icine metin
basilmaz (kural degismedi) — oyunda rozet adi TMP etiketi olacak.

Kullanim (proje kokunden):
    python Tools/generate_icons_family.py   (glifleri uretir)
    python Tools/render_badge_proof.py
"""
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from generate_icons_family import PAL, ROOT, BADGE_OUT  # noqa: E402
from render_ui_kit_proof import font, nine_slice, DOCS, BG  # noqa: E402

KIT_C = os.path.join(ROOT, "Tools", "ui-kit", "C")
FRAME = os.path.join(KIT_C, "badge_frame_normal.png")

# (anahtar, ad, glif, bugun kazanilabilir mi, kosul / faz)
# Tablo Assets/Scripts/Progression/BadgeTable.cs ile AYNI sirada.
BADGES = [
    ("first_synthesis", "Ilk Sentez", "flask", True, "ilk birlesme"),
    ("all_reactions", "Tum Tepkimeler", "book", True, "21 / 21 tarif kesfi"),
    ("flawless_level", "Kazasiz Bolum", "shield", True, "kazasiz bitirilen bolum"),
    ("three_stars", "Uc Yildizli Bolum", "star_full", False, "2.2 — yildizlar"),
    ("streak_7", "7 Gun Seri", "flame", False, "Faz 3 — gun serisi"),
    ("streak_30", "30 Gun Seri", "flame", False, "Faz 3 — gun serisi"),
    ("streak_100", "100 Gun Seri", "flame", False, "Faz 3 — gun serisi"),
    ("daily_puzzle_10", "Gunluk Bulmaca x10", "calendar", False, "Faz 3 — gunluk bulmaca"),
    ("encyclopedia", "Ansiklopedi Kurdu", "book", False, "Faz 4 — kart okuma"),
]

BADGE = 160          # cerceve olcusu (oyundaki kullanim)
GLYPH = 96           # glif olcusu cerceve icinde
COL_W = 300
ROW_H = 250
PAD = 56
COLS = 3


def zemin(w, h):
    """Oyunun gercek arka plani: okunurluk gercek zeminde degerlendirilsin."""
    bg = Image.open(BG).convert("RGBA")
    oran = max(w / bg.width, h / bg.height)
    bg = bg.resize((int(bg.width * oran) + 1, int(bg.height * oran) + 1), Image.LANCZOS)
    bg = bg.crop((0, 0, w, h))
    karart = Image.new("RGBA", (w, h), (0, 0, 0, 110))
    return Image.alpha_composite(bg, karart)


def rozet(glif_ad, kazanilmis):
    """Cerceve + glif. Cerceve 9-slice, glif ortalanir."""
    kare = Image.new("RGBA", (BADGE, BADGE), (0, 0, 0, 0))

    frame = Image.open(FRAME).convert("RGBA")
    pay = max(2, int(min(frame.size) * 0.22))
    kare.alpha_composite(nine_slice(frame, [pay] * 4, BADGE, BADGE))

    durum = "normal" if kazanilmis else "disabled"
    yol = os.path.join(BADGE_OUT, "badge_%s_%s_384.png" % (glif_ad, durum))
    glif = Image.open(yol).convert("RGBA").resize((GLYPH, GLYPH), Image.LANCZOS)
    kare.alpha_composite(glif, ((BADGE - GLYPH) // 2, (BADGE - GLYPH) // 2))

    if not kazanilmis:
        # Kilitli rozet SOLUK: cerceve de sonuyor, yalnizca glif degil.
        sol = Image.new("RGBA", (BADGE, BADGE), (0, 0, 0, 0))
        sol.alpha_composite(kare)
        alpha = sol.getchannel("A").point(lambda v: int(v * 0.45))
        sol.putalpha(alpha)
        return sol

    return kare


def main():
    os.makedirs(DOCS, exist_ok=True)

    satir = (len(BADGES) + COLS - 1) // COLS
    # Sayfa genisligi BASLIK satirina gore: izgara 1012'ye sigiyordu ama
    # aciklama satiri kenardan tasiyordu.
    w = max(PAD * 2 + COLS * COL_W, 1240)
    izgara_sol = (w - COLS * COL_W) // 2
    h = PAD * 2 + 150 + satir * ROW_H

    sayfa = zemin(w, h)
    d = ImageDraw.Draw(sayfa)

    baslik = font("Orbitron-Bold.ttf", 40)
    altbaslik = font("Oxanium-Medium.ttf", 24)
    ad_f = font("Orbitron-SemiBold.ttf", 22)
    not_f = font("Oxanium-Regular.ttf", 20)

    d.text((PAD, PAD), "NEXUM — ROZETLER (2.1 onerisi)",
           font=baslik, fill=PAL["textPrimary"] + (255,))
    d.text((PAD, PAD + 54),
           "Cerceve: kit madalya cercevesi (badge_frame_normal, 9-slice). "
           "Glif: simge ailesi, ayni cizgi kalinligi.",
           font=altbaslik, fill=PAL["textSecondary"] + (255,))
    d.text((PAD, PAD + 86),
           "Solmus olanlar bugun KAZANILAMAZ: tabloda tanimli, verisi yok.",
           font=altbaslik, fill=PAL["textSecondary"] + (255,))

    for i, (key, ad, glif, acik, kosul) in enumerate(BADGES):
        cx = izgara_sol + (i % COLS) * COL_W + COL_W // 2
        cy = PAD + 150 + (i // COLS) * ROW_H

        sayfa.alpha_composite(rozet(glif, acik), (cx - BADGE // 2, cy))

        renk = PAL["textPrimary"] if acik else PAL["textMuted"]
        kutu = d.textbbox((0, 0), ad, font=ad_f)
        d.text((cx - (kutu[2] - kutu[0]) // 2, cy + BADGE + 12), ad,
               font=ad_f, fill=renk + (255,))

        kutu = d.textbbox((0, 0), kosul, font=not_f)
        d.text((cx - (kutu[2] - kutu[0]) // 2, cy + BADGE + 44), kosul,
               font=not_f, fill=PAL["textSecondary" if acik else "textMuted"] + (255,))

    cikti = os.path.join(DOCS, "badges.png")
    sayfa.convert("RGB").save(cikti)
    print("rozet kanit sayfasi -> %s (%dx%d)" % (cikti, w, h))


if __name__ == "__main__":
    main()
