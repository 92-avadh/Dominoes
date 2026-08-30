import os
import math
import random
from PIL import Image, ImageDraw, ImageFilter

output_dir = r"c:\Users\dhame\Dominoes_MVP_Prototype\Assets\UI\Textures"
os.makedirs(output_dir, exist_ok=True)

def generate_luxury_tropical_lobby_bg():
    W, H = 1080, 1920
    img = Image.new("RGB", (W, H))
    draw = ImageDraw.Draw(img)

    # 1. Sky & Ocean Vertical Radiant Gradient
    for y in range(H):
        t = y / float(H)
        if t < 0.35:
            # Azure Tropical Sky (#0284C7 to #38BDF8)
            interp = t / 0.35
            r = int(2 + (56 - 2) * interp)
            g = int(132 + (189 - 132) * interp)
            b = int(199 + (248 - 199) * interp)
        elif t < 0.65:
            # Crystal Turquoise Ocean with Sunlight Reflection (#06B6D4 to #0D9488)
            interp = (t - 0.35) / 0.30
            r = int(6 + (13 - 6) * interp)
            g = int(182 + (148 - 182) * interp)
            b = int(212 + (136 - 212) * interp)
        else:
            # Warm Golden Sand Beach with Soft Sunset Lighting (#D97706 to #B45309)
            interp = (t - 0.65) / 0.35
            r = int(245 - (245 - 217) * interp)
            g = int(200 - (200 - 119) * interp)
            b = int(130 - (130 - 6) * interp)
        draw.line([(0, y), (W, y)], fill=(r, g, b))

    # 2. Radiant Sunburst Light Rays
    sun_center_x, sun_center_y = W // 2, int(H * 0.18)
    ray_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ray_draw = ImageDraw.Draw(ray_layer)
    
    num_rays = 24
    for i in range(num_rays):
        if i % 2 == 0:
            angle1 = (i / num_rays) * 2 * math.pi
            angle2 = ((i + 0.8) / num_rays) * 2 * math.pi
            length = H * 1.2
            p1 = (sun_center_x, sun_center_y)
            p2 = (sun_center_x + length * math.cos(angle1), sun_center_y + length * math.sin(angle1))
            p3 = (sun_center_x + length * math.cos(angle2), sun_center_y + length * math.sin(angle2))
            ray_draw.polygon([p1, p2, p3], fill=(255, 255, 255, 28))

    ray_layer = ray_layer.filter(ImageFilter.GaussianBlur(30))
    img.paste(Image.alpha_composite(img.convert("RGBA"), ray_layer), (0, 0))

    # 3. Soft Puffy Clouds
    cloud_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    cdraw = ImageDraw.Draw(cloud_layer)
    clouds = [
        (180, 260, 220, 90), (450, 220, 280, 110), (820, 280, 240, 95),
        (100, 360, 320, 120), (700, 380, 350, 130)
    ]
    for (cx, cy, cw, ch) in clouds:
        cdraw.ellipse([cx - cw//2, cy - ch//2, cx + cw//2, cy + ch//2], fill=(255, 255, 255, 90))
        cdraw.ellipse([cx - cw//3, cy - ch//3, cx + cw//3, cy + ch//3], fill=(255, 255, 255, 140))

    cloud_layer = cloud_layer.filter(ImageFilter.GaussianBlur(24))
    img = Image.alpha_composite(img.convert("RGBA"), cloud_layer).convert("RGB")

    # 4. Lush Palm Fronds Framing the Top Corners and Bottom Edges
    frond_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    fdraw = ImageDraw.Draw(frond_layer)

    def draw_palm_branch(origin_x, origin_y, base_angle, length, num_leaflets, flip=False):
        for i in range(num_leaflets):
            t = i / float(num_leaflets)
            cur_len = length * t
            ang = base_angle + (0.25 * t if not flip else -0.25 * t)
            bx = origin_x + cur_len * math.cos(ang)
            by = origin_y + cur_len * math.sin(ang)

            leaf_len = (1.0 - 0.4 * abs(t - 0.5)) * 140
            leaf_ang = ang + (math.pi / 3.0 if not flip else -math.pi / 3.0)
            lx = bx + leaf_len * math.cos(leaf_ang)
            ly = by + leaf_len * math.sin(leaf_ang)

            # Draw tapered leaf
            fdraw.line([(bx, by), (lx, ly)], fill=(22, 101, 52, 230), width=8)
            fdraw.line([(bx, by), (lx, ly)], fill=(74, 222, 128, 200), width=4)

    # Top Left Palm Leaves
    draw_palm_branch(0, 0, math.pi * 0.22, 600, 32, flip=False)
    draw_palm_branch(0, 100, math.pi * 0.12, 520, 28, flip=False)

    # Top Right Palm Leaves
    draw_palm_branch(W, 0, math.pi * 0.78, 600, 32, flip=True)
    draw_palm_branch(W, 100, math.pi * 0.88, 520, 28, flip=True)

    # Bottom Corners Palm Fronds
    draw_palm_branch(0, H, -math.pi * 0.25, 480, 24, flip=True)
    draw_palm_branch(W, H, -math.pi * 0.75, 480, 24, flip=False)

    frond_layer = frond_layer.filter(ImageFilter.GaussianBlur(3))
    img = Image.alpha_composite(img.convert("RGBA"), frond_layer).convert("RGB")

    # 5. Vignette & Contrast Polish
    vignette = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    vdraw = ImageDraw.Draw(vignette)
    vdraw.rectangle([0, 0, W, H], outline=(5, 20, 15, 110), width=60)
    vignette = vignette.filter(ImageFilter.GaussianBlur(50))
    img = Image.alpha_composite(img.convert("RGBA"), vignette).convert("RGB")

    # Save to bg_beach_sky.jpg and bg_tropical_resort.jpg
    target1 = os.path.join(output_dir, "bg_beach_sky.jpg")
    target2 = os.path.join(output_dir, "bg_tropical_resort.jpg")
    img.save(target1, quality=95)
    img.save(target2, quality=95)
    print(f"Generated luxury tropical lobby background: {target1}")

if __name__ == "__main__":
    generate_luxury_tropical_lobby_bg()
