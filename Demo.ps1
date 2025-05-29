# ZIP Splitter Demonstration Script

Write-Host "=== ZIP Splitter Demo Script ===" -ForegroundColor Green
Write-Host ""
Write-Host "This script demonstrates both demo modes of the ZIP Splitter utility:" -ForegroundColor Cyan
Write-Host "1. Quick Demo - Fast demonstration with 2.9MB files" -ForegroundColor Yellow
Write-Host "2. Enhanced Progress Demo - Visual progress bar with 15MB files" -ForegroundColor Yellow
Write-Host ""

# Clean up previous demo files
@("DemoSource", "DemoOutput", "EnhancedDemoSource", "EnhancedDemoOutput") | ForEach-Object {
    if (Test-Path $_) {
        Write-Host "Cleaning up previous demo files ($_)..." -ForegroundColor Yellow
        Remove-Item $_ -Recurse -Force
    }
}

Write-Host "Building the solution..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Running Quick Demo (Option 1)..." -ForegroundColor Yellow
Write-Host ""

# Run quick demo
"1" | dotnet run --project ZipSplitter.Console

Write-Host ""
Write-Host "Running Enhanced Progress Demo (Option 2)..." -ForegroundColor Yellow
Write-Host ""

# Run enhanced demo
"2" | dotnet run --project ZipSplitter.Console

Write-Host ""
Write-Host "Analyzing results..." -ForegroundColor Yellow

Write-Host ""
Write-Host "Quick Demo Results:" -ForegroundColor Green
if (Test-Path "DemoOutput") {
    Get-ChildItem "DemoOutput" -Filter "*.zip" | ForEach-Object {
        $size = [math]::Round($_.Length / 1KB, 2)
        Write-Host "  $($_.Name) - $size KB" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "Enhanced Demo Results:" -ForegroundColor Green
if (Test-Path "EnhancedDemoOutput") {
    Get-ChildItem "EnhancedDemoOutput" -Filter "*.zip" | ForEach-Object {
        $size = [math]::Round($_.Length / 1MB, 2)
        Write-Host "  $($_.Name) - $size MB" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "Demo completed! Both demo modes showcase different aspects:" -ForegroundColor Green
Write-Host "- Quick Demo: Fast processing with basic progress reporting" -ForegroundColor Yellow
Write-Host "- Enhanced Demo: Visual progress bar with realistic file sizes" -ForegroundColor Yellow
Write-Host "You can examine the files in DemoSource/DemoOutput and EnhancedDemoSource/EnhancedDemoOutput directories." -ForegroundColor Cyan
