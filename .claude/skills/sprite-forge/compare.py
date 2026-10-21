"""FAST STATIC before-and-after sheet (local to this repo, not upstream).

usage: python3 .claude/skills/sprite-forge/compare.py <dir> <out.png>
Reads <dir>/before_full.png, after_full.png, before_game.png, after_game.png and lays them out
before | after, full size above game scale, on a day (white) and a night (dark) background.
"""
import sys
from PIL import Image

d, out = sys.argv[1], sys.argv[2]
ims = [Image.open(f"{d}/{n}_{k}.png").convert("RGBA") for k in ("full", "game") for n in ("before", "after")]
rows = (ims[:2], ims[2:])
W = sum(i.width for i in ims[:2]) + 40
H = 2 * sum(r[0].height + 5 for r in rows) + 30
sheet = Image.new("RGBA", (W, H), (128, 128, 128, 255))
y = 10
for bg in ((255, 255, 255, 255), (24, 22, 30, 255)):
    for row in rows:
        x = 10
        for im in row:
            tile = Image.new("RGBA", im.size, bg)
            tile.alpha_composite(im)
            sheet.paste(tile, (x, y))
            x += im.width + 20
        y += row[0].height + 5
sheet.save(out)
print(out, sheet.size)
