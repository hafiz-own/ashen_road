"""Cuts the CC0 Gothicvania packs (ansimuz) into the tiles, backgrounds, props and animation frames Ashen Road uses.

Usage:  python3 Tools/extract_gothicvania.py <folder with the unzipped packs>
The folder holds x_town, x_cemetery, x_church, x_swamp, x_rocky, x_junk, x_bridge, x_patreon (one per zip).
Writes to Assets/_Project/Art/Gothicvania.
"""
import os
import sys

from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Project", "Art", "Gothicvania")
SRC = sys.argv[1] if len(sys.argv) > 1 else "."
PAT = "x_patreon/ gothicvania patreon collection/"

TILESETS = {
    "town": "x_town/GothicVania-town-files/PNG/environment/layers/tileset.png",
    "cemetery": "x_cemetery/gothicvania-cemetery-files/PNG/Environment/tileset.png",
    "swamp": "x_swamp/Gothicvania Swamp files/Evironment/tileset.png",
    "rocky": "x_rocky/Rocky Pass Files/PNG/tileset.png",
    "castle": PAT + "Gothic-Castle-Files/PNG/layers/gothic-castle-tileset.png",
    "horror": PAT + "Gothic-Horror-Files/PNG/layers/tiles.png",
    "dark": PAT + "Old-dark-Castle-tileset-Files/PNG/old-dark-castle-interior-tileset.png",
    "junk": "x_junk/Junk Wastelands Files/layers/tileset.png",
}

# Per theme: top tiles as (tile x, tile y, overhang px above the cell), fill and deep tiles as (tile x, tile y),
# platform as (tile x, tile y, pixel row where the plank starts).
THEMES = {
    "town":     dict(src="town", top=[(20, 9, 3), (21, 9, 3)], fill=[(20, 10), (21, 10)], deep=[(20, 10)], plat=(10, 7, 0)),
    "cemetery": dict(src="cemetery", top=[(4, 4, 8), (5, 4, 8), (7, 4, 8)], fill=[(4, 5), (5, 5), (6, 5), (7, 5)], deep=[(2, 6)], plat=(10, 7, 0)),
    "swamp":    dict(src="swamp", top=[(14, 0, 0), (15, 0, 0), (16, 0, 0), (17, 0, 0)], fill=[(14, 1), (15, 1), (16, 1)], deep=[(15, 3)], plat=(14, 5, 0)),
    "rocky":    dict(src="rocky", top=[(3, 1, 0), (4, 1, 0)], fill=[(3, 2), (4, 2), (3, 3), (4, 3)], deep=[(3, 6)], plat=(3, 1, 0)),
    "castle":   dict(src="castle", top=[(2, 2, 0), (3, 2, 0)], fill=[(2, 3), (3, 3)], deep=[(2, 5)], plat=(5, 2, 0)),
    "horror":   dict(src="horror", top=[(2, 4, 0)], fill=[(2, 5)], deep=[(2, 5)], plat=(3, 1, 0)),
    "dark":     dict(src="dark", top=[(20, 10, 2), (23, 10, 2)], fill=[(20, 11), (20, 12), (23, 11)], deep=[(20, 13)], plat=(4, 5, 0)),
    "junk":     dict(src="junk", top=[(4, 0, 0), (5, 0, 0)], fill=[(4, 2), (5, 2)], deep=[(4, 5)], plat=(4, 0, 0)),
}


def out(*parts):
    p = os.path.join(ROOT, *parts)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    return p


def load(rel):
    return Image.open(os.path.join(SRC, rel)).convert("RGBA")


def tiles():
    for theme, t in THEMES.items():
        sheet = load(TILESETS[t["src"]])
        for i, (x, y, over) in enumerate(t["top"]):
            sheet.crop((x * 16, y * 16 - over, x * 16 + 16, y * 16 + 16)).save(out("Tiles", f"{theme}_top_{i}.png"))
        for i, (x, y) in enumerate(t["fill"]):
            sheet.crop((x * 16, y * 16, x * 16 + 16, y * 16 + 16)).save(out("Tiles", f"{theme}_fill_{i}.png"))
        for i, (x, y) in enumerate(t["deep"]):
            sheet.crop((x * 16, y * 16, x * 16 + 16, y * 16 + 16)).save(out("Tiles", f"{theme}_deep_{i}.png"))
        x, y, row = t["plat"]
        sheet.crop((x * 16, y * 16 + row, x * 16 + 16, y * 16 + 16)).save(out("Tiles", f"{theme}_platform.png"))


# ------------------------------------------------------------------ characters
CEM = "x_cemetery/gothicvania-cemetery-files/PNG/Sprites/"
CHURCH = "x_church/gothicvania church files/SPRITES/"


def files(pattern, count):
    return [load(pattern.format(i)) for i in range(1, count + 1)]


def sheet(rel, frames):
    im = load(rel)
    w = im.width // frames
    return [im.crop((i * w, 0, (i + 1) * w, im.height)) for i in range(frames)]


# name: clips {clip: frames}, body centre x (pixels in the source frame), feet y (pixels), flip so it faces right
CHARACTERS = {
    "knight": dict(cx=45, feet=59, flip=False, clips={
        "idle": lambda: files(CEM + "hero/hero-idle/hero-idle-{}.png", 4),
        "run": lambda: files(CEM + "hero/hero-run/hero-run-{}.png", 6),
        "jump": lambda: files(CEM + "hero/hero-jump/hero-jump-{}.png", 4),
        "attack": lambda: files(CEM + "hero/hero-attack/hero-attack-{}.png", 5),
        "hurt": lambda: [load(CEM + "hero/hero-hurt/hero-hurt.png")],
        "crouch": lambda: [load(CEM + "hero/hero-crouch/hero-crouch.png")],
    }),
    "shambler": dict(cx=17, feet=52, flip=False, clips={
        "walk": lambda: files(CEM + "skeleton-clothed/skeleton-clothed-{}.png", 8),
        "rise": lambda: files(CEM + "skeleton-rise-clothed/skeleton-rise-clothed-{}.png", 6),
    }),
    "runner": dict(cx=25, feet=60, flip=True, clips={
        "walk": lambda: files(CHURCH + "burning-ghoul/sprites/v1/burning-ghoul-{}.png", 8),
    }),
    "brute": dict(cx=29, feet=67, flip=True, clips={
        "walk": lambda: sheet(PAT + "Hell-Beast-Files/PNG/with-stroke/hell-beast-idle.png", 6),
    }),
    "boss": dict(cx=85, feet=128, flip=False, clips={
        "idle": lambda: sheet(PAT + "demon-Files/PNG/demon-idle.png", 6),
    }),
    "princess": dict(cx=18, feet=46, flip=False, clips={
        "idle": lambda: sheet("x_town/GothicVania-town-files/PNG/spritesheets/woman-idle.png", 7),
    }),
}


def characters():
    for name, c in CHARACTERS.items():
        for clip, get in c["clips"].items():
            frames = get()
            for i, f in enumerate(frames):
                save_centered(f, c["cx"], c["feet"], c["flip"], out("Characters", name, f"{clip}_{i}.png"))
    # the demon's attack frames sit on a bigger canvas: align them by their own bounding box
    attack = sheet(PAT + "demon-Files/PNG/demon-attack-no-breath.png", 8)
    for i, f in enumerate(attack):
        box = attack[0].getbbox()
        save_centered(f, (box[0] + box[2]) // 2 - 6, box[3], False, out("Characters", "boss", f"attack_{i}.png"))


def save_centered(frame, cx, feet, flip, path):
    """Pads a frame so the body's centre is the middle column and the feet the bottom row (pivot = bottom centre)."""
    frame = frame.crop((0, 0, frame.width, min(frame.height, feet)))
    half = max(cx, frame.width - cx)
    canvas = Image.new("RGBA", (half * 2, frame.height), (0, 0, 0, 0))
    canvas.paste(frame, (half - cx, 0), frame)
    if flip:
        canvas = canvas.transpose(Image.FLIP_LEFT_RIGHT)
    canvas.save(path)


# ------------------------------------------------------------------ effects
def effects():
    for i, f in enumerate(files(CEM + "enemy-death/enemy-death-{}.png", 5)):
        f.save(out("Effects", f"death_{i}.png"))


# ------------------------------------------------------------------ backgrounds and props
NIGHT = PAT + "night-town-background-files/layers/night-town-background-"
BACKGROUNDS = {
    "town_sky": "x_town/GothicVania-town-files/PNG/environment/layers/background.png",
    "town_far": "x_town/GothicVania-town-files/PNG/environment/layers/middleground.png",
    "cemetery_sky": "x_cemetery/gothicvania-cemetery-files/PNG/Environment/background.png",
    "cemetery_far": "x_cemetery/gothicvania-cemetery-files/PNG/Environment/mountains.png",
    "cemetery_near": "x_cemetery/gothicvania-cemetery-files/PNG/Environment/graveyard.png",
    "swamp_sky": "x_swamp/Gothicvania Swamp files/Evironment/background.png",
    "swamp_far": "x_swamp/Gothicvania Swamp files/Evironment/mid-layer-01.png",
    "swamp_near": "x_swamp/Gothicvania Swamp files/Evironment/mid-layer-02.png",
    "rocky_sky": "x_rocky/Rocky Pass Files/PNG/back.png",
    "rocky_far": "x_rocky/Rocky Pass Files/PNG/middle.png",
    "rocky_near": "x_rocky/Rocky Pass Files/PNG/near.png",
    "castle_sky": PAT + "Gothic-Castle-Files/PNG/layers/gothic-castle-background.png",
    "horror_sky": PAT + "Gothic-Horror-Files/PNG/layers/clouds.png",
    "horror_far": PAT + "Gothic-Horror-Files/PNG/layers/town.png",
    "dark_sky": PAT + "Old-dark-Castle-tileset-Files/PNG/old-dark-castle-interior-background.png",
    "night_sky": NIGHT + "sky.png",
    "night_mountains": NIGHT + "mountains.png",
    "night_mountains_lights": NIGHT + "mountains-lights.png",
    "night_far": NIGHT + "far-buildings.png",
    "night_town": NIGHT + "town.png",
    "junk_far": "x_junk/Junk Wastelands Files/layers/middle.png",
}

PROPS = {
    "town": ["wagon", "barrel", "crate-stack", "street-lamp", "well", "sign", "crate"],
    "town_big": ["house-a", "house-b", "house-c"],
}


def backgrounds():
    for name, rel in BACKGROUNDS.items():
        load(rel).save(out("Backgrounds", name + ".png"))


def props():
    tp = "x_town/GothicVania-town-files/PNG/environment/props-sliced/"
    for n in PROPS["town"] + PROPS["town_big"]:
        load(tp + n + ".png").save(out("Props", "town", n + ".png"))
    cp = "x_cemetery/gothicvania-cemetery-files/PNG/Environment/sliced-objects/"
    for n in ["statue", "stone-1", "stone-2", "stone-3", "stone-4", "bush-large", "bush-small", "tree-1", "tree-2", "tree-3"]:
        load(cp + n + ".png").save(out("Props", "cemetery", n + ".png"))
    rp = "x_rocky/Rocky Pass Files/PNG/Props/"
    for n in ["crystal-1", "crystal-2", "plant-1", "plant-2"]:
        load(rp + n + ".png").save(out("Props", "rocky", n + ".png"))
    bp = "x_bridge/Bridge expansion Files/Props/"
    for n in ["castle", "house", "tree"]:
        load(bp + n + ".png").save(out("Props", "bridge", n + ".png"))
    load("x_church/gothicvania church files/ENVIRONMENT/column.png").save(out("Props", "castle", "column.png"))
    # swamp: trees and plants sheets cut into single props
    trees = load("x_swamp/Gothicvania Swamp files/Evironment/trees.png")
    half = trees.width // 2
    for i in range(2):
        t = trees.crop((i * half, 0, (i + 1) * half, trees.height))
        t.crop(t.getbbox()).save(out("Props", "swamp", f"tree-{i + 1}.png"))
    plants = load("x_swamp/Gothicvania Swamp files/Evironment/props.png")
    cuts = split_by_gaps(plants)
    for i, c in enumerate(cuts):
        c.save(out("Props", "swamp", f"plant-{i + 1}.png"))
    # gothic horror: the leaning wrecked house and the brick house
    horror = load(TILESETS["horror"])
    horror.crop((49 * 16, 0, 66 * 16, 11 * 16)).save(out("Props", "horror", "wrecked-house.png"))
    horror.crop((28 * 16, 0, 47 * 16, 11 * 16)).save(out("Props", "horror", "brick-house.png"))
    horror.crop((14 * 16, 4 * 16, 16 * 16, 10 * 16)).save(out("Props", "horror", "shrine.png"))
    horror.crop((16 * 16, 4 * 16, 19 * 16, 10 * 16)).save(out("Props", "horror", "brazier.png"))


def shift_hue(im, degrees):
    """Rotates the hue of every pixel, keeping alpha."""
    alpha = im.split()[3]
    hsv = im.convert("RGB").convert("HSV")
    h, sat, v = hsv.split()
    h = h.point(lambda x: (x + int(degrees * 255 / 360)) % 256)
    rgb = Image.merge("HSV", (h, sat, v)).convert("RGB")
    rgb.putalpha(alpha)
    return rgb


def split_by_gaps(im):
    """Cuts a strip of sprites apart at fully transparent columns."""
    alpha = im.split()[3]
    cols = [any(alpha.getpixel((x, y)) for y in range(im.height)) for x in range(im.width)]
    pieces, start = [], None
    for x, filled in enumerate(cols + [False]):
        if filled and start is None:
            start = x
        elif not filled and start is not None:
            piece = im.crop((start, 0, x, im.height))
            pieces.append(piece.crop(piece.getbbox()))
            start = None
    return pieces


if __name__ == "__main__":
    tiles()
    characters()
    effects()
    backgrounds()
    props()
    print("written to", os.path.normpath(ROOT))
