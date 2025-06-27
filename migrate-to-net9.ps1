Write-Host "=== Migration Guide từ .NET 8 sang .NET 9 ===" -ForegroundColor Cyan
Write-Host ""

# Kiểm tra .NET SDK
Write-Host "1. Kiểm tra .NET 9 SDK đã được cài đặt:" -ForegroundColor Yellow
dotnet --list-sdks | Where-Object { $_ -like "*9.0*" }
Write-Host ""

# Bước 1: Clean solution
Write-Host "2. Clean toàn bộ solution:" -ForegroundColor Yellow
Write-Host "   Đang thực hiện clean..." -ForegroundColor Gray
dotnet clean

# Bước 2: Build library
Write-Host ""
Write-Host "3. Build InferencingSampleCore library:" -ForegroundColor Yellow
Write-Host "   Đang build library..." -ForegroundColor Gray
dotnet build InferencingSampleCore/InferencingSampleCore.csproj --configuration Release

# Bước 3: Pack library
Write-Host ""
Write-Host "4. Tạo NuGet package mới:" -ForegroundColor Yellow
Write-Host "   Đang tạo package..." -ForegroundColor Gray
dotnet pack InferencingSampleCore/InferencingSampleCore.csproj --configuration Release --output ./nupkg

# Bước 4: Clear NuGet cache
Write-Host ""
Write-Host "5. Clear NuGet cache để đảm bảo sử dụng package mới:" -ForegroundColor Yellow
dotnet nuget locals all --clear

# Bước 5: Restore MauiApp1
Write-Host ""
Write-Host "6. Restore packages cho MauiApp1:" -ForegroundColor Yellow
Write-Host "   Đang restore..." -ForegroundColor Gray
dotnet restore MauiApp1/MauiApp1.csproj

# Bước 6: Build MauiApp1
Write-Host ""
Write-Host "7. Build MauiApp1:" -ForegroundColor Yellow
Write-Host "   Đang build MauiApp1..." -ForegroundColor Gray
dotnet build MauiApp1/MauiApp1.csproj --configuration Release

Write-Host ""
Write-Host "=== Hoàn thành migration sang .NET 9! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Lưu ý:" -ForegroundColor Cyan
Write-Host "- Đảm bảo bạn đã cài đặt .NET 9 SDK từ: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor White
Write-Host "- Workload MAUI cho .NET 9: dotnet workload install maui" -ForegroundColor White
Write-Host "- Nếu gặp lỗi, hãy chạy: dotnet workload restore" -ForegroundColor White 