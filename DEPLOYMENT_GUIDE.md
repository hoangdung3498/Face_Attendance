# 📦 Hướng dẫn đóng gói và sử dụng InferencingSampleCore Library

## 🎯 Tổng quan các phương pháp

Có **4 cách chính** để đóng gói và sử dụng thư viện `InferencingSampleCore` trong app .NET MAUI khác:

---

## 1. 🏆 **NuGet Package (Khuyến nghị nhất)**

### Ưu điểm:
- ✅ Quản lý dependencies tự động
- ✅ Versioning rõ ràng
- ✅ Dễ cập nhật và phân phối
- ✅ Tích hợp tốt với Visual Studio

### Cách tạo:
```bash
# Build package
dotnet pack InferencingSampleCore/InferencingSampleCore.csproj --configuration Release --output ./nupkg
```

### Cách sử dụng trong project mới:

#### Cách 1: Local NuGet Source
```xml
<!-- Trong .csproj của app mới -->
<ItemGroup>
  <PackageReference Include="InferencingSampleCore" Version="1.0.0" />
</ItemGroup>
```

#### Cách 2: Thêm local package source
```bash
# Thêm local source
nuget sources add -name "Local" -source "C:\path\to\your\nupkg\folder"

# Hoặc trong NuGet.config
```

```xml
<!-- NuGet.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Local" value="./nupkg" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

---

## 2. 📁 **Project Reference (Trong cùng solution)**

### Ưu điểm:
- ✅ Debug trực tiếp vào library code
- ✅ Thay đổi realtime
- ✅ Không cần build package

### Cách sử dụng:
```xml
<!-- Trong .csproj của app mới -->
<ItemGroup>
  <ProjectReference Include="..\InferencingSampleCore\InferencingSampleCore.csproj" />
</ItemGroup>
```

---

## 3. 📚 **DLL Reference**

### Ưu điểm:
- ✅ Đơn giản, nhanh chóng
- ✅ Không cần source code

### Cách tạo DLL:
```bash
dotnet build InferencingSampleCore/InferencingSampleCore.csproj --configuration Release
```

### Cách sử dụng:
```xml
<!-- Trong .csproj của app mới -->
<ItemGroup>
  <Reference Include="InferencingSampleCore">
    <HintPath>path\to\InferencingSampleCore.dll</HintPath>
  </Reference>
</ItemGroup>
```

### Copy các file cần thiết:
- `InferencingSampleCore.dll`
- `*.onnx` files (model files)
- Dependencies DLLs

---

## 4. 🔗 **Git Submodule/Subtree**

### Ưu điểm:
- ✅ Version control toàn bộ source
- ✅ Có thể modify library

### Cách sử dụng:
```bash
# Thêm submodule
git submodule add <repository-url> Libraries/InferencingSampleCore

# Trong .csproj
<ProjectReference Include="Libraries\InferencingSampleCore\InferencingSampleCore.csproj" />
```

---

## 🚀 **Hướng dẫn sử dụng trong app mới**

### Bước 1: Tạo MAUI project mới
```bash
dotnet new maui -n MyNewApp
```

### Bước 2: Thêm thư viện (chọn 1 trong 4 cách trên)

### Bước 3: Thêm using statement
```csharp
using InferencingSample;
```

### Bước 4: Sử dụng trong code
```csharp
public partial class MainPage : ContentPage
{
    private RetinaFace? _detector;
    private CheckPose? _checker;
    private Aligner? _reader;
    private CheckQuality? _quality;
    private Embedding? _embedding;
    private FAS? _fas;

    public MainPage()
    {
        InitializeComponent();
        InitializeModels();
    }

    private void InitializeModels()
    {
        _detector = new RetinaFace();
        _checker = new CheckPose();
        _reader = new Aligner();
        _quality = new CheckQuality();
        _embedding = new Embedding();
        _fas = new FAS();
    }

    // Thêm các pose check methods
    private bool PoseCheckStraight(float yaw, float pitch)
    {
        return Math.Abs(yaw) <= 10 && Math.Abs(pitch) <= 30;
    }

    private bool PoseCheckLeft(float yaw, float pitch)
    {
        return yaw <= 55 && Math.Abs(pitch) <= 30;
    }

    private bool PoseCheckRight(float yaw, float pitch)
    {
        return yaw >= -55 && Math.Abs(pitch) <= 30;
    }
}
```

---

## 📋 **Dependencies cần thiết**

Đảm bảo app mới có các NuGet packages:

```xml
<PackageReference Include="MathNet.Numerics" Version="5.0.0" />
<PackageReference Include="Microsoft.ML.OnnxRuntime" Version="1.16.3" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="SkiaSharp" Version="2.88.7" />
<PackageReference Include="SkiaSharp.Views.Maui" Version="2.88.7" />
<PackageReference Include="System.Drawing.Common" Version="8.0.0" />
```

---

## 🔧 **Troubleshooting**

### Lỗi thiếu ONNX models:
- Đảm bảo copy các file `.onnx` vào project
- Hoặc set chúng là `EmbeddedResource`

### Lỗi Platform support:
- Kiểm tra `TargetFrameworks` trong `.csproj`
- Đảm bảo support platform cần thiết

### Memory issues:
- Dispose objects sau khi sử dụng
- Gọi `GC.Collect()` sau inference nặng

---

## 📝 **Khuyến nghị**

1. **Sử dụng NuGet Package** cho production
2. **Project Reference** cho development
3. **Versioning** rõ ràng khi release
4. **Test trên tất cả platforms** target
5. **Documentation** đầy đủ cho API

---

*Generated: $(Get-Date)* 