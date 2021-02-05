using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAccessibleBot.Controllers
{
    public class MessageContainer
    {
        public string bot_id { get; set; }
        public string message { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class SendController : ControllerBase
    {
        [HttpPost]
        public void Send(MessageContainer mess)
        {
            if (mess != null&&mess.message!=null&&mess.message!=string.Empty&&Bots.Container.TryGetValue(mess.bot_id==null?"1": mess.bot_id, out BaseTelegramBot.BaseBot bot))
            {
                NotificationBot.NotificationBot noti_bot = bot as NotificationBot.NotificationBot;
                if (noti_bot != null)
                {
                    noti_bot.SendMessages(mess.message);
                }
            }
        }
    }
}
