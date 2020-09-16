using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataBaseWorker
{
    public class DataContainer
    {
        public enum ContainerType
        {
            Message,
            User,
            Chat,
            Fine,
            Ban
        }

        #region fields
        public ContainerType containerType;
        public DateTime MessageTimestamp;
        public DateTime ClientTimestamp;
        public long message_id;
        public long chat_id;
        public long? user_id;
        public long? pair_chat_id;
        public long? pair_message_id;
        public long in_reply_of;
        public string bot_token;
        public string text;
        public string caption;
        public string media_id;
        public string media_group_id;
        public bool is_output;
        public string username;
        public string first_name;
        public string last_name;
        public bool Fine;
        public bool Ban;
        public bool is_channel;
        public bool is_group;
        public bool? is_active=null;
        public DateTime? FineEnd;
        public DateTime? BanEnd;
        #endregion

        #region constructors
        public DataContainer(DateTime MessageTimestamp, DateTime ClientTimestamp, long message_id, long chat_id, long user_id, long in_reply_of,
            string bot_token, string text = null, string caption = null, string media_tg_id = null,string media_group_id=null, 
            bool is_output = false,long? pair_message_id=null, long? pair_chat_id =null)
        {
            containerType = DataContainer.ContainerType.Message;
            this.MessageTimestamp = MessageTimestamp;
            this.ClientTimestamp = ClientTimestamp;
            this.message_id = message_id;
            this.chat_id = chat_id;
            this.user_id = user_id;
            this.in_reply_of = in_reply_of;
            this.bot_token = bot_token;
            this.text = text;
            this.caption = caption;
            this.media_id = media_tg_id;
            this.media_group_id = media_group_id;
            this.is_output = is_output;
            this.pair_chat_id = pair_chat_id;
            this.pair_message_id = pair_message_id;
        }

        public DataContainer(DateTime ClientTimestamp, long user_id,
            string bot_token, string username = null, string first_name = null, string last_name = null)
        {
            containerType = DataContainer.ContainerType.User;
            this.ClientTimestamp = ClientTimestamp;
            this.user_id = user_id;
            this.bot_token = bot_token;
            this.username = username;
            this.first_name = first_name;
            this.last_name = last_name;
        }
        public DataContainer(DateTime ClientTimestamp, long chat_id, string bot_token, long? user_id=null, string title = null,
             string username = null, bool is_group = false, bool is_channel = false, bool? is_active = null)
        {
            containerType = DataContainer.ContainerType.Chat;
            this.ClientTimestamp = ClientTimestamp;
            this.chat_id = chat_id;
            this.user_id = user_id;
            this.bot_token = bot_token;
            this.username = username;
            this.first_name = title;
            this.is_group = is_group;
            this.is_active = is_active;
            this.is_channel = is_channel;
        }
        public DataContainer(ContainerType containerType, DateTime ClientTimestamp, long chat_id, long user_id,
            string bot_token, bool Restriction, DateTime? RestrictionEnd = null)
        {
            this.containerType = containerType;
            this.ClientTimestamp = ClientTimestamp;
            this.chat_id = chat_id;
            this.user_id = user_id;
            this.bot_token = bot_token;
            if (containerType == ContainerType.Fine)
            {
                this.Fine = Restriction;
                this.FineEnd = RestrictionEnd;
            }
            else if (containerType == ContainerType.Ban)
            {
                this.Ban = Restriction;
                this.BanEnd = RestrictionEnd;
            }
        }

        #endregion
    }
    public class DBWorker
    {
        #region test

        static Random rnd = new Random();
        public async void TestMessageCreator()
        {
            Dictionary<long, int> users_and_messages = new Dictionary<long, int>();
            string token1 = "token1";
            long user = rnd.Next(0, 1000);
            while (true)
            {
                int message_id = users_and_messages.TryGetValue(user, out int val) ? val++ : 1;
                if (users_and_messages.ContainsKey(user))
                    users_and_messages[user] = message_id;
                else
                    users_and_messages.Add(user, message_id);
                //this.logger.Trace("user: " + user.ToString());
                this.add_user(DateTime.UtcNow, user, token1);
                this.add_message(DateTime.UtcNow, DateTime.UtcNow, message_id, 1, user, 0, token1, "textdadsadad " + rnd.Next().ToString());
                //Thread.Sleep(0.1);
            }

        }
        /*
        public void TestMessageReader()
        {
            Dictionary<long, int> users_and_messages = new Dictionary<long, int>();
            Random rnd = new Random();
            string token1 = "token1";
            bool CreatorStopping = true;
            while (CreatorStopping)
            {
                long user = rnd.Next(0, 1000);
                List<string> mess = this.get_messages(user);
                logger.Info(mess.Count.ToString() + " messages readed");
            }
        }
        */
        #endregion

        #region fields
        Task WritingTask;
        public int transaction_size = 1;
        public object WriteLocker = new object();
        public object ReadLocker = new object();
        public string ConnectionString { get; private set; } = string.Empty;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private System.Collections.Concurrent.ConcurrentQueue<DataContainer> write_quenue;
        NpgsqlConnection WriteConnention;
        NpgsqlConnection ReadConnention;
        NpgsqlCommand _get_bot_id;
        NpgsqlCommand _get_messages;
        NpgsqlCommand _get_active_groups;
        NpgsqlCommand _get_media_ids;
        NpgsqlCommand _get_caption;
        NpgsqlCommand _add_user;
        NpgsqlCommand _add_chat;
        NpgsqlCommand _add_message;
        NpgsqlCommand _check_ban;
        NpgsqlCommand _ban_user;
        NpgsqlCommand _get_pair_chat_id;
        NpgsqlCommand _get_pair_message_id;
        NpgsqlCommand _get_user_by_chat;
        NpgsqlCommand _set_group;
        NpgsqlCommand _get_active_private_chats;
        NpgsqlCommand _get_groups;
        #endregion

        #region constructor and destructor

        public DBWorker(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
            write_quenue = new System.Collections.Concurrent.ConcurrentQueue<DataContainer>();
            this.WriteConnention = new NpgsqlConnection(ConnectionString);
            this.ReadConnention = new NpgsqlConnection(ConnectionString);
            #region sql commands init
            this._get_bot_id = ReadConnention.CreateCommand();
            this._get_bot_id.CommandType = System.Data.CommandType.StoredProcedure;
            this._get_bot_id.CommandText = "get_bot_id";
            this._get_bot_id.Parameters.Add(new NpgsqlParameter("bot_token", NpgsqlTypes.NpgsqlDbType.Text));
            

            this._get_pair_chat_id = ReadConnention.CreateCommand();
            this._get_pair_chat_id.CommandType = System.Data.CommandType.StoredProcedure;
            this._get_pair_chat_id.CommandText = "get_pair_chat_id";
            this._get_pair_chat_id.Parameters.Add(new NpgsqlParameter("_message_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._get_pair_chat_id.Parameters.Add(new NpgsqlParameter("_chat_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._get_pair_chat_id.Parameters.Add(new NpgsqlParameter("bot_token", NpgsqlTypes.NpgsqlDbType.Text));

            this._get_pair_message_id = ReadConnention.CreateCommand();
            this._get_pair_message_id.CommandType = System.Data.CommandType.StoredProcedure;
            this._get_pair_message_id.CommandText = "get_pair_message_id";
            this._get_pair_message_id.Parameters.Add(new NpgsqlParameter("_message_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._get_pair_message_id.Parameters.Add(new NpgsqlParameter("_chat_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._get_pair_message_id.Parameters.Add(new NpgsqlParameter("bot_token", NpgsqlTypes.NpgsqlDbType.Text));

            this._add_chat = WriteConnention.CreateCommand();
            this._add_chat.CommandType = System.Data.CommandType.StoredProcedure;
            this._add_chat.CommandText = "add_chat";
            this._add_chat.Parameters.Add(new NpgsqlParameter("id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._add_chat.Parameters.Add(new NpgsqlParameter("_user_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._add_chat.Parameters.Add(new NpgsqlParameter("_name", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_chat.Parameters.Add(new NpgsqlParameter("bot_token", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_chat.Parameters.Add(new NpgsqlParameter("_chat_username", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_chat.Parameters.Add(new NpgsqlParameter("_is_channel", NpgsqlTypes.NpgsqlDbType.Boolean));
            this._add_chat.Parameters.Add(new NpgsqlParameter("_is_group", NpgsqlTypes.NpgsqlDbType.Boolean));
            this._add_chat.Parameters.Add(new NpgsqlParameter("_is_active", NpgsqlTypes.NpgsqlDbType.Boolean));

            this._add_user = WriteConnention.CreateCommand();
            this._add_user.CommandType = System.Data.CommandType.StoredProcedure;
            this._add_user.CommandText = "add_user";
            this._add_user.Parameters.Add(new NpgsqlParameter("id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._add_user.Parameters.Add(new NpgsqlParameter("_user_name", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_user.Parameters.Add(new NpgsqlParameter("_first_name", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_user.Parameters.Add(new NpgsqlParameter("_last_name", NpgsqlTypes.NpgsqlDbType.Text));

            this._add_message = WriteConnention.CreateCommand();
            this._add_message.CommandType = System.Data.CommandType.StoredProcedure;
            this._add_message.CommandText = "add_message";
            this._add_message.Parameters.Add(new NpgsqlParameter("_tg_timestamp", NpgsqlTypes.NpgsqlDbType.Timestamp));
            this._add_message.Parameters.Add(new NpgsqlParameter("_client_timestamp", NpgsqlTypes.NpgsqlDbType.Timestamp));
            this._add_message.Parameters.Add(new NpgsqlParameter("_message_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._add_message.Parameters.Add(new NpgsqlParameter("_chat_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._add_message.Parameters.Add(new NpgsqlParameter("_user_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._add_message.Parameters.Add(new NpgsqlParameter("_in_reply_of", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._add_message.Parameters.Add(new NpgsqlParameter("bot_token", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_message.Parameters.Add(new NpgsqlParameter("_text", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_message.Parameters.Add(new NpgsqlParameter("_caption", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_message.Parameters.Add(new NpgsqlParameter("_media_id", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_message.Parameters.Add(new NpgsqlParameter("_media_group_id", NpgsqlTypes.NpgsqlDbType.Text));
            this._add_message.Parameters.Add(new NpgsqlParameter("_is_output", NpgsqlTypes.NpgsqlDbType.Boolean));
            this._add_message.Parameters.Add(new NpgsqlParameter("_pair_chat_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._add_message.Parameters.Add(new NpgsqlParameter("_pair_message_id", NpgsqlTypes.NpgsqlDbType.Bigint));

            this._get_messages = ReadConnention.CreateCommand();
            this._get_messages.CommandType = System.Data.CommandType.Text;
            this._get_messages.CommandText = "select text from public.messages where user_id = @_user_id;";
            this._get_messages.Parameters.Add(new NpgsqlParameter("_user_id", NpgsqlTypes.NpgsqlDbType.Bigint));


            this._get_active_groups = ReadConnention.CreateCommand();
            this._get_active_groups.CommandType = System.Data.CommandType.Text;
            this._get_active_groups.CommandText = "select chat_id from public.chats where is_group=true and is_active=true;";

            this._get_media_ids = ReadConnention.CreateCommand();
            this._get_media_ids.CommandType = System.Data.CommandType.Text;
            this._get_media_ids.CommandText = "select distinct media_id from public.messages where media_group_id=@mgrid and is_actual;";
            this._get_media_ids.Parameters.Add("mgrid", NpgsqlTypes.NpgsqlDbType.Text);

            this._set_group = ReadConnention.CreateCommand();
            this._set_group.CommandType = System.Data.CommandType.Text;
            this._set_group.CommandText = "update public.chats set user_group=@_user_group where chat_id=@_chat_id and bot_id = @_bot_id;";
            this._set_group.Parameters.Add("_chat_id", NpgsqlTypes.NpgsqlDbType.Bigint);
            this._set_group.Parameters.Add("_user_group", NpgsqlTypes.NpgsqlDbType.Text);
            this._set_group.Parameters.Add("_bot_id", NpgsqlTypes.NpgsqlDbType.Integer);


            this._get_active_private_chats = ReadConnention.CreateCommand();
            this._get_active_private_chats.CommandType = System.Data.CommandType.Text;
            this._get_active_private_chats.CommandText = "select chat_id,user_group,bot_id from public.chats" +
                " where is_group=false and is_channel=false and is_active=true";

            this._get_groups = ReadConnention.CreateCommand();
            this._get_groups.CommandType = System.Data.CommandType.Text;
            this._get_groups.CommandText = "select distinct user_group from public.chats " +
                "where is_group=false and is_channel=false and is_active=true and  bot_id = {0}; ";


            this._get_caption = ReadConnention.CreateCommand();
            this._get_caption.CommandType = System.Data.CommandType.Text;
            this._get_caption.CommandText = "select distinct caption from public.messages where media_group_id=@mgid and caption is not null;";
            this._get_caption.Parameters.Add("mgid", NpgsqlTypes.NpgsqlDbType.Text);

            this._get_user_by_chat = ReadConnention.CreateCommand();
            this._get_user_by_chat.CommandType = System.Data.CommandType.Text;
            this._get_user_by_chat.CommandText = "select user_id from public.chats where chat_id=@_chat_id and is_group=false and not is_channel;";
            this._get_user_by_chat.Parameters.Add("_chat_id",NpgsqlTypes.NpgsqlDbType.Bigint);

            this._check_ban = ReadConnention.CreateCommand();
            this._check_ban.CommandType = System.Data.CommandType.StoredProcedure;
            this._check_ban.CommandText = "check_ban_user";
            this._check_ban.Parameters.Add(new NpgsqlParameter("_user_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._check_ban.Parameters.Add(new NpgsqlParameter("_chat_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._check_ban.Parameters.Add(new NpgsqlParameter("token", NpgsqlTypes.NpgsqlDbType.Text));

            this._ban_user = ReadConnention.CreateCommand();
            this._ban_user.CommandType = System.Data.CommandType.StoredProcedure;
            this._ban_user.CommandText = "ban_user";
            this._ban_user.Parameters.Add(new NpgsqlParameter("_user_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._ban_user.Parameters.Add(new NpgsqlParameter("_chat_id", NpgsqlTypes.NpgsqlDbType.Bigint));
            this._ban_user.Parameters.Add(new NpgsqlParameter("token", NpgsqlTypes.NpgsqlDbType.Text));
            #endregion
        }

        ~DBWorker()
        {
            this.WriteConnention.Close();
            this.ReadConnention.Close();
        }
        #endregion

        #region methods
        /// <summary>
        /// Возвращает личные чаты. Если group==null - возвращает все, не имеющие группы. 
        /// Если group!=null - возвращает все, чья группировка совпадает со значением gropu
        /// </summary>
        /// <param name="bot_token">токен бота, выдаваемый BotFather</param>
        /// <param name="group">Название группы пользователей в БД</param>
        /// <returns></returns>
        public List<long> get_active_private_chats(string bot_token,string group = null)
        {
            List<long> result = new List<long>();
            int bot_id = get_bot_id(bot_token);
            lock (ReadLocker)
            {
                using (NpgsqlDataReader reader = _get_active_private_chats.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            int temp_bot_id = reader.GetInt32(2);
                            
                            string grop_from_db = reader.IsDBNull(1)?null: reader.GetString(1);
                            if (bot_id == temp_bot_id)
                            {
                                if (group == null)
                                {
                                    if (grop_from_db == null)
                                        result.Add(reader.GetInt64(0));
                                }
                                else
                                {
                                    if (grop_from_db != null)
                                    {
                                        if (grop_from_db.Equals(group))
                                            result.Add(reader.GetInt64(0));
                                    }
                                        
                                }
                            }
                        }
                        catch (InvalidCastException) { }

                    }
                    reader.Close();
                }
            }
            return result;
        }

        public List<string> get_groupping(string bot_token)
        {
            List<string> result = new List<string>();
            int bot_id = get_bot_id(bot_token);
            lock (ReadLocker)
            {
                _get_groups.CommandText = string.Format(_get_groups.CommandText, bot_id);
                using (NpgsqlDataReader reader = _get_groups.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            result.Add(reader.GetString(0));
                        }
                        catch (System.InvalidCastException) { }
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public void set_group(long chat_id, string group_name, string bot_token)
        {
            int bot_id = get_bot_id(bot_token);
            lock (ReadLocker)
            {
                _set_group.Parameters["_chat_id"].Value = chat_id;
                _set_group.Parameters["_user_group"].Value = group_name;
                _set_group.Parameters["_bot_id"].Value = bot_id;
                _set_group.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot_token">not null telegram api token</param>
        /// <returns></returns>
        /// <exception cref="Exception">While get null value</exception>
        public int get_bot_id(string bot_token)
        {
            int result=0;
            lock (ReadLocker)
            {
                this._get_bot_id.Parameters["bot_token"].Value = SetDBNullIfNull(bot_token);
                using (NpgsqlDataReader reader = _get_bot_id.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            result = reader.GetInt32(0);
                        }
                        catch (InvalidCastException) { }
                    }
                    reader.Close();
                }
            }
            return result;
        }
        public void ban_user(long user_id, long chat_id, string bot_token)
        {
            lock (ReadLocker)
            {
                this._ban_user.Parameters["_user_id"].Value = user_id;
                this._ban_user.Parameters["_chat_id"].Value = chat_id;
                this._ban_user.Parameters["token"].Value = bot_token;
                _ban_user.ExecuteNonQuery();
            }
        }
        public bool check_user_ban(int user_id,long chat_id,string bot_token)
        {
            bool result = false;
            lock (ReadLocker)
            {
                this._check_ban.Parameters["_user_id"].Value = user_id;
                this._check_ban.Parameters["_chat_id"].Value = chat_id;
                this._check_ban.Parameters["token"].Value = bot_token;
                using (NpgsqlDataReader reader = _check_ban.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            result = reader.GetBoolean(0);
                        }
                        catch (InvalidCastException) { }
                                               
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public void add_user(DateTime ClientTimestamp, long id, string bot_token,
            string user_name = null, string first_name = null, string last_name = null)
        {
            logger.Trace(string.Format("add_user called! id: {0}, user_name: {1}, first_name: {2}, last_name: {3}",
                id, user_name ?? "null", first_name ?? "null", last_name ?? "null"));
            AddContainer(new DataContainer(ClientTimestamp, id, bot_token, user_name, first_name, last_name));
        }

        public void add_chat(DateTime ClientTimestamp, long chat_id, string bot_token, long? user_id = null, string title = null,
             string username = null, bool is_group = false, bool is_channel = false, bool? is_active = null)
        {
            logger.Trace(string.Format("add_chat called! chat_id: {0}, user_name: {1}, title: {2}, is_channel: {3}, is_group: {4};",
                chat_id, username ?? "null", title ?? "null", is_channel,is_group));
            AddContainer(new DataContainer(ClientTimestamp, chat_id, bot_token, user_id, title,
             username , is_group, is_channel, is_active));
        }

        public long? get_user_by_chat(long chat_id)
        {
            long? result = null;
            lock (ReadLocker)
            {
                //_get_user_by_chat.CommandText = string.Format(_get_user_by_chat.CommandText, chat_id);
                _get_user_by_chat.Parameters["_chat_id"].Value = chat_id;
                using (NpgsqlDataReader reader = _get_user_by_chat.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            result = reader.GetInt64(0);
                        }
                        catch (InvalidCastException) { };
                        break;
                    }
                    reader.Close();
                }
            }
            return result;
        }


        public List<long> get_active_groups()
        {
            List<long> ForReturn = new List<long>();
            lock (ReadLocker)
            {
                using (NpgsqlDataReader reader = _get_active_groups.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            ForReturn.Add(reader.GetInt64(0));
                        }
                        catch (System.InvalidCastException) { }                       
                    }
                    reader.Close();
                }
            }
            return ForReturn;
        }

        public string get_caption(string media_group_id)
        {
            List<string> temp = new List<string>();
            lock (ReadLocker)
            {
                _get_caption.Parameters["mgid"].Value = media_group_id;
                using (NpgsqlDataReader reader = _get_caption.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            temp.Add(reader.GetString(0));
                        }
                        catch (InvalidCastException) { }
                        
                    }
                    reader.Close();
                }
            }
            return temp.Max();
        }

        public long? get_pair_chat_id(long chat_id,long message_id,string bot_token)
        {
            long? result=null;
            lock (ReadLocker)
            {
                _get_pair_chat_id.Parameters["_message_id"].Value = message_id;
                _get_pair_chat_id.Parameters["_chat_id"].Value = chat_id;
                _get_pair_chat_id.Parameters["bot_token"].Value = bot_token;
                using (NpgsqlDataReader reader = _get_pair_chat_id.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            result = reader.GetInt64(0);
                        }
                        catch (System.InvalidCastException) { }
                        break;
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public long? get_pair_message_id(long chat_id, long message_id, string bot_token)
        {
            long? result = null;
            lock (ReadLocker)
            {
                _get_pair_message_id.Parameters["_message_id"].Value = message_id;
                _get_pair_message_id.Parameters["_chat_id"].Value = chat_id;
                _get_pair_message_id.Parameters["bot_token"].Value = bot_token;
                using (NpgsqlDataReader reader = _get_pair_message_id.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            result = reader.GetInt64(0);
                        }
                        catch (System.InvalidCastException) { }
                        break;
                    }
                    reader.Close();
                }
            }
            return result;
        }

        public List<string> get_media_ids(string media_group_id)
        {
            List<string> ForReturn = new List<string>();
            lock (ReadLocker)
            {
                //_get_media_ids.CommandText = string.Format(_get_media_ids.CommandText, media_group_id);
                _get_media_ids.Parameters["mgrid"].Value = media_group_id;
                using (NpgsqlDataReader reader = _get_media_ids.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            ForReturn.Add(reader.GetString(0));
                        }
                        catch (InvalidCastException) { }
                    }
                    reader.Close();
                }
            }
            return ForReturn;
        }

        public void AddContainer(DataContainer DataContainer)
        {
            write_quenue.Enqueue(DataContainer);
            logger.Trace(string.Format("{0} added to write_quenue. Quenue size: {1}.", DataContainer.containerType, write_quenue.Count));
            Write();
        }
        public void add_message(DateTime MessageTimestamp, DateTime ClientTimestamp, long message_id,
            long chat_id, long user_id, long in_reply_of, string bot_token, string text = null, 
            string caption = null, string media_tg_id = null, string media_group_id = null, bool is_output = false,
            long? pair_chat_id=null, long? pair_message_id = null)
        {
            logger.Trace(string.Format("add_message called! ClientTimestamp: {0}, message_id: {1}, chat_id: {2}, user_id: {3}, text: {4}, reciver(bot/client) token: {5}",
                ClientTimestamp, message_id, chat_id, user_id, text ?? caption ?? "null", bot_token));
            AddContainer(new DataContainer(MessageTimestamp, ClientTimestamp, message_id, chat_id, user_id, in_reply_of,
                bot_token, text, caption, media_tg_id, media_group_id, is_output, pair_message_id, pair_chat_id));
        }
        private void Write()
        {
            bool LockToken = false;
            Monitor.Enter(WriteLocker, ref LockToken);
            if (LockToken&&WritingTask == null)
            {
                WritingTask = Task.Factory.StartNew(() =>
                {
                    logger.Debug("Creating new WritingTask.");
                    try
                    {
                        while (!write_quenue.IsEmpty)
                        {

                            using (NpgsqlTransaction transaction = WriteConnention.BeginTransaction())
                            {
                                try
                                {
                                    int count = 0;
                                    for (int i = 0; i < transaction_size && !write_quenue.IsEmpty; i++)
                                    {
                                        if (write_quenue.TryDequeue(out DataContainer MessCont))
                                        {
                                            WriteContainer(MessCont, transaction);
                                            count++;
                                        }
                                    }
                                    transaction.Commit();
                                    logger.Debug(string.Format("{0} values commited!", count));
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    throw ex;
                                }                              
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error while DB writing to DB");
                    }

                });
                WritingTask.ContinueWith((t) => EndTask(t));
                
            }
            Monitor.Exit(WriteLocker);
        }
        void EndTask(Task currentTask)
        {
            try
            {
                currentTask.Dispose();
            }
            catch { };
            WritingTask = null;
        }
        private object SetDBNullIfNull(object value)
        {
            return value ?? DBNull.Value;
        }
        private void WriteContainer(DataContainer cont, NpgsqlTransaction tran)
        {
            try
            {
                switch (cont.containerType)
                {
                    case DataContainer.ContainerType.Message:
                        {
                            this._add_message.Parameters["_tg_timestamp"].Value = cont.MessageTimestamp;
                            this._add_message.Parameters["_client_timestamp"].Value = cont.ClientTimestamp;
                            this._add_message.Parameters["_message_id"].Value = cont.message_id;
                            this._add_message.Parameters["_chat_id"].Value = cont.chat_id;
                            this._add_message.Parameters["_user_id"].Value = SetDBNullIfNull(cont.user_id);
                            this._add_message.Parameters["_in_reply_of"].Value = cont.in_reply_of;
                            this._add_message.Parameters["bot_token"].Value = cont.bot_token;
                            this._add_message.Parameters["_text"].Value = SetDBNullIfNull(cont.text);
                            this._add_message.Parameters["_caption"].Value = SetDBNullIfNull(cont.caption);
                            this._add_message.Parameters["_media_id"].Value = SetDBNullIfNull(cont.media_id);
                            this._add_message.Parameters["_media_group_id"].Value = SetDBNullIfNull(cont.media_group_id);
                            this._add_message.Parameters["_is_output"].Value = SetDBNullIfNull(cont.is_output);
                            this._add_message.Parameters["_pair_message_id"].Value = SetDBNullIfNull(cont.is_output);
                            this._add_message.Parameters["_pair_chat_id"].Value = SetDBNullIfNull(cont.pair_chat_id);
                            this._add_message.Parameters["_pair_message_id"].Value = SetDBNullIfNull(cont.pair_message_id);
                            int n_rows = _add_message.ExecuteNonQuery();
                            logger.Info(n_rows.ToString() + " rows wrote");
                            break;
                        }
                    case DataContainer.ContainerType.User:
                        {
                            if (cont.user_id != null)
                            {
                                _add_user.Transaction = tran;
                                _add_user.Parameters["id"].Value = (long)cont.user_id;
                                _add_user.Parameters["_user_name"].Value = SetDBNullIfNull(cont.username);
                                _add_user.Parameters["_first_name"].Value = SetDBNullIfNull(cont.last_name);
                                _add_user.Parameters["_last_name"].Value = SetDBNullIfNull(cont.first_name);
                                _add_user.ExecuteNonQuery();
                            }
                            break;
                        }
                    case DataContainer.ContainerType.Chat:
                        {
                            _add_chat.Transaction = tran;
                            _add_chat.Parameters["id"].Value = cont.chat_id;
                            _add_chat.Parameters["_user_id"].Value = SetDBNullIfNull(cont.user_id);
                            _add_chat.Parameters["bot_token"].Value = cont.bot_token;
                            _add_chat.Parameters["_chat_username"].Value = SetDBNullIfNull(cont.username);
                            _add_chat.Parameters["_name"].Value = SetDBNullIfNull(cont.first_name);
                            _add_chat.Parameters["_is_channel"].Value = cont.is_channel;
                            _add_chat.Parameters["_is_group"].Value = cont.is_group;
                            _add_chat.Parameters["_is_active"].Value = SetDBNullIfNull(cont.is_active);

                            _add_chat.ExecuteNonQuery();
                            break;

                        }
                    case DataContainer.ContainerType.Ban:
                        break;
                    case DataContainer.ContainerType.Fine:
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

        }

        public void Connect()
        {
            try
            {
                WriteConnention.Open();
                ReadConnention.Open();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while oppening connection to DB! Reconnecting in 120 sec...");
                System.Threading.Thread.Sleep(120000);
                this.Connect();
            }
        }

        public void Disconnect()
        {
            WriteConnention.Close();
            ReadConnention.Close();
        }

        #endregion
    }
}
