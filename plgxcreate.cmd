@echo off
set plgxnet=%1
set plgxkp=%2
set plgxos=%3

cls
cd %~dp0

for %%* in (.) do set CurrDirName=%%~nx*
echo Processing %CurrDirName%

echo Deleting existing plgx folder
rmdir /s /q plgx

echo Creating plgx folder
mkdir plgx

echo Copying files
xcopy src plgx /s /e /exclude:plgxexclude.txt > nul

echo Compiling plgx
src\bin\ReleasePlgx\KeePass.exe --plgx-create "%~dp0plgx" -plgx-prereq-net:%plgxnet% -plgx-prereq-kp:%plgxkp% -plgx-prereq-os:%plgxos%

echo Move plgx to KeePass plugin folder
rmdir /s / q src\bin\ReleasePlgx\Plugins
mkdir src\bin\ReleasePlgx\Plugins
move /y plgx.plgx "src\bin\ReleasePlgx\Plugins\%CurrDirName%.plgx"

echo Cleaning up
rmdir /s /q plgx
del src\bin\ReleasePlgx\AutoTypeExclude.dll

echo Compiled with following minimum requirements:
echo .NET    = %plgxnet%
echo KeePass = %plgxkp%
echo OS      = %plgxos%