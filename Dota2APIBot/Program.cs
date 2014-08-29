using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using IrcBotFramework;
using System.Threading;


namespace Dota2APIBot
{
    class Program
    {
        public static IRCBot bot;

        static void Main(string[] args)
        {
           
            string name = "lebot";
            bot = new IRCBot("irc.gamesurge.net", new IrcUser(name, name));

            bot.Run();

            TimeSpan WikiPushInterval = TimeSpan.FromHours(3);

            DateTime NextUpdate = DateTime.Now + WikiPushInterval;
            while (true)
            {
                if(DateTime.Now > NextUpdate)
                {
                    bot.UpdatePages();
                    NextUpdate = DateTime.Now + WikiPushInterval;
                }

                Thread.Sleep(1000);

            };
           

                   /*   
            FunctionDB db = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText("FunctionDB.txt"));

            string rst = db.RSTDump();

            File.WriteAllText("apidump.rst", rst);

            db.Save();
             */

        }


        private static void ParseWikiDump()
        {
            FunctionDB Database = new FunctionDB();

            string Data = File.ReadAllText("WikiDump.txt");


            string[] lines = Data.Split('\n');

            Function currentFunction;
            string CurrentClass = "Global";

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("="))
                {
                    if (line == "= Global Scope =")
                    {
                        CurrentClass = "Global";
                    }
                    else
                    {
                        //Find the word "Class"
                        int start = line.IndexOf("Class") + "Class".Length;
                        int end = line.IndexOf("=", start);

                        string Classname = line.Substring(start + 1, end - start - 2);
                        CurrentClass = Classname;



                    }
                    if (Database.Classes.FirstOrDefault(x => x.ClassName == CurrentClass) == null)
                    {
                        Database.Classes.Add(new ClassType()
                            {
                                ClassName = CurrentClass,

                            });
                    }
                }






            }


            string text = JsonConvert.SerializeObject(Database, Formatting.Indented);

            File.WriteAllText("FunctionDB_classes.txt", text);

        }
    }
}
