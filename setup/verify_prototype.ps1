param([ValidateSet('input','rally','both')][string]$Mode='both')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$exePath=Join-Path $taskRoot 'Builds\RoboOpen-Windows\RoboOpen.exe'
$evidencePath=Join-Path $taskRoot 'Evidence\Prototype'
if (-not (Test-Path -LiteralPath $exePath)) { throw 'Build the Windows prototype first.' }
New-Item -ItemType Directory -Path $evidencePath -Force | Out-Null
$tests=if($Mode -eq 'both'){@('input','rally')}else{@($Mode)}
foreach($testName in $tests) {
    $flag=if($testName -eq 'input'){'--prototype-input-test'}else{'--prototype-test'}
    $receiptName=if($testName -eq 'input'){'prototype-input-tests.json'}else{'prototype-runtime.json'}
    $logPath=Join-Path $evidencePath "windows-$testName.log"
    $receiptPath=Join-Path $evidencePath $receiptName
    $started=Get-Date
    $arguments=@('-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile',('"'+$logPath+'"'),$flag,'--prototype-output',('"'+$evidencePath+'"'))
    $playerProcess=Start-Process -FilePath $exePath -ArgumentList $arguments -WindowStyle Hidden -PassThru -WorkingDirectory (Split-Path -Parent $exePath)
    Write-Output "Running standalone $testName validation (PID $($playerProcess.Id))."
    while(-not $playerProcess.WaitForExit(1000)) {
        if(((Get-Date)-$started).TotalSeconds -gt 150) {
            $playerProcess.Kill()
            throw "Standalone $testName validation timed out. See $logPath"
        }
    }
    if($playerProcess.ExitCode -ne 0) {throw "Standalone $testName exited $($playerProcess.ExitCode). See $logPath"}
    if(-not(Test-Path -LiteralPath $receiptPath) -or (Get-Item -LiteralPath $receiptPath).LastWriteTime -lt $started) {throw "Fresh $testName receipt missing."}
    $receipt=Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    if(-not $receipt.passed) {throw "Standalone $testName receipt failed."}
    $errors=Select-String -LiteralPath $logPath -Pattern 'NullReferenceException|MissingReferenceException|IndexOutOfRangeException|ArgumentException|Shader error|Crash!!!'
    if($errors) {throw "Standalone $testName reported runtime errors. See $logPath"}
    Write-Output "$testName PASSED, process exit 0."
    $receipt | ConvertTo-Json -Depth 5
}
