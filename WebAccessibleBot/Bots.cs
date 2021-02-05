using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseTelegramBot;

namespace WebAccessibleBot
{
    public static class Bots
    {
        public static ConcurrentDictionary<string, BaseTelegramBot.BaseBot> Container = new ConcurrentDictionary<string, BaseBot>();

        public static void TryAddBot(string id, string token,string db)
        {
            BaseTelegramBot.BaseBot bot = new NotificationBot.NotificationBot(token,
                db);
            bot.Start();
            Container.TryAdd(id,bot);
        }
    }
}
