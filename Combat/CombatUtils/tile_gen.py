from PIL import Image, ImageDraw

# Generate 10 frames with increasing alpha values
for i in range(10):
    img = Image.new("RGBA", (13, 13), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    alpha = int(255 * (i + 1) / 10)  # gradually from 25 to 255
    blue = (255, 0, 0, alpha)
    white = (255, 255, 255, 255)

    # Draw inner blue square (leave 1px border)
    draw.rectangle([1, 1, 11, 11], fill=blue)

    # Draw white border
    draw.rectangle([0, 0, 12, 12], outline=white, width=1)

    img.save(f"red_square_frame_{i+1}.png")
