using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using System;
using System.Text;

namespace Monq.Core.BasicDotNetMicroservice.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureBasicMicroservice()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureMetricsAndHealth();
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://0.0.0.0:5005");
                });
    }
}
