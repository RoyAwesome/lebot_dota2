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

        public Function()
        {
            FunctionName = "Unknown";
            Class = "Global";

            FunctionDescription = "No Description Set";
            ReturnType = "Unknown";
            ReturnDescription = "No Description Set";

            Example = "";
        }

        public string ToIRCFormat()
        {
            //ret ClassName::FunctionName(p1, p2, p3) - Description
            string functionheader = string.Format("{0} {1}:{2}(", ReturnType, Class, FunctionName);
            if (Params.Count != 0)
            {
                for(int i = 0; i < Params.Count; i++)
                {
                    Param p = Params[i];

                    functionheader += string.Format("{0} {1}", p.Type, p.Name);

                    if(i != Params.Count - 1)
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
    }

   
}
