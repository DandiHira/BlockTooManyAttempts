echo off
cls
sc.exe stop "BlockTooManyAttempts"
sc.exe delete "BlockTooManyAttempts"

pause
