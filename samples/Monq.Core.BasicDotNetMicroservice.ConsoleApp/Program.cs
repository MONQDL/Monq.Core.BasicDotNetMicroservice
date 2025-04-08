using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.BasicDotNetMicroservice.Models;
using Newtonsoft.Json;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureBasicConsoleMicroservice(new ConsulConfigurationOptions
    {
        AppsettingsFileName = "appsettings-async.json"
    })
    .Build();

Console.WriteLine(JsonConvert.SerializeObject(host.Services.GetRequiredService<IConfiguration>()));
Console.ReadKey();

await host.RunAsync();
