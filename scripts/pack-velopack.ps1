[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Version = '2.5.1'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Get-Item -LiteralPath (Join-Path -Path $PSScriptRoot -ChildPath '..')).FullName
$projectPath = Join-Path -Path $repositoryRoot -ChildPath 'CassieWordCheck.WinUI\CassieWordCheck.WinUI.csproj'
$publishPathCandidate = Join-Path -Path $repositoryRoot -ChildPath 'dist\publish\win-x64'
$releasesPathCandidate = Join-Path -Path $repositoryRoot -ChildPath 'dist\velopack\Releases'
$packId = 'qingranawa.CassieWordCheck'
$fileVersion = "$Version.0"

function Get-SafeOutputPath {
    param(
        [Parameter(Mandatory)]
        [string]$CandidatePath,

        [Parameter(Mandatory)]
        [string]$ExpectedRelativePath
    )

    $rootPath = [System.IO.Path]::GetFullPath($repositoryRoot).TrimEnd('\')
    $expectedPath = [System.IO.Path]::GetFullPath((Join-Path -Path $rootPath -ChildPath $ExpectedRelativePath))
    $candidatePath = [System.IO.Path]::GetFullPath($CandidatePath)
    $rootPrefix = "$rootPath\"

    if (-not [string]::Equals($candidatePath, $expectedPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "输出目录路径与预期不符：$candidatePath"
    }

    if (-not $expectedPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "拒绝处理仓库根目录外的输出目录：$expectedPath"
    }

    $existingPath = $candidatePath
    while (-not (Test-Path -LiteralPath $existingPath)) {
        $parentPath = Split-Path -Path $existingPath -Parent
        if ([string]::IsNullOrWhiteSpace($parentPath) -or [string]::Equals($parentPath, $existingPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "无法解析输出目录的现存父路径：$candidatePath"
        }

        $existingPath = $parentPath
    }

    $existingItem = Get-Item -LiteralPath $existingPath -Force
    if (($existingItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "拒绝处理重解析输出路径：$existingPath"
    }

    $resolvedExistingPath = (Resolve-Path -LiteralPath $existingPath).Path
    $isRepositoryRoot = [string]::Equals($resolvedExistingPath, $rootPath, [System.StringComparison]::OrdinalIgnoreCase)
    $isRepositoryDescendant = $resolvedExistingPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)
    if (-not ($isRepositoryRoot -or $isRepositoryDescendant)) {
        throw "输出目录的现存父路径不在仓库内：$resolvedExistingPath"
    }

    if (Test-Path -LiteralPath $candidatePath) {
        $item = Get-Item -LiteralPath $candidatePath -Force
        if (-not $item.PSIsContainer) {
            throw "输出路径不是目录：$candidatePath"
        }

        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "拒绝处理重解析输出目录：$candidatePath"
        }

        $resolvedPath = (Resolve-Path -LiteralPath $candidatePath).Path
        if (-not [string]::Equals($resolvedPath, $expectedPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "输出目录解析后不在预期位置：$resolvedPath"
        }
    }

    return $candidatePath
}

function Remove-SafeOutputDirectory {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (Test-Path -LiteralPath $Path) {
        Write-Host "清理输出目录：$Path"
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Invoke-DotnetCommand {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet 命令失败，退出码 $LASTEXITCODE：dotnet $($Arguments -join ' ')"
    }
}

function Get-RequiredOutputFile {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "缺少必需输出文件：$Path"
    }

    return (Get-Item -LiteralPath $Path).FullName
}

$publishPath = Get-SafeOutputPath -CandidatePath $publishPathCandidate -ExpectedRelativePath 'dist\publish\win-x64'
$releasesPath = Get-SafeOutputPath -CandidatePath $releasesPathCandidate -ExpectedRelativePath 'dist\velopack\Releases'
$locationPushed = $false
$exitCode = 0

try {
    Push-Location -LiteralPath $repositoryRoot
    $locationPushed = $true

    Remove-SafeOutputDirectory -Path $publishPath
    Remove-SafeOutputDirectory -Path $releasesPath
    New-Item -ItemType Directory -Path $releasesPath -Force | Out-Null

    Write-Host "恢复本地 .NET 工具清单"
    Invoke-DotnetCommand -Arguments @('tool', 'restore')

    Write-Host "发布 WinUI 应用：$Configuration / $Version"
    Invoke-DotnetCommand -Arguments @(
        'publish',
        $projectPath,
        '-c', $Configuration,
        '-r', 'win-x64',
        '--self-contained', 'true',
        '-p:PublishProfile=velopack-win-x64.pubxml',
        '-p:Platform=x64',
        "-p:Version=$Version",
        "-p:FileVersion=$fileVersion",
        '-o', $publishPath
    )

    Write-Host "生成 Velopack 安装包：$packId / $Version"
    Invoke-DotnetCommand -Arguments @(
        'tool', 'run', 'vpk', 'pack',
        '--packId', $packId,
        '--packVersion', $Version,
        '--packDir', $publishPath,
        '--mainExe', 'CassieWordCheck.exe',
        '--packTitle', 'CassieWordCheck',
        '--outputDir', $releasesPath
    )

    $setupFiles = @(Get-ChildItem -LiteralPath $releasesPath -Filter '*-Setup.exe' -File)
    if ($setupFiles.Count -ne 1) {
        throw "未找到唯一的 vpk 安装器：$releasesPath\*-Setup.exe"
    }

    $setupPath = $setupFiles[0].FullName
    $feedPath = Get-RequiredOutputFile -Path (Join-Path -Path $releasesPath -ChildPath 'releases.win.json')
    $mainExePath = Get-RequiredOutputFile -Path (Join-Path -Path $publishPath -ChildPath 'CassieWordCheck.exe')
    $fullPackages = @(Get-ChildItem -LiteralPath $releasesPath -Filter '*-full.nupkg' -File)

    if ($fullPackages.Count -eq 0) {
        throw "缺少必需完整包：$releasesPath\*-full.nupkg"
    }

    Write-Host "已生成安装器：$setupPath"
    Write-Host "已生成发布 feed：$feedPath"
    foreach ($fullPackage in $fullPackages) {
        Write-Host "已生成完整包：$($fullPackage.FullName)"
    }
    Write-Host "已生成主程序：$mainExePath"
}
catch {
    Write-Error $_
    $exitCode = 1
}
finally {
    if ($locationPushed) {
        Pop-Location
    }
}

if ($exitCode -ne 0) {
    exit $exitCode
}
