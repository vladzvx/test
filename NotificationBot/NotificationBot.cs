using System;
using System.Collections.Generic;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace NotificationBot
{
    public class NotificationBot :BaseTelegramBot.BaseBot
    {
        public NotificationBot(string token,string db_connection_string):base(token,db_connection_string)
        {
            
        }

        public override void PrivateChatProcessing(Message message, ref bool continuation)
        {
            base.PrivateChatProcessing(message, ref continuation);
            if (!dBWorker.check_chat_activation(message.Chat.Id, token))
            {
                sender_to_tg.Put(factory.CreateMessage(message.Chat.Id,
                    "Активируйте бота отправив команду вида \n /activate 111:AAA \n где 111:AAA - токен этого бота."));
            }
        }

        public void SendMessages(string message)
        {
            List<long> chats = dBWorker.get_active_private_chats(token);
            foreach (long g in chats)
            {
                if (dBWorker.check_chat_activation(g, token))
                {
                    sender_to_tg.Put(factory.CreateMessage(g, message));
                }
                
            }
        }
    }
}
