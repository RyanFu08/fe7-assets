import os
import argparse
from PIL import Image

def parse_color(s):
    """Parse a color string “R,G,B” or “#RRGGBB” into an (R,G,B) tuple."""
    s = s.strip()
    if s.startswith('#'):
        hexcol = s.lstrip('#')
        if len(hexcol) != 6:
            raise argparse.ArgumentTypeError("Hex color must be 6 digits, e.g. #FF00FF")
        return tuple(int(hexcol[i:i+2], 16) for i in (0,2,4))
    parts = s.split(',')
    if len(parts) != 3:
        raise argparse.ArgumentTypeError("RGB color must be three comma-separated ints, e.g. 255,0,255")
    return tuple(int(p) for p in parts)

def remove_color(image_path, target_rgb):
    im = Image.open(image_path).convert("RGBA")
    data = im.getdata()
    new_data = []
    removed_any = False

    for r, g, b, a in data:
        if (r, g, b) == target_rgb:
            # make fully transparent
            new_data.append((r, g, b, 0))
            removed_any = True
        else:
            new_data.append((r, g, b, a))

    if removed_any:
        im.putdata(new_data)
        im.save(image_path)
    return removed_any

def main():
    target_rgb = parse_color("#a8d0a0")
    for root, _, files in os.walk('.'):
        for fname in files:
            if not fname.lower().endswith(".png"):
                continue
            full = os.path.join(root, fname)
            try:
                if remove_color(full, target_rgb):
                    print(f"✔ removed color from {full}")
            except Exception as e:
                print(f"✖ failed {full}: {e}")

if __name__ == "__main__":
    main()
