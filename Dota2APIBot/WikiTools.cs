using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Specialized;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Text.RegularExpressions;

namespace Dota2APIBot
{
    static class WikiTools
    {
        static WebClientExt wc;

        const string VDCWiki = "https://developer.valvesoftware.com";

        const string UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.125 Safari/537.36";


        static Dictionary<string, string> Captchas = new Dictionary<string, string>()
        {
            {"There are three words that make up this site's name. What is the first letter of the *second* word?", "d"},
            {"What is Valve CEO Gabe Newell's first name?", "gabe"},
            {"What is the company name in 'Valve Developer Community?'", "valve"},
            {"This site's name is three words. What is the first letter of the *third* word?", "c"},
            {"This site's name consists of three words. What is the first letter of the *first* word?", "v"},


        };


        public static void ConnectToWiki(BotSettings settings)
        {

            CookieContainer cc = new CookieContainer();
            wc = new WebClientExt(cc);
            wc.Headers.Add("User-Agent", UserAgent);


            string loginPage = "https://developer.valvesoftware.com/w/index.php?title=Special:UserLogin&returnto=Main_Page";

            string loginSubmit = "https://developer.valvesoftware.com/w/index.php?title=Special:UserLogin&action=submitlogin&type=login&returnto=Main_Page";

            //Go to the login page to get the session cookie
            string s = wc.DownloadString(loginPage);


            //Search the string for the login token:
            string tokenkey = "name=\"wpLoginToken\" value=\"";
            int indexOfStart = s.IndexOf(tokenkey) + tokenkey.Length;
            int end = s.IndexOf("\"", indexOfStart);

            string token = s.Substring(indexOfStart, end - indexOfStart);

            string username = settings.VDCLogin;
            string password = settings.VDCPassword;

            NameValueCollection nvc = new NameValueCollection();

            nvc.Add("wpName", username);
            nvc.Add("wpPassword", password);
            nvc.Add("pwLoginAttempt", "Log In");
            nvc.Add("wpLoginToken", token);
            nvc.Add("wpRemember", "1");

            byte[] response = wc.UploadValues(loginSubmit, "POST", nvc);

            File.WriteAllText("test.html", Encoding.ASCII.GetString(response));

        }


        private static readonly Dictionary<string, Regex> Matches = new Dictionary<string, Regex>()
        {
            {"wpStarttime", new Regex(@"<input type='hidden' value="".+"" name=""wpStarttime"" />", RegexOptions.Compiled)},
            {"wpEdittime", new Regex(@"<input type='hidden' value="".+"" name=""wpEdittime"" />", RegexOptions.Compiled)},
            {"wpAutoSummary", new Regex(@"<input type=""hidden"" value="".+"" name=""wpAutoSummary"" />", RegexOptions.Compiled)},
            {"wpEditToken", new Regex(@"<input type=""hidden"" value="".+\+\\"" name=""wpEditToken"" />", RegexOptions.Compiled)},


        };

        static Regex captcha = new Regex(@"<label for=""wpCaptchaWord"">.+</label>", RegexOptions.Compiled);

        static Regex captchaflag = new Regex(@"<input type=""hidden"" name=""wpCaptchaId"" id=""wpCaptchaId"" value="".+"" />", RegexOptions.Compiled);

        public static void WriteTextToPage(string Page, string Content)
        {

            string Editurl = VDCWiki + "/w/index.php?title=Dota_2_Workshop_Tools/Scripting/API";
            if(Page != "") Editurl += "/" + Page;
                

            string page = wc.DownloadString(Editurl + "&action=edit");

            string search = "value=\"";
            int s;
            int e;

            string match;

          
            string boundry = "----WebKitFormBoundarymI2XVIzC2NkASaVa";

            StringBuilder header = new StringBuilder();



            header.Append("--" + boundry + "\r\n");
            string content = "Content-Disposition: form-data; name=\"{0}\"";



            foreach (KeyValuePair<string, Regex> regex in Matches)
            {
                match = regex.Value.Match(page).Value;
                s = match.IndexOf(search) + search.Length;
                e = match.IndexOf("\"", s + 1);

                match = match.Substring(s, e - s);

                header.Append(string.Format(content, regex.Key));
                header.Append("\r\n");
                header.Append("\r\n");
                header.Append(match);
                header.Append("\r\n--" + boundry + "\r\n");

            }

            header.Append(string.Format(content, "wpScrollTop"));
            header.Append("\r\n");
            header.Append("\r\n");
            header.Append("0");
            header.Append("\r\n--" + boundry + "\r\n");
            header.Append(string.Format(content, "oldid"));
            header.Append("\r\n");
            header.Append("\r\n");
            header.Append("0");
            header.Append("\r\n--" + boundry + "\r\n");
            header.Append(string.Format(content, "wpTextbox1"));
            header.Append("\r\n");
            header.Append("\r\n");
            header.Append(Content);
            header.Append("\r\n");
            header.Append("\r\n--" + boundry + "\r\n");
            header.Append(string.Format(content, "wpSummary"));
            header.Append("\r\n");
            header.Append("\r\n");
            header.Append("Dota_Lebot push: Updated Page");
            header.Append("\r\n--" + boundry + "\r\n");
            header.Append(string.Format(content, "wpSave"));
            header.Append("\r\n");
            header.Append("\r\n");
            header.Append("Save page");
            header.Append("\r\n--" + boundry + "\r\n");
            header.Append(string.Format(content, "wpSection"));
            header.Append("\r\n");
            header.Append("\r\n");
            header.Append("");

            string endBoundry = "\r\n--" + boundry + "--\r\n";
            string text = SendDataToWebsite(Editurl, header.ToString() + endBoundry);

            
            //Check to see if the response has a captcha in it
            Match m = captchaflag.Match(text);

            //Uncaptcha this 
            if (m.Success)
            {
                string label = @"<label for=""wpCaptchaWord"">";
                s = text.IndexOf(label) + label.Length;
                e = text.IndexOf("</label>", s + 1);

                string captcha = text.Substring(s, e - s);

                string answer = Captchas[captcha];

                label = @"id=""wpCaptchaId"" value=""";
                s = text.IndexOf(label) + label.Length;
                e = text.IndexOf("\"", s + 1);

                string id = text.Substring(s, e - s);


                header.Append("\r\n--" + boundry + "\r\n");
                header.Append(string.Format(content, "wpCaptchaWord"));
                header.Append("\r\n");
                header.Append("\r\n");
                header.Append(answer);

                header.Append("\r\n--" + boundry + "\r\n");
                header.Append(string.Format(content, "wpCaptchaId"));
                header.Append("\r\n");
                header.Append("\r\n");
                header.Append(id);

                string res = SendDataToWebsite(Editurl, header.ToString() + endBoundry);

            }



            string newPage = "https://developer.valvesoftware.com/wiki/Dota_2_Workshop_Tools/Scripting/API/" + Page;

            wc.DownloadString(newPage);



        }

        private static string SendDataToWebsite(string URL, string data)
        {
            string boundry = "----WebKitFormBoundarymI2XVIzC2NkASaVa";

            HttpWebRequest wr = WebRequest.Create(URL + "&action=submit") as HttpWebRequest;
            wr.Method = "POST";


            wr.CookieContainer = wc.container;
            wr.ContentType = "multipart/form-data; boundary=" + boundry;
            wr.KeepAlive = true;
            wr.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            wr.UserAgent = UserAgent;

            Stream req = wr.GetRequestStream();

            byte[] buff = Encoding.ASCII.GetBytes(data);

            req.Write(buff, 0, buff.Length);


            WebResponse response = wr.GetResponse();

            StreamReader sr = new StreamReader(response.GetResponseStream());

            return sr.ReadToEnd();

        }

    }
}
