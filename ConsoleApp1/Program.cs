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
			//string path = @"C:\work\test1.txt";
			int tran_size = 50000;
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
						while (q.Count > 2 * tran_size)
							Thread.Sleep(100);
					}
					
				}
			});
			Thread.Sleep(100);
			Task t2 =  Task.Factory.StartNew(() => {
				int total = 0;
				while (!t1.IsCompleted&& !q.IsEmpty)
                {
					Thread.Sleep(100);
					using (NpgsqlTransaction transaction = dBWorker.WriteConnention.BeginTransaction())
                    {
						int count = 0;
						while (count < tran_size && q.TryDequeue(out data d))
						{
							dBWorker._add_phone.Parameters["_user_id"].Value = d.id;
							dBWorker._add_phone.Parameters["_phone"].Value = d.phone;
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
