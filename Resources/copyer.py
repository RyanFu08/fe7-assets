import os
import shutil

def duplicate_pngs():
    for dirpath, dirnames, filenames in os.walk('.'):
        for fname in filenames:
            if fname.lower().endswith('.png'):
                if '_ready_2' in fname:
                    src = os.path.join(dirpath, fname)
                    dst = os.path.join(dirpath, fname.replace('_ready_2', '_ready_4'))
                    if not os.path.exists(dst):
                        shutil.copy2(src, dst)
                        print(f"Copied {src} → {dst}")
                if '_stand_2' in fname:
                    src = os.path.join(dirpath, fname)
                    dst = os.path.join(dirpath, fname.replace('_stand_2', '_stand_4'))
                    if not os.path.exists(dst):
                        shutil.copy2(src, dst)
                        print(f"Copied {src} → {dst}")

if __name__ == "__main__":
    duplicate_pngs()
