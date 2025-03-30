using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace BlockTooManyAttempts
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;

		public Worker(ILogger<Worker> logger)
		{
			_logger = logger;
		}

		private static string? runNetshAdvfirewall(string arguments, bool returnRedirectedString)
		{
			using Process process = new Process();

			process.StartInfo.FileName = "netsh";
			process.StartInfo.Arguments = "advfirewall firewall " + arguments;
			process.StartInfo.RedirectStandardOutput = returnRedirectedString;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.Start();

			process.WaitForExit();

			if (!returnRedirectedString)
				return null;
			else
				return process.StandardOutput.ReadToEnd();
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Dictionary<IPAddress, int> ipAndAttemptCount = new Dictionary<IPAddress, int>(10);
			HashSet<IPAddress> blockedAddress = new HashSet<IPAddress>(10);
			string concatedBlockingIPs = string.Empty;

			_logger.LogInformation($"{Program.SERVICE_NAME} running at: {DateTimeOffset.Now}");

			// read the already blocked IPs using netsh 
			{
				string? output = runNetshAdvfirewall("show rule name=" + Program.SERVICE_NAME, true);
				if (string.IsNullOrEmpty(output))
				{
					goto LB_LOOP;
				}

				Match match = Regex.Match(output, @"RemoteIP:\s+([\d\./,]+)");
				if (!match.Success)
				{
					goto LB_LOOP;
				}

				string[] ipStrings = match.Groups[1].Value.Split(',');

				foreach (string ipString in ipStrings)
				{
					if (!IPNetwork.TryParse(ipString, out IPNetwork ipNetwork))
					{
						continue;
					}

					if (!blockedAddress.Add(ipNetwork.BaseAddress))
					{
						continue;
					}

					concatedBlockingIPs += ipNetwork.BaseAddress.ToString() + ", ";
				}
			}

			_logger.LogInformation($"Already Blocked IPs: {concatedBlockingIPs.TrimEnd(',', ' ')}");

		LB_LOOP:

			// at the beginning, check the event log for the last 3 days
			DateTime lastCheckedTime = DateTime.Now.AddDays(-3);

			// check the event log and block the IP if it has more than 30 failed login attempts
			while (!stoppingToken.IsCancellationRequested)
			{
				using (EventLog log = new EventLog("Security"))
				{
					IEnumerable<EventLogEntry> entries = log.Entries.Cast<EventLogEntry>();
					const string TRIM_TARGET_STRING = "\r\n\t SourcePort";
					char[] TRIM_TARGETS = TRIM_TARGET_STRING.ToCharArray();

					for (int i = log.Entries.Count - 1; i >= 0; --i)
					{
						EventLogEntry entry = log.Entries[i];

						if (entry.TimeGenerated <= lastCheckedTime)
							break;

						if (entry.InstanceId == 4625) // 4625: audit failure
						{
							string sourceAddress = entry.Message.Substring(entry.Message.IndexOf("Source Network Address:") + 24, 15).TrimEnd(TRIM_TARGETS);

							if (IPAddress.TryParse(sourceAddress, out IPAddress? sourceIP))
							{
								if (IPAddress.IsLoopback(sourceIP) || blockedAddress.Contains(sourceIP))
									continue;

								if (ipAndAttemptCount.ContainsKey(sourceIP))
									ipAndAttemptCount[sourceIP]++;
								else
									ipAndAttemptCount[sourceIP] = 1;
							}
						}
					}

					lastCheckedTime = log.Entries[log.Entries.Count - 1].TimeGenerated;
				}

				foreach (KeyValuePair<IPAddress, int> pair in ipAndAttemptCount)
				{
					IPAddress ip = pair.Key;
					int count = pair.Value;

					if (count > 30)
					{
						if (blockedAddress.Add(ip))
						{
							_logger.LogInformation($"Blocked IP: {ip}");

							ipAndAttemptCount.Remove(ip);
							concatedBlockingIPs += ip.ToString() + ",";
						}
					}
				}

				runNetshAdvfirewall($"delete rule name={Program.SERVICE_NAME}", false);

				concatedBlockingIPs = concatedBlockingIPs.TrimEnd(',', ' ');
				runNetshAdvfirewall($"add rule name={Program.SERVICE_NAME} dir=in action=block remoteip={concatedBlockingIPs} enable=yes profile=domain,private,public", false);

				await Task.Delay(TimeSpan.FromMinutes(20), stoppingToken);
			}

		}
	}
}

