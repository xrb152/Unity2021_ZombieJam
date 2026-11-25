"""把僵尸精灵图上烘焙的英文 STOP 文本替换成中文「停车」。

流程：定位红色八角牌 -> 用牌面扁平红色抹掉白色文字 -> 用中文字体重绘。
"""
from PIL import Image, ImageDraw, ImageFont

RED = (205, 77, 58)
FONT_PATH = r"C:\Windows\Fonts\msyhbd.ttc"

TARGETS = [
    "Assets/Art/Sprites/Units/FullHeight/Hitchhiker_256x384.png",
    "Assets/Art/Sprites/Units/HalfHeight/Hitchhiker_256x256.png",
]
TEXT = "停车"


def is_red(p):
    r, g, b, a = p
    return a > 200 and r > 140 and g < 90 and b < 90


def process(path):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    W, H = im.size

    red_pts = [(x, y) for y in range(H) for x in range(W) if is_red(px[x, y])]
    bx0 = min(p[0] for p in red_pts)
    bx1 = max(p[0] for p in red_pts)
    by0 = min(p[1] for p in red_pts)
    by1 = max(p[1] for p in red_pts)

    # 1) 逐行擦除：红色跨度内部、非红色的亮像素 -> 牌面红
    #    跨度内缩 3px，避开八角牌外圈描边
    for y in range(by0, by1 + 1):
        xs = [x for x in range(bx0, bx1 + 1) if is_red(px[x, y])]
        if not xs:
            continue
        lo, hi = min(xs) + 3, max(xs) - 2
        for x in range(lo, hi + 1):
            r, g, b, a = px[x, y]
            if a > 60 and (r + g + b) / 3 > 120:   # 白色字 / 半透明抗锯齿
                px[x, y] = (RED[0], RED[1], RED[2], a)

    # 2) 自动选字号：两字总宽 ≈ 牌面宽的 0.88
    sign_w = bx1 - bx0 + 1
    target_w = sign_w * 0.88
    size = 20
    for s in range(20, 120):
        f = ImageFont.truetype(FONT_PATH, s)
        l, t, r, b = f.getbbox(TEXT)
        if r - l > target_w:
            break
        size = s

    f = ImageFont.truetype(FONT_PATH, size)
    l, t, r, b = f.getbbox(TEXT)
    draw = ImageDraw.Draw(im)
    cx = (bx0 + bx1) / 2
    cy = (by0 + by1) / 2
    # 用字形实际视觉中心对齐
    draw.text((cx - (l + r) / 2, cy - (t + b) / 2), TEXT, font=f, fill=(255, 255, 255, 255))

    im.save(path)
    print(f"{path}: sign={sign_w}x{by1 - by0 + 1} font={size} textbox={l},{t},{r},{b}")


for t in TARGETS:
    process(t)
