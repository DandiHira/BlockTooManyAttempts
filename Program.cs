
using Microsoft.Extensions.Hosting;

namespace BlockTooManyAttempts
{
	public class Program
	{
		public const string SERVICE_NAME = "BlockTooManyAttempts";
		public static void Main(string[] args)
		{
			var builder = Host.CreateApplicationBuilder(args);
			builder.Services.AddWindowsService(options =>
			{
				options.ServiceName = SERVICE_NAME;
			});

			builder.Services.AddHostedService<Worker>();

			var host = builder.Build();
			host.Run();
		}
	}
}