using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dota2APIBot
{
    static class Util
    {
        private static readonly Regex HasteKeyRegex = new Regex(@"{""key"":""(?<key>[a-z].*)""}", RegexOptions.Compiled);


        public static string Haste(this string message)
        {

            string hastebin = @"http://hastebin.com/";

            using (WebClient client = new WebClient())
            {
                string response;
                try
                {
                    response = client.UploadString(hastebin + "documents", message);
                }
                catch (System.Net.WebException e)
                {
                    if (e.Status == WebExceptionStatus.ProtocolError)
                    {
                        if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.ServiceUnavailable)
                        {
                            return "Error: string was too big for hastebin!";
                        }
                        else if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotFound)
                        {
                            return "Error: Page not found";
                        }
                    }
                    throw e;
                }


                var match = HasteKeyRegex.Match(response);
                if (!match.Success)
                {
                    return "Error: " + response;
                }


                return hastebin + match.Groups["key"];
            }

        }


        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }
}
