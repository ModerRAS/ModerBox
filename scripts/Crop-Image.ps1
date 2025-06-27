#requires -Version 5.1

Add-Type -AssemblyName System.Drawing

# --- Configuration ---
$projectRoot = $PSScriptRoot | Split-Path -Parent
$inputFile = Join-Path $projectRoot "ModerBox\Assets\avalonia-logo.png"
$outputFile = Join-Path $projectRoot "ModerBox\Assets\avalonia-logo-cropped.png"
$alphaThreshold = 10 # Pixels with alpha value below this will be considered transparent

# --- Script ---
try {
    Write-Host "Loading image from '$inputFile'..."
    if (-not (Test-Path $inputFile)) {
        throw "Input file not found: $inputFile"
    }

    $sourceBitmap = [System.Drawing.Bitmap]::FromFile($inputFile)

    # Find content bounds
    $minX = $sourceBitmap.Width
    $maxX = 0
    $minY = $sourceBitmap.Height
    $maxY = 0

    Write-Host "Scanning for content bounds..."
    for ($y = 0; $y -lt $sourceBitmap.Height; $y++) {
        for ($x = 0; $x -lt $sourceBitmap.Width; $x++) {
            if ($sourceBitmap.GetPixel($x, $y).A -gt $alphaThreshold) {
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }

    if ($maxX -lt $minX -or $maxY -lt $minY) {
        throw "Could not find any content in the image. It might be fully transparent."
    }

    $contentWidth = $maxX - $minX + 1
    $contentHeight = $maxY - $minY + 1
    Write-Host "Content found at ($minX, $minY) with size ($contentWidth x $contentHeight)"

    # Determine square size
    $squareSize = [Math]::Max($contentWidth, $contentHeight)
    Write-Host "Creating new square canvas of size ($squareSize x $squareSize)"

    # Create new square bitmap and graphics context
    $destBitmap = New-Object System.Drawing.Bitmap($squareSize, $squareSize)
    $graphics = [System.Drawing.Graphics]::FromImage($destBitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)

    # Calculate centered position
    $destX = ($squareSize - $contentWidth) / 2
    $destY = ($squareSize - $contentHeight) / 2

    # Define source and destination rectangles for cropping and drawing
    $sourceRect = New-Object System.Drawing.Rectangle($minX, $minY, $contentWidth, $contentHeight)
    $destRect = New-Object System.Drawing.Rectangle($destX, $destY, $contentWidth, $contentHeight)

    Write-Host "Drawing cropped content onto new canvas..."
    $graphics.DrawImage($sourceBitmap, $destRect, $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)

    Write-Host "Saving cropped square image to '$outputFile'..."
    $destBitmap.Save($outputFile, [System.Drawing.Imaging.ImageFormat]::Png)

    Write-Host "Script completed successfully."
}
catch {
    Write-Error "An error occurred: $_"
}
finally {
    if ($sourceBitmap) { $sourceBitmap.Dispose() }
    if ($destBitmap) { $destBitmap.Dispose() }
    if ($graphics) { $graphics.Dispose() }
} 