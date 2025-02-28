# BlockTooManyAttempts

BlockTooManyAttempts is a Windows background service that monitors Windows Security Event Logs for failed login attempts and automatically blocks IP addresses that exceed a specified threshold of failed attempts. This helps to prevent brute force attacks and unauthorized access to your system.

## Features

- Monitors Windows Security Event Logs for failed login attempts.
- Blocks IP addresses with excessive failed login attempts.

## Note

Upon execution (after PC reboot or initial installation), the program searches for Audit Failure logs from the past 3 days, then continues to scan for new Audit Failure logs every 20 minutes. IPs with 30 or more failed login attempts will be added to the firewall blacklist.

Once running, the program continuously accumulates Audit Failure counts for each IP without resetting. Therefore, if the computer runs for an extended period without rebooting, even legitimate users might get blocked if their failure count exceeds 30.

## Disclaimer

Use at your own risk. The authors are not responsible for any damage or data loss.
