using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Dota2APIBot
{
    class FunctionDB
    {
        public List<Function> Functions = new List<Function>();
        public List<ClassType> Classes = new List<ClassType>();
        public string DatabaseHeader = "";
        public DateTime LastPush = DateTime.Now;


        public string[] LookupFunction(string searchText)
        {
            return Functions.Where(x => x.FunctionName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).Select(x => x.ToIRCFormat()).ToArray();
        }

        public void Save()
        {
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);

            File.WriteAllText("FunctionDB.txt", text);
        }

        public string RSTDump()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Dota 2 Lua API");
            builder.AppendLine("==============");

            builder.AppendLine(DatabaseHeader);

            builder.AppendLine();
            builder.AppendLine();

            //Write the quick list
            foreach(ClassType c in Classes)
            {
                
                builder.AppendLine(c.ClassName);
                builder.AppendLine("############");

                if(c.BaseClass != "") builder.AppendLine("extends " + c.BaseClass);

                builder.AppendLine(c.Description);

                builder.AppendLine();
                

                foreach (Function func in Functions.Where(x => x.Class == c.ClassName).OrderBy(x => x.FunctionName))
                {
                    builder.AppendLine("  * :ref:`" +func.GetSimpleName() + " <" + func.GetQualifiedName() + ">`");

                }

                builder.AppendLine();

                
            }

            //Write the detail pages
            foreach (ClassType c in Classes)
            {
                foreach (Function func in Functions.Where(x => x.Class == c.ClassName).OrderBy(x => x.FunctionName))
                {

                    builder.AppendLine(" .. _" + func.GetQualifiedName() + ":");

                    builder.Append("" + func.ReturnType + " " + func.GetFullName());
                    builder.AppendLine("---------------");

                    builder.AppendLine();

                    builder.AppendLine(func.FunctionDescription);

                    builder.AppendLine();
                    builder.AppendLine();

                    if (func.Example != "")
                    {                        
                        builder.AppendLine("::");
                        builder.Append("    ");
                        builder.AppendLine(func.Example);
                    }

                    if(func.Params.Count > 0)
                    {
                        builder.AppendLine("+-----------+--------------+--------------+");
                        builder.AppendLine("|Type       |  Name        |  Description |");
                        builder.AppendLine("+===========+==============+==============+");
                        for(int i = 0; i < func.Params.Count; i++)
                        {
                            Param p = func.Params[i];
                            builder.AppendLine("|  " + p.Type + " | " + p.Name + " | " + p.Description + " |");
                        }
                        builder.AppendLine("+-----------+--------------+--------------+");
                    }

                    if(func.ReturnType != "void")
                    {
                        builder.AppendLine("Returns:");

                        builder.AppendLine(func.ReturnType + " - " + func.ReturnDescription);
                    }

                    builder.AppendLine();
                    builder.AppendLine();

                }
            }
            


            return builder.ToString();

        }


        public string WikiDump()
        {
            //Wiki markup dump
            StringBuilder builder = new StringBuilder();

            builder.AppendLine(DatabaseHeader);

            
           
            builder.AppendLine("__TOC__");

            foreach(ClassType type in Classes)
            {

                builder.AppendLine("=== " + type.ClassName + " ===");

                
                if (type.BaseClass != "")
                {
                    builder.AppendLine(":::::extends [[#" + type.BaseClass + "| " + type.BaseClass + "]]");
                }
                builder.AppendLine("''" + type.Description + "''");
                if(type.Accessor != "")
                {
                    builder.AppendLine();
                    builder.AppendLine("''Global accessor variable:'' <code>" + type.Accessor + "</code>");
                }

                builder.AppendLine("{| class=\"standard-table\" style=\"width: 100%;\"");
                builder.AppendLine("! Function ");
                builder.AppendLine("! Signature ");
                builder.AppendLine("! Description ");

                foreach(Function func in Functions.Where(x => x.Class == type.ClassName).OrderBy(x => x.FunctionName))
                {
                    builder.Append(func.ToWikiFormat());
                }

                builder.AppendLine("|}");
                builder.AppendLine();
                builder.AppendLine();
            }

           return builder.ToString();

        }
    }
}
