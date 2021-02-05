using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace WebAccessibleBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cfg = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            string token = cfg.GetSection("BotSettings:TelegramBotToken").Value;
            string DBConnectionString = cfg.GetSection("BotSettings:DBConnectionString").Value;
            Bots.TryAddBot("1", token, DBConnectionString);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.Listen(IPAddress.Any, 5020);
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
