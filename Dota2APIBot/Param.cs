using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dota2APIBot
{
    public class ClassType : ICloneable
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

        public object Clone()
        {
            return new ClassType()
            {
                ClassName = this.ClassName,
                Description = this.Description,
                Accessor = this.Accessor,
                BaseClass = this.BaseClass
            };
        }
    }

    public class Param : ICloneable
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

        public object Clone()
        {
            return new Param()
            {
                Name = this.Name,
                Type = this.Type,
                Description = this.Description
            };
        }
    }

    public class Function : ICloneable
    {
        public string FunctionName { get; set; }
        public string Class { get; set; }
        public string FunctionDescription { get; set; }

        public List<Param> Params = new List<Param>();
        public string ReturnType { get; set; }
        public string ReturnDescription { get; set; }

        public string Example { get; set; }

        public DateTime LastUpdate { get; set; }

        public Function()
        {
            FunctionName = "Unknown";
            Class = "Global";

            FunctionDescription = "No Description Set";
            ReturnType = "Unknown";
            ReturnDescription = "No Description Set";

            Example = "";
            LastUpdate = DateTime.Now;
        }

        public object Clone()
        {
            Function f = new Function()
            {
                FunctionName = this.FunctionName,
                Class = this.Class,
                FunctionDescription = this.FunctionDescription,
                ReturnType = this.ReturnType,
                ReturnDescription = this.ReturnDescription,
                Example = this.Example,
                LastUpdate = this.LastUpdate,
            };

            foreach(Param p in Params)
            {
                f.Params.Add(p.Clone() as Param);
            }
            return f;
        }

        public string GetQualifiedName()
        {
            return Class + "." + FunctionName;
        }

        public string GetSimpleName()
        {
            string name = FunctionName;
            name += "(";
            for (int i = 0; i < Params.Count; i++)
            {
                Param p = Params[i];
                string pname = p.Name;
                if (pname == "") pname = Letters[i].ToString();

                name += string.Format("{0} {1}", p.Type, pname);

                if (i != Params.Count - 1)
                {
                    name += ", ";
                }
            }

            name += ")";

            return name;
        }

        public string GetFullName()
        {
            return Class + "." + GetSimpleName();
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
                    string pname = p.Name;
                    if (pname == "") pname = Letters[i].ToString();

                    functionheader += string.Format("{0} {1}", p.Type, pname);

                    if (i != Params.Count - 1)
                    {
                        functionheader += ", ";
                    }


                }

            }

            functionheader += ") - " + WikiTools.APIPage + "/"+Class + "." + FunctionName;

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
                    string pname = p.Name;
                    if (pname == "") pname = Letters[i].ToString();

                    wikiFormat += string.Format("{0} {1}", p.Type, pname);

                    if (i != Params.Count - 1)
                    {
                        wikiFormat += ", ";
                    }

                }

            }

            wikiFormat += ") </code>\n";

            string[] lines = FunctionDescription.Split('\n');

            wikiFormat += string.Format("| {0}\n", lines[0]);

            return wikiFormat;
        }
        const string Letters = "abcdefghijklmnopqrstuvwxyz";
        public string ToDetailedWikiFormat()
        {

            StringBuilder page = new StringBuilder();
            //HEADER
            page.AppendLine("{{Note | This page is automatically generated.  Any changes may be overwritten}}");
            page.AppendLine("[[Category:Dota2Function]]");
            page.AppendFormat("[[Category:{0}]]\n", Class);
            page.AppendLine();
            page.AppendLine("== Function Description ==");
            page.AppendLine();
            page.AppendLine();

            //Function Name
            page.Append("''' " + ReturnType + " " + FunctionName + "(");
            for (int i = 0; i < Params.Count; i++)
            {
                Param p = Params[i];
                string pname = p.Name;
                if (pname == "") pname = Letters[i].ToString();
                page.Append(p.Type + " ''" + pname + "''");
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
                    string pname = p.Name;
                    if (pname == "") pname = Letters[i].ToString();

                    page.AppendLine("| " + pname);
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
