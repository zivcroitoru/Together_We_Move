param([string]$UnityEditor='C:/Program Files/Unity/Hub/Editor/6000.3.21f1/Editor')
$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testOutput=Join-Path $projectRoot 'Logs/TutorialTests'
New-Item -ItemType Directory -Force -Path $testOutput | Out-Null
$dotnet=Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
$compiler=Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
$mono=Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe'
$api=Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.7.1-api'
$cases=@(
    @{Name='TutorialRegression'; Sources=@('Tools/Tests/TutorialRegression.cs','Assets/Scripts/VisualUI/GuidanceTiming.cs','Assets/Scripts/VisualUI/TutorialProgress.cs','Assets/Scripts/VisualUI/TutorialSession.cs'); Refs=@('mscorlib','System','System.Core')},
    @{Name='PlaytestDataRegression'; Sources=@('Tools/Tests/PlaytestDataRegression.cs','Assets/Scripts/Playtest/CooperationProgress.cs','Assets/Scripts/Playtest/PlaytestStore.cs','Assets/Scripts/VisualUI/TutorialDashboard.cs','Assets/Scripts/Playtest/LevelCooperationScore.cs'); Refs=@('mscorlib','System','System.Core','System.Web.Extensions')}
)
foreach($case in $cases) {
    $exe=Join-Path $testOutput ($case.Name+'.exe')
    $compilerArgs=@('-nologo','-target:exe','-langversion:9',('-out:'+ $exe))
    $compilerArgs+=@($case.Refs | ForEach-Object {'-r:'+(Join-Path $api ($_+'.dll'))})
    $compilerArgs+=@($case.Sources | ForEach-Object {Join-Path $projectRoot $_})
    & $dotnet $compiler @compilerArgs
    if($LASTEXITCODE -ne 0){throw ('Compilation failed: '+$case.Name)}
    & $mono $exe
    if($LASTEXITCODE -ne 0){throw ('Regression failed: '+$case.Name)}
}
