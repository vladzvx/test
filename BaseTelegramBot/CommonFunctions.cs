using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BaseTelegramBot
{
    public static class CommonFunctions
    {
        public class EntityComparer : IComparer<MessageEntity>
        {
            int IComparer<MessageEntity>.Compare(MessageEntity x, MessageEntity y)
            {
                if (x.Offset > y.Offset)
                {
                    return 1;
                }
                else if (x.Offset < y.Offset)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }
        public class UserComparer : IEqualityComparer<User>
        {
            bool IEqualityComparer<User>.Equals(User x, User y)
            {
                return x.Id == y.Id;
            }

            int IEqualityComparer<User>.GetHashCode(User obj)
            {
                return obj.GetHashCode();

            }
        }


        public static string ApplyFormat(MessageEntity entity, string StartSymbols, string EndSymbols, string startText, ref int offset)
        {
            string newText = string.Empty;
            for (int iter = 0; iter < startText.Length; iter++)
            {
                if (iter == entity.Offset + offset)
                {
                    newText += StartSymbols;
                }
                if (iter == entity.Offset + entity.Length + offset)
                {
                    newText += EndSymbols;
                }
                newText += startText[iter];
            }
            if (entity.Offset + entity.Length + offset == startText.Length)
                newText += EndSymbols;
            offset += StartSymbols.Length;
            offset += EndSymbols.Length;
            return newText;
        }

        public static string TextFormatingRecovering(MessageEntity[] Entities, string text)
        {
            if (Entities == null|| text==null) return text;
            int offset = 0;
            Array.Sort(Entities, new EntityComparer());//TODO удостовериться, что они всегда сортированные
            foreach (MessageEntity entity in Entities)
            {
                string StartSymbols = string.Empty;
                string EndSymbols = string.Empty;
                switch (entity.Type)
                {
                    case MessageEntityType.Bold:
                        StartSymbols = "<b>";// "*";
                        EndSymbols = "</b>";// "*";
                        break;
                    case MessageEntityType.Italic:
                        StartSymbols = "<i>";// "_";
                        EndSymbols = "</i>";//"_";
                        break;
                    case MessageEntityType.Strikethrough:
                        StartSymbols = "<strike>";//"-";
                        EndSymbols = "</strike>";
                        break;
                    case MessageEntityType.Underline:
                        StartSymbols = "<u>";
                        EndSymbols = "</u>";
                        break;
                    case MessageEntityType.Code:
                        StartSymbols = "<code>";//"'"
                        EndSymbols = "</code>";
                        break;
                    case MessageEntityType.Pre:
                        StartSymbols = "<pre>";
                        EndSymbols = "</pre>";
                        break;
                    case MessageEntityType.TextLink:
                        StartSymbols = "<a href=\"" + entity.Url + "\">";
                        EndSymbols = "</a>";
                        break;
                    case MessageEntityType.TextMention:
                        StartSymbols = "<a href=\"tg://user?id=" + entity.User.Id.ToString() + "\">";
                        EndSymbols = "</a>";
                        break;
                }
                text = ApplyFormat(entity, StartSymbols, EndSymbols, text, ref offset);
            }
            return text;
        }
    }
}
