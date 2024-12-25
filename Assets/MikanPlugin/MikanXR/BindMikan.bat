@echo off
setlocal

::Select the path to the root Boost folder
if DEFINED MIKAN_DIST_PATH (goto setup_copy_script)
set "psCommand="(new-object -COM 'Shell.Application')^
.BrowseForFolder(0,'Please select the root folder for Mikan CSharp Bindings (ex: C:\Github\MikanXR\bindings\csharp).',0,0).self.path""
for /f "usebackq delims=" %%I in (`powershell %psCommand%`) do set "MIKAN_BINDINGS_PATH=%%I"
if NOT DEFINED MIKAN_BINDINGS_PATH (goto failure)

:setup_copy_script

:: Write out the paths to a batch file
del SetMikanVars_x64.bat
echo @echo off >> SetMikanVars_x64.bat
echo set "MIKAN_BINDINGS_PATH=%MIKAN_BINDINGS_PATH%" >> SetMikanVars_x64.bat

:: Copy latest Mikan C# Bindings
call FetchMikan.bat || goto failure
EXIT /B 0

:failure
pause
EXIT /B 1