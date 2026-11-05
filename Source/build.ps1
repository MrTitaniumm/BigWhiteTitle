#requires -Version 5.1
<#
.SYNOPSIS
    编译 BigWhiteTitle，支持 1.6（Unity）与 1.7（Waylith）两个目标。

.DESCRIPTION
    两个版本的引用集完全不同：

    1.6  Unity 引擎
         Assembly-CSharp.dll + 9 个 UnityEngine.* 模块，位于 RimWorldWin64_Data\Managed
         依赖 netstandard2.1 引用包

    1.7  Waylith 引擎（自研，自带 .NET 10 运行时）
         程序集全部搬到游戏根目录
         Unity 兼容层由 UnityStubs.dll 提供（建模用桩，不是运行时实现）
         没有 UnityEngine.* 模块，改用游戏自带的 System.*.dll

    产物按版本分目录：<mod>\1.6\Assemblies、<mod>\1.7\Assemblies，
    由 LoadFolders.xml 选择。

.PARAMETER Target
    1.6 / 1.7 / all（默认 all）

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Target 1.7
#>
[CmdletBinding()]
param(
    [ValidateSet('1.6', '1.7', 'all')]
    [string]$Target = 'all',
    [string]$GameDir = 'D:\steam\steamapps\common\RimWorld',
    [string]$Dotnet  = 'C:\Program Files\dotnet\dotnet.exe'
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$modRoot = Split-Path $here -Parent

# --- 找 csc ---
$sdkDir = Get-ChildItem 'C:\Program Files\dotnet\sdk' -Directory -ErrorAction SilentlyContinue |
          Sort-Object Name -Descending | Select-Object -First 1
if (-not $sdkDir) { throw '没找到 .NET SDK 目录' }
$csc = Join-Path $sdkDir.FullName 'Roslyn\bincore\csc.dll'
if (-not (Test-Path -LiteralPath $csc)) { throw "没找到 csc.dll: $csc" }

$sources = @(Get-ChildItem $here -Filter *.cs -Recurse)
if ($sources.Count -eq 0) { throw '没有找到 .cs 源文件' }

function Build-Version([string]$ver) {
    Write-Host ""
    Write-Host "===== $ver =====" -ForegroundColor Cyan

    $refs = New-Object System.Collections.Generic.List[string]
    $nostdlib = $false

    if ($ver -eq '1.6') {
        $managed = Join-Path $GameDir 'RimWorldWin64_Data\Managed'

        # 游戏升到 1.7 后这个目录会消失，此时退回本地备份
        if (-not (Test-Path -LiteralPath $managed)) {
            $backup = Join-Path (Split-Path $modRoot -Parent) 'refs\1.6\Managed'
            if (Test-Path -LiteralPath $backup) {
                Write-Host "  用备份引用: $backup" -ForegroundColor DarkYellow
                $managed = $backup
            } else {
                throw "找不到 1.6 程序集目录。`n  实际路径: $managed`n  备份路径: $backup`n游戏已更新到 1.7 时，需先切回 1.6 分支备份一次 Managed 目录。"
            }
        }

        $refPack = Get-ChildItem 'C:\Program Files\dotnet\packs\NETStandard.Library.Ref' -Directory -ErrorAction SilentlyContinue |
                   Sort-Object Name -Descending | Select-Object -First 1
        if (-not $refPack) { throw '没找到 NETStandard.Library.Ref 引用包' }
        $refDir = Join-Path $refPack.FullName 'ref\netstandard2.1'
        foreach ($d in Get-ChildItem $refDir -Filter *.dll) { $refs.Add($d.FullName) }

        $names = @('Assembly-CSharp.dll','UnityEngine.dll','UnityEngine.CoreModule.dll',
                   'UnityEngine.IMGUIModule.dll','UnityEngine.UIModule.dll','UnityEngine.UI.dll',
                   'UnityEngine.TextRenderingModule.dll','UnityEngine.AudioModule.dll',
                   'UnityEngine.ImageConversionModule.dll')
        foreach ($n in $names) {
            $p = Join-Path $managed $n
            if (-not (Test-Path -LiteralPath $p)) { throw "缺少程序集: $p" }
            $refs.Add($p)
        }
    }
    else {
        # 1.7：程序集在游戏根目录
        $nostdlib = $true
        foreach ($n in @('Assembly-CSharp.dll','UnityStubs.dll','Waylith.dll')) {
            $p = Join-Path $GameDir $n
            if (-not (Test-Path -LiteralPath $p)) { throw "缺少程序集: $p （游戏是否还是 1.6？）" }
            $refs.Add($p)
        }
        $bcl = Get-ChildItem $GameDir -File -Filter 'System.*.dll' |
               Where-Object { $_.Name -notmatch 'Native' } | Select-Object -ExpandProperty FullName
        foreach ($b in $bcl) { $refs.Add($b) }
        $ns = Join-Path $GameDir 'netstandard.dll'
        if (Test-Path -LiteralPath $ns) { $refs.Add($ns) }
    }

    # --- 输出 ---
    $outDir = Join-Path $modRoot "$ver\Assemblies"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $outDll = Join-Path $outDir 'BigWhiteTitle.dll'

    if (Test-Path -LiteralPath $outDll) {
        try { Remove-Item -LiteralPath $outDll -Force -ErrorAction Stop }
        catch { throw "输出文件被占用，删不掉：$outDll`n先完全退出 RimWorld 再编译。" }
    }

    $cscArgs = New-Object System.Collections.Generic.List[string]
    foreach ($a in @('/nologo','/target:library','/optimize+','/deterministic',
                     '/langversion:latest','/noconfig','/nowarn:1701,1702,0618')) {
        $cscArgs.Add($a)
    }
    if ($nostdlib) { $cscArgs.Add('/nostdlib+') }
    $cscArgs.Add("/out:$outDll")
    foreach ($r in $refs) { $cscArgs.Add("/reference:$r") }
    foreach ($s in $sources) { $cscArgs.Add($s.FullName) }

    Write-Host "  refs: $($refs.Count)  sources: $($sources.Count)"

    & $Dotnet $csc @cscArgs
    if ($LASTEXITCODE -ne 0) { throw "$ver 编译失败，exit=$LASTEXITCODE" }

    $kb = [math]::Round((Get-Item $outDll).Length / 1KB, 1)
    Write-Host "  OK -> $outDll  ($kb KB)" -ForegroundColor Green
}

if ($Target -eq 'all') {
    $ok = @()
    foreach ($v in @('1.6', '1.7')) {
        try { Build-Version $v; $ok += $v }
        catch { Write-Host "  $v 跳过：$($_.Exception.Message)" -ForegroundColor Yellow }
    }
    Write-Host ""
    Write-Host "完成：$($ok -join ', ')"
} else {
    Build-Version $Target
}
