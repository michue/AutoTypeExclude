@echo off
set plgxnet=%1
set plgxkp=%2
set plgxos=%3

cls
cd %~dp0

for %%* in (.) do set CurrDirName=%%~nx*
echo Processing %CurrDirName%

echo Deleting existing PlgX folder
rmdir /s /q plgx

echo Creating PlgX folder
mkdir plgx

echo Copying files
xcopy src plgx /s /e /exclude:plgxexclude.txt > nul

echo Compiling PlgX
REM cd..
REM cd _KeePass_Release
"C:\Program Files (x86)\KeePass\KeePass.exe" --plgx-create "%~dp0plgx" -plgx-prereq-net:%plgxnet% -plgx-prereq-kp:%plgxkp% -plgx-prereq-os:%plgxos%
REM cd ..
REM cd %CurrDirName%

cd
echo Copying PlgX to KeePass plugin folder
copy plgx.plgx "..\KeePass2.x\Build\KeePass\Release\Plugins\%CurrDirName%.plgx"

REM echo Releasing PlgX
REM move /y plgx.plgx "..\_Releases\%CurrDirName%.plgx"

echo Cleaning up
rmdir /s /q plgx

echo Compiled with following minimum requirements:
echo .NET    = %plgxnet%
echo KeePass = %plgxkp%
echo OS      = %plgxos%