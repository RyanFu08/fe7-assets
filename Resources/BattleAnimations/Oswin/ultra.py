#!/usr/bin/env python3
import os
import shutil

INPUT_FILE = "instruct.txt"
OUTPUT_DIR = "florina_copies"


def main():
    # Check that the instruction file exists
    if not os.path.isfile(INPUT_FILE):
        print(f"Error: '{INPUT_FILE}' not found.")
        return

    # Create the output directory if needed
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    seq = 1  # Global sequence counter

    with open(INPUT_FILE, 'r') as f:
        for line in f:
            line = line.strip()
            # Skip empty lines or those starting with 'c'
            if not line or line.startswith('C'):
                continue

            # Parse lines like "N p- basename"
            parts = line.split(' p- ', 1)
            if len(parts) == 2 and parts[0].isdigit():
                count = int(parts[0])
                basename = parts[1]
            else:
                # Default to one copy if no count prefix
                count = 1
                basename = line

            # Prefix with 'florina_' when locating the source file
            source_name = f"{basename}"
            # Determine extension (default to .png if none)
            _, ext = os.path.splitext(source_name)
            ext = ext or ".png"

            for _ in range(count):
                if not os.path.isfile(source_name):
                    print(f"Skipped (not found): {source_name}")
                    continue

                # Zero-pad the sequence number to three digits and append extension
                dest_filename = f"oswin_Axe_{str(seq).zfill(3)}{ext}"
                dest_path = os.path.join(OUTPUT_DIR, dest_filename)

                shutil.copy(source_name, dest_path)
                print(f"Copied: {source_name} → {dest_path}")
                seq += 1


if __name__ == "__main__":
    main()
