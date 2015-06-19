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
             

            //ParseDotaDumpDiff("dotascriptapioutput.txt");
        }

        private static void ParseDotaDumpDiff(string data)
        {
            FunctionDB db = JsonConvert.DeserializeObject<FunctionDB>(File.ReadAllText("FunctionDB.txt"));

            FunctionDB parsed = ParseScriptDump(File.ReadAllText(data));

            FunctionDB DiffDB = new FunctionDB();

            foreach(Function parsedfunc in parsed.Functions)
            {
                //Figure out if we have it in our db
                var dbFunc = db.Functions.FirstOrDefault(x => x.GetQualifiedName() == parsedfunc.GetQualifiedName());
                if(dbFunc == null) //We don't have the function.  Take it.
                {
                    DiffDB.Functions.Add(parsedfunc.Clone() as Function);

                }
                else //Function exists, lets check to see if anything has changed
                {
                    Function c = dbFunc.Clone() as Function;
                    if (parsedfunc.Params.Count > dbFunc.Params.Count) //Parsedfunc has more params.  Get the new ones
                    {
                        c.Params.Clear();
                        for (int i = 0; i < dbFunc.Params.Count; i++)
                        {
                            if(dbFunc.Params[i].Type == parsedfunc.Params[i].Type) //It's the same, preserve it
                            {
                                c.Params.Add(dbFunc.Params[i].Clone() as Param);
                            }
                            else //It's different.  Just grab the parsed func
                            {
                                c.Params.Add(parsedfunc.Params[i].Clone() as Param);
                            }
                        }
                        //Add the rest
                        for(int i = dbFunc.Params.Count; i < parsedfunc.Params.Count; i++)
                        {
                            c.Params.Add(parsedfunc.Params[i].Clone() as Param);
                        }
                        

                        DiffDB.Functions.Add(c);
                    }
                    else if(parsedfunc.Params.Count != dbFunc.Params.Count) //They are different in another way, just copy the parsed func ones
                    {
                        c.Params.Clear();
                        foreach(Param p in parsedfunc.Params)
                        {
                            c.Params.Add(p.Clone() as Param);
                        }
                        DiffDB.Functions.Add(c);
                    }

                    if(string.IsNullOrEmpty(dbFunc.FunctionDescription) && !string.IsNullOrEmpty(parsedfunc.FunctionDescription)) // Do they have a better description?  Take it
                    {
                        c.FunctionDescription = parsedfunc.FunctionDescription;
                        DiffDB.Functions.Add(c);
                    }

                }

            }

            File.WriteAllText("Diffdb.txt", JsonConvert.SerializeObject(DiffDB, Formatting.Indented));


            Console.WriteLine("Done");

        }

        private static FunctionDB ParseScriptDump(string data)
        {
            FunctionDB parsedDB = new FunctionDB();
            //Parse the outputdata

            string[] Text = data.Split('\n');

            Function currentFunction = null;

            foreach (string line in Text)
            {
                //remove all the [    Vscript   ]: text
                string l = line.Remove(0, line.IndexOf(':') + 1);
                l = l.Trim();

                //Three - means description, and the start of a new function definition
                if (l.StartsWith("---"))
                {
                    if (currentFunction != null)
                    {
                        Console.WriteLine("Error: Found a new function definition while already writing a function");
                        return null;
                    }

                    currentFunction = new Function();

                    //Remove the comment lines
                    l = l.Replace("---[[", "");
                    l = l.Replace(" ]]", "").Trim();

                    if (l.Contains(' ')) l = l.Substring(l.IndexOf(' ') + 1).Trim(); //If there is a space, that means we have a description.  Remove the function name
                                                                                    //from it and just use it
                    else l = ""; //If the above fails, the description only contains the function name.  Ignore it.  

                    //Add this line as the current functions description
                    currentFunction.FunctionDescription = l;


                }
                else if (l.StartsWith("--"))
                {
                    if (currentFunction == null)
                    {
                        Console.WriteLine("Error: Trying to add on to a function without a function created");
                        return null;
                    }

                    //Possible cases
                    //@return type - Return value
                    //@param Name Type - Param name and type.  Can be multiple


                    l = l.Replace("--", "").Trim(); //Remove the comment stuff

                    if (l.StartsWith("@return"))
                    {
                        currentFunction.ReturnType = l.Replace("@return", "").Trim();
                    }
                    if (l.StartsWith("@param"))
                    {
                        string paramdat = l.Replace("@param", "").Trim();
                        string[] d = paramdat.Split(' ');
                        Param p = new Param();
                        p.Name = d[0];
                        p.Type = d[1];

                        currentFunction.Params.Add(p);
                    }

                }
                else if (l.StartsWith("function"))
                {
                    if (currentFunction == null)
                    {
                        Console.WriteLine("Error: Trying to add on to a function without a function created");
                        return null;
                    }

                    //Pattern: function [ClassName:]FunctionName( param ...) end

                    //We want to pull out a class name and a function name name out of this

                    currentFunction.Example = l;

                    l = l.Replace("function", "").Trim();

                    if (l.Contains(":"))
                    {
                        //We have a classname
                        l = l.Substring(0, l.IndexOf("("));
                        var s = l.Split(':');
                        currentFunction.Class = s[0];
                        currentFunction.FunctionName = s[1];

                        //Check to see if the class exists
                        ClassType c = parsedDB.Classes.FirstOrDefault(x => x.ClassName == currentFunction.Class);
                        if (c == null)
                        {
                            //Create the class if it doesn't exist
                            c = new ClassType();
                            c.ClassName = currentFunction.Class;
                            parsedDB.Classes.Add(c);
                        }
                    }
                    else
                    {
                        l = l.Substring(0, l.IndexOf("("));
                        currentFunction.FunctionName = l;
                        currentFunction.Class = "Global";
                    }


                }
                else if (string.IsNullOrEmpty(l))
                {
                    if (currentFunction == null)
                    {
                        Console.WriteLine("Error: Tried to commit a function when none was being worked on");
                        return null;
                    }
                    //Commit the changes
                    parsedDB.Functions.Add(currentFunction);


                    Console.WriteLine("Parsed " + currentFunction.GetQualifiedName());

                    currentFunction = null;
                }

            }

            return parsedDB;
        }


        private static void ParseWikiDump()
        {
            FunctionDB Database = new FunctionDB();

            string Data = File.ReadAllText("WikiDump.txt");


            string[] lines = Data.Split('\n');

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
