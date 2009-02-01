using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using Rebex.Mail;
using Rebex.Net;
using System.Collections.Specialized;
using System.IO;


namespace FetchTheMail
{
    class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("FetchTheMail.exe /server <server name> /user <user name> /pwd <password> /outdir <message output directory>");
            Console.WriteLine("Other options:");
            Console.WriteLine("/noremove 1 : don't remove the messages");
            Console.WriteLine("/port <port number> : 110 by default, 995 for ssl pop connections");
        }

        NameValueCollection GetDefaultParameters()
        {
            NameValueCollection nvc =  new NameValueCollection();
            nvc["port"] = "110";
            nvc["security"] = "Implicit";
            nvc["auth"] = "Auto";
            nvc["noremove"] = "0";
            nvc["maxcount"] = "-1";
            nvc["ssl"] = "";
            return nvc;
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            Program prg = new Program();
            NameValueCollection callParams = prg.GetDefaultParameters();
            for (int i = 0; i < args.Length - 1; i += 2)
            {
                string pname = args[i];
                string pval = args[i + 1];
                if (!pname.StartsWith("/"))
                {
                    Console.WriteLine("Invalid argument: {0}. Argument names should start with a slash, like: /server pop3.email.com", pname);
                    return;
                }
                pname = pname.Substring(1).ToLower();
                callParams[pname] = pval;
            }
            try
            {
                prg.FetchMessages(callParams);
            }
            catch (Exception ex)
            {
                log.Error("Error: {0}", ex);
                Console.Error.WriteLine("Error: {0}", ex.Message);
            }
        }

        static string RequireParam(string name, NameValueCollection param)
        {
            string s = param[name];
            if (s == null || s.Length == 0) throw new Exception("Required parameter not specified: " + name);
            return s;
        }

        public void FetchMessages(NameValueCollection param)
        {
            Pop3 client = new Pop3();
            bool connected = false; 
            try
            {
                string outdir = RequireParam("outdir", param);
                if (!Directory.Exists(outdir)) Directory.CreateDirectory(outdir);

                string server = RequireParam("server", param);
                int port = Convert.ToInt32(RequireParam("port", param));
                string ssl = param["ssl"];
                if (ssl.Length == 0)
                    ssl = port == 995 ? "1" : "0";

                TlsParameters par = new TlsParameters();
                par.CommonName = server;
                par.CertificateVerifier = CertificateVerifier.AcceptAll;
                Pop3Security sec = (Pop3Security)Enum.Parse(typeof(Pop3Security), RequireParam("security", param));
                log.Info("Connecting to {0}:{1}. SSL: {2}", server, port, ssl);
                
                string sess = client.Connect(server, port, par, sec);
                
                connected = true;
                log.Info("Connected: {0}", sess);
                Pop3Authentication auth = (Pop3Authentication)Enum.Parse(typeof(Pop3Authentication), RequireParam("auth", param));
                log.Info("Logging in: {0}", RequireParam("user", param));
                client.Login(RequireParam("user", param), RequireParam("pwd", param), auth);
                log.Info("Logged in.");


                Pop3MessageCollection messages = client.GetMessageList();
                log.Info("There are {0} messages", messages.Count);
                int maxcount = Convert.ToInt32(RequireParam("maxcount", param));
                if (maxcount <= 0 || maxcount > messages.Count) maxcount = messages.Count;
                for (int i = 0; i < maxcount; i++)
                {
                    Pop3MessageInfo mi = messages[i];
                    log.Info("Downloading message {0}", mi.SequenceNumber);
                    MailMessage mm = client.GetMailMessage(mi.SequenceNumber);
                    log.Info("Message from: {0}, to: {1}, subject: {2}", mm.From, mm.To, mm.Subject);
                    string g = Guid.NewGuid().ToString();
                    string file = Path.Combine(outdir, g + ".eml");
                    log.Info("Saving message to {0}", file);
                    mm.Save(file);
                    client.Delete(mi.SequenceNumber);
                }

                bool rollback = !"0".Equals(RequireParam("noremove", param));
                client.Disconnect(rollback);
            }
            catch (Exception ex)
            {
                if (connected) 
                    client.Disconnect(true);
                throw;
            }
            
        }

        /// <summary>
        /// Saves message XML file and 
        /// </summary>
        /// <param name="mm"></param>
        /// <param name="outDir"></param>
        /// <returns></returns>
        private string SaveMessage(MailMessage mm, string outDir)
        {
            return null;
        }

    }
}
