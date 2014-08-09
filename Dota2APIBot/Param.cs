using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dota2APIBot
{
    public class ClassType
    {
        public string ClassName { get; set; }
        public string Description { get; set; }
        public string Accessor { get; set; }
        public string BaseClass { get; set; }

        public ClassType()
        {
            ClassName = "Unknown";
            Description = "No Description Set";
            Accessor = "Unknown";
            BaseClass = "";
        }
    }

    public class Param
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }

        public Param()
        {
            Name = "Unknown";
            Type = "No Type";
            Description = "No Description Set";
        }


    }

    public class Function
    {
        public string FunctionName { get; set; }
        public string Class { get; set; }
        public string FunctionDescription { get; set; }

        public List<Param> Params = new List<Param>();
        public string ReturnType { get; set; }
        public string ReturnDescription { get; set; }

        public string Example { get; set; }

        public DateTime NextUpdate { get; set; }

        public Function()
        {
            FunctionName = "Unknown";
            Class = "Global";

            FunctionDescription = "No Description Set";
            ReturnType = "Unknown";
            ReturnDescription = "No Description Set";

            Example = "";
            NextUpdate = DateTime.Now + TimeSpan.FromDays(1);
        }

        public string ToIRCFormat()
        {
            //ret ClassName::FunctionName(p1, p2, p3) - Description
            string functionheader = string.Format("{0} {1}:{2}(", ReturnType, Class, FunctionName);
            if (Params.Count != 0)
            {
                for (int i = 0; i < Params.Count; i++)
                {
                    Param p = Params[i];

                    functionheader += string.Format("{0} {1}", p.Type, p.Name);

                    if (i != Params.Count - 1)
                    {
                        functionheader += ", ";
                    }


                }

            }

            functionheader += ") - " + FunctionDescription;

            return functionheader;

        }

        public string ToWikiFormat()
        {
            string wikiFormat = "";
            wikiFormat += "|-\n";
            wikiFormat += string.Format("| [[Dota 2 Workshop Tools/Scripting/API/{0}.{1} | {2}]]\n", Class, FunctionName, FunctionName);

            wikiFormat += string.Format("| <code>{0} {1}(", ReturnType, FunctionName);

            if (Params.Count != 0)
            {
                for (int i = 0; i < Params.Count; i++)
                {
                    Param p = Params[i];
                    wikiFormat += string.Format("{0} {1}", p.Type, p.Name);

                    if (i != Params.Count - 1)
                    {
                        wikiFormat += ", ";
                    }

                }

            }

            wikiFormat += ") </code>\n";

            wikiFormat += string.Format("| {0}\n", FunctionDescription);

            return wikiFormat;
        }

        public string ToDetailedWikiFormat()
        {

            StringBuilder page = new StringBuilder();
            //HEADER
            page.AppendLine("{{Note | This page is automatically generated.  Any changes may be overwritten}}");
            page.AppendLine();
            page.AppendLine("== Function Description ==");
            page.AppendLine();
            page.AppendLine();

            //Function Name
            page.Append("''' " + ReturnType + " " + FunctionName + "(");
            for (int i = 0; i < Params.Count; i++)
            {
                Param p = Params[i];
                page.Append(p.Type + " ''" + p.Name + "''");
                if (i != Params.Count - 1)
                {
                    page.Append(", ");
                }
            }
            page.AppendLine(") '''");
            page.AppendLine();

            //Function Description
            page.AppendFormat("''{0}''", FunctionDescription);

            page.AppendLine();
            page.AppendLine();
            page.AppendLine();
            page.AppendLine();

            if(Example != "")
            {
                //Example
                page.AppendLine(";Example");
                page.AppendLine(@"<source lang=""lua"">");
                page.AppendLine(Example);
                page.AppendLine("</source>");
            }           


            //Parameters
            if (Params.Count != 0)
            {


                page.AppendLine("== Parameters ==");
                page.AppendLine(@"{| class=""standard-table"" style=""width: 50%;""");
                page.AppendLine("! Type");
                page.AppendLine("! Name");
                page.AppendLine("! Description");

                for (int i = 0; i < Params.Count; i++)
                {
                    Param p = Params[i];
                    page.AppendLine("|-");
                    page.AppendLine("| " + p.Type);
                    page.AppendLine("| " + p.Name);
                    page.AppendLine("| " + p.Description);
                }
                page.AppendLine("|}");
            }

            //RETVAL
            if (ReturnType != "void")
            {

                page.AppendLine();
                page.AppendLine("== Returns ==");
                page.AppendLine();

                page.AppendFormat("''{0}'' - {1}", ReturnType, ReturnDescription);
                page.AppendLine();
            }


            return page.ToString();
        }
    }




}
