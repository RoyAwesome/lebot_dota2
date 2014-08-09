using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;

using IrcBotFramework;


namespace Dota2APIBot
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
                       string name = "lebot";
                       IRCBot bot = new IRCBot("irc.gamesurge.net", new IrcUser(name, name));

                       bot.Run();

                       while (true) ;
             */

            FunctionDB db = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText("FunctionDB.txt"));

              BotSettings settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText("BotSettings.txt"));
              WikiTools.ConnectToWiki(settings);

              WikiTools.WriteTextToPage("", db.WikiDump());

             /* foreach(Function f in db.Functions.Where(x => x.Class == "CDOTAPlayer" ))
              {

                  WikiTools.WriteTextToPage(f.Class + "." + f.FunctionName, f.ToDetailedWikiFormat());
              } */


            db.Save();


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
