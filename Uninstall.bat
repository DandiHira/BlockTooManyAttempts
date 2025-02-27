echo off
cls
sc.exe stop "BlockTooManyAttempts"
sc.exe delete "BlockTooManyAttempts"
netsh advfirewall firewall delete rule name="BlockTooManyAttempts"
pause
