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
#if !DIFFCREATOR  
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
#else

            ParseDotaDumpDiff(args[0]);
#endif
        }

        private static void ParseDotaDumpDiff(string data)
        {
            FunctionDB db = JsonConvert.DeserializeObject<FunctionDB>(Util.QuickDownload("http://rhoyne.cloudapp.net/FunctionDB.txt"));

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
                    else if(parsedfunc.ReturnType != dbFunc.ReturnType)
                    {
                        DiffDB.Functions.Add(c);
                    }
                    else
                    {
                        bool changed = false;
                        //Lets walk through the params and see if we have better names
                        for (int i = 0; i < dbFunc.Params.Count; i++)
                        {
                            Param oldp = c.Params[i];
                            Param newp = parsedfunc.Params[i];

                            if(string.IsNullOrEmpty(oldp.Name))
                            {
                                oldp.Name = newp.Name;
                                changed = true;                        
                            }

                        }

                        if (changed)
                            DiffDB.Functions.Add(c);
                    }
                   


                    if(string.IsNullOrEmpty(dbFunc.FunctionDescription) && !string.IsNullOrEmpty(parsedfunc.FunctionDescription)) // Do they have a better description?  Take it
                    {
                        c.FunctionDescription = parsedfunc.FunctionDescription;
                        DiffDB.Functions.Add(c);
                    }

                }

            }
            foreach(ClassType c in parsed.Classes)
            {
                ClassType oc = db.Classes.FirstOrDefault(x => x.ClassName == c.ClassName);
                if(oc == null)
                {
                    DiffDB.Classes.Add(c);
                }
            }
            foreach (ConstantGroup group in parsed.Constants)
            {
                //Get the previous constant group

                ConstantGroup ogcg = db.Constants.FirstOrDefault(x => x.EnumName == group.EnumName);
                if(ogcg == null)
                {
                    DiffDB.Constants.Add(group.Clone() as ConstantGroup);
                }
                else
                {
                    foreach(ConstantEntry entry in group.Entries)
                    {
                        var oge = ogcg.Entries.FirstOrDefault(x => x.Name == entry.Name);

                        if(oge == null)
                        {
                            DiffDB.Constants.Add(group.Clone() as ConstantGroup);
                            break;
                        }
                        else if(oge.Value != entry.Value)
                        {
                            DiffDB.Constants.Add(group.Clone() as ConstantGroup);
                            break;
                        }
                    }
                }

                


            }


            File.WriteAllText("Diffdb.txt", JsonConvert.SerializeObject(DiffDB, Formatting.Indented));


            Console.WriteLine("Done");

        }
        enum Parsing_E
        {
            None,
            Function,
            Enum,
        }
        private static FunctionDB ParseScriptDump(string data)
        {
            FunctionDB parsedDB = new FunctionDB();
            //Parse the outputdata

            string[] Text = data.Split('\n');


            Parsing_E Parsing = Parsing_E.None;

            Function currentFunction = null;
            ConstantGroup currentConstants = null;

            string previousLine = "";

            foreach (string line in Text)
            {
                //remove all the [    Vscript   ]: text
                string l = line.Remove(0, line.IndexOf(':') + 1);
                l = l.Trim();

                //Three - means description, and the start of a new function definition
                if (l.StartsWith("---"))
                {
                    if(Parsing == Parsing_E.Enum)
                    {
                        //Commit the enum
                        //commit the changes
                        parsedDB.Constants.Add(currentConstants);

                        Console.WriteLine("Parsed " + currentConstants.EnumName);

                        currentConstants = null;

                        Parsing = Parsing_E.None;
                    }

                    if (Parsing != Parsing_E.None)
                    {
                        throw new Exception("Error: Found a new definition while already writing a function");
                    }

                    l = l.Replace("---", "").Trim();

                    if (l.StartsWith("[["))
                    {
                        currentFunction = new Function();

                        //Remove the comment lines
                        l = l.Replace("[[", "");
                        l = l.Replace(" ]]", "").Trim();

                        if (l.Contains(' ')) l = l.Substring(l.IndexOf(' ') + 1).Trim(); //If there is a space, that means we have a description.  Remove the function name
                                                                                         //from it and just use it
                        else l = ""; //If the above fails, the description only contains the function name.  Ignore it.  

                        //Add this line as the current functions description
                        currentFunction.FunctionDescription = l;

                        Parsing = Parsing_E.Function;
                    }
                    else if(l.StartsWith("Enum"))
                    {
                        currentConstants = new ConstantGroup();

                        l = l.Replace("Enum", "").Trim();
                        currentConstants.EnumName = l;


                        Parsing = Parsing_E.Enum;
                    }

                }
                else if (Parsing == Parsing_E.Function && l.StartsWith("--"))
                {
                    if (currentFunction == null)
                    {
                        throw new Exception("Error: Parse a function when no function created");
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
                else if (Parsing == Parsing_E.Function && l.StartsWith("function"))
                {
                    if (currentFunction == null)
                    {
                        throw new Exception("Error: Trying to add on to a function without a function created");
                       
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
                else if(Parsing == Parsing_E.Enum && !string.IsNullOrEmpty(l)) //Modifer entry
                {
                    if(currentConstants == null)
                    {
                        throw new Exception("Tried to parse constant but group was null");
                    }

                    string[] Words = l.Split(' ');
                    ConstantEntry ce = new ConstantEntry()
                    {
                        Name = Words[0],
                        Value = Words[2]
                    };
                    if (l.Contains("--"))
                    {
                        string comment = l.Substring(l.IndexOf("--") + 2).Trim();
                        ce.Description = comment;
                    }

                    currentConstants.Entries.Add(ce);

                }
                else if (string.IsNullOrEmpty(l))
                {
                    if(previousLine == line && Parsing == Parsing_E.None)
                    {
                        continue; //Skip multiple blank lines
                    }
                    if (Parsing == Parsing_E.None)
                    {
                        throw new Exception("Tried to commit something when nothing was being worked on");                        
                    }

                    if(Parsing == Parsing_E.Function)
                    {
                        //Commit the changes
                        parsedDB.Functions.Add(currentFunction);


                        Console.WriteLine("Parsed " + currentFunction.GetQualifiedName());

                        currentFunction = null;
                    }
                    if(Parsing == Parsing_E.Enum)
                    {
                        //commit the changes
                        parsedDB.Constants.Add(currentConstants);

                        Console.WriteLine("Parsed " + currentConstants.EnumName);

                        currentConstants = null;
                    }

                    Parsing = Parsing_E.None;
                }

                previousLine = line;
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
