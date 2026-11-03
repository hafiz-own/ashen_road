"""Composes the eight Ashen Road level layouts from reusable segments and checks each one can be finished.

Run from the project root:  python3 Tools/compose_levels.py
Writes Assets/_Project/Levels/level1.txt ... level8.txt (the files Unity reads) and preview PNGs to Tools/previews.

The .txt files are the real level data: you can edit them by hand afterwards and re-run
"Ashen Road > Rebuild Project Assets" in Unity. Re-running this script overwrites them.
Legend: see LevelLayout.cs.
"""
import os
import random
from collections import deque

H = 15  # rows
BASE = 12  # default ground top row (standing row = 11)
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(ROOT, "Assets", "_Project", "Levels")
PREVIEW = os.path.join(ROOT, "Tools", "previews")

# knight numbers (keep in sync with PlayerStats defaults)
JUMP_HEIGHT = 3.2
TIME_TO_APEX = 0.38
FALL_MULT = 1.6
MAX_SPEED = 8.0
PLATFORM_RANGE = 4


def horizontal_reach(dy):
    if dy > JUMP_HEIGHT:
        return 0.0
    g = 2 * JUMP_HEIGHT / TIME_TO_APEX ** 2
    drop = JUMP_HEIGHT - dy
    fall = (2 * drop / (g * FALL_MULT)) ** 0.5
    return MAX_SPEED * (TIME_TO_APEX + fall)


class Level:
    def __init__(self, seed, ceiling=False):
        self.cols = []  # each column: list of H chars, row 0 = top
        self.g = BASE
        self.rnd = random.Random(seed)
        self.ceiling = ceiling
        self.ceiling_row = None

    # ------------------------------------------------------------ primitives
    def column(self, ground_top=None, fill="#"):
        gt = self.g if ground_top is None else ground_top
        col = ["."] * H
        for r in range(gt, H):
            col[r] = fill
        if self.ceiling_row is not None:
            for r in range(0, self.ceiling_row + 1):
                col[r] = "#"
        self.cols.append(col)
        return len(self.cols) - 1

    def put(self, x, row, ch):
        self.cols[x][row] = ch

    def stand_row(self, x):
        """Row a character stands in at column x (first empty row above the ground)."""
        col = self.cols[x]
        for r in range(H):
            if col[r] in "#=":
                if r > 0 and (self.ceiling_row is None or r - 1 > self.ceiling_row):
                    return r - 1
        return None

    def mark(self, x, ch):
        r = self.stand_row(x)
        if r is not None and self.cols[x][r] == ".":
            self.cols[x][r] = ch

    # ------------------------------------------------------------ segments
    def flat(self, n, enemies="", deco=0.0, extras=None):
        start = len(self.cols)
        for _ in range(n):
            self.column()
        free = list(range(start + 1, start + n - 1))
        self.rnd.shuffle(free)
        for e in enemies:
            if free:
                self.mark(free.pop(), e)
        for x in list(free):
            if self.rnd.random() < deco:
                self.mark(x, "d")
        if extras:
            for off, ch in extras:
                self.mark(start + off, ch)
        return start

    def start(self, hint=True):
        s = self.flat(10)
        self.mark(s + 2, "P")
        if hint:
            self.mark(s + 6, "!")
        return s

    def pit(self, width):
        for _ in range(width):
            col = ["."] * H
            if self.ceiling_row is not None:
                for r in range(0, self.ceiling_row + 1):
                    col[r] = "#"
            self.cols.append(col)

    def step(self, delta, run=4, enemies=""):
        self.g = max(6, min(BASE + 1, self.g + delta))
        return self.flat(run, enemies)

    def spikes(self, width, run_before=2):
        self.flat(run_before)
        s = len(self.cols)
        for _ in range(width):
            x = self.column()
            self.mark(x, "^")
        self.flat(2)
        return s

    def fire_row(self, width, enemies=""):
        s = self.flat(width + 2, enemies)
        for i in range(width):
            if i % 3 != 2:
                self.mark(s + 1 + i, "F")
        return s

    def platform_bridge(self, gap, height=2, plat_len=3, enemies_on_top=""):
        """A pit with one-way platforms floating above it."""
        self.flat(1)
        s = len(self.cols)
        self.pit(gap)
        row = self.g - height  # platform row: standing on it is `height` tiles above the ground
        x = s + 1
        while True:
            end = min(x + plat_len - 1, s + gap - 1)
            for i in range(x, end + 1):
                self.cols[i][row] = "="
            if s + gap - end <= 3:
                break
            x = end + 3
        for e in enemies_on_top:
            pass
        self.flat(2)
        return s

    def mover(self, gap, vertical=False, width=3):
        self.flat(1)
        s = len(self.cols)
        self.pit(gap)
        row = self.g - 1 if not vertical else self.g
        if not vertical:
            for i in range(width):
                self.cols[s + 1 + i][self.g] = "M"
        self.flat(2)
        return s

    def lift(self, rise=4):
        """A vertical moving platform up to a higher ledge."""
        self.flat(2)
        x = len(self.cols)
        self.pit(3)
        for i in range(3):
            self.cols[x + i][self.g] = "V" if i < 2 else "."
        self.g = max(6, self.g - rise)
        self.flat(3)
        return x

    def debris(self, n, spacing=4):
        s = self.flat(n * spacing + 2)
        for i in range(n):
            self.mark(s + 2 + i * spacing, "D")
        return s

    def low_ceiling(self, length, headroom=3, enemies=""):
        self.ceiling_row = self.g - 1 - headroom
        s = self.flat(length, enemies)
        self.ceiling_row = None
        return s

    def checkpoint(self):
        s = self.flat(5)
        self.mark(s + 2, "C")
        return s

    def heart(self):
        s = self.flat(4)
        self.mark(s + 2, "H")
        return s

    def high_heart(self):
        """A heart on a little ledge you have to jump to."""
        s = self.flat(6)
        row = self.g - 2
        for i in range(2, 5):
            self.cols[s + i][row] = "="
        self.cols[s + 3][row - 1] = "H"
        return s

    def exit(self):
        s = self.flat(10)
        self.mark(s + 6, "E")
        self.flat(4)
        return s

    def sign(self):
        s = self.flat(3)
        self.mark(s + 1, "!")
        return s

    def arena(self, width=26):
        s = self.flat(2)
        self.mark(s, "|")
        a = self.flat(width)
        self.mark(a + width - 6, "K")
        self.flat(1)
        e = len(self.cols) - 1
        self.mark(e, "|")
        q = self.flat(8)
        self.mark(q + 4, "Q")
        return a

    # ------------------------------------------------------------ output
    def text(self):
        rows = []
        for r in range(H):
            rows.append("".join(self.cols[x][r] for x in range(len(self.cols))))
        return "\n".join(rows)


# ------------------------------------------------------------------ reachability (same rules as LevelReachability.cs)
def check(text, boss, debug=False):
    rows = text.split("\n")
    h, w = len(rows), max(len(r) for r in rows)

    def at(x, y):
        if x < 0 or y < 0 or x >= w or y >= h:
            return "."
        row = rows[h - 1 - y]
        return row[x] if x < len(row) else "."

    solid = lambda x, y: at(x, y) == "#"
    sup = {}
    for x in range(w):
        for y in range(h):
            sup[(x, y)] = at(x, y) in "#="
    for x in range(w):
        for y in range(h):
            c = at(x, y)
            if c == "M":
                for i in range(PLATFORM_RANGE + 1):
                    sup[(x + i, y)] = True
            if c == "V":
                for i in range(PLATFORM_RANGE + 1):
                    sup[(x, y + i)] = True

    def standable(x, y):
        if x < 0 or x >= w or y <= 0 or y >= h:
            return False
        if solid(x, y) or at(x, y) in "^MV":
            return False
        return sup.get((x, y - 1), False) and at(x, y - 1) != "^"

    max_rise = int(JUMP_HEIGHT - 0.25)

    def reach(dy):
        return int(horizontal_reach(dy) * 0.75)

    def clear(x0, y0, x1, y1):
        top = max(y0, y1) + (1 if y1 >= y0 else 0)
        top = min(top, h - 1)
        for y in range(y0, top + 1):
            if solid(x0, y):
                return False
        step = 1 if x1 > x0 else -1
        x = x0
        while x != x1:
            if solid(x, top) or solid(x, max(y0, y1)):
                return False
            x += step
        for y in range(y1, top + 1):
            if solid(x1, y):
                return False
        return True

    def find(ch):
        for x in range(w):
            for y in range(h):
                if at(x, y) == ch:
                    yield (x, y)

    starts = list(find("P")) + list(find("C"))
    goal = next(find("Q" if boss else "E"))
    maxr = reach(-h)
    problems = []
    for s in starts:
        seen = {s}
        q = deque([s])
        ok = False
        while q:
            x, y = q.popleft()
            if (x, y) == goal:
                ok = True
                break
            for dx in range(-maxr, maxr + 1):
                nx = x + dx
                if nx < 0 or nx >= w:
                    continue
                for ny in range(max(1, y - h), min(h - 1, y + max_rise) + 1):
                    if (nx, ny) in seen or (nx == x and ny == y) or not standable(nx, ny):
                        continue
                    dy = ny - y
                    if not (abs(dx) <= 1 and dy == 0):
                        if abs(dx) > reach(dy) or not clear(x, y, nx, ny):
                            continue
                    seen.add((nx, ny))
                    q.append((nx, ny))
        if not ok:
            problems.append(s)
            if debug:
                far = max(seen)
                print("  from", s, "furthest reached", far)
    return problems


# ------------------------------------------------------------------ the eight levels
def level1():
    L = Level(1)
    L.start()
    L.flat(10)
    L.sign()                         # sword hint
    L.flat(10, "S")
    L.step(-1, 6)
    L.flat(8, "S", 0.2)
    L.sign()                         # jump hint
    L.pit(2)
    L.flat(10, "", 0.3)
    L.heart()
    L.flat(8, "S")
    L.step(-1, 5)
    L.step(-1, 6, "S")
    L.checkpoint()
    L.flat(6)
    L.step(1, 6)
    L.pit(3)
    L.flat(8, "SS", 0.2)
    L.step(1, 8)
    L.fire_row(5)
    L.flat(10, "S", 0.3)
    L.checkpoint()
    L.step(-2, 6, "S")
    L.pit(2)
    L.flat(6)
    L.step(2, 10, "SS")
    L.high_heart()
    L.flat(8, "S", 0.2)
    L.exit()
    return L


def level2():
    L = Level(2)
    L.start()
    L.flat(6)
    L.sign()                         # roll hint
    L.flat(8, "S")
    L.pit(2)
    L.flat(6)
    L.pit(3)
    L.flat(8, "S", 0.2)
    L.debris(2)
    L.platform_bridge(8)
    L.flat(6, "S")
    L.checkpoint()
    L.flat(4)
    L.pit(2)
    L.step(-1, 6, "S")
    L.pit(2)
    L.step(1, 6)
    L.debris(3, 3)
    L.heart()
    L.flat(8, "SS", 0.2)
    L.platform_bridge(9)
    L.checkpoint()
    L.spikes(2)
    L.flat(8, "S")
    L.pit(3)
    L.flat(6, "S")
    L.platform_bridge(7)
    L.high_heart()
    L.flat(6, "S")
    L.exit()
    return L


def level3():
    L = Level(3)
    L.start()
    L.flat(8)
    L.sign()                         # runner hint
    L.flat(14, "R")
    L.pit(2)
    L.flat(8, "S", 0.2)
    L.step(-1, 6)
    L.flat(10, "RS")
    L.platform_bridge(8)
    L.checkpoint()
    L.flat(10, "R", 0.2)
    L.pit(2)
    L.step(-1, 8, "S")
    L.fire_row(4)
    L.step(1, 8, "R")
    L.heart()
    L.flat(12, "SR")
    L.pit(2)
    L.checkpoint()
    L.flat(8, "R")
    L.platform_bridge(9)
    L.flat(10, "SS", 0.2)
    L.step(-2, 6)
    L.flat(10, "RR")
    L.high_heart()
    L.step(2, 8, "S")
    L.exit()
    return L


def level4():
    L = Level(4)
    L.start()
    L.flat(8, "", 0.4)
    L.sign()                         # brute hint
    L.flat(12, "B", 0.3)
    L.sign()                         # lure hint
    L.flat(4)
    L.fire_row(4, "B")
    L.flat(6, "S", 0.3)
    L.checkpoint()
    L.flat(8, "SR", 0.3)
    L.pit(3)
    L.flat(10, "B", 0.3)
    L.step(-1, 6, "S")
    L.platform_bridge(8)
    L.heart()
    L.flat(12, "BS", 0.3)
    L.checkpoint()
    L.fire_row(5, "B")
    L.flat(8, "R", 0.3)
    L.spikes(2)
    L.flat(10, "SB", 0.3)
    L.high_heart()
    L.flat(10, "RS", 0.3)
    L.exit()
    return L


def level5():
    L = Level(5, ceiling=True)
    L.start()
    L.flat(6)
    L.low_ceiling(14, 4, "S")
    L.debris(3, 3)
    L.pit(2)
    L.low_ceiling(12, 3, "R")
    L.flat(6)
    L.checkpoint()
    L.debris(3, 4)
    L.low_ceiling(14, 4, "B")
    L.fire_row(4)
    L.pit(3)
    L.flat(6, "S")
    L.heart()
    L.low_ceiling(16, 4, "SR")
    L.checkpoint()
    L.debris(4, 3)
    L.platform_bridge(8)
    L.low_ceiling(14, 3, "RS")
    L.high_heart()
    L.flat(6, "B")
    L.exit()
    return L


def level6():
    L = Level(6)
    L.start()
    L.flat(6)
    L.sign()                         # moving platform hint
    L.mover(8)
    L.flat(8, "S")
    L.pit(3)                         # running jump
    L.flat(8, "R")
    L.lift(4)
    L.flat(10, "S", 0.2)
    L.checkpoint()
    L.mover(9)
    L.flat(6, "R")
    L.step(2, 6)
    L.pit(3)
    L.flat(10, "BS")
    L.heart()
    L.mover(8)
    L.checkpoint()
    L.flat(8, "RR")
    L.pit(3)
    L.step(1, 6, "S")
    L.mover(9)
    L.high_heart()
    L.flat(8, "B")
    L.exit()
    return L


def level7():
    L = Level(7)
    L.start(hint=False)
    L.flat(8, "SS", 0.3)
    L.fire_row(4)
    L.flat(10, "RSB", 0.3)
    L.debris(3, 3)
    L.pit(3)
    L.checkpoint()
    L.flat(12, "SRS", 0.3)
    L.platform_bridge(8)
    L.fire_row(5, "B")
    L.heart()
    L.flat(12, "RRS", 0.3)
    L.step(-2, 6, "S")
    L.pit(3)
    L.step(2, 8, "B")
    L.checkpoint()
    L.debris(3, 3)
    L.flat(12, "SRBS", 0.3)
    L.spikes(3)
    L.flat(10, "RS", 0.3)
    L.high_heart()
    L.fire_row(4, "B")
    L.flat(8, "SR", 0.3)
    L.exit()
    return L


def level8():
    L = Level(8)
    L.start(hint=False)
    L.flat(8, "S", 0.2)
    L.pit(3)
    L.flat(10, "SR")
    L.debris(2, 4)
    L.checkpoint()
    L.flat(8, "B", 0.2)
    L.platform_bridge(8)
    L.heart()
    L.flat(8, "RS")
    L.checkpoint()
    L.flat(10)
    L.arena(28)
    return L


LEVELS = [level1, level2, level3, level4, level5, level6, level7, level8]


def preview(name, text):
    try:
        from PIL import Image
    except ImportError:
        return
    colors = {"#": (110, 80, 60), "=": (180, 130, 80), "^": (200, 200, 220), "F": (255, 120, 30), "D": (140, 120, 100),
              "M": (90, 90, 200), "V": (90, 90, 200), "S": (90, 200, 90), "R": (240, 220, 60), "B": (200, 60, 60),
              "H": (255, 80, 140), "C": (255, 255, 255), "P": (60, 160, 255), "E": (255, 220, 120), "K": (180, 0, 200),
              "Q": (255, 150, 255), "|": (120, 40, 120), "!": (200, 170, 120), "d": (80, 70, 60)}
    rows = text.split("\n")
    w, h = max(len(r) for r in rows), len(rows)
    img = Image.new("RGB", (w, h), (25, 22, 30))
    for y, row in enumerate(rows):
        for x, c in enumerate(row):
            if c in colors:
                img.putpixel((x, y), colors[c])
    os.makedirs(PREVIEW, exist_ok=True)
    img.resize((w * 5, h * 5), Image.NEAREST).save(os.path.join(PREVIEW, name + ".png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    ok = True
    for i, fn in enumerate(LEVELS, 1):
        L = fn()
        text = L.text()
        boss = i == 8
        problems = check(text, boss, debug=True)
        width = len(text.split("\n")[0])
        status = "ok" if not problems else f"UNREACHABLE from {problems}"
        print(f"level{i}: {width} x {H}  {status}")
        ok &= not problems
        with open(os.path.join(OUT, f"level{i}.txt"), "w") as f:
            f.write(text + "\n")
        preview(f"level{i}", text)
    if not ok:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
