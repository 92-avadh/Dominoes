import os
import math
from PIL import Image, ImageDraw, ImageFilter, ImageFont

output_dir = r"c:\Users\dhame\Dominoes_MVP_Prototype\Assets\UI\Textures"
os.makedirs(output_dir, exist_ok=True)

def create_radial_gradient(w, h, inner_color, outer_color, center_ratio=(0.5, 0.45)):
    img = Image.new("RGBA", (w, h), outer_color)
    cx = int(w * center_ratio[0])
    cy = int(h * center_ratio[1])
    max_r = math.sqrt(max(cx, w - cx)**2 + max(cy, h - cy)**2)
    
    # Generate radial mask
    mask = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(mask)
    steps = 120
    for i in range(steps, 0, -1):
        r = (i / float(steps)) * max_r
        alpha = int(255 * (1.0 - (i / float(steps))**1.5))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=alpha)
    
    mask = mask.filter(ImageFilter.GaussianBlur(15))
    inner_img = Image.new("RGBA", (w, h), inner_color)
    img.paste(inner_img, (0, 0), mask)
    return img

def generate_luxury_felt():
    # 1080x1920 deep emerald felt with spotlight & subtle weave
    W, H = 1080, 1920
    # Inner emerald #065F46, Outer dark forest #022C22
    felt = create_radial_gradient(W, H, (6, 95, 70, 255), (2, 44, 34, 255), (0.5, 0.42))
    
    # Add subtle weave noise
    noise = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ndraw = ImageDraw.Draw(noise)
    for y in range(0, H, 4):
        ndraw.line([(0, y), (W, y)], fill=(255, 255, 255, 4))
    for x in range(0, W, 4):
        ndraw.line([(x, 0), (x, H)], fill=(0, 0, 0, 8))
    
    felt = Image.alpha_composite(felt, noise)
    
    # Warm golden vignette glow at bottom for hand tray area
    warm_glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    wdraw = ImageDraw.Draw(warm_glow)
    wdraw.ellipse([-200, H - 400, W + 200, H + 200], fill=(217, 119, 6, 25))
    warm_glow = warm_glow.filter(ImageFilter.GaussianBlur(60))
    felt = Image.alpha_composite(felt, warm_glow).convert("RGB")
    
    felt.save(os.path.join(output_dir, "bg_emerald_felt_luxury.png"), quality=95)
    felt.save(os.path.join(output_dir, "bg_emerald_felt.jpg"), quality=95)
    felt.save(os.path.join(output_dir, "bg_table_green_leaves.jpg"), quality=95)
    print("[OK] Generated bg_emerald_felt_luxury.png")

def generate_luxury_loading_bg():
    W, H = 1080, 1920
    # Deep midnight emerald background
    bg = create_radial_gradient(W, H, (10, 80, 60, 255), (2, 25, 20, 255), (0.5, 0.38))
    draw = ImageDraw.Draw(bg)
    
    # Draw decorative table border
    border_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bdraw = ImageDraw.Draw(border_layer)
    bdraw.rectangle([30, 40, W - 30, H - 40], outline=(245, 158, 11, 40), width=3)
    bdraw.rectangle([45, 55, W - 45, H - 55], outline=(254, 240, 138, 25), width=1)
    
    # Corner gold ornaments
    def draw_corner(cx, cy, flipx, flipy):
        dx = 1 if not flipx else -1
        dy = 1 if not flipy else -1
        pts = [
            (cx, cy),
            (cx + 40 * dx, cy),
            (cx + 40 * dx, cy + 8 * dy),
            (cx + 8 * dx, cy + 8 * dy),
            (cx + 8 * dx, cy + 40 * dy),
            (cx, cy + 40 * dy)
        ]
        bdraw.polygon(pts, fill=(245, 158, 11, 140))

    draw_corner(45, 55, False, False)
    draw_corner(W - 45, 55, True, False)
    draw_corner(45, H - 55, False, True)
    draw_corner(W - 45, H - 55, True, True)
    
    # Floating ivory dominoes illustration in center background
    domino_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ddraw = ImageDraw.Draw(domino_layer)
    
    # Left tilted domino [6|4]
    def draw_tilted_domino(cx, cy, angle, pip1, pip2):
        tw, th = 110, 220
        d_img = Image.new("RGBA", (tw + 40, th + 40), (0, 0, 0, 0))
        d_draw = ImageDraw.Draw(d_img)
        # Shadow
        d_draw.rounded_rectangle([24, 24, tw + 20, th + 20], radius=16, fill=(0, 0, 0, 100))
        d_img = d_img.filter(ImageFilter.GaussianBlur(6))
        d_draw = ImageDraw.Draw(d_img)
        # Ivory body
        d_draw.rounded_rectangle([18, 18, tw + 14, th + 14], radius=14, fill=(250, 247, 238, 255), outline=(217, 119, 6, 180), width=3)
        # Divider
        mid_y = 18 + th // 2
        d_draw.line([(28, mid_y), (tw + 4, mid_y)], fill=(100, 116, 139, 200), width=3)
        
        # Pips
        def draw_pips_half(hx, hy, count):
            pips_pos = {
                0: [],
                1: [(0, 0)],
                2: [(-20, -20), (20, 20)],
                3: [(-20, -20), (0, 0), (20, 20)],
                4: [(-20, -20), (20, -20), (-20, 20), (20, 20)],
                5: [(-20, -20), (20, -20), (0, 0), (-20, 20), (20, 20)],
                6: [(-20, -24), (20, -24), (-20, 0), (20, 0), (-20, 24), (20, 24)]
            }
            for px, py in pips_pos.get(count, []):
                d_draw.ellipse([hx + px - 7, hy + py - 7, hx + px + 7, hy + py + 7], fill=(15, 23, 42, 240))
        
        draw_pips_half(18 + tw // 2, 18 + th // 4, pip1)
        draw_pips_half(18 + tw // 2, 18 + 3 * th // 4, pip2)
        
        rotated = d_img.rotate(angle, resample=Image.BICUBIC, expand=True)
        return rotated

    d1 = draw_tilted_domino(0, 0, -18, 6, 3)
    d2 = draw_tilted_domino(0, 0, 14, 5, 5)
    d3 = draw_tilted_domino(0, 0, 4, 6, 6)
    
    domino_layer.paste(d1, (W//2 - 260, int(H * 0.42)), d1)
    domino_layer.paste(d2, (W//2 + 80, int(H * 0.44)), d2)
    domino_layer.paste(d3, (W//2 - 90, int(H * 0.40)), d3)
    
    bg = Image.alpha_composite(bg, border_layer)
    bg = Image.alpha_composite(bg, domino_layer).convert("RGB")
    
    bg.save(os.path.join(output_dir, "bg_loading_luxury.png"), quality=95)
    bg.save(os.path.join(output_dir, "bg_beach_sky.jpg"), quality=95)
    bg.save(os.path.join(output_dir, "bg_home_luxury.png"), quality=95)
    print("[OK] Generated bg_loading_luxury.png & bg_home_luxury.png")

def generate_wood_tray_texture():
    # 512x256 warm mahogany wood texture with carved beveled rack slot
    W, H = 512, 256
    img = Image.new("RGB", (W, H), (92, 42, 12))
    draw = ImageDraw.Draw(img)
    
    # Wood grain layers
    for y in range(H):
        t = y / float(H)
        # Rich wood gradient
        r = int(120 - 45 * t + 10 * math.sin(y * 0.08))
        g = int(58 - 25 * t + 5 * math.sin(y * 0.08))
        b = int(22 - 12 * t + 3 * math.sin(y * 0.08))
        draw.line([(0, y), (W, y)], fill=(r, g, b))
    
    # Grain lines
    for i in range(25):
        gy = int((i / 25.0) * H)
        draw.line([(0, gy), (W, gy + 8)], fill=(70, 28, 8, 120), width=2)
    
    # Top golden bevel highlight
    draw.line([(0, 0), (W, 0)], fill=(217, 119, 6), width=3)
    draw.line([(0, 2), (W, 2)], fill=(254, 240, 138, 160), width=1)
    
    # Recessed rack slot with inner shadow
    slot_top = 40
    slot_bot = H - 20
    draw.rounded_rectangle([12, slot_top, W - 12, slot_bot], radius=14, fill=(45, 18, 5))
    draw.rounded_rectangle([14, slot_top + 2, W - 14, slot_bot - 2], radius=12, outline=(120, 53, 15), width=2)
    # Slot bottom highlight
    draw.line([(24, slot_bot - 2), (W - 24, slot_bot - 2)], fill=(180, 83, 9), width=2)
    
    img.save(os.path.join(output_dir, "wood_tray_luxury.png"), quality=95)
    img.save(os.path.join(output_dir, "wood_rack_texture.png"), quality=95)
    print("[OK] Generated wood_tray_luxury.png")

def generate_mode_icons():
    # 128x128 mode icons with glowing circular emblem
    size = 160
    
    # 1. ONLINE ICON (Globe with network arcs)
    img_online = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img_online)
    cx, cy = size // 2, size // 2
    r = 64
    # Gold outer glow & ring
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(5, 150, 105, 255), outline=(245, 158, 11, 255), width=5)
    d.ellipse([cx - r + 8, cy - r + 8, cx + r - 8, cy + r - 8], outline=(254, 240, 138, 180), width=2)
    # Globe grid lines
    gr = r - 16
    d.ellipse([cx - gr, cy - gr, cx + gr, cy + gr], outline=(255, 255, 255, 220), width=4)
    d.ellipse([cx - gr // 2, cy - gr, cx + gr // 2, cy + gr], outline=(255, 255, 255, 180), width=3)
    d.line([(cx - gr, cy), (cx + gr, cy)], fill=(255, 255, 255, 200), width=3)
    d.line([(cx, cy - gr), (cx, cy + gr)], fill=(255, 255, 255, 200), width=3)
    # Orbiting pulse dots
    d.ellipse([cx - gr + 4, cy - 14, cx - gr + 16, cy - 2], fill=(253, 224, 71, 255))
    d.ellipse([cx + gr - 16, cy + 4, cx + gr - 4, cy + 16], fill=(253, 224, 71, 255))
    img_online.save(os.path.join(output_dir, "icon_mode_online.png"))
    
    # 2. VS COMPUTER ICON (Smart AI Robot & Game Controller)
    img_comp = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img_comp)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(30, 58, 138, 255), outline=(56, 189, 248, 255), width=5)
    d.ellipse([cx - r + 8, cy - r + 8, cx + r - 8, cy + r - 8], outline=(186, 230, 253, 180), width=2)
    # Robot face / CPU
    rw, rh = 56, 44
    d.rounded_rectangle([cx - rw//2, cy - rh//2 + 4, cx + rw//2, cy + rh//2 + 4], radius=10, fill=(241, 245, 249, 255), outline=(14, 165, 233, 255), width=3)
    # Eyes
    d.ellipse([cx - 16, cy - 6, cx - 6, cy + 4], fill=(2, 132, 199, 255))
    d.ellipse([cx + 6, cy - 6, cx + 16, cy + 4], fill=(2, 132, 199, 255))
    # Smile
    d.line([(cx - 10, cy + 14), (cx + 10, cy + 14)], fill=(14, 165, 233, 255), width=3)
    # Antenna
    d.line([(cx, cy - rh//2 + 4), (cx, cy - rh//2 - 10)], fill=(56, 189, 248, 255), width=3)
    d.ellipse([cx - 5, cy - rh//2 - 18, cx + 5, cy - rh//2 - 8], fill=(253, 224, 71, 255))
    img_comp.save(os.path.join(output_dir, "icon_mode_computer.png"))
    
    # 3. WITH FRIENDS ICON (Duo friendly avatars / Domino pair)
    img_friends = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img_friends)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(159, 18, 57, 255), outline=(251, 146, 60, 255), width=5)
    d.ellipse([cx - r + 8, cy - r + 8, cx + r - 8, cy + r - 8], outline=(254, 215, 170, 180), width=2)
    # Person 1 (left)
    d.ellipse([cx - 30, cy - 24, cx - 6, cy], fill=(255, 241, 242, 255))
    d.chord([cx - 40, cy + 4, cx + 4, cy + 48], start=180, end=0, fill=(255, 241, 242, 255))
    # Person 2 (right - overlay)
    d.ellipse([cx + 6, cy - 24, cx + 30, cy], fill=(254, 240, 138, 255))
    d.chord([cx - 4, cy + 4, cx + 40, cy + 48], start=180, end=0, fill=(254, 240, 138, 255))
    img_friends.save(os.path.join(output_dir, "icon_mode_friends.png"))
    
    # 4. Domino Logo Banner
    logo_w, logo_h = 560, 140
    img_logo = Image.new("RGBA", (logo_w, logo_h), (0, 0, 0, 0))
    ld = ImageDraw.Draw(img_logo)
    # Gold badge container
    ld.rounded_rectangle([10, 10, logo_w - 10, logo_h - 10], radius=24, fill=(15, 23, 42, 240), outline=(245, 158, 11, 255), width=4)
    ld.rounded_rectangle([18, 18, logo_w - 18, logo_h - 18], radius=18, outline=(254, 240, 138, 140), width=2)
    
    # Draw mini dominoes on left and right of logo
    def draw_mini_logo_tile(lx, ly):
        ld.rounded_rectangle([lx, ly, lx + 36, ly + 64], radius=6, fill=(250, 247, 238, 255), outline=(217, 119, 6, 255), width=2)
        ld.line([(lx + 4, ly + 32), (lx + 32, ly + 32)], fill=(100, 116, 139, 200), width=2)
        ld.ellipse([lx + 14, ly + 12, lx + 22, ly + 20], fill=(15, 23, 42, 255))
        ld.ellipse([lx + 14, ly + 44, lx + 22, ly + 52], fill=(15, 23, 42, 255))

    draw_mini_logo_tile(36, 38)
    draw_mini_logo_tile(logo_w - 72, 38)
    
    img_logo.save(os.path.join(output_dir, "dominoes_logo_gold.png"))
    print("[OK] Generated mode icons and dominoes_logo_gold.png")

if __name__ == "__main__":
    generate_luxury_felt()
    generate_luxury_loading_bg()
    generate_wood_tray_texture()
    generate_mode_icons()
    print("All UI assets generated successfully!")
