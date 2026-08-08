$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$packScript = Join-Path $repositoryRoot 'scripts\pack-velopack.ps1'
$testProject = Join-Path $repositoryRoot 'CassieWordCheck.Tests\CassieWordCheck.Tests.csproj'
$publishDirectory = Join-Path $repositoryRoot 'dist\publish\win-x64'
$releasesDirectory = Join-Path $repositoryRoot 'dist\velopack\Releases'

Push-Location $repositoryRoot
try {
    Write-Host '[1/3] 验证核心测试...'
    & dotnet test $testProject --configuration Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw '核心测试失败' }

    Write-Host '[2/3] 生成并校验 Velopack 发布产物...'
    & $packScript -Configuration Release -Version 2.5.1
    if ($LASTEXITCODE -ne 0) { throw 'Velopack 打包失败' }

    if (-not (Test-Path -LiteralPath $releasesDirectory -PathType Container)) {
        throw "未找到 Velopack 发布目录：$releasesDirectory"
    }
    if (-not (Test-Path -LiteralPath $publishDirectory -PathType Container)) {
        throw "未找到应用发布目录：$publishDirectory"
    }

    $setupFiles = @(Get-ChildItem -LiteralPath $releasesDirectory -Filter '*-Setup.exe' -File)
    if ($setupFiles.Count -ne 1) {
        throw "未找到唯一的 Velopack 安装器：$releasesDirectory\*-Setup.exe"
    }

    $feedPath = Join-Path $releasesDirectory 'releases.win.json'
    if (-not (Test-Path -LiteralPath $feedPath -PathType Leaf)) {
        throw "未找到 Velopack 发布 feed：$feedPath"
    }

    $fullPackages = @(Get-ChildItem -LiteralPath $releasesDirectory -Filter '*-full.nupkg' -File)
    if ($fullPackages.Count -eq 0) {
        throw "未找到 Velopack 完整包：$releasesDirectory\*-full.nupkg"
    }

    $mainExecutablePath = Join-Path $publishDirectory 'CassieWordCheck.exe'
    if (-not (Test-Path -LiteralPath $mainExecutablePath -PathType Leaf)) {
        throw "未找到发布目录中的主程序：$mainExecutablePath"
    }

    $fileVersion = (Get-Item -LiteralPath $mainExecutablePath).VersionInfo.FileVersion
    if ($fileVersion -notmatch '^2\.5\.1\.0(?:\s|$)') {
        throw "主程序文件版本不正确：$fileVersion"
    }

    Write-Host '[3/3] 扫描生产代码中的 WPF 残留...'
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
    Write-Host $setupFiles[0].FullName
    Write-Host $feedPath
    foreach ($fullPackage in $fullPackages) {
        Write-Host $fullPackage.FullName
    }
    Write-Host $mainExecutablePath
}
finally {
    Pop-Location
}
