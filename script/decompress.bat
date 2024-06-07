@echo off
setlocal

rem Caminho do script PowerShell
set scriptPath=%~dp0decompress.ps1

rem Loop por todos os arquivos na pasta e subpastas, exceto os scripts batch e PowerShell
for /R %%f in (*.*) do (
    if not "%%~f" == "%scriptPath%" (
        rem Exibe o nome do arquivo que está sendo processado
        echo Processing file: %%~f
        rem Chama o script PowerShell para descomprimir cada arquivo
        powershell -ExecutionPolicy Bypass -File "%scriptPath%" -filePath "%%~f"
    )
)

endlocal
pause
