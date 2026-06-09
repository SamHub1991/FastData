# FastData 统一构建脚本
# 用途：本地构建、测试、打包全流程编排
# 使用：.\build.ps1 [-Action Build|Test|Pack|All] [-Platform cross|windows] [-Configuration Release|Debug]

param(
    [ValidateSet('Build', 'Test', 'Pack', 'All')]
    [string]$Action = 'All',
    
    [ValidateSet('cross', 'windows')]
    [string]$Platform = 'cross',
    
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$SolutionPath = "$PSScriptRoot\FastData.sln"
$ArtifactsDir = "$PSScriptRoot\artifacts"

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Invoke-Build {
    Write-Step '执行构建'
    dotnet build $SolutionPath -p:BuildPlatform=$Platform -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw '构建失败'
    }
}

function Invoke-Test {
    Write-Step '执行单元测试'
    dotnet test "$PSScriptRoot\FastData.Tests\FastData.Tests.csproj" `
        -c $Configuration `
        --no-build `
        -v minimal `
        --logger "console;verbosity=normal" `
        --filter "FullyQualifiedName!~Integration"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning '部分测试未通过，继续执行'
    }
    
    Write-Step '生成代码覆盖率报告'
    if (Test-Path "$PSScriptRoot\FastData.Tests\FastData.Tests.csproj") {
        dotnet test "$PSScriptRoot\FastData.Tests\FastData.Tests.csproj" `
            -c $Configuration `
            --no-build `
            --collect:"XPlat Code Coverage" `
            --results-directory "$ArtifactsDir\coverage" `
            --filter "FullyQualifiedName!~Integration"
    }
}

function Invoke-Pack {
    Write-Step '执行打包'
    New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null
    
    dotnet pack "$PSScriptRoot\FastData\FastData.csproj" -c $Configuration --no-build -o $ArtifactsDir
    if ($LASTEXITCODE -ne 0) {
        throw 'FastData 打包失败'
    }
    
    dotnet pack "$PSScriptRoot\FastData.Untility\FastData.Untility.csproj" -c $Configuration --no-build -o $ArtifactsDir
    if ($LASTEXITCODE -ne 0) {
        throw 'FastData.Untility 打包失败'
    }
    
    Write-Host "`n打包产物:" -ForegroundColor Green
    Get-ChildItem $ArtifactsDir\*.nupkg | ForEach-Object {
        Write-Host "  $($_.Name) ($([math]::Round($_.Length / 1KB, 1)) KB)" -ForegroundColor Yellow
    }
}

switch ($Action) {
    'Build' { Invoke-Build }
    'Test'  { Invoke-Build; Invoke-Test }
    'Pack'  { Invoke-Build; Invoke-Pack }
    'All'   { Invoke-Build; Invoke-Test; Invoke-Pack }
}

Write-Host "`n构建完成: $Action ($Platform, $Configuration)" -ForegroundColor Green
