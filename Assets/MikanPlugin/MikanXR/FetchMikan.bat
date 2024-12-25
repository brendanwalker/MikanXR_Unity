@echo off
call %~dp0SetMikanVars_x64.bat

echo "Clear CSharp Bindings"
del "%~dp0Generated\*.cs"
del "%~dp0Serialization\*.cs"

echo "Copy Mikan DLLs"
copy "%MIKAN_BINDINGS_PATH%\Generated\*.cs" "%~dp0Generated\*.cs"
copy "%MIKAN_BINDINGS_PATH%\Serialization\*.cs" "%~dp0Serialization\*.cs"
IF %ERRORLEVEL% NEQ 0 (
  echo "Error copying CSharp Bindings"
  goto failure
)

popd
EXIT /B 0

:failure
pause
EXIT /B 1