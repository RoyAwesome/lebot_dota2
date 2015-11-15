using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IrcBotFramework;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Threading;
using System.Reflection;


namespace Dota2APIBot
{

    class IRCBot : IrcBotFramework.IrcBot
    {


        static StreamWriter LogWriter;
        const bool Logging = false;


        BotSettings Settings;




        /// <summary>
        /// Returns true if the command can be executed
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool AccessCheck(IrcCommand command)
        {
            return Settings.TrustedUsers.Contains(command.Source.Nick) || Settings.TrustedChannels.Contains(command.Destination);
        }

        public IRCBot(string ServerAddress, IrcUser USer)
            : base(ServerAddress, USer)
        {
            Settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText("BotSettings.txt"));



            if (Settings.Logging) LogWriter = new StreamWriter("IRCBotLog.txt");


            this.ConnectionComplete += IRCBot_ConnectionComplete;

            this.RawMessageRecieved += bot_RawMessage;

            RegisterCommand("function", FunctionMod);
            RegisterCommand("f", FunctionMod);


            RegisterCommand("ping", Ping);
            RegisterCommand("class", ModifyClass);
            
            RegisterCommand("param", ParamMod);
            RegisterCommand("diffdb", DiffDB);
            RegisterCommand("dumpwiki", DumpWiki);
            RegisterCommand("writepage", ForceWriteWikiPage);

            RegisterCommand("writeallpages", UpdateAllWikiPages);
            RegisterCommand("updatestatus", WikiStatus);

            RegisterCommand("user", UserMod);
            RegisterCommand("reload", Reload);

            RegisterCommand("addcaptcha", AddCaptcha);

            RegisterCommand("insult", InsultBot);

            RegisterCommand("help", Help);
            RegisterCommand("?", Help);


        }
        public string Help(IrcCommand command)
        {
            return "https://developer.valvesoftware.com/wiki/Dota_2_Workshop_Tools/Community/IRC/lebot";
        }

        public string InsultBot(IrcCommand command)
        {
            Random rng = new Random();
            //Get a random bot
            string bot = "";
            if (command.Parameters.Length == 1)
            {
                bot = command.Parameters[0];
                if (bot == "lebot")
                {
                    return "That lebot guy is an amazing bot.  Well written and very useful.  All bots should strive to be him.";
                }
                if (!Settings.OtherBots.Contains(bot))
                {
                    return bot + " isn't a bot.  He is pretty chill";
                }
                
            }
            else
            {
                bot = Settings.OtherBots[rng.Next(0, Settings.OtherBots.Count)];
            }



            //get an insult
            string insult = Settings.Insults[rng.Next(0, Settings.Insults.Count)];

            //replace the name
            insult = insult.Replace("%s", bot);

            //Sling it
            return insult;
        }

        public void UpdatePages(bool force = false)
        {
           
            Worker = new Thread(() =>
            {

                FunctionDB database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText(Settings.DatabaseFilename));

                Total = database.Functions.Count;

                WikiTools.ConnectToWiki(Settings);

                bool Updated = false;
               

                for (int i = 0; i < database.Functions.Count; i++)
                {
                    Function f = database.Functions[i];

                    if (!force && f.LastUpdate < database.LastPush) continue; //Skip this if it hasn't been updated


                    string pageName = f.Class + "." + f.FunctionName;
                    PageBeingWritten = pageName;
                    Done = i;
                    string wikiText = f.ToDetailedWikiFormat();
                    try
                    {
                        WikiTools.WriteTextToPage(pageName, wikiText);
                    }
                    catch (Exception e)
                    {
                        
                        //WRite an exception
                        File.WriteAllText(DateTime.Now.ToString("ddMM-hhmm") + "Exception.txt", e.ToString());
                                                                        
                        Program.bot.SendMessage("#dota2api", "I pooped out an error while writing " + pageName + ": " + e.ToString().Haste());

                        Worker = null;
                        return; //Kill the loop

                    }


                    Updated = true;
    

                    Thread.Sleep(200);
                }


                if (Updated)
                {
                    WikiTools.WriteTextToPage("", database.WikiDump());
                    Program.bot.SendMessage("#dota2api", "Completed a wiki write job");
                }


                database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText(Settings.DatabaseFilename));
                database.LastPush = DateTime.Now;
                database.Save(Settings.DatabaseFilename);


                Worker = null;

                

            });

          



            Worker.Start();

        }



        void IRCBot_ConnectionComplete(object sender, EventArgs e)
        {
            foreach (string channel in Settings.BotChannels)
            {
                JoinChannel(channel);
            }

        }
        void bot_RawMessage(object sender, RawMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
            if (Settings.Logging)
            {
                LogWriter.WriteLine(e.Message);
                LogWriter.Flush();
            }

        }

        string UserMod(IrcCommand command)
        {
            if (!AccessCheck(command)) return "No Permision";

            string action = command.Parameters[0];

            string user = command.Parameters[1];

            if (action == "add")
            {
                Settings.TrustedUsers.Add(user);

            }
            if (action == "remove")
            {
                Settings.TrustedUsers.Remove(user);
            }

            Settings.Save();
            return "Done";

        }

        public string AddCaptcha(IrcCommand command)
        {
            if (!AccessCheck(command)) return "No Permision";

            if (command.Parameters.Length != 1 || !command.Parameters[0].Contains("hastebin.com/raw/")) return "'.addcaptcha <hastebinlink>'  First line should be the captcha, second line the answer";



            string data = QuickDownload(command.Parameters[0]);

            string[] spl = data.Split('\n');

            Settings.VDCCaptchas[spl[0]] = spl[1];

            Settings.Save();


            return "Added";
        }


        string Reload(IrcCommand command)
        {
            if (!AccessCheck(command)) return "No Permision";

            Settings = JsonConvert.DeserializeObject<BotSettings>(File.ReadAllText("BotSettings.txt"));

            return "Done";

        }

        public string DumpWiki(IrcCommand command)
        {
            if (!AccessCheck(command)) return "No Permision";
            if (Worker != null) return "Writer in use";

            WikiTools.ConnectToWiki(Settings);
            FunctionDB database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText(Settings.DatabaseFilename));

            WikiTools.WriteTextToPage("", database.WikiDump());

            return "Done";
        }

        public string ForceWriteWikiPage(IrcCommand command)
        {
            if (!AccessCheck(command)) return "No Permision";

            if (Worker != null) return "Writer in use";

            string functionName = command.Parameters[0];



            FunctionDB database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText(Settings.DatabaseFilename));

            if (functionName == "front")
            {
                WikiTools.ConnectToWiki(Settings);
                WikiTools.WriteTextToPage("", database.WikiDump());

                return "Done";
            }

            Function f = database.Functions.FirstOrDefault(x => x.FunctionName == functionName);
            if (f == null) return "Function not found";

            string pageName = f.Class + "." + f.FunctionName;

            WikiTools.ConnectToWiki(Settings);
            WikiTools.WriteTextToPage(pageName, f.ToDetailedWikiFormat());

            return "Done";
        }

        volatile int Total;
        volatile int Done;
        volatile string PageBeingWritten;

        Thread Worker;

        public string UpdateAllWikiPages(IrcCommand command)
        {
            if (!AccessCheck(command)) return "No Permision";

            if (Worker != null) return "Job Running, do .wikistatus to see where the job is";

            bool force = false;

            if (command.Parameters.Length > 0 && command.Parameters[0] == "force") force = true;

            UpdatePages(force);


            return "Job In Progress";
        }

        public string WikiStatus(IrcCommand command)
        {
            if (Worker == null) return "No Job In Progress";
            return "Page: " + PageBeingWritten + " (" + Done + "/" + Total + ")";
        }


        public string Ping(IrcCommand command)
        {
            return "Get off my dick";
        }

        public string ModifyClass(IrcCommand command)
        {


            if (command.Parameters.Length == 0 || command.Parameters[0] == "help")
            {
                return "'class <string classname> <action> <property> [data]' OR 'class find <searchtext>' OR 'class properties'";
            }
            if (command.Parameters[0] == "properties")
            {
                PropertyInfo[] props = typeof(ClassType).GetProperties();
                string availableProperties = "";
                for (int i = 0; i < props.Length; i++)
                {
                    availableProperties += props[i].Name;
                    if (i != props.Length - 1) availableProperties += ", ";
                }

                return "properties: [" + availableProperties + "]";
            }

            FunctionDB database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText(Settings.DatabaseFilename));

            string ClassName = command.Parameters[0];

            if (ClassName == "find")
            {
                string searchText = command.Parameters[1];


                string[] results = database.Classes.Where(x => x.ClassName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).Select(x => x.ClassName).ToArray();

                if (results.Length == 0) return "No Function Found";
                if (results.Length < 5)
                {
                    for (int i = 0; i < results.Length - 1; i++)
                    {
                        SendMessage(command.Destination, results[i]);

                    }
                    return results[results.Length - 1];
                }
                else
                {
                    return "Results: " + string.Join("\n", results).Haste();
                }
            }

            if (ClassName == "add")
            {
                if (!command.Parameters[1].StartsWith("http://hastebin.com/raw/")) return "Please give me a raw hastebin link with the json blob to add";

                string json = QuickDownload(command.Parameters[1]);
                ClassType f = JsonConvert.DeserializeObject<ClassType>(json);
                //f.LastUpdate = DateTime.Now;

                database.Classes.Add(f);
                database.Save(Settings.DatabaseFilename);
                return "Added " + f.ClassName;
            }
            if (ClassName == "<delete>")
            {
                ClassType c = database.Classes.FirstOrDefault(x => x.ClassName == command.Parameters[1]);
                if (c == null) return "Class not found";

                database.Classes.Remove(c);
                int funcs = database.Functions.RemoveAll(x => x.Class == c.ClassName);
                database.Save(Settings.DatabaseFilename);

                return "Removed " + c.ClassName + " and " + funcs + " functions";

            }
            if (ClassName == "<rebuild>")
            {
                if (!AccessCheck(command)) return "No Access";

                List<string> BrokenClasses = new List<string>();

                foreach (Function f in database.Functions)
                {
                    if (database.Classes.FirstOrDefault(x => x.ClassName == f.Class) == null)
                    {
                        BrokenClasses.Add(f.Class);
                        ClassType c = new ClassType();
                        c.ClassName = f.Class;
                        database.Classes.Add(c);
                    }
                }

                if (BrokenClasses.Count == 0) return "No Broken classes found";

                database.Save(Settings.DatabaseFilename);

                string bc = string.Join("\n", BrokenClasses);

                return "Found " + BrokenClasses.Count + " broken classes: " + bc.Haste();

            }



            ClassType clazz = database.Classes.FirstOrDefault(x => x.ClassName == ClassName);

            if (clazz == null) return "Class Not Found";

            string action = command.Parameters[1].ToLower();

            if (action == "modify" && !AccessCheck(command))
            {
                return "No Permision";
            }

            string property = command.Parameters[2].ToLower();

            string[] cpy = new string[command.Parameters.Length - 3];
            Array.Copy(command.Parameters, 3, cpy, 0, cpy.Length);

            string data = string.Join(" ", cpy);

            if (data.StartsWith("http://hastebin.com/raw/"))
            {
                data = QuickDownload(data);
            }


            PropertyInfo prop = typeof(ClassType).GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

            if (prop == null) return "Property " + property + " Not found";

            if (action == "modify") prop.SetValue(clazz, data);
            if (action == "view") return prop.GetValue(clazz).ToString();


            if (action == "modify") database.Save(Settings.DatabaseFilename);

            return "[" + action + "] " + ClassName + "'s " + property + " has been set";
        }


        public string FunctionMod(IrcCommand command)
        {
            if (command.Parameters.Length == 0 || command.Parameters[0] == "help")
            {
                return "'function <string funcName> <action (modify or view)> <property> [data]' OR 'function find <searchtext>' OR 'function properties'";
            }
            if (command.Parameters[0] == "properties")
            {
                PropertyInfo[] props = typeof(Function).GetProperties();
                string availableProperties = "";
                for (int i = 0; i < props.Length; i++)
                {
                    availableProperties += props[i].Name;
                    if (i != props.Length - 1) availableProperties += ", ";
                }

                return "properties: [" + availableProperties + "]";
            }
            string FunctionName = command.Parameters[0];

            FunctionDB database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText(Settings.DatabaseFilename));

            if (FunctionName == "find")
            {
                string functionName = command.Parameters[1];

                string[] results = database.LookupFunction(functionName);

                if (results.Length == 0) return "No Function Found";
                if (results.Length < 5)
                {
                    for (int i = 0; i < results.Length - 1; i++)
                    {
                        SendMessage(command.Destination, results[i]);

                    }
                    return results[results.Length - 1];
                }
                else
                {
                    return "Results: " + string.Join("\n", results).Haste();
                }
            }

            if (FunctionName == "add")
            {
                if (!command.Parameters[1].StartsWith("http://hastebin.com/raw/")) return "Please give me a raw hastebin link with the json blob to add";

                string json = QuickDownload(command.Parameters[1]);
                Function f = JsonConvert.DeserializeObject<Function>(json);
                f.LastUpdate = DateTime.Now;

                database.Functions.Add(f);
                database.Save(Settings.DatabaseFilename);
                return "Added " + f.GetQualifiedName();
            }
            if (FunctionName == "<addall>")
            {
                if (!command.Parameters[1].StartsWith("http://hastebin.com/raw/")) return "Please give me a raw hastebin link with the json blob to add";

                string json = QuickDownload(command.Parameters[1]);
                Function[] funcs = JsonConvert.DeserializeObject<Function[]>(json);
                foreach (Function f in funcs)
                {
                    f.LastUpdate = DateTime.Now;

                    database.Functions.Add(f);
                }

                database.Save(Settings.DatabaseFilename);
                return "Added " + funcs.Length + " functions";
            }

            if (FunctionName == "<replace>")
            {
                if (command.Parameters.Length != 3) return ".function <replace> oldFuncName hastebinblob";

                string functionName = command.Parameters[1];

                if (!command.Parameters[2].StartsWith("http://hastebin.com/raw/")) return "Please give me a raw hastebin link with the json blob to replace";

                string json = QuickDownload(command.Parameters[2]);

                Function f = JsonConvert.DeserializeObject<Function>(json);

                Function oldfunc = database.Functions.FirstOrDefault(x => x.FunctionName == f.FunctionName && x.Class == f.Class);

                if (oldfunc == null) return "Unable to replace function, cannot find it";

                f.LastUpdate = DateTime.Now;
                database.Functions.Remove(oldfunc);
                database.Functions.Add(f);
                database.Save(Settings.DatabaseFilename);

                return "Replaced";
            }

            if (FunctionName == "<dump>")
            {
                if (command.Parameters.Length != 2) return ".function <dump> functionName";

                string functionName = command.Parameters[1];

                Function f;
                if (functionName.Contains("."))
                {
                    string[] spl = functionName.Split('.');
                    functionName = spl[1];
                    string ClassName = spl[0];
                    f = database.Functions.FirstOrDefault(x => x.FunctionName == functionName && ClassName == x.Class);
                }
                else
                {
                    IEnumerable<Function> functions = database.Functions.Where(x => x.FunctionName == functionName);
                    if (functions.Count() > 1) return "Ambiguous function name: " + functionName + ".  Please use ClassName.FunctionName";
                    f = functions.FirstOrDefault();
                }

                if (f == null) return "Function Not Found";


                return "Done: " + JsonConvert.SerializeObject(f, Formatting.Indented).Haste();

            }


            Function func;
            if (FunctionName.Contains("."))
            {
                string[] spl = FunctionName.Split('.');
                FunctionName = spl[1];
                string ClassName = spl[0];
                func = database.Functions.FirstOrDefault(x => x.FunctionName == FunctionName && ClassName == x.Class);
            }
            else
            {
                IEnumerable<Function> functions = database.Functions.Where(x => x.FunctionName == FunctionName);
                if (functions.Count() > 1) return "Ambiguous function name: " + FunctionName + ".  Please use ClassName.FunctionName";
                func = functions.FirstOrDefault();
            }


            if (func == null) return "Function Not Found";

            string action = command.Parameters[1].ToLower();
            if (action == "modify" && !AccessCheck(command))
            {
                return "No Permision";
            }

            string property = command.Parameters[2].ToLower();

            string[] cpy = new string[command.Parameters.Length - 3];
            Array.Copy(command.Parameters, 3, cpy, 0, cpy.Length);

            string data = string.Join(" ", cpy);

            if (data.StartsWith("http://hastebin.com/raw/"))
            {
                data = QuickDownload(data);
            }

            PropertyInfo prop = typeof(Function).GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

            if (prop == null) return "Property " + property + " Not found";

            if (action == "modify") prop.SetValue(func, data);
            if (action == "view") return prop.GetValue(func).ToString();


            if (action == "modify")
            {
                func.LastUpdate = DateTime.Now;
                database.Save(Settings.DatabaseFilename);
            }

            return "[" + action + "] " + FunctionName + "'s " + property + " is now has been set";

        }

        public string ParamMod(IrcCommand command)
        {
            if (command.Parameters.Length == 0 || command.Parameters[0] == "help")
            {
                return "'param <string funcName> <paramId> <action (modify or view)> <property> [data]' OR 'param list <func>' OR 'param properties'";
            }
            if (command.Parameters[0] == "properties")
            {
                PropertyInfo[] props = typeof(Param).GetProperties();
                string availableProperties = "";
                for (int i = 0; i < props.Length; i++)
                {
                    availableProperties += props[i].Name;
                    if (i != props.Length - 1) availableProperties += ", ";
                }

                return "properties: [" + availableProperties + "]";
            }

            FunctionDB database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText(Settings.DatabaseFilename));

            string FunctionName = command.Parameters[0];



            if (FunctionName == "list")
            {
                Function f = database.Functions.FirstOrDefault(x => x.FunctionName == command.Parameters[2]);

                if (f == null) return "Function Not Found";

                int i;
                for (i = 0; i < f.Params.Count - 1; i++)
                {
                    SendMessage(command.Destination, "(" + i + ") " + f.Params[i].Type + " " + f.Params[i].Name);
                }
                return "(" + i + ") " + f.Params[i].Type + " " + f.Params[i].Name;
            }
            Function func;
            if (FunctionName.Contains("."))
            {
                string[] spl = FunctionName.Split('.');
                FunctionName = spl[1];
                string ClassName = spl[0];
                func = database.Functions.FirstOrDefault(x => x.FunctionName == FunctionName && ClassName == x.Class);
            }
            else
            {
                IEnumerable<Function> functions = database.Functions.Where(x => x.FunctionName == FunctionName);
                if (functions.Count() > 1) return "Ambiguous function name: " + FunctionName + ".  Please use ClassName.FunctionName";
                func = functions.FirstOrDefault();
            }




            if (func == null) return "Function Not Found";

            int paramId = int.Parse(command.Parameters[1]);

            if (func.Params.Count < paramId) return "Parameter doesn't exist";

            Param param = func.Params[paramId];

            string action = command.Parameters[2].ToLower();
            if (action == "modify" && !AccessCheck(command))
            {
                return "No Permision";
            }

            string property = command.Parameters[3].ToLower();

            string[] cpy = new string[command.Parameters.Length - 4];
            Array.Copy(command.Parameters, 4, cpy, 0, cpy.Length);

            string data = string.Join(" ", cpy);

            if (data.StartsWith("http://hastebin.com/raw/"))
            {
                data = QuickDownload(data);
            }

            PropertyInfo prop = typeof(Param).GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

            if (prop == null) return "Property " + property + " Not found";

            if (action == "modify") prop.SetValue(param, data);
            if (action == "view") return prop.GetValue(param).ToString();



            if (action == "modify")
            {
                func.LastUpdate = DateTime.Now;
                database.Save(Settings.DatabaseFilename);
            }

            return "[" + action + "] " + FunctionName + "'s " + property + " is now has been set";
        }

        private string QuickDownload(string url)
        {
            WebClient wc = new WebClient();
            return wc.DownloadString(url);
        }



        public string DiffDB(IrcCommand command)
        {
            if (!AccessCheck(command))
            {
                return "No Permision";
            }

            string haste = QuickDownload(command.Parameters[0]);

            FunctionDB diff = JsonConvert.DeserializeObject<FunctionDB>(haste);

            FunctionDB database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText(Settings.DatabaseFilename));

            foreach (Function f in diff.Functions)
            {

                Function d = database.Functions.FirstOrDefault(x => x.GetQualifiedName() == f.GetQualifiedName());
                if (d == null)
                {

                    database.Functions.Add(f);
                    Console.WriteLine("Added " + f.GetQualifiedName());
                }
                else
                {

                    database.Functions.Remove(d);
                    database.Functions.Add(f);
                    Console.WriteLine("Added " + f.GetQualifiedName());
                }
                f.LastUpdate = DateTime.Now;
            }
            foreach(ClassType t in diff.Classes)
            {
                ClassType c = database.Classes.FirstOrDefault(x => x.ClassName == t.ClassName);
                if(c == null)
                {
                    database.Classes.Add(t);
                    Console.WriteLine("Added " + t.ClassName);
                }
                else
                {
                    SendMessage(command.Destination, "Tried to add class " + t.ClassName + " but it already exists! (Old/Broken diff?)");
                }
       
            }
            if (database.Constants == null) database.Constants = new List<ConstantGroup>(); //Add a ConstantGroup list if we dont have one
            foreach(ConstantGroup cg in diff.Constants)
            {
                ConstantGroup oldcg = database.Constants.FirstOrDefault(x => x.EnumName == cg.EnumName);
                if(oldcg == null)
                {
                    database.Constants.Add(cg);
                    Console.WriteLine("Added " + cg.EnumName);
                }
                else
                {
                    database.Constants.Remove(oldcg);
                    database.Constants.Add(cg);
                    Console.WriteLine("Added " + cg.EnumName);
                }
            }

            database.Save(Settings.DatabaseFilename);

            return "Done";
        }

    }

}