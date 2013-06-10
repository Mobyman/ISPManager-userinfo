using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;



namespace isp {
    class Program {
        
        const int lpIP = 0;
        const int lpLOGIN = 1;
        const int lpPASSWORD = 2;
        
        [STAThread]
        static int Main(string[] args) {
            
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            string input;
            if (args.Length == 0) {
                if (Clipboard.ContainsText()) {
                    input = Clipboard.GetText();
                } else {
                    return 1;
                }
            } else {
                input = args[0];
            }
            
            try {

                bool isValidUrl = Regex.IsMatch(input, @"^((http|https|ftp)\://)?[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*$");

                if (isValidUrl) {
                    string domain = Regex.Replace(input.Replace("http://", ""), "/.*", "");

                    IdnMapping idn = new IdnMapping();
                    string idnDomain = idn.GetAscii(domain);
                    Console.WriteLine("\n\tIDN: {0}", idnDomain);

                    string[] access = getAdminUserPassword(idnDomain);

                    if (access != null) {
                        string key = authIsp(access[lpIP], access[lpLOGIN], access[lpPASSWORD]);
                        string username = getIspUsername(access[lpIP], key, domain);
                        Console.WriteLine("\tUser: {0}", username);
                        
                        Console.WriteLine("\tPress any key to copy username");
                        ConsoleKeyInfo answer = Console.ReadKey();
                        
                        Clipboard.SetText(username, System.Windows.Forms.TextDataFormat.Text);
                        Console.WriteLine("\tUsername copied");
                        return 0;
                    }
                } else {
                    throw new System.ArgumentException();
                }
                return 1;
            }
            catch (System.Net.Sockets.SocketException) { Console.Write("\n\tIncorrect host\n"); return 1; }
            catch (System.Net.WebException) { Console.Write("\n\tNetwork error. Exit.\n"); return 1; }
            catch (System.ArgumentException) { Console.Write("\n\tInvalid arguments\n"); return 1; } 
            catch (System.Exception e) { Console.Write(e.ToString());  return 1; }
        }
        
        static private string[] getAdminUserPassword(string idnDomain) {
            string[,] loginPasswords = new string[,] {
                    { "server1", "login1", "password1" },
                    { "server2", "login2", "password2" },
                };

            System.Net.IPAddress ip = System.Net.Dns.GetHostEntry(idnDomain).AddressList[0];
            string ipString = ip.ToString();
            Console.Write("\tIP: {0} \n", ipString);

            for (int i = 0; i < loginPasswords.GetLength(0); i++) {
                if (loginPasswords[i, lpIP] == ipString) {
                    string user = loginPasswords[i, lpLOGIN];
                    string password = loginPasswords[i, lpPASSWORD];
                    return new string[] { ipString, user, password};
                }
            }
            return null;
        }

        static private string authIsp(string ip, string username, string password) {
            string authUri = String.Format("https://{0}:1500/manager/ispmgr?out=json&func=auth&username={1}&password={2}", ip, username, password);
            WebRequest auth = HttpWebRequest.Create(authUri);
            auth.Proxy = System.Net.WebRequest.DefaultWebProxy;
            auth.Credentials = CredentialCache.DefaultCredentials;
            auth.Method = "GET";

            string authJson;
            using (StreamReader reader = new StreamReader(auth.GetResponse().GetResponseStream())) {
                authJson = reader.ReadToEnd();
            }
            Newtonsoft.Json.Linq.JObject jAuthObject = Newtonsoft.Json.Linq.JObject.Parse(authJson);
            
            return jAuthObject["auth"].ToString();
        }

        static private string getIspUsername(string ip, string key, string domain) {
            string queryUri = String.Format("https://{0}:1500/manager/ispmgr?auth={1}&out=json&func=wwwdomain.edit&elid={2}", ip, key, domain);
            WebRequest req = HttpWebRequest.Create(queryUri);
            req.Method = "GET";
            req.Proxy = System.Net.WebRequest.DefaultWebProxy;
            req.Credentials = CredentialCache.DefaultCredentials;

            string source;
            using (StreamReader reader = new StreamReader(req.GetResponse().GetResponseStream())) {
                source = reader.ReadToEnd();
            }
            Newtonsoft.Json.Linq.JObject jObject = Newtonsoft.Json.Linq.JObject.Parse(source);
            return jObject["owner"].ToString();
        }
    }
}
