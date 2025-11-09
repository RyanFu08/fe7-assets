#!/usr/bin/env python3
"""
generate_pair_shade_rect.py
Create a PNG where pixels can only be nudged darker, and whenever
a dark pixel appears an equally light pixel is placed right below it.

Example
-------
python generate_pair_shade_rect.py --width 256 --height 128 \
       --color "#4CAF50" --nudge 30 --p_dark 0.3 --output rect.png
"""

import argparse
import random
from pathlib import Path
from typing import Tuple

from PIL import Image


# ─────────────────── helpers ───────────────────
def parse_color(text: str) -> Tuple[int, int, int]:
    text = text.strip()
    if text.startswith("#"):
        if len(text) != 7:
            raise ValueError("Colour must be #RRGGBB.")
        r, g, b = text[1:3], text[3:5], text[5:7]
        return tuple(int(c, 16) for c in (r, g, b))
    parts = text.replace(",", " ").split()
    if len(parts) != 3:
        raise ValueError("Colour must be #RRGGBB or R,G,B.")
    return tuple(int(p) for p in parts)


def clamp(v: int) -> int:
    return 0 if v < 0 else 255 if v > 255 else v


# ─────────────────── core ───────────────────
def make_image(
    size: Tuple[int, int],
    base_rgb: Tuple[int, int, int],
    nudge: int,
    p_dark: float,
) -> Image.Image:
    """
    Loop top‑to‑bottom, left‑to‑right.
    • For rows 0 … h‑2:
        – with prob p_dark: pixel gets a negative Δ, pixel below gets +|Δ|.
        – else: both stay base.
    • Bottom row: never darkened (no pixel below it).
    Already‑set pixels are skipped to avoid overwriting the “light” partner.
    """
    w, h = size
    pixels = [None] * (w * h)  # type: list[Tuple[int, int, int]]

    for y in range(h):
        for x in range(w):
            idx = y * w + x
            if pixels[idx] is not None:  # already set by the pixel above
                continue

            # Bottom row can't host dark‑light pairs (no pixel below)
            if y == h - 1 or random.random() >= p_dark:
                pixels[idx] = base_rgb
                continue

            # Draw a darker pixel
            delta = random.randint(-nudge, -1)  # strictly negative
            dark_px = tuple(clamp(c + delta) for c in base_rgb)
            pixels[idx] = dark_px

            # …and an equally lighter pixel right below
            idx_below = idx + w
            light_delta = -delta  # positive
            light_px = tuple(clamp(c + light_delta) for c in base_rgb)
            pixels[idx_below] = light_px

    # Convert flat list to image
    img = Image.new("RGB", (w, h))
    img.putdata(pixels)
    return img


# ─────────────────── CLI ───────────────────
def main() -> None:
    p = argparse.ArgumentParser(
        description="Generate PNG with darker pixels paired with lighter ones below."
    )
    p.add_argument("--width", type=int, required=True, help="Image width in pixels.")
    p.add_argument("--height", type=int, required=True, help="Image height in pixels.")
    p.add_argument("--color", required=True, help="Base colour (#RRGGBB or R,G,B).")
    p.add_argument(
        "--nudge",
        type=int,
        default=16,
        help="Max brightness change (positive, default 16).",
    )
    p.add_argument(
        "--p_dark",
        type=float,
        default=0.25,
        help="Probability a pixel starts a dark‑light pair (0–1, default 0.25).",
    )
    p.add_argument("--output", required=True, help="Output PNG filepath.")
    args = p.parse_args()

    if not (0 <= args.nudge <= 255):
        p.error("--nudge must be 0–255.")
    if not (0.0 <= args.p_dark <= 1.0):
        p.error("--p_dark must be between 0 and 1.")

    base_rgb = parse_color(args.color)
    img = make_image((args.width, args.height), base_rgb, args.nudge, args.p_dark)

    out_path = Path(args.output)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(out_path, format="PNG")
    print(f"✓ Saved {args.width}×{args.height} PNG → {out_path}")


if __name__ == "__main__":
    main()
