# ZIP Splitter Demonstration Script

Write-Host "=== ZIP Splitter Demo Script ===" -ForegroundColor Green
Write-Host ""

# Clean up previous demo files
if (Test-Path "DemoSource") {
    Write-Host "Cleaning up previous demo files..." -ForegroundColor Yellow
    Remove-Item "DemoSource" -Recurse -Force
}
if (Test-Path "DemoOutput") {
    Remove-Item "DemoOutput" -Recurse -Force
}

Write-Host "Building the solution..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Running ZIP Splitter demo..." -ForegroundColor Yellow
Write-Host ""

# Run the demo
dotnet run --project ZipSplitter.Console

Write-Host ""
Write-Host "Analyzing results..." -ForegroundColor Yellow

if (Test-Path "DemoOutput") {
    Write-Host ""
    Write-Host "Created ZIP files:" -ForegroundColor Green
    Get-ChildItem "DemoOutput" -Filter "*.zip" | ForEach-Object {
        $size = [math]::Round($_.Length / 1KB, 2)
        Write-Host "  $($_.Name) - $size KB" -ForegroundColor Cyan
    }
    
    Write-Host ""
    Write-Host "Original source files:" -ForegroundColor Green
    if (Test-Path "DemoSource") {
        Get-ChildItem "DemoSource" -Recurse -File | ForEach-Object {
            $size = [math]::Round($_.Length / 1KB, 2)
            $relativePath = $_.FullName.Replace((Get-Location).Path + "\DemoSource\", "")
            Write-Host "  $relativePath - $size KB" -ForegroundColor Cyan
        }
    }
}

Write-Host ""
Write-Host "Demo completed! You can examine the files in DemoSource and DemoOutput directories." -ForegroundColor Green
