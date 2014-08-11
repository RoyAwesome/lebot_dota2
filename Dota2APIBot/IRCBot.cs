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

       
            RegisterCommand("ping", Ping);
            RegisterCommand("class", ModifyClass);
            RegisterCommand("function", FunctionMod);
            RegisterCommand("param", ParamMod);
            RegisterCommand("dumpwiki", DumpWiki);
            RegisterCommand("writepage", ForceWriteWikiPage);

            RegisterCommand("writeallpages", UpdateAllWikiPages);
            RegisterCommand("updatestatus", WikiStatus);

            RegisterCommand("user", UserMod);
            RegisterCommand("reload", Reload);
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

                    WikiTools.WriteTextToPage(pageName, wikiText);

                    Updated = true;
                }

                if(Updated) WikiTools.WriteTextToPage("", database.WikiDump());


                database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText(Settings.DatabaseFilename));
                database.LastPush = DateTime.Now;
                database.Save();


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

            if(action == "add")
            {
                Settings.TrustedUsers.Add(user);

            }
            if(action == "remove")
            {
                Settings.TrustedUsers.Remove(user);
            }

            Settings.Save();
            return "Done";

        }

        string Reload(IrcCommand command)
        {
            if (!AccessCheck(command)) return "No Permision";

              Settings = JsonConvert.DeserializeObject<BotSettings>("BotSettings.txt");

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
                database.Save();
                return "Added " + f.ClassName;
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


            if (action == "modify") database.Save();

            return "[" + action + "] " + ClassName + "'s " + property + " is now " + data;
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
                for (int i = 0; i < props.Length; i++ )
                {
                    availableProperties += props[i].Name;
                    if (i != props.Length - 1) availableProperties += ", ";
                }

                    return "properties: [" +availableProperties + "]";
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

            if(FunctionName == "add")
            {
                if (!command.Parameters[1].StartsWith("http://hastebin.com/raw/")) return "Please give me a raw hastebin link with the json blob to add";

                string json = QuickDownload(command.Parameters[1]);
                Function f = JsonConvert.DeserializeObject<Function>(json);
                f.LastUpdate = DateTime.Now;

                database.Functions.Add(f);
                database.Save();
                return "Added " + f.GetQualifiedName();
            }

     
            Function func = database.Functions.FirstOrDefault(x => x.FunctionName == FunctionName);

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
                database.Save();
            }

            return "[" + action + "] " + FunctionName + "'s " + property + " is now " + data;

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

            Function func = database.Functions.FirstOrDefault(x => x.FunctionName == FunctionName);

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

            if (action == "modify") prop.SetValue(func, data);
            if (action == "view") return prop.GetValue(func).ToString();



            if (action == "modify")
            {
                func.LastUpdate = DateTime.Now;
                database.Save();
            }

            return "[" + action + "] " + FunctionName + "'s " + property + " is now " + data;
        }

        private string QuickDownload(string url)
        {
            WebClient wc = new WebClient();
            return wc.DownloadString(url);
        }
    }

}