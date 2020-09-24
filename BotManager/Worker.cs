using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace BotManager
{
    public class BotService : BackgroundService
    {
        private BotManager bot;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bot = new BotManager("893004929:AAFnnCwTMnZ8wnz3pFIC290dfBQ5Nbs8Zn8", "User ID=postgres;Password=qw12cv90;Host=localhost;Port=5432;Database=bot_manager_db;Pooling=true");
            bot.Start();
            await Task.Delay(100);
        }


    }
}
