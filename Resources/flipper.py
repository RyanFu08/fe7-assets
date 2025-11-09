import os
from PIL import Image

def flip_right_to_left_pngs(root='.'):
    for dirpath, dirnames, filenames in os.walk(root):
        for fname in filenames:
            if fname.lower().endswith('.png') and 'right' in fname:
                src_path = os.path.join(dirpath, fname)
                dst_fname = fname.replace('right', 'left')
                dst_path = os.path.join(dirpath, dst_fname)

                # Skip if target already exists (optional)
                if os.path.exists(dst_path):
                    print(f"Skipping (already exists): {dst_path}")
                    continue

                # Flip and save
                with Image.open(src_path) as img:
                    flipped = img.transpose(Image.FLIP_LEFT_RIGHT)
                    flipped.save(dst_path)
                    print(f"Saved flipped: {dst_path}")

if __name__ == "__main__":
    flip_right_to_left_pngs('.')
