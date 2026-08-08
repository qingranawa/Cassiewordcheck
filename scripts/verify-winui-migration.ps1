$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$winUiProject = Join-Path $repositoryRoot 'CassieWordCheck.WinUI\CassieWordCheck.WinUI.csproj'
$testProject = Join-Path $repositoryRoot 'CassieWordCheck.Tests\CassieWordCheck.Tests.csproj'
$publishProfile = 'Properties\PublishProfiles\win10-x64.pubxml'
$inspectDirectory = $null

Push-Location $repositoryRoot
try {
    Write-Host '[1/4] 验证核心测试...'
    & dotnet restore $winUiProject --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'WinUI restore 失败' }
    & dotnet test $testProject --configuration Release --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw '核心测试失败' }

    Write-Host '[2/4] 构建 WinUI 并生成 MSIX...'
    & dotnet build $winUiProject --configuration Release --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'WinUI 构建失败' }
    & dotnet build $winUiProject --configuration Release --no-restore --verbosity quiet `
        -p:Platform=x64 `
        -p:PublishProfile=$publishProfile `
        -p:GenerateAppxPackageOnBuild=true `
        -p:AppxPackageSigningEnabled=false
    if ($LASTEXITCODE -ne 0) { throw 'MSIX 生成失败' }

    $msix = Get-ChildItem (Join-Path $repositoryRoot 'CassieWordCheck.WinUI\bin\Release\AppPackages') -Recurse -Filter '*.msix' -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -eq $msix) { throw '未找到 MSIX 输出' }

    Write-Host '[3/4] 解包并校验 MSIX 清单...'
    $inspectDirectory = Join-Path ([IO.Path]::GetTempPath()) 'CassieWordCheck-msix-inspect'
    if (Test-Path -LiteralPath $inspectDirectory) {
        Remove-Item -LiteralPath $inspectDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $inspectDirectory -Force | Out-Null

    $makeAppx = Get-Command makeappx.exe -ErrorAction SilentlyContinue
    if ($null -eq $makeAppx) {
        $makeAppx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Filter makeappx.exe -Recurse -File -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
    }
    if ($null -eq $makeAppx) { throw '未找到 makeappx.exe，请安装 Windows 10/11 SDK' }

    $makeAppxPath = if ($makeAppx.PSObject.Properties.Name -contains 'Source') { $makeAppx.Source } else { $makeAppx.FullName }
    & $makeAppxPath unpack /p $msix.FullName /d $inspectDirectory /o
    if ($LASTEXITCODE -ne 0) { throw 'MSIX 解包失败' }
    $manifestPath = Join-Path $inspectDirectory 'AppxManifest.xml'
    [xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
    $identity = $manifest.Package.Identity
    if ($identity.Name -ne 'CassieWordCheck' -or $identity.Version -ne '2.5.0.0') {
        throw "MSIX 身份不正确：$($identity.Name) $($identity.Version)"
    }

    Write-Host '[4/4] 扫描生产代码中的 WPF 残留...'
    $excludedDirectories = '[\\/](bin|obj|\.git|docs|\.superpowers)[\\/]'
    $productionFiles = Get-ChildItem $repositoryRoot -Recurse -File |
        Where-Object { $_.FullName -notmatch $excludedDirectories -and $_.Extension -in '.cs', '.xaml', '.csproj', '.sln', '.wapproj' }
    $forbiddenPattern = 'System\.Windows|UseWPF|FlowDocument|MaterialDesignThemes|Microsoft\.Win32\.(OpenFileDialog|SaveFileDialog)|Microsoft\.DesktopBridge'
    $matches = $productionFiles | Select-String -Pattern $forbiddenPattern
    if ($matches) {
        $matches | ForEach-Object { Write-Error $_.ToString() }
        throw '发现 WPF 或旧 WAP 生产引用'
    }

    Write-Host '验证通过'
    Write-Host $msix.FullName
}
finally {
    if ($inspectDirectory -and (Test-Path -LiteralPath $inspectDirectory)) {
        Remove-Item -LiteralPath $inspectDirectory -Recurse -Force
    }
    Pop-Location
}
