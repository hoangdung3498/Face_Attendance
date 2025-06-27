# Build NuGet Package Script
Write-Host "Building InferencingSampleCore NuGet Package..." -ForegroundColor Green

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean InferencingSampleCore/InferencingSampleCore.csproj

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build InferencingSampleCore/InferencingSampleCore.csproj --configuration Release

# Pack the project
Write-Host "Creating NuGet package..." -ForegroundColor Yellow
dotnet pack InferencingSampleCore/InferencingSampleCore.csproj --configuration Release --output ./nupkg

Write-Host "NuGet package created successfully!" -ForegroundColor Green
Write-Host "Package location: ./nupkg/" -ForegroundColor Cyan

# List created packages
Get-ChildItem ./nupkg/*.nupkg | ForEach-Object {
    Write-Host "Created: $($_.Name)" -ForegroundColor White
} 