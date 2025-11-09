#!/usr/bin/env python3
"""
Batch‐crop all 488×160 PNGs in this folder down to 288×160
by keeping only the leftmost 288 pixels.
"""

import os
from PIL import Image

def crop_leftmost(input_path: str, output_path: str, target_width: int = 288):
    """
    Open the image at input_path, crop it to (target_width x original_height)
    from the left edge, and save it to output_path.
    """
    with Image.open(input_path) as img:
        w, h = img.size
        if w == 488 and h == 160:
            # Define box: (left, upper, right, lower)
            box = (0, 0, target_width, h)
            cropped = img.crop(box)
            cropped.save(output_path)
            return True
    return False

def main():
    # Directory containing this script
    base_dir = os.path.dirname(os.path.abspath(__file__))
    count = 0

    for fname in os.listdir(base_dir):
        if not fname.lower().endswith('.png'):
            continue

        full_path = os.path.join(base_dir, fname)
        try:
            if crop_leftmost(full_path, full_path):
                print(f"Cropped: {fname}")
                count += 1
        except Exception as e:
            print(f"Failed to process {fname}: {e}")

    print(f"\nDone! {count} file(s) cropped.")

if __name__ == '__main__':
    main()
