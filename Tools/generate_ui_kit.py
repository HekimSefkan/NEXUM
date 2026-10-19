# -*- coding: utf-8 -*-
"""NEXUM UI kiti ureteci — butonlar, cerceveler, kontroller (9-slice).

NEDEN: Bugun her buton AYRI bir PNG ve cogunda metin GORSELIN ICINDE basili.
Sonuc: yeni bir buton eklemek yeni bir gorsel uretmek demek, ceviri imkansiz,
olculer tutarsiz (522x220, 300x64, 236x55 ...). Bu uretec tek bir 9-slice
cerceveden her olcude buton cikarir ve hicbir gorselde metin TASIMAZ.

KURALLAR
- 9-SLICE: her cerceve sabit bir tuvalde cizilir, kenar paylari manifest.json
  icinde yazilir. Unity tarafinda ayni paylar sprite border'ina gecirilir;
  TEK sprite her boyutta kullanilir.
- METIN YOK: tum yazilar TMP etiketi olarak gelir (ceviri ve %85 sigma kurali).
- RENK UYDURULMAZ: palet Assets/Data/Theme/NexumTheme.asset dosyasindan okunur.
- Her bilesen dort durumda uretilir: normal / pressed / disabled / selected.

CIKTI
    Tools/ui-kit/<varyant>/<bilesen>_<durum>.png
    Tools/ui-kit/manifest.json      (boyut + border + tip)

Kullanim (proje kokunden):
    python Tools/generate_ui_kit.py
"""
import json
import os
import re

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Tools", "ui-kit")
THEME = os.path.join(ROOT, "Assets", "Data", "Theme", "NexumTheme.asset")

SS = 4          # superornekleme carpani
CLEAR = (0, 0, 0, 0)


# ---------------------------------------------------------------- palet

def load_palette():
    """NexumTheme.asset icindeki renk rollerini okur (tek kaynak)."""
    s = open(THEME, encoding="utf-8").read()
    pal = {}
    blok = s[s.find("palette:"):s.find("typography:")]
    for m in re.finditer(r"(\w+): \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+), a: ([\d.e-]+)\}", blok):
        pal[m.group(1)] = tuple(int(round(float(x) * 255)) for x in m.groups()[1:4])
    eksik = {"primary", "primaryBright", "secondary", "danger", "success", "warning",
             "textPrimary", "textSecondary", "textOnLight", "panelBackground",
             "cardBackground"} - set(pal)
    if eksik:
        raise SystemExit("NexumTheme paletinde eksik rol: %s" % ", ".join(sorted(eksik)))
    return pal


PAL = load_palette()


def rgba(name, alpha=1.0):
    r, g, b = PAL[name]
    return (r, g, b, int(round(255 * alpha)))


def mix(c1, c2, t):
    return tuple(int(round(a + (b - a) * t)) for a, b in zip(c1, c2))


# ------------------------------------------------------------- sekiller

def body_mask(w, h, radius=0, chamfer=0):
    """Govde maskesi: yuvarlatilmis ya da pahli dikdortgen (L kanali)."""
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    if chamfer > 0:
        c = chamfer
        d.polygon([(c, 0), (w - c, 0), (w, c), (w, h - c),
                   (w - c, h), (c, h), (0, h - c), (0, c)], fill=255)
    elif radius > 0:
        d.rounded_rectangle([0, 0, w - 1, h - 1], radius=radius, fill=255)
    else:
        d.rectangle([0, 0, w - 1, h - 1], fill=255)
    return m


def inner_mask(w, h, radius, chamfer, inset):
    """Govdenin her kenardan <inset> birim iceri cekilmis hali.

    MinFilter ile asindirma yapmak cekirdek buyudukce cok yavas (57x57'ye
    kadar cikiyordu); ayni sonuc ic sekli dogrudan cizerek elde edilir.
    """
    if inset <= 0:
        return body_mask(w, h, radius, chamfer)

    iw, ih = w - 2 * inset, h - 2 * inset
    m = Image.new("L", (w, h), 0)
    if iw <= 0 or ih <= 0:
        return m
    ic = body_mask(iw, ih, max(0, radius - inset), max(0, chamfer - inset))
    m.paste(ic, (inset, inset))
    return m


def ring_mask(w, h, radius, chamfer, thickness):
    """Cerceve seridi maskesi."""
    dis = body_mask(w, h, radius, chamfer)
    ic = inner_mask(w, h, radius, chamfer, thickness)
    return Image.composite(Image.new("L", (w, h), 0), dis, ic)


def glow(mask, radius, strength):
    """Maskenin disina tasan yumusak parilti (L kanali)."""
    g = mask.filter(ImageFilter.GaussianBlur(radius))
    return g.point(lambda v: int(min(255, v * strength)))


# --------------------------------------------------------------- stiller
# Her varyant ayni bilesen ailesini FARKLI bir tasarim yorumuyla cizer.

VARIANTS = {
    "A": {
        "ad": "Cizgi",
        "aciklama": "Ince cizgi agirlikli analitik cihaz paneli: 2 birim kenarlik, "
                    "pahli koseler, duz koyu zemin, parilti yok denecek kadar az.",
        "corner": "chamfer", "corner_size": 10,
        "border": 2, "fill_alpha": 0.92, "glow": 0.0, "glow_radius": 0,
        "inner_line": False, "bezel": 0.0, "ticks": True,
    },
    "B": {
        "ad": "Cam",
        "aciklama": "Cam yuzey agirlikli: yuvarlatilmis koseler, yari saydam zemin, "
                    "ustte ic parilti seridi, disarida yumusak hale.",
        "corner": "radius", "corner_size": 20,
        "border": 3, "fill_alpha": 0.52, "glow": 1.0, "glow_radius": 11,
        "inner_line": True, "bezel": 0.0, "ticks": False,
    },
    "C": {
        "ad": "Metal",
        "aciklama": "Metal cerceve agirlikli: kalin pahli bezel, acik-koyu gecisli "
                    "kenarlik, ice cokmus koyu zemin.",
        "corner": "chamfer", "corner_size": 14,
        "border": 8, "fill_alpha": 1.0, "glow": 0.18, "glow_radius": 4,
        "inner_line": False, "bezel": 1.0, "ticks": False,
    },
}

# Durum -> (zemin rengi rolu, kenarlik rengi rolu, zemin parlaklik carpani, genel alfa)
STATES = {
    "normal":   dict(fill="cardBackground", edge="primary",       lift=1.00, alpha=1.00),
    "pressed":  dict(fill="cardBackground", edge="primaryBright", lift=0.72, alpha=1.00),
    "disabled": dict(fill="panelBackground", edge="textSecondary", lift=0.85, alpha=0.45),
    "selected": dict(fill="cardBackground", edge="primaryBright", lift=1.28, alpha=1.00),
}

# Rol -> kenarlik rengini degistiren bilesenler
ROLE_EDGE = {
    "primary": "primary",
    "secondary": "secondary",
    "ghost": "textSecondary",
    "destructive": "danger",
    "success": "success",
    "warning": "warning",
}


def frame(w, h, variant, state, role="primary", fill_role=None, ghost=False):
    """9-slice cerceve: kenar detayi KENAR SERIDINDE kalir, merkez duz."""
    v = VARIANTS[variant]
    st = STATES[state]

    W, H = w * SS, h * SS
    radius = v["corner_size"] * SS if v["corner"] == "radius" else 0
    chamfer = v["corner_size"] * SS if v["corner"] == "chamfer" else 0
    bw = max(1, int(v["border"] * SS))

    img = Image.new("RGBA", (W, H), CLEAR)

    govde = body_mask(W, H, radius, chamfer)
    kenar = ring_mask(W, H, radius, chamfer, bw)

    edge_role = ROLE_EDGE.get(role, "primary")
    edge_col = PAL[edge_role] if state != "disabled" else PAL["textSecondary"]
    if state in ("pressed", "selected"):
        edge_col = mix(edge_col, PAL["primaryBright"], 0.45)

    # --- dis parilti: yalnizca GOVDENIN DISINDA. Govdenin uzerine de dusseydi
    # hayalet (ghost) buton dolu gorunurdu.
    if v["glow"] > 0 and state != "disabled":
        g = glow(govde, v["glow_radius"] * SS, v["glow"])
        g = Image.composite(Image.new("L", (W, H), 0), g, govde)
        kat = Image.new("RGBA", (W, H), edge_col + (0,))
        kat.putalpha(g.point(lambda x: int(x * 0.55)))
        img = Image.alpha_composite(img, kat)

    # --- zemin
    fill_col = PAL[fill_role or st["fill"]]
    fill_col = tuple(min(255, int(c * st["lift"])) for c in fill_col)
    zemin_alpha = 0.0 if ghost else v["fill_alpha"]
    ic = inner_mask(W, H, radius, chamfer, bw)
    kat = Image.new("RGBA", (W, H), fill_col + (0,))
    kat.putalpha(ic.point(lambda x: int(x * zemin_alpha)))
    img = Image.alpha_composite(img, kat)

    # --- kenarlik
    kat = Image.new("RGBA", (W, H), edge_col + (0,))
    kat.putalpha(kenar)
    img = Image.alpha_composite(img, kat)

    # --- bezel: kenarligin ust yarisi acik, alt yarisi koyu (metal)
    if v["bezel"] > 0:
        ust = Image.new("L", (W, H), 0)
        ImageDraw.Draw(ust).rectangle([0, 0, W, H // 2], fill=255)
        acik = Image.new("RGBA", (W, H), mix(edge_col, (255, 255, 255), 0.55) + (0,))
        acik.putalpha(Image.composite(kenar, Image.new("L", (W, H), 0), ust)
                      .point(lambda x: int(x * 0.75 * v["bezel"])))
        img = Image.alpha_composite(img, acik)

        koyu = Image.new("RGBA", (W, H), mix(edge_col, (0, 0, 0), 0.55) + (0,))
        koyu.putalpha(Image.composite(Image.new("L", (W, H), 0), kenar, ust)
                      .point(lambda x: int(x * 0.65 * v["bezel"])))
        img = Image.alpha_composite(img, koyu)

    # --- ic parilti seridi: UST KENAR SERIDINDE kalir (9-slice'ta yatay gerilir)
    if v["inner_line"] and state != "disabled":
        serit = Image.new("L", (W, H), 0)
        kalin = max(2, int(bw * 1.1))
        ImageDraw.Draw(serit).rectangle([0, bw, W, bw + kalin], fill=255)
        serit = Image.composite(serit, Image.new("L", (W, H), 0), ic)
        kat = Image.new("RGBA", (W, H), PAL["primaryBright"] + (0,))
        kat.putalpha(serit.point(lambda x: int(x * 0.70)))
        img = Image.alpha_composite(img, kat)

    # --- kose isaretleri: cihaz paneli dili. 9-slice'ta KOSE karolarinda
    # kaldiklari icin gerilmezler.
    if v.get("ticks") and state != "disabled":
        d = ImageDraw.Draw(img)
        uz = int(v["corner_size"] * SS * 1.9)
        kal = max(2, bw)
        ic_renk = mix(edge_col, PAL["primaryBright"], 0.5) + (235,)
        pay = bw * 3
        for sx, sy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
            x = pay if sx > 0 else W - pay
            y = pay if sy > 0 else H - pay
            d.line([(x, y), (x + sx * uz, y)], fill=ic_renk, width=kal)
            d.line([(x, y), (x, y + sy * uz)], fill=ic_renk, width=kal)

    img = img.resize((w, h), Image.LANCZOS)
    if st["alpha"] < 1.0:
        a = img.split()[3].point(lambda x: int(x * st["alpha"]))
        img.putalpha(a)
    return img


def border_payi(variant, w=None, h=None):
    """9-slice kenar payi: kose geometrisi + kenarlik + parilti.

    Pay sprite'in YARISINI gecemez (Unity de gecemez): ince cubuklarda
    (ilerleme cubugu, kaydirici izi) otomatik kisilir.
    """
    v = VARIANTS[variant]
    pay = int(v["corner_size"] + v["border"] + v["glow_radius"] + 2)
    if w is not None:
        pay = min(pay, (w - 1) // 2)
    if h is not None:
        pay = min(pay, (h - 1) // 2)
    return max(0, pay)


# ------------------------------------------------------- sabit sekiller

def icon_button(size, variant, state):
    """Yuvarlak ikon butonu (geri, duraklat, kapat, ses). 9-slice DEGIL."""
    v = VARIANTS[variant]
    st = STATES[state]
    S = size * SS
    img = Image.new("RGBA", (S, S), CLEAR)
    d = ImageDraw.Draw(img)

    edge = PAL["primary"] if state != "disabled" else PAL["textSecondary"]
    if state in ("pressed", "selected"):
        edge = PAL["primaryBright"]
    bw = max(2, int(v["border"] * SS))

    fill = tuple(min(255, int(c * st["lift"])) for c in PAL["cardBackground"])
    d.ellipse([0, 0, S - 1, S - 1], fill=fill + (int(255 * v["fill_alpha"]),))
    d.ellipse([0, 0, S - 1, S - 1], outline=edge + (255,), width=bw)
    if v["inner_line"] and state != "disabled":
        d.arc([bw * 2, bw * 2, S - 1 - bw * 2, S - 1 - bw * 2], 200, 340,
              fill=PAL["primaryBright"] + (110,), width=max(1, bw // 2))

    img = img.resize((size, size), Image.LANCZOS)
    if st["alpha"] < 1.0:
        img.putalpha(img.split()[3].point(lambda x: int(x * st["alpha"])))
    return img


def star(size, dolu, variant, state):
    S = size * SS
    img = Image.new("RGBA", (S, S), CLEAR)
    d = ImageDraw.Draw(img)
    import math
    c = S / 2.0
    dis, ic = S * 0.46, S * 0.19
    noktalar = []
    for i in range(10):
        a = math.radians(-90 + i * 36)
        r = dis if i % 2 == 0 else ic
        noktalar.append((c + r * math.cos(a), c + r * math.sin(a)))

    renk = PAL["warning"] if dolu else PAL["textSecondary"]
    if state == "disabled":
        renk = PAL["textSecondary"]
    d.polygon(noktalar, fill=(renk + (235,)) if dolu else CLEAR,
              outline=renk + (255,), width=max(2, int(VARIANTS[variant]["border"] * SS * 0.7)))
    img = img.resize((size, size), Image.LANCZOS)
    if STATES[state]["alpha"] < 1.0:
        img.putalpha(img.split()[3].point(lambda x: int(x * STATES[state]["alpha"])))
    return img


def level_node(size, durum, variant):
    """Bolum dugumu: kilitli / acik / tamamlanmis."""
    v = VARIANTS[variant]
    S = size * SS
    img = Image.new("RGBA", (S, S), CLEAR)
    d = ImageDraw.Draw(img)
    chamfer = int(v["corner_size"] * SS * 0.9)

    renk = {"locked": PAL["textSecondary"],
            "open": PAL["primary"],
            "done": PAL["success"]}[durum]
    zemin = PAL["panelBackground"] if durum == "locked" else PAL["cardBackground"]

    m = body_mask(S, S, 0, chamfer)
    kat = Image.new("RGBA", (S, S), zemin + (0,))
    kat.putalpha(m.point(lambda x: int(x * (0.6 if durum == "locked" else v["fill_alpha"]))))
    img = Image.alpha_composite(img, kat)

    kenar = ring_mask(S, S, 0, chamfer, max(2, int(v["border"] * SS)))
    kat = Image.new("RGBA", (S, S), renk + (0,))
    kat.putalpha(kenar)
    img = Image.alpha_composite(img, kat)

    if durum == "done":
        d = ImageDraw.Draw(img)
        d.line([(S * 0.30, S * 0.52), (S * 0.45, S * 0.68), (S * 0.72, S * 0.34)],
               fill=PAL["success"] + (255,), width=max(3, int(S * 0.055)))
    return img.resize((size, size), Image.LANCZOS)


def toggle(w, h, variant, acik, state):
    v = VARIANTS[variant]
    st = STATES[state]
    W, H = w * SS, h * SS
    img = Image.new("RGBA", (W, H), CLEAR)
    r = H // 2
    renk = PAL["success"] if acik else PAL["textSecondary"]
    if state == "disabled":
        renk = PAL["textSecondary"]

    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, W - 1, H - 1], radius=r,
                        fill=PAL["cardBackground"] + (int(255 * v["fill_alpha"]),),
                        outline=renk + (255,), width=max(2, int(v["border"] * SS)))
    kx = W - r if acik else r
    d.ellipse([kx - r * 0.66, r - r * 0.66, kx + r * 0.66, r + r * 0.66],
              fill=renk + (255,))
    img = img.resize((w, h), Image.LANCZOS)
    if st["alpha"] < 1.0:
        img.putalpha(img.split()[3].point(lambda x: int(x * st["alpha"])))
    return img


def ring(size, variant, kalinlik_orani=0.105):
    """Ilerleme halkasi — mevcut ring_progress.png'nin varyant dili."""
    v = VARIANTS[variant]
    S = size * SS
    img = Image.new("RGBA", (S, S), CLEAR)
    d = ImageDraw.Draw(img)
    c = S / 2.0
    dis = S * 0.47
    kal = S * kalinlik_orani * (1.6 if v["bezel"] > 0 else 1.0)
    d.ellipse([c - dis, c - dis, c + dis, c + dis], fill=(255, 255, 255, 255))
    d.ellipse([c - dis + kal, c - dis + kal, c + dis - kal, c + dis - kal], fill=CLEAR)
    return img.resize((size, size), Image.LANCZOS)


def lock_overlay(size, variant):
    """Kilit katmani — mevcut lock_icon.png'nin varyant dili."""
    S = size * SS
    img = Image.new("RGBA", (S, S), CLEAR)
    d = ImageDraw.Draw(img)
    bw = max(2, int(VARIANTS[variant]["border"] * SS))
    gw, gh = S * 0.60, S * 0.42
    gx, gy = (S - gw) / 2, S * 0.50
    d.rounded_rectangle([gx, gy, gx + gw, gy + gh],
                        radius=S * 0.07, outline=(255, 255, 255, 255), width=bw)
    kw = S * 0.34
    d.arc([(S - kw) / 2, gy - S * 0.30, (S + kw) / 2, gy + S * 0.10],
          180, 360, fill=(255, 255, 255, 255), width=bw)
    return img.resize((size, size), Image.LANCZOS)


# -------------------------------------------------------- bilesen tablosu
# (ad, tip, genislik, yukseklik, rol/ek)
#   tip "frame" -> 9-slice; digerleri sabit sekil
COMPONENTS = [
    ("btn_primary",     "frame", 320, 96,  dict(role="primary")),
    ("btn_secondary",   "frame", 280, 80,  dict(role="secondary")),
    ("btn_ghost",       "frame", 280, 80,  dict(role="ghost", ghost=True)),
    ("btn_destructive", "frame", 280, 80,  dict(role="destructive")),
    ("chip",            "frame", 180, 56,  dict(role="secondary")),
    ("panel_lg",        "frame", 420, 300, dict(role="primary", fill_role="panelBackground")),
    ("panel_md",        "frame", 300, 220, dict(role="primary", fill_role="panelBackground")),
    ("panel_sm",        "frame", 220, 150, dict(role="primary", fill_role="panelBackground")),
    ("modal",           "frame", 360, 260, dict(role="primaryBright", fill_role="panelBackground")),
    ("tab",             "frame", 200, 72,  dict(role="secondary")),
    ("list_row",        "frame", 420, 92,  dict(role="secondary", fill_role="panelBackground")),
    ("progress_track",  "frame", 320, 36,  dict(role="ghost", fill_role="panelBackground")),
    ("progress_fill",   "frame", 320, 36,  dict(role="primary", fill_role="primary")),
    ("toast",           "frame", 420, 84,  dict(role="primaryBright", fill_role="panelBackground")),
    ("tooltip",         "frame", 320, 120, dict(role="secondary", fill_role="panelBackground")),
    ("quiz_option",     "frame", 400, 88,  dict(role="secondary")),
    ("badge_frame",     "frame", 160, 160, dict(role="warning", fill_role="panelBackground")),
    ("avatar_frame",    "frame", 200, 200, dict(role="primary", fill_role="cardBackground")),
    ("input_field",     "frame", 400, 88,  dict(role="ghost", fill_role="cardBackground")),
]

ICONS = [
    ("icon_button", "icon", 96),
    ("ring_progress", "ring", 256),
    ("lock_overlay", "lock", 128),
]


def uret():
    manifest = {"varyantlar": {}, "not": "border = [sol, alt, sag, ust] (Unity spriteBorder sirasi)"}

    for vk, v in VARIANTS.items():
        klasor = os.path.join(OUT, vk)
        os.makedirs(klasor, exist_ok=True)
        kayit = {"ad": v["ad"], "aciklama": v["aciklama"], "sprites": {}}

        for ad, tip, w, h, ek in COMPONENTS:
            for durum in STATES:
                img = frame(w, h, vk, durum, **ek)
                dosya = "%s_%s.png" % (ad, durum)
                img.save(os.path.join(klasor, dosya))
                pay = border_payi(vk, w, h)
                kayit["sprites"][dosya] = {
                    "tip": "9slice", "boyut": [w, h], "border": [pay, pay, pay, pay],
                }

        for durum in STATES:
            img = icon_button(96, vk, durum)
            dosya = "icon_button_%s.png" % durum
            img.save(os.path.join(klasor, dosya))
            kayit["sprites"][dosya] = {"tip": "sabit", "boyut": [96, 96], "border": [0, 0, 0, 0]}

            for dolu, etiket in ((False, "empty"), (True, "full")):
                img = star(72, dolu, vk, durum)
                dosya = "star_%s_%s.png" % (etiket, durum)
                img.save(os.path.join(klasor, dosya))
                kayit["sprites"][dosya] = {"tip": "sabit", "boyut": [72, 72], "border": [0, 0, 0, 0]}

            for acik, etiket in ((False, "off"), (True, "on")):
                img = toggle(120, 56, vk, acik, durum)
                dosya = "toggle_%s_%s.png" % (etiket, durum)
                img.save(os.path.join(klasor, dosya))
                kayit["sprites"][dosya] = {"tip": "sabit", "boyut": [120, 56], "border": [0, 0, 0, 0]}

        for durum in ("locked", "open", "done"):
            img = level_node(140, durum, vk)
            dosya = "level_node_%s.png" % durum
            img.save(os.path.join(klasor, dosya))
            kayit["sprites"][dosya] = {"tip": "sabit", "boyut": [140, 140], "border": [0, 0, 0, 0]}

        img = ring(256, vk)
        img.save(os.path.join(klasor, "ring_progress.png"))
        kayit["sprites"]["ring_progress.png"] = {"tip": "sabit", "boyut": [256, 256], "border": [0, 0, 0, 0]}

        img = lock_overlay(128, vk)
        img.save(os.path.join(klasor, "lock_overlay.png"))
        kayit["sprites"]["lock_overlay.png"] = {"tip": "sabit", "boyut": [128, 128], "border": [0, 0, 0, 0]}

        # Kaydirici: iz + dolgu + topuz
        img = frame(320, 24, vk, "normal", role="ghost", fill_role="panelBackground")
        img.save(os.path.join(klasor, "slider_track.png"))
        spay = border_payi(vk, 320, 24)
        kayit["sprites"]["slider_track.png"] = {"tip": "9slice", "boyut": [320, 24], "border": [spay] * 4}
        img = frame(320, 24, vk, "selected", role="primary", fill_role="primary")
        img.save(os.path.join(klasor, "slider_fill.png"))
        kayit["sprites"]["slider_fill.png"] = {"tip": "9slice", "boyut": [320, 24], "border": [spay] * 4}
        img = icon_button(56, vk, "normal")
        img.save(os.path.join(klasor, "slider_knob.png"))
        kayit["sprites"]["slider_knob.png"] = {"tip": "sabit", "boyut": [56, 56], "border": [0, 0, 0, 0]}

        manifest["varyantlar"][vk] = kayit
        print("%s (%s): %d sprite -> %s" % (vk, v["ad"], len(kayit["sprites"]), klasor))

    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)
    print("manifest: %s" % os.path.join(OUT, "manifest.json"))
    return manifest


if __name__ == "__main__":
    uret()
