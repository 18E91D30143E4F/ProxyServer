using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Proxy_Server
{
    class Http
    {
        public string modifiedRequest { get; private set; }
        public IPHostEntry host { get; private set; }
        public IPAddress ip { get; private set; }
        public int port { get; private set; }

        public Http(string request)
        {
            GetHost(request);
            modifiedRequest = ConvertUri(request);
        }

        private void GetHost(string request)
        {
            // Split on lines by separators
            string[] lines = request.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string preparedHost = "";

            // Take host from request
            foreach (string line in lines)
            {
                if (line.Contains("Host: "))
                {
                    preparedHost = line.Substring(line.IndexOf("Host: ") + 6);
                    break;
                }
            }

            if (preparedHost != "")
            {
                // Cuts port and ip
                if (preparedHost.Contains(":"))
                {
                    port = Int32.Parse(preparedHost.Substring(preparedHost.IndexOf(":") + 1));
                    preparedHost = preparedHost.Substring(0, preparedHost.IndexOf(":"));
                }
                else
                {
                    // if no port in request PORT STANDART
                    port = 80;
                }

                host = Dns.GetHostEntry(preparedHost);
                ip = host.AddressList[0];
            }
            else
            {
                ip = null;
                host = null;
            }
        }

        private string ConvertUri(string request)
        {
            const string pattern = @"http:\/\/[a-z0-9а-яё\:\.]*";
            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(request);

            if (matches.Count != 0)
            {
                // Absolute path -> Relative path
                string uri = matches[0].Value;
                string result = request.Replace(uri, "");


                return result;
            }
            else
            {
                return null;
            }
        }

        //Cuts out the absolute path from request
        private string GetAbsoluteUri(string request)
        {
            // Split on lines by separators
            string[] lines = request.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string preparedUri = "";
            foreach (string line in lines)
            {
                if (line.Contains("GET "))
                {
                    preparedUri = line.Substring(line.IndexOf("GET ") + 4);
                    preparedUri = preparedUri.Substring(0, preparedUri.IndexOf(" "));
                    break;
                }
            }

            return preparedUri;
        }

    }
}
