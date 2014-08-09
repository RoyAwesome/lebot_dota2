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


        public string[] LookupFunction(string searchText)
        {
            return Functions.Where(x => x.FunctionName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).Select(x => x.ToIRCFormat()).ToArray();
        }

        public void Save()
        {
            string text = JsonConvert.SerializeObject(this, Formatting.Indented);

            File.WriteAllText("FunctionDB.txt", text);
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
