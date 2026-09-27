"""NEXUM uygulama ikonu üreteci.

Oyunun HUD'undaki azot karosunu (Assets/Art/Figures/NEXUM_logo-.png) temel alır:
koyu mavi karo, camgöbeği kenarlık, ortada büyük "N", sol üstte "7", altta "14.007".

Kullanım (proje kökünden):
    python Tools/generate_icons.py

Gereksinim: Pillow (pip install Pillow)

Her çıktı hedef çözünürlüğünde vektör olarak çizilir; hiçbir görsel büyütülmez.
Kenar yumuşatma için içeride SS kat süperörnekleme yapılıp LANCZOS ile küçültülür.

Renk / metin değişikliği için yalnızca aşağıdaki PARAMS sözlüğü düzenlenir.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PARAMS = {
    # --- metin ---
    "letter": "N",
    "top_text": "7",
    "bottom_text": "14.007",
    "font": "Assets/Fonts/Orbitron-Bold.ttf",

    # --- renkler ---
    "tile_top": (0x0E, 0x3B, 0x63),        # karo gradyanı üst
    "tile_bottom": (0x0A, 0x29, 0x49),     # karo gradyanı alt
    "border": (0x35, 0xD0, 0xF0),          # camgöbeği kenarlık ve dış parlama
    "letter_color": (0xE8, 0xFB, 0xFF),    # büyük harf
    "small_text": (0x8F, 0xD8, 0xFF),      # "7" ve "14.007"
    "bg_inner": (0x0A, 0x1B, 0x33),        # arka plan radyal gradyan merkez
    "bg_outer": (0x06, 0x10, 0x20),        # arka plan radyal gradyan kenar

    # --- yerleşim (kenar uzunluğuna oran) ---
    "tile_inset": 0.055,        # karonun kenardan boşluğu
    "corner_radius": 0.20,      # karo köşe yarıçapı (karo kenarına oran)
    "border_width": 0.015,      # kenarlık kalınlığı
    "glow_blur": 0.030,         # dış parlama yumuşaklığı
    "letter_size": 0.56,        # büyük harf punto
    "letter_shift_y": -0.015,   # harfin dikey ince ayarı
    "top_text_size": 0.17,
    "bottom_text_size": 0.135,
}

SS = 4          # süperörnekleme katsayısı
SAFE_RATIO = 288.0 / 432.0   # Android adaptive icon güvenli alanı


def font_path():
    p = os.path.join(ROOT, PARAMS["font"])
    if os.path.exists(p):
        return p
    raise SystemExit("Font bulunamadı: " + p)


def load_font(size):
    return ImageFont.truetype(font_path(), max(1, int(size)))


def vertical_gradient(size, top, bottom):
    """Dikey gradyan (tek piksel genişliğinde üretip yatayda genişletir)."""
    w, h = size
    strip = Image.new("RGB", (1, h))
    px = strip.load()
    for y in range(h):
        t = y / max(1, h - 1)
        px[0, y] = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
    return strip.resize((w, h), Image.NEAREST)


def radial_gradient(size, inner, outer):
    w, h = size
    img = Image.new("RGB", (w, h), outer)
    px = img.load()
    cx, cy = (w - 1) / 2.0, (h - 1) / 2.0
    rmax = (cx ** 2 + cy ** 2) ** 0.5
    for y in range(h):
        for x in range(w):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5 / rmax
            d = min(1.0, d)
            px[x, y] = tuple(int(inner[i] + (outer[i] - inner[i]) * d) for i in range(3))
    return img


def centered_text(draw, box, text, font, fill):
    """Metni verilen kutuya gerçek sınırlarına göre ortalar."""
    x0, y0, x1, y1 = box
    l, t, r, b = draw.textbbox((0, 0), text, font=font)
    x = x0 + (x1 - x0 - (r - l)) / 2.0 - l
    y = y0 + (y1 - y0 - (b - t)) / 2.0 - t
    draw.text((x, y), text, font=font, fill=fill)


def draw_tile(size, with_background, circle=False):
    """Karoyu (kenarlık + harf + metinler) çizer. RGBA döner, SS katında.

    circle=True: Android "round" ikonu için karo yerine daire çizilir; daire
    maskesi kare karonun köşelerini kesmesin diye şekil baştan yuvarlak olur.
    """
    s = size * SS
    p = PARAMS

    layer = Image.new("RGBA", (s, s), (0, 0, 0, 0))

    inset = p["tile_inset"] * s
    box = (inset, inset, s - inset, s - inset)
    tile_side = box[2] - box[0]
    radius = p["corner_radius"] * tile_side

    # gövde maskesi
    mask = Image.new("L", (s, s), 0)
    if circle:
        ImageDraw.Draw(mask).ellipse(box, fill=255)
    else:
        ImageDraw.Draw(mask).rounded_rectangle(box, radius=radius, fill=255)

    # dış parlama: maskenin bulanık hâli, camgöbeği
    glow = Image.new("RGBA", (s, s), p["border"] + (0,))
    glow_alpha = mask.filter(ImageFilter.GaussianBlur(p["glow_blur"] * s))
    glow.putalpha(glow_alpha.point(lambda v: int(v * 0.55)))
    layer = Image.alpha_composite(layer, glow)

    # karo gövdesi: dikey gradyan
    body = vertical_gradient((s, s), p["tile_top"], p["tile_bottom"]).convert("RGBA")
    body.putalpha(mask)
    layer = Image.alpha_composite(layer, body)

    # kenarlık
    border_layer = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    bd = ImageDraw.Draw(border_layer)
    width = max(1, int(p["border_width"] * s))
    if circle:
        bd.ellipse(box, outline=p["border"] + (255,), width=width)
    else:
        bd.rounded_rectangle(box, radius=radius, outline=p["border"] + (255,), width=width)
    layer = Image.alpha_composite(layer, border_layer)

    # büyük harf: önce parlama, sonra harf
    # Dairede kenarlar daraldığı için harf küçültülür, "7" ve kütle numarasına yer kalsın
    font_big = load_font(p["letter_size"] * (0.80 if circle else 1.0) * s)
    letter_box = (box[0], box[1] + p["letter_shift_y"] * s, box[2], box[3])

    halo = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    centered_text(ImageDraw.Draw(halo), letter_box, p["letter"], font_big, p["border"] + (200,))
    halo = halo.filter(ImageFilter.GaussianBlur(0.018 * s))
    layer = Image.alpha_composite(layer, halo)

    text_layer = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(text_layer)
    centered_text(d, letter_box, p["letter"], font_big, p["letter_color"] + (255,))

    # sol üst "7" — dairede kenar eğrildiği için içeri alınır
    font_top = load_font(p["top_text_size"] * s)
    pad = (0.16 if circle else 0.055) * tile_side
    d.text((box[0] + pad, box[1] + pad * (1.0 if circle else 0.7)),
           p["top_text"], font=font_top, fill=p["small_text"] + (255,))

    # alt kütle numarası
    font_bottom = load_font(p["bottom_text_size"] * s)
    l, t, r, b = d.textbbox((0, 0), p["bottom_text"], font=font_bottom)
    bx = box[0] + (tile_side - (r - l)) / 2.0 - l
    bottom_pad = (0.13 if circle else 0.055) * tile_side
    by = box[3] - bottom_pad * 1.2 - (b - t) - t
    d.text((bx, by), p["bottom_text"], font=font_bottom, fill=p["small_text"] + (255,))

    layer = Image.alpha_composite(layer, text_layer)

    if with_background:
        bg = radial_gradient((size, size), PARAMS["bg_inner"], PARAMS["bg_outer"]).resize((s, s), Image.BILINEAR)
        bg = bg.convert("RGBA")
        layer = Image.alpha_composite(bg, layer)

    return layer


def render(size, with_background):
    return draw_tile(size, with_background).resize((size, size), Image.LANCZOS)


def save(img, path, opaque):
    full = os.path.join(ROOT, path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    if opaque:
        flat = Image.new("RGB", img.size, PARAMS["bg_outer"])
        flat.paste(img, (0, 0), img)
        flat.save(full, "PNG")
        mode = "opak (RGB)"
    else:
        img.save(full, "PNG")
        mode = "şeffaf (RGBA)"
    kb = os.path.getsize(full) / 1024.0
    print("  %-52s %4dx%-4d %-14s %6.1f KB" % (path, img.size[0], img.size[1], mode, kb))


def foreground(size):
    """Adaptive icon ön planı: içerik ortadaki güvenli alana sığar, zemin şeffaf."""
    inner = int(round(size * SAFE_RATIO))
    art = render(inner, with_background=False)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    off = (size - inner) // 2
    canvas.paste(art, (off, off), art)
    return canvas


def background(size):
    """Adaptive icon arka planı: yalnızca gradyan zemin, opak."""
    return radial_gradient((size, size), PARAMS["bg_inner"], PARAMS["bg_outer"]).convert("RGBA")


def round_icon(size):
    """Android round ikonu: şekil baştan daire çizilir, dışı şeffaf kalır."""
    return draw_tile(size, with_background=False, circle=True).resize((size, size), Image.LANCZOS)


def main():
    print("NEXUM ikon üreteci - font:", PARAMS["font"])
    print("%-54s %-10s %-14s %s" % ("dosya", "boyut", "şeffaflık", "ağırlık"))

    save(render(1024, True), "Assets/Art/Icons/icon_master_1024.png", opaque=True)
    save(foreground(432), "Assets/Art/Icons/icon_adaptive_foreground_432.png", opaque=False)
    save(background(432), "Assets/Art/Icons/icon_adaptive_background_432.png", opaque=True)
    save(render(512, True), "Assets/Art/Icons/icon_legacy_512.png", opaque=True)
    save(round_icon(512), "Assets/Art/Icons/icon_round_512.png", opaque=False)

    # Mağaza görseli build'e girmemeli: Assets dışında duruyor
    save(render(512, True), "Tools/store/icon_store_512.png", opaque=True)

    # Küçük boyutta okunaklılık önizlemesi (commit edilmez, Tools/store altında)
    master = Image.open(os.path.join(ROOT, "Assets/Art/Icons/icon_master_1024.png"))
    for px in (48, 72, 96):
        master.resize((px, px), Image.LANCZOS).save(
            os.path.join(ROOT, "Tools/store/preview_%d.png" % px), "PNG")
    print("  önizlemeler: Tools/store/preview_48.png, _72, _96")


if __name__ == "__main__":
    main()
