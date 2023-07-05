using Monq.Core.BasicDotNetMicroservice.Host;
using Monq.Core.BasicDotNetMicroservice.Models;
using Newtonsoft.Json;
using System.Text;

namespace Monq.Core.BasicDotNetMicroservice.ConsoleApp;

class Program
{
    static IConsoleApplication _consoleApplication;

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        _consoleApplication = ConsoleHost
            .CreateDefaultBuilder(args, new ConsoleHostConfigurationOptions
            {
                ConsulConfigurationOptions = new ConsulConfigurationOptions
                {
                    AppsettingsFileName = "appsettings-async.json"
                }
            })
        .Build() as IConsoleApplication;

        Console.WriteLine(JsonConvert.SerializeObject(_consoleApplication.Configuration));

        Console.ReadKey();
    }
}
