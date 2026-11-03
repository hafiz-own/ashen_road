"""Generates the procedural pixel art for Ashen Road (tiles, backgrounds, disaster fronts, props, UI).

Run from the project root:  python3 Tools/gen_art.py
Everything is written to Assets/_Project/Art/Generated. Characters come from Kenney's Tiny Dungeon (CC0).
"""
import math
import os
import random

from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Project", "Art", "Generated")


def out(*parts):
    path = os.path.join(ROOT, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    return path


def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def shade(c, f):
    return (max(0, min(255, int(c[0] * f))), max(0, min(255, int(c[1] * f))), max(0, min(255, int(c[2] * f))), c[3])


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(4))


# ---------------------------------------------------------------- themes
# key: (cap, fill, accent, plank, sky_top, sky_bottom, far, near, style)
THEMES = {
    "village":   ("#5a8f3c", "#6b4a32", "#4a3222", "#8a5a34", "#3a1f2b", "#d0603a", "#5a2a2a", "#2a1418", "houses"),
    "bridge":    ("#7a5634", "#5a5a66", "#3c3c46", "#8a5a34", "#2b2440", "#b06a5a", "#3a3450", "#1e1a2c", "arches"),
    "fields":    ("#4f7a46", "#4a3c30", "#30261e", "#7a5a3a", "#3a4a5a", "#8aa0a8", "#3a4a48", "#1e2a28", "trees"),
    "graveyard": ("#3f5a3a", "#3e3a3a", "#28252a", "#6a5a4a", "#1a1a2a", "#5a4a6a", "#2a2a3a", "#141420", "graves"),
    "mines":     ("#6a5a4a", "#4a3e36", "#2e2620", "#7a5230", "#120e0c", "#3a2a20", "#2a201a", "#16100c", "beams"),
    "cliffs":    ("#5f7a5a", "#5c5e66", "#3a3c44", "#7a6a5a", "#1c2234", "#4a5a7a", "#2a3448", "#141a26", "peaks"),
    "city":      ("#7a6a5a", "#6a5040", "#40302a", "#8a6040", "#2a1410", "#c0502a", "#4a2018", "#200c0a", "ruins"),
    "castle":    ("#8a8a96", "#5a5a6a", "#3a3a48", "#7a5a40", "#1a1020", "#7a3040", "#3a2030", "#1a0c16", "towers"),
}


def noise_fill(img, box, base, rnd, amount=0.12, specks=0.18):
    x0, y0, x1, y1 = box
    px = img.load()
    for y in range(y0, y1):
        for x in range(x0, x1):
            f = 1.0 + (rnd.random() - 0.5) * amount
            c = shade(base, f)
            if rnd.random() < specks:
                c = shade(base, 0.82 if rnd.random() < 0.6 else 1.15)
            px[x, y] = c


def make_tiles(name, theme, rnd):
    cap, fill, accent, plank = (hexc(theme[i]) for i in range(4))
    stone = name in ("bridge", "graveyard", "mines", "cliffs", "city", "castle")
    # fill tile
    fill_img = Image.new("RGBA", (16, 16))
    noise_fill(fill_img, (0, 0, 16, 16), fill, rnd)
    d = ImageDraw.Draw(fill_img)
    if stone:
        for row in range(0, 16, 8):
            off = 0 if (row // 8) % 2 == 0 else 4
            d.line([(0, row), (15, row)], fill=accent)
            for x in range(off, 16, 8):
                d.line([(x, row), (x, row + 7)], fill=accent)
    else:
        for _ in range(3):
            x, y = rnd.randrange(1, 14), rnd.randrange(1, 14)
            d.rectangle([x, y, x + 1, y + 1], fill=shade(fill, 0.7))
    fill_img.save(out("Tiles", f"{name}_fill.png"))

    # top tile: fill + cap with ragged edge
    top = fill_img.copy()
    px = top.load()
    for x in range(16):
        depth = 4 + (1 if rnd.random() < 0.4 else 0)
        for y in range(depth):
            px[x, y] = shade(cap, 1.15 if y == 0 else (1.0 if y < depth - 1 else 0.8))
        if not stone and rnd.random() < 0.3:
            px[x, depth] = shade(cap, 0.75)
    top.save(out("Tiles", f"{name}_top.png"))

    # one-way platform: plank strip on transparent tile
    plat = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    noise_fill(plat, (0, 0, 16, 5), plank, rnd, specks=0.1)
    d = ImageDraw.Draw(plat)
    d.line([(0, 0), (15, 0)], fill=shade(plank, 1.25))
    d.line([(0, 4), (15, 4)], fill=shade(plank, 0.6))
    d.point([(2, 2), (13, 2)], fill=shade(plank, 0.5))
    plat.save(out("Tiles", f"{name}_platform.png"))


# ---------------------------------------------------------------- backgrounds
def silhouette(style, w, h, color, rnd, near):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    base = h - (30 if near else 55)
    # rolling ground line
    pts = [(0, h)]
    for x in range(0, w + 8, 8):
        pts.append((x, base + int(6 * math.sin(x * 0.03 + (1.3 if near else 0)))))
    pts.append((w, h))
    d.polygon(pts, fill=color)
    x = 0
    while x < w:
        step = rnd.randint(30, 70) if near else rnd.randint(40, 90)
        hgt = rnd.randint(25, 60) if near else rnd.randint(35, 80)
        gx = x + rnd.randint(0, 10)
        if style == "houses":
            bw = rnd.randint(18, 30)
            d.rectangle([gx, base - hgt, gx + bw, base + 5], fill=color)
            d.polygon([(gx - 3, base - hgt), (gx + bw // 2, base - hgt - 14), (gx + bw + 3, base - hgt)], fill=color)
            if rnd.random() < 0.5:  # broken roof gap
                d.rectangle([gx + bw // 3, base - hgt - 6, gx + bw // 3 + 4, base - hgt + 4], fill=(0, 0, 0, 0))
        elif style == "arches":
            aw = rnd.randint(30, 50)
            d.rectangle([gx, base - hgt, gx + aw, base - hgt + 8], fill=color)
            d.rectangle([gx, base - hgt, gx + 6, base + 5], fill=color)
            d.rectangle([gx + aw - 6, base - hgt, gx + aw, base + 5], fill=color)
        elif style == "trees":
            d.rectangle([gx + 6, base - hgt // 2, gx + 9, base + 5], fill=color)
            d.ellipse([gx - 4, base - hgt, gx + 20, base - hgt // 2 + 6], fill=color)
        elif style == "graves":
            if rnd.random() < 0.5:
                d.rectangle([gx + 5, base - 26, gx + 8, base + 2], fill=color)
                d.rectangle([gx, base - 20, gx + 13, base - 17], fill=color)
            else:
                d.rounded_rectangle([gx, base - 18, gx + 12, base + 2], radius=5, fill=color)
            if rnd.random() < 0.3:
                d.polygon([(gx + 20, base), (gx + 26, base - hgt - 10), (gx + 32, base)], fill=color)
        elif style == "beams":
            d.rectangle([gx, 0, gx + 5, base + 5], fill=color)
            by = 10 + rnd.randint(0, 20)
            d.rectangle([gx - 10, by, gx + 30, by + 6], fill=color)
        elif style == "peaks":
            pw = rnd.randint(40, 80)
            d.polygon([(gx - pw // 2, base + 5), (gx, base - hgt - 20), (gx + pw // 2, base + 5)], fill=color)
        elif style == "ruins":
            bw = rnd.randint(14, 26)
            top = base - hgt - 10
            d.rectangle([gx, top, gx + bw, base + 5], fill=color)
            for k in range(3):  # jagged broken top
                kx = gx + rnd.randint(0, bw - 4)
                d.rectangle([kx, top - rnd.randint(2, 10), kx + 3, top], fill=color)
            for wy in range(top + 6, base - 4, 9):
                for wx in range(gx + 3, gx + bw - 3, 6):
                    if rnd.random() < 0.5:
                        d.rectangle([wx, wy, wx + 2, wy + 3], fill=(0, 0, 0, 0))
        elif style == "towers":
            tw = rnd.randint(14, 22)
            top = base - hgt - 25
            d.rectangle([gx, top, gx + tw, base + 5], fill=color)
            for k in range(0, tw, 5):
                d.rectangle([gx + k, top - 4, gx + k + 2, top], fill=color)
        x += step
    return img


def make_backgrounds(name, theme, rnd):
    sky_top, sky_bot, far, near = hexc(theme[4]), hexc(theme[5]), hexc(theme[6]), hexc(theme[7])
    sky = Image.new("RGBA", (4, 180))
    for y in range(180):
        t = y / 179
        sky.putpixel((0, y), mix(sky_top, sky_bot, t))
        for x in range(1, 4):
            sky.putpixel((x, y), sky.getpixel((0, y)))
    sky.save(out("Backgrounds", f"{name}_sky.png"))
    silhouette(theme[8], 480, 180, far, rnd, False).save(out("Backgrounds", f"{name}_far.png"))
    silhouette(theme[8], 480, 180, near, rnd, True).save(out("Backgrounds", f"{name}_near.png"))


# ---------------------------------------------------------------- disaster fronts
FRONTS = {
    "fire":      ("#ff5a1a", "#ffd23a", "#7a1a0a"),
    "collapse":  ("#6a5a4a", "#a08a70", "#2a2018"),
    "flood":     ("#2a6aa0", "#8ad0f0", "#0a2a4a"),
    "fissure":   ("#5a2a1a", "#ff7a2a", "#1a0a08"),
    "cavein":    ("#5a4a3a", "#8a7a6a", "#1a1410"),
    "lightning": ("#3a3a7a", "#e0e8ff", "#10102a"),
    "firedebris": ("#e04a1a", "#ffb030", "#3a1a10"),
    "crumble":   ("#6a6a7a", "#b0b0c0", "#2a2a34"),
}


def make_front(name, cols, rnd):
    main, bright, dark = (hexc(c) for c in cols)
    w, h = 48, 192
    for frame in range(3):
        img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        px = img.load()
        for y in range(h):
            # ragged right edge that moves per frame
            edge = int(w - 10 + 6 * math.sin(y * 0.18 + frame * 2.1) + 3 * math.sin(y * 0.51 + frame))
            for x in range(w):
                if x > edge:
                    continue
                t = x / max(1, edge)
                c = mix(dark, main, t)
                if x > edge - 4:
                    c = bright
                elif rnd.random() < 0.06:
                    c = mix(main, bright, rnd.random())
                if name == "flood":
                    if (y + frame * 3) % 12 < 2:
                        c = mix(c, bright, 0.5)
                if name in ("collapse", "cavein", "crumble", "firedebris") and rnd.random() < 0.08:
                    c = shade(c, 0.6)
                px[x, y] = c
        if name == "lightning":
            d = ImageDraw.Draw(img)
            x = w - 14
            for y in range(0, h, 8):
                nx = x + rnd.randint(-6, 6)
                d.line([(x, y), (nx, y + 8)], fill=bright, width=2)
                x = max(4, min(w - 6, nx))
        img.save(out("Fronts", f"front_{name}_{frame}.png"))


# ---------------------------------------------------------------- props and effects
def make_props(rnd):
    # spikes
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for i in range(4):
        x = i * 4
        d.polygon([(x, 15), (x + 2, 6), (x + 3, 15)], fill=hexc("#c0c0d0"))
        d.line([(x + 2, 6), (x + 2, 14)], fill=hexc("#ffffff"))
    d.rectangle([0, 14, 15, 15], fill=hexc("#505060"))
    img.save(out("Props", "spikes.png"))

    # fire patch: base embers + 3 flame frames
    base = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(base)
    d.rectangle([0, 13, 15, 15], fill=hexc("#3a2018"))
    for x in range(0, 16, 3):
        d.point((x, 13), fill=hexc("#ff7a2a"))
    base.save(out("Props", "fire_base.png"))
    for f in range(3):
        img = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
        px = img.load()
        for x in range(16):
            hgt = 14 + int(8 * abs(math.sin(x * 0.7 + f * 1.9))) + rnd.randint(0, 4)
            for y in range(32 - hgt, 32):
                t = (32 - y) / hgt
                c = hexc("#fff07a") if t < 0.25 else (hexc("#ffa020") if t < 0.6 else hexc("#e03a10", 230))
                if rnd.random() < 0.08:
                    continue
                px[x, y] = c
        img.save(out("Props", f"fire_{f}.png"))

    # glowing warning (telegraph)
    img = Image.new("RGBA", (16, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([0, 2, 15, 7], fill=hexc("#ff6a1a", 160))
    img.save(out("Props", "fire_warn.png"))

    # debris rock and shadow
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.polygon([(2, 6), (6, 1), (12, 2), (15, 8), (12, 15), (4, 14), (1, 10)], fill=hexc("#7a6a5a"))
    d.polygon([(6, 1), (12, 2), (10, 6), (5, 5)], fill=hexc("#a0907a"))
    d.line([(4, 9), (9, 11), (12, 8)], fill=hexc("#4a3e34"))
    img.save(out("Props", "rock.png"))
    img = Image.new("RGBA", (16, 6), (0, 0, 0, 0))
    ImageDraw.Draw(img).ellipse([0, 0, 15, 5], fill=(0, 0, 0, 140))
    img.save(out("Props", "shadow.png"))

    # moving platform
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, 15, 6], fill=hexc("#6a6a7a"))
    d.line([(0, 0), (15, 0)], fill=hexc("#a0a0b0"))
    d.line([(0, 6), (15, 6)], fill=hexc("#3a3a48"))
    d.point([(2, 3), (13, 3)], fill=hexc("#ffd23a"))
    img.save(out("Props", "moving_platform.png"))

    # checkpoint flag (off / on)
    for state, col in (("off", "#5a5a6a"), ("on", "#ff4a3a")):
        img = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        d.rectangle([3, 2, 4, 31], fill=hexc("#8a7a6a"))
        d.polygon([(5, 3), (15, 7), (5, 12)], fill=hexc(col))
        d.rectangle([1, 29, 7, 31], fill=hexc("#4a4a54"))
        img.save(out("Props", f"checkpoint_{state}.png"))

    # exit gate (frame + bars)
    img = Image.new("RGBA", (32, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, 31, 47], fill=hexc("#4a4a5a"))
    d.rectangle([4, 6, 27, 47], fill=hexc("#ffd88a"))
    d.ellipse([4, 0, 27, 14], fill=hexc("#ffd88a"))
    img.save(out("Props", "gate_frame.png"))
    img = Image.new("RGBA", (24, 42), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for x in range(1, 24, 5):
        d.rectangle([x, 0, x + 1, 41], fill=hexc("#2a2a34"))
    for y in (6, 20, 34):
        d.rectangle([0, y, 23, y + 1], fill=hexc("#2a2a34"))
    img.save(out("Props", "gate_bars.png"))

    # sign post
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([7, 8, 8, 15], fill=hexc("#6a4a2a"))
    d.rectangle([1, 2, 14, 9], fill=hexc("#a0703a"))
    d.rectangle([1, 2, 14, 2], fill=hexc("#c08a4a"))
    for x in (3, 6, 9):
        d.line([(x, 5), (x + 2, 5)], fill=hexc("#4a3020"))
    img.save(out("Props", "sign.png"))

    # arena wall
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle([2, 0, 13, 15], fill=hexc("#3a2a3a"))
    for y in range(0, 16, 4):
        d.line([(2, y), (13, y)], fill=hexc("#5a3a5a"))
    d.line([(7, 0), (7, 15)], fill=hexc("#ff4a6a"))
    img.save(out("Props", "arena_wall.png"))

    # effects
    img = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    ImageDraw.Draw(img).ellipse([0, 0, 7, 7], fill=(230, 220, 200, 255))
    img.save(out("Effects", "dust.png"))
    img = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.line([(0, 3), (7, 4)], fill=(255, 255, 255, 255))
    d.line([(3, 0), (4, 7)], fill=(255, 240, 160, 255))
    img.save(out("Effects", "spark.png"))
    img = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.arc([0, 0, 23, 23], start=-170, end=-10, fill=(255, 255, 255, 230), width=3)  # opens toward +Y
    img.save(out("Effects", "slash.png"))
    Image.new("RGBA", (4, 4), (255, 255, 255, 255)).save(out("Effects", "white.png"))

    # vignette-ish lightning bolt for storm cliffs flashes
    img = Image.new("RGBA", (16, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    x = 8
    for y in range(0, 64, 8):
        nx = max(2, min(13, x + rnd.randint(-4, 4)))
        d.line([(x, y), (nx, y + 8)], fill=(230, 240, 255, 255), width=2)
        x = nx
    img.save(out("Effects", "bolt.png"))


def make_ui():
    # hearts
    heart = [
        "..##...##..",
        ".####.####.",
        "###########",
        "###########",
        ".#########.",
        "..#######..",
        "...#####...",
        "....###....",
        ".....#.....",
    ]
    for name, fillc, edge in (("heart_full", "#e83a4a", "#6a0a1a"), ("heart_empty", "#3a2a30", "#1a1014")):
        img = Image.new("RGBA", (13, 11), (0, 0, 0, 0))
        px = img.load()
        for y, row in enumerate(heart):
            for x, ch in enumerate(row):
                if ch == "#":
                    px[x + 1, y + 1] = hexc(fillc)
        # outline
        src = img.copy()
        sp = src.load()
        for y in range(11):
            for x in range(13):
                if sp[x, y][3] == 0 and any(
                    0 <= x + dx < 13 and 0 <= y + dy < 11 and sp[x + dx, y + dy][3] > 0
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                ):
                    px[x, y] = hexc(edge)
        if name == "heart_full":
            px[3, 3] = hexc("#ffb0b8")
            px[4, 3] = hexc("#ffb0b8")
        img.save(out("UI", f"{name}.png"))

    # 9-slice panel and buttons
    def panel(name, fillc, border, light):
        img = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        d.rectangle([1, 1, 22, 22], fill=hexc(fillc))
        d.rectangle([0, 1, 0, 22], fill=hexc(border))
        d.rectangle([23, 1, 23, 22], fill=hexc(border))
        d.rectangle([1, 0, 22, 0], fill=hexc(border))
        d.rectangle([1, 23, 22, 23], fill=hexc(border))
        d.line([(2, 2), (21, 2)], fill=hexc(light))
        img.save(out("UI", f"{name}.png"))

    panel("panel", "#1e1620", "#5a4a5a", "#3a2e3a")
    panel("button", "#3a2a2a", "#8a6a4a", "#5a4038")
    panel("button_hover", "#6a3a2a", "#ffc070", "#8a5038")
    # front distance meter icons
    img = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    ImageDraw.Draw(img).polygon([(0, 0), (7, 4), (0, 7)], fill=hexc("#ffffff"))
    img.save(out("UI", "arrow.png"))


def main():
    rnd = random.Random(1234)
    for name, theme in THEMES.items():
        make_tiles(name, theme, rnd)
        make_backgrounds(name, theme, rnd)
    for name, cols in FRONTS.items():
        make_front(name, cols, rnd)
    make_props(rnd)
    make_ui()
    print("art written to", os.path.normpath(ROOT))


if __name__ == "__main__":
    main()
