using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using IrcBotFramework;


namespace Dota2APIBot
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string name = "lebot";
            IRCBot bot = new IRCBot("irc.gamesurge.net", new IrcUser(name, name));

            bot.Run();

            while (true) ;
            

            /*

            JObject jo = JObject.Parse(File.ReadAllText("WikiDump.txt"));

            FunctionDB db = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText("FunctionDB.txt"));


            foreach(var j in jo)
            {
                string classname = j.Key;
                string description = (string)j.Value["description"];

                ClassType c = db.Classes.FirstOrDefault(x => x.ClassName.ToLower() == classname.ToLower());
                c.Description = description;

                foreach(var f in j.Value["funcs"].Children())
                {
                    string functionName;
                    if(f.Type == JTokenType.Property)
                    {
                        JProperty p = (JProperty)f;

                        functionName = p.Name;

                        Function func = db.Functions.FirstOrDefault(x => x.FunctionName.ToLower() == functionName.ToLower() && x.Class.ToLower() == classname.ToLower());

                        string desc = (string)p.Value["description"];

                        func.FunctionDescription = desc;

                        for(int i = 0; i < p.Value["args"].Count(); i++)
                        {
                            string val = (string)p.Value["args"][i];
                            func.Params[i].Name = val;
                        }
                        func.LastUpdate = DateTime.Now;
                    }

                   
                }

            }

           

            db.Save();
             * */

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
