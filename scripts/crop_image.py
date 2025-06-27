import os
from PIL import Image

def main():
    try:
        # --- Configuration ---
        project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        input_path = os.path.join(project_root, "ModerBox", "Assets", "avalonia-logo.png")
        output_path = os.path.join(project_root, "ModerBox", "Assets", "avalonia-logo-cropped.png")
        inset_margin = 100  # Pixels to skip from each border as per user's instruction

        print(f"Loading image from: {input_path}")
        if not os.path.exists(input_path):
            raise FileNotFoundError(f"Input file not found: {input_path}")

        # Open the image
        with Image.open(input_path) as img:
            img = img.convert("RGBA")

            # Step 1: Perform an initial crop to remove the problematic outer 100px border
            if img.width <= 2 * inset_margin or img.height <= 2 * inset_margin:
                raise ValueError(f"Image is too small to apply an inset of {inset_margin} pixels.")
            
            print(f"Applying initial inset crop of {inset_margin}px from all sides.")
            pre_cropped_box = (inset_margin, inset_margin, img.width - inset_margin, img.height - inset_margin)
            pre_cropped_img = img.crop(pre_cropped_box)

            # Step 2: Find the bounding box of the content within the pre-cropped image
            alpha = pre_cropped_img.split()[-1]
            bbox_on_precropped = alpha.getbbox()

            if not bbox_on_precropped:
                raise ValueError("No content found after initial inset crop. The image inside the border might be fully transparent.")

            print(f"Content bounding box found at: {bbox_on_precropped} (relative to the pre-cropped image)")

            # Step 3: Crop to the final content
            final_cropped_img = pre_cropped_img.crop(bbox_on_precropped)
            
            # Step 4: Make it square
            width, height = final_cropped_img.size
            square_size = max(width, height)
            
            print(f"Creating new square canvas of size: {square_size}x{square_size}")
            
            square_img = Image.new("RGBA", (square_size, square_size), (0, 0, 0, 0))
            
            paste_x = (square_size - width) // 2
            paste_y = (square_size - height) // 2
            
            square_img.paste(final_cropped_img, (paste_x, paste_y), final_cropped_img)

            print(f"Saving final cropped square image to: {output_path}")
            square_img.save(output_path, "PNG")

        print("Script completed successfully.")

    except Exception as e:
        print(f"An error occurred: {e}")

if __name__ == "__main__":
    main() 