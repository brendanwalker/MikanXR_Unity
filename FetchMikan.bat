@echo off
call SetMikanVars_x64.bat

:: Set path variables
set "MIKAN_CSHARP=%MIKAN_DIST_PATH%\csharp"

set "SCRIPTS_FOLDER=Assets\Scripts\MikanAPI"
set "PLUGINS_FOLDER=Assets\Plugins"

:: delete old script files
del "%SCRIPTS_FOLDER%\*.cs"

:: Copy over the C# files
xcopy /y /r "%MIKAN_CSHARP%\*.cs" "%SCRIPTS_FOLDER%" || goto failure

:: Copy over the client DLLs
xcopy /y /r "%MIKAN_DIST_PATH%\bin\MikanClient_csharp.dll" "%PLUGINS_FOLDER%" || goto failure
xcopy /y /r "%MIKAN_DIST_PATH%\bin\MikanClient_swig_csharp.dll" "%PLUGINS_FOLDER%" || goto failure
xcopy /y /r "%MIKAN_DIST_PATH%\Mikan_CAPI.dll" "%PLUGINS_FOLDER%" || goto failure
xcopy /y /r "%MIKAN_DIST_PATH%\SpoutLibrary.dll" "%PLUGINS_FOLDER%" || goto failure

echo "Successfully updated MikanXR from: %MIKAN_DIST_PATH%"
pause
EXIT /B 0

:failure
echo "Failed to copy files from MikanXR distribution"
pause
EXIT /B 1