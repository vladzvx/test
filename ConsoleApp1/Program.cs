using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataBaseWorker;
using Npgsql;

namespace ConsoleApp1
{
	class data
    {
		public long phone;
		public long id;

    }
    class Program
    {
        static void Main(string[] args)
        {
			NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
			DBWorker dBWorker = new DBWorker("User ID=postgres;Password=qw12cv90;Host=localhost;Port=5432;Database=bot_manager_db;Pooling=true");
			dBWorker.Connect();
			string path = "/root/db.txt";
			ConcurrentQueue<data> q = new ConcurrentQueue<data>(); 
			Random end = new Random();
			Task t1 = 	Task.Factory.StartNew(() => {
				using (StreamReader sr = new StreamReader(path))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						string[] splitted_line = line.Split('|');
						if (long.TryParse(splitted_line[4], out long phone) &&
							long.TryParse(splitted_line[5], out long id))
						{
							q.Enqueue(new data() { id = id, phone = phone });
						}
					}
					
				}
			});

			Task t2 =  Task.Factory.StartNew(() => {

				while (!t1.IsCompleted&& !q.IsEmpty)
                {
					int total = 0;
					using (NpgsqlTransaction transaction = dBWorker.WriteConnention.BeginTransaction())
                    {
						int count = 0;
						while (count < 30000 && q.TryDequeue(out data d))
						{
							dBWorker._add_phone.Parameters["user_id"].Value = d.id;
							dBWorker._add_phone.Parameters["phone"].Value = d.phone;
							dBWorker._add_phone.ExecuteNonQuery();
							count++;
						}
						transaction.Commit();
						total += count;
						logger.Info("Commit! Total: "+total.ToString());
					}
					Thread.Sleep(100);
                }
                    
            });

			Task.WaitAll(t1, t2);
		}
    }
}
