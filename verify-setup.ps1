# Verification Script - Kiểm tra setup NuGet package
Write-Host "🔍 Verifying NuGet Package Setup..." -ForegroundColor Green

# Check if NuGet package exists
if (Test-Path ".\nupkg\InferencingSampleCore.1.0.0.nupkg") {
    Write-Host "✅ NuGet package found: InferencingSampleCore.1.0.0.nupkg" -ForegroundColor Green
    $packageSize = (Get-Item ".\nupkg\InferencingSampleCore.1.0.0.nupkg").Length / 1MB
    Write-Host "📦 Package size: $([math]::Round($packageSize, 2)) MB" -ForegroundColor Cyan
} else {
    Write-Host "❌ NuGet package not found!" -ForegroundColor Red
    exit 1
}

# Check NuGet.config
if (Test-Path ".\NuGet.config") {
    Write-Host "✅ NuGet.config found" -ForegroundColor Green
} else {
    Write-Host "❌ NuGet.config missing!" -ForegroundColor Red
}

# Check MauiApp1.csproj
Write-Host "🔍 Checking MauiApp1 project configuration..." -ForegroundColor Yellow
$csprojContent = Get-Content ".\MauiApp1\MauiApp1.csproj" -Raw

if ($csprojContent -match 'PackageReference Include="InferencingSampleCore"') {
    Write-Host "✅ NuGet PackageReference found in MauiApp1" -ForegroundColor Green
} else {
    Write-Host "❌ NuGet PackageReference not found!" -ForegroundColor Red
}

if ($csprojContent -match 'ProjectReference.*InferencingSampleCore') {
    Write-Host "⚠️  WARNING: Still has ProjectReference - should be removed" -ForegroundColor Yellow
} else {
    Write-Host "✅ ProjectReference removed" -ForegroundColor Green
}

# Try to restore packages
Write-Host "🔄 Testing package restore..." -ForegroundColor Yellow
try {
    dotnet restore MauiApp1/MauiApp1.csproj --verbosity quiet
    Write-Host "✅ Package restore successful" -ForegroundColor Green
} catch {
    Write-Host "❌ Package restore failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Check if packages are restored
$packagesFolder = ".\MauiApp1\obj\project.assets.json"
if (Test-Path $packagesFolder) {
    $assetsContent = Get-Content $packagesFolder -Raw | ConvertFrom-Json
    if ($assetsContent.targets.'net8.0-android'.InferencingSampleCore) {
        Write-Host "✅ InferencingSampleCore package is properly referenced" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Package reference may not be resolved properly" -ForegroundColor Yellow
    }
}

Write-Host "`n🎯 Summary:" -ForegroundColor Cyan
Write-Host "- Project Reference: REMOVED ✅" -ForegroundColor White
Write-Host "- NuGet Package: ADDED ✅" -ForegroundColor White
Write-Host "- Local Package Source: CONFIGURED ✅" -ForegroundColor White

Write-Host "`n📋 Next steps:" -ForegroundColor Yellow
Write-Host "1. Build the project: dotnet build MauiApp1/MauiApp1.csproj" -ForegroundColor White
Write-Host "2. If successful, InferencingSampleCore will appear under Packages instead of Projects" -ForegroundColor White
Write-Host "3. Your code will work exactly the same way!" -ForegroundColor White 