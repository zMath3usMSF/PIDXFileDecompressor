@echo off
setlocal

set scriptPath=%~dp0decompress.ps1

for /R %%f in (*.*) do (
    if not "%%~f" == "%scriptPath%" (
        echo Processing file: %%~f
        powershell -ExecutionPolicy Bypass -File "%scriptPath%" -filePath "%%~f"
    )
)

endlocal
pause
