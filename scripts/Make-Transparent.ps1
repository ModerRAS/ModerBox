#requires -Version 5.1

Add-Type -AssemblyName System.Drawing

# --- Configuration ---
$projectRoot = $PSScriptRoot | Split-Path -Parent
$inputFile = Join-Path $projectRoot "ModerBox\Assets\avalonia-logo.bmp"
$outputFile = Join-Path $projectRoot "ModerBox\Assets\avalonia-logo-transparent.png"
$targetColor = [System.Drawing.Color]::FromArgb(255, 255, 255, 255) # White
$tolerance = 10 # Tolerance for color matching to handle anti-aliasing artifacts near edges

# --- Script ---
try {
    Write-Host "Loading image from '$inputFile'..."
    if (-not (Test-Path $inputFile)) {
        throw "Input file not found: $inputFile"
    }

    $originalBitmap = [System.Drawing.Bitmap]::FromFile($inputFile)
    $transparentBitmap = New-Object System.Drawing.Bitmap($originalBitmap.Width, $originalBitmap.Height)

    Write-Host "Processing pixels..."
    for ($y = 0; $y -lt $originalBitmap.Height; $y++) {
        for ($x = 0; $x -lt $originalBitmap.Width; $x++) {
            $originalColor = $originalBitmap.GetPixel($x, $y)

            # Check if the color is within the tolerance range of pure white
            $isWhite = ($originalColor.R -ge ($targetColor.R - $tolerance)) -and
                       ($originalColor.G -ge ($targetColor.G - $tolerance)) -and
                       ($originalColor.B -ge ($targetColor.B - $tolerance))

            if ($isWhite) {
                # Set pixel to transparent
                $transparentBitmap.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            } else {
                # Copy original pixel
                $transparentBitmap.SetPixel($x, $y, $originalColor)
            }
        }
    }

    Write-Host "Saving new transparent image to '$outputFile'..."
    $transparentBitmap.Save($outputFile, [System.Drawing.Imaging.ImageFormat]::Png)

    Write-Host "Script completed successfully."
}
catch {
    Write-Error "An error occurred: $_"
}
finally {
    if ($originalBitmap) { $originalBitmap.Dispose() }
    if ($transparentBitmap) { $transparentBitmap.Dispose() }
} 