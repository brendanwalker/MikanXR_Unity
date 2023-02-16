@echo off
setlocal

::Select the path to the root MikanXR Folder
set "psCommand="(new-object -COM 'Shell.Application')^
.BrowseForFolder(0,'Please select the root folder for MikanXR (ex: c:\git-bwalker\MikanXR).',0,0).self.path""
for /f "usebackq delims=" %%I in (`powershell %psCommand%`) do set "MIKAN_ROOT_PATH=%%I"
if NOT DEFINED MIKAN_ROOT_PATH (goto failure)

:: Find the distribution folder
For /D %%D in ("%MIKAN_ROOT_PATH%\dist") Do (
    set "DISTRIBUTION_FOLDER=%%~fD"
)
if NOT DEFINED DISTRIBUTION_FOLDER (
    echo "Failed to find the distribution folder for MikanXR!"
    echo "Did you run BuildOfficialDistribution.bat?"
    goto failure
)

:: Set path variables
set "MIKAN_RELEASE_FOLDER=%DISTRIBUTION_FOLDER%\Win64\Release"
set "MIKAN_CSHARP=%MIKAN_RELEASE_FOLDER%\csharp"

set "SCRIPTS_FOLDER=Assets\Scripts\MikanAPI"
set "PLUGINS_FOLDER=Assets\Plugins"

:: delete old script files
del "%SCRIPTS_FOLDER%\*.cs"

:: Copy over the C# files
xcopy /y /r "%MIKAN_CSHARP%\*.cs" "%SCRIPTS_FOLDER%" || goto failure

:: Copy over the client DLLs
xcopy /y /r "%MIKAN_RELEASE_FOLDER%\bin\MikanClient_csharp.dll" "%PLUGINS_FOLDER%" || goto failure
xcopy /y /r "%MIKAN_RELEASE_FOLDER%\bin\MikanClient_swig_csharp.dll" "%PLUGINS_FOLDER%" || goto failure
xcopy /y /r "%MIKAN_RELEASE_FOLDER%\Mikan_CAPI.dll" "%PLUGINS_FOLDER%" || goto failure
xcopy /y /r "%MIKAN_RELEASE_FOLDER%\SpoutLibrary.dll" "%PLUGINS_FOLDER%" || goto failure

echo "Successfully updated MikanXR from: %DISTRIBUTION_FOLDER%"
pause
EXIT /B 0

:failure
echo "Failed to copy files from MikanXR distribution"
pause
EXIT /B 1