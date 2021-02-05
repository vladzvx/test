using System;
using System.Collections.Generic;

namespace NotificationBot
{
    public class NotificationBot :BaseTelegramBot.BaseBot
    {
        public NotificationBot(string token,string db_connection_string):base(token,db_connection_string)
        {
            
        }

        public void SendMessages(string message)
        {
            List<long> chats = dBWorker.get_active_private_chats(token);
            foreach (long g in chats)
            {
                if (!dBWorker.check_user_ban((int)g, g, token))
                    sender_to_tg.Put(factory.CreateMessage(g, message));
            }
        }
    }
}
