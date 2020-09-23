using BaseTelegramBot;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static BaseTelegramBot.BaseBot;

namespace FeedbackBot
{
    class BotManager : BaseBot
    {
        private List<BaseBot> bots = new List<BaseBot>();
        private enum commands
        {
            ban,
            setgreeting,
            viewgroups,
            push
        }
        public BotManager(string token="", string DBConnectionString="") : base(token, DBConnectionString)
        {
            
            PrivateChatGreeting = "Добрый день! Напишите боту сообщение и мы его прочтем.";
            SupportedCommands = new List<BotCommand>()
            {
                new BotCommand(@"/addbot","^/addbot (.+)$","Введите команду /addbot чтобы добавить бота"),
            };
        }
        public override void Start()
        {
            base.Start();
            RestoreBotsFromDB();
        }
        
        private void RestoreBotsFromDB()
        {
            foreach (string token in dBWorker.get_all_bots())
            {
                try
                {
                    if (!token.Equals(this.token))
                        AddBot(token);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
            
        }

        private void AddBot(string token)
        {
            dBWorker.get_bot_id(token);
            if (this.bots.FindIndex(bot => bot.token == token)<0)
            {
                BaseBot bot = new FeedBackBot(token, this.DBConnectionString);
                bot.Start();
                this.bots.Add(bot);
            }
        }

        public override void OnMessageReceivedAction(Task<bool> task, object? state)
        {
            if (task.Result)
            {
                Message message = state as Message;
                if (ChatIsGroup(message))
                {
                    Message inReplyOf = message.ReplyToMessage;

                    if (inReplyOf != null)
                    {
                        long? targer = dBWorker.get_pair_chat_id(inReplyOf.Chat.Id, inReplyOf.MessageId, token);
                        if (targer != null)
                        {
                            IMessageToSend MyMess = RecreateMessage(message, (long)targer);
                            sender_to_tg.Put(MyMess);
                        }
                            
                    }                    
                }
                else if (ChatIsPrivate(message))
                {
                    List<long> groups = dBWorker.get_active_groups();
                    string appendix = "\n\n#id{0}\n<a href =\"tg://user?id={0}\">{1}</a>";
                    foreach (long group_id in groups)
                    {
                        IMessageToSend MyMess = RecreateMessage(message, group_id, string.Format(appendix, message.From.Id, message.From.FirstName));
                        if (MyMess!=null)
                            MyMess.AddLinkedMessage(message);
                        sender_to_tg.Put(MyMess);
                    }
                       
                }
            }
        }
        public override void GroupChatProcessing(Message message, ref bool continuation)
        { 
        
        }

        public override void PrivateChatProcessing(Message message, ref bool continuation)
        {
            base.PrivateChatProcessing(message, ref continuation);
            Match TokenChecking = SupportedCommands.First().CommandReg.Match(message.Text);
            if (TokenChecking.Success)
            {
                AddBot(TokenChecking.Groups[1].Value);
            }
        }
    }
}

