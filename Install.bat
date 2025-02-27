echo off
cls

set "currentPath=%~dp0"
sc.exe create "BlockTooManyAttempts" binpath= "\"%currentPath%BlockTooManyAttempts.exe\""

echo.
echo.

echo.
sc.exe start "BlockTooManyAttempts"
echo.
echo.

echo.
sc.exe config "BlockTooManyAttempts" start=auto
echo.
echo.

echo BlockTooManyAttempts service installed and started
echo.
echo.
pause