using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IrcBotFramework;
using Newtonsoft.Json;
using System.IO;


namespace Dota2APIBot
{

    class IRCBot : IrcBotFramework.IrcBot
    {

        FunctionDB database;

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

            database = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText("FunctionDB.txt"));

            RegisterCommand("ping", Ping);
            RegisterCommand("class", ModifyClass);
            RegisterCommand("function", FunctionMod);
            RegisterCommand("param", ParamMod);
            RegisterCommand("dumpwiki", DumpWiki);
        }

        void IRCBot_ConnectionComplete(object sender, EventArgs e)
        {
            foreach(string channel in Settings.BotChannels)
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


        public string DumpWiki(IrcCommand command)
        {
            if (!AccessCheck(command)) return "No Permision";
            database.WikiDump();
            return "Done";
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
            if(command.Parameters[0] == "properties") return "properties: [Description, Accessor, BaseClass]";

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


            ClassType clazz = database.Classes.FirstOrDefault(x => x.ClassName == ClassName);

            if (clazz == null) return "Class Not Found";

            string action = command.Parameters[1].ToLower();

            string property = command.Parameters[2].ToLower();

            string[] cpy = new string[command.Parameters.Length - 3];
            Array.Copy(command.Parameters, 3, cpy, 0, cpy.Length);

            string data = string.Join(" ", cpy);


            if (property == "accessor".ToLower())
            {
                if (action == "modify") clazz.Accessor = data;
                if (action == "view") return clazz.Accessor;
            }

            if (property == "description")
            {
                if (action == "modify") clazz.Description = data;
                if (action == "view") return clazz.Description;
            }

            if (property == "BaseClass".ToLower())
            {
                if (action == "modify") clazz.BaseClass = data;
                if (action == "view") return clazz.BaseClass;
            }

            if (action == "modify") database.Save();

            return "[" + action + "] " + ClassName + "'s " + property + " is now " + data;
        }


        public string FunctionMod(IrcCommand command)
        {
            if (command.Parameters.Length == 0 || command.Parameters[0] == "help")
            {
                return "'function <string funcName> <action (modify or view)> <property> [data]' OR 'function find <searchtext>' OR 'function properties'";
            }
            if(command.Parameters[1] == "properties")
            {
                return "properties: [Class, FunctionDescription, ReturnType, ReturnDescription, Example]";
            }
            string FunctionName = command.Parameters[0];

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

            Function func = database.Functions.FirstOrDefault(x => x.FunctionName == FunctionName);

            if (func == null) return "Function Not Found";

            string action = command.Parameters[1].ToLower();

            string property = command.Parameters[2].ToLower();

            string[] cpy = new string[command.Parameters.Length - 3];
            Array.Copy(command.Parameters, 3, cpy, 0, cpy.Length);

            string data = string.Join(" ", cpy);


            if (property == "FunctionDescription".ToLower())
            {
                if (action == "modify") func.FunctionDescription = data;
                if (action == "view") return func.FunctionDescription;
            }

            if (property == "Class".ToLower())
            {
                if (action == "modify") func.Class = data;
                if (action == "view") return func.Class;
            }

            if (property == "ReturnDescription".ToLower())
            {
                if (action == "modify") func.ReturnDescription = data;
                if (action == "view") return func.ReturnDescription;
            }

            if (property == "ReturnType".ToLower())
            {
                if (action == "modify") func.ReturnType = data;
                if (action == "view") return func.ReturnType;
            }
            if (property == "Example".ToLower())
            {
                if (action == "modify") func.Example = data;
                if (action == "view") return func.Example;
            }

            if (action == "modify") database.Save();

            return "[" + action + "] " + FunctionName + "'s " + property + " is now " + data;

        }

        public string ParamMod(IrcCommand command)
        {
            if (command.Parameters.Length == 0 || command.Parameters[0] == "help")
            {
                return "'param <string funcName> <paramId> <action (modify or view)> <property> [data]' OR 'param list <func>' OR 'param properties'";
            }
            if (command.Parameters[1] == "properties")
            {
                return "properties: [Name, Type, Description]";
            }
            string FunctionName = command.Parameters[0];

            if (FunctionName == "list")
            {
                Function f = database.Functions.FirstOrDefault(x => x.FunctionName == command.Parameters[2]);

                if (f == null) return "Function Not Found";

                int i;
                for(i = 0; i < f.Params.Count - 1; i++)
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

            string property = command.Parameters[3].ToLower();

            string[] cpy = new string[command.Parameters.Length - 4];
            Array.Copy(command.Parameters, 4, cpy, 0, cpy.Length);

            string data = string.Join(" ", cpy);


            if (property == "Name".ToLower())
            {
                if (action == "modify") param.Name = data;
                if (action == "view") return param.Name;
            }

            if (property == "Type".ToLower())
            {
                if (action == "modify") param.Type = data;
                if (action == "view") return param.Type;
            }

            if (property == "Description".ToLower())
            {
                if (action == "modify") param.Description = data;
                if (action == "view") return param.Description;
            }

            if (action == "modify") database.Save();

            return "[" + action + "] " + FunctionName + "'s " + property + " is now " + data;
        }

    }

}