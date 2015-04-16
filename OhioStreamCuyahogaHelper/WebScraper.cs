using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web;

namespace OhioStreamCuyahogaHelper
{
    class WebScraper
    {
        /// <summary>
        /// Append a url parameter to a string builder, url-encodes the value
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected void AppendParameter(StringBuilder sb, string name, string value)
        {
            
            string encodedValue = HttpUtility.UrlEncode(value);
            sb.AppendFormat("{0}={1}&", name, encodedValue);
        }
        public string SendSearchRequest(StringBuilder sb, Dictionary<string,string> hidden, Dictionary<string,string> input, CookieCollection cookies)
        {
            if(sb == null)
                sb = new StringBuilder();

            if(hidden != null)
                foreach (KeyValuePair<string, string> pair in hidden)
                {
                    AppendParameter(sb, pair.Key, pair.Value);
                }

            if (input != null)
                foreach (KeyValuePair<string, string> pair in input)
                {
                    AppendParameter(sb, pair.Key, pair.Value);
                }
            

            byte[] byteArray = Encoding.UTF8.GetBytes(sb.ToString());

            string url = "http://recorder.cuyahogacounty.us/Searchs/ParcelSearchs.aspx"; 

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            foreach (Cookie c in cookies)
            {
                request.CookieContainer.Add(c);
            }

            
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:15.0) Gecko/20100101 Firefox/15.0";
            //request.AllowAutoRedirect = true;
            //request.Credentials = CredentialCache.DefaultNetworkCredentials; // ??

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(byteArray, 0, byteArray.Length);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // do something with response
            byte[] buff = new byte[4096];

            Stream stream = response.GetResponseStream();

            int count = 0;
            StringBuilder responseString = new StringBuilder();

            string parseString = null;
            do
            {
                count = stream.Read(buff, 0, buff.Length);

                if (count != 0)
                {
                    parseString = Encoding.ASCII.GetString(buff, 0, count);

                    responseString.Append(parseString);
                }
            } while (count > 0);


            return responseString.ToString();
        }

    }
}
