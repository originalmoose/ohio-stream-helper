using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Threading;

namespace OhioStreamCuyahogaHelper
{

    public class CuyahogaRecorderResult
    {
        public string ParcelID { get; set; }
        public int NumberOfResults { get; set; }
    }


    class CuyahogaRecorderProvider
    {
        private BackgroundWorker _BgWorker;

        private List<string> _ParcelNumbers;

        private Form1 _MainForm;

        private int _Interval;

        private DateTime _Start;
        private DateTime _End;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="parcelNumbers">a list of parcel numbers</param>
        /// <param name="mainForm"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="interval">time in seconds between requests</param>
        public CuyahogaRecorderProvider(List<string> parcelNumbers, Form1 mainForm, DateTime start, DateTime end, int interval)
        {
            _MainForm = mainForm;

            _ParcelNumbers = parcelNumbers;

            _Start = start;
            _End = end;

            _Interval = interval;
            
            _BgWorker = new BackgroundWorker();

            _BgWorker.WorkerReportsProgress = true;

            _BgWorker.DoWork += new DoWorkEventHandler(_BgWorker_DoWork);

            _BgWorker.ProgressChanged += new ProgressChangedEventHandler(_BgWorker_ProgressChanged);

            _BgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgWorkerRunWorkerCompleted);
        }

        void BgWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string result = (string)e.Result;
            _MainForm.Complete(result);
        }

        public void StartWorker()
        {
            _BgWorker.RunWorkerAsync(_ParcelNumbers);
        }

        void _BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if(e.ProgressPercentage + 1 != _ParcelNumbers.Count)
                _MainForm.UpdateProgress(e.ProgressPercentage, _ParcelNumbers[e.ProgressPercentage + 1], (string)e.UserState);
            else
                _MainForm.UpdateProgress(e.ProgressPercentage, _ParcelNumbers[e.ProgressPercentage], (string)e.UserState);
        }

        void _BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //get our data to work with
            List<string> _parcelList = (List<string>)e.Argument;


            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Properties.Settings.Default.CuyahogaSearchWebAddress);
            request.CookieContainer = new CookieContainer();
            request.Method = "GET";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:15.0) Gecko/20100101 Firefox/15.0";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            byte[] buff = new byte[response.ContentLength];

            Stream stream = response.GetResponseStream();

            int read;
            StringBuilder responseString = new StringBuilder();
            while ((read = stream.Read(buff, 0, buff.Length)) > 0)
            {
                responseString.Append(ASCIIEncoding.ASCII.GetString(buff));
            }

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();

            doc.LoadHtml(responseString.ToString());

            Dictionary<string,string> _HiddenFields = new Dictionary<string, string>();
            Dictionary<string, string> _InputFields = new Dictionary<string, string>();


            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//input[@type='hidden']"))
            {
                _HiddenFields.Add(link.Attributes["id"].Value, link.Attributes["value"].Value);
            }

            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//input"))
            {
                if (!_HiddenFields.ContainsKey(link.Attributes["id"].Value))
                {
                    _InputFields.Add(link.Attributes["id"].Value, "");
                }
            }

            CookieCollection _Cookies = response.Cookies;


            _InputFields["txtRecStart"] = _Start.ToShortDateString();
            _InputFields["txtRecEnd"] = _End.ToShortDateString();
            _InputFields.Add("1stQuery", "1");
            _InputFields["ValidateButton"] = "Begin Search";

            StringBuilder sb = new StringBuilder();
            int i = 0;

            foreach (string parcelID in _parcelList)
            {
                _InputFields["ParcelID"] = parcelID;

                WebScraper scraper = new WebScraper();

                HtmlAgilityPack.HtmlDocument resultDoc = new HtmlAgilityPack.HtmlDocument();

                string s = scraper.SendSearchRequest(null, _HiddenFields, _InputFields, _Cookies);
                resultDoc.LoadHtml(s);

                bool resultsFound = false;
                int numResults = 0;
                bool error = false;
                try
                {
                    foreach (HtmlNode line in resultDoc.DocumentNode.SelectNodes("//span[@id='ctl00_ContentPlaceHolder1_lblSummary']"))
                    {
                        Regex reg = new Regex(@"<b>([A-Za-z0-9]+)</b>");
                        //Match match = reg.Match(line.InnerHtml);
                        foreach (Match m in reg.Matches(line.InnerHtml))
                        {
                            if (m.Groups[1].Value.Length == 1)
                            {
                                if (int.Parse(m.Groups[1].Value) > 0)
                                {
                                    resultsFound = true;
                                    numResults = int.Parse(m.Groups[1].Value);
                                }
                            }
                        }
                        // sb.AppendLine(Regex.Replace(line.InnerHtml, @"<[^>]*>", String.Empty));
                    }
                }
                catch
                {
                    sb.Append("Parcel ");
                    sb.Append(parcelID);
                    sb.Append(" - ");
                    sb.Append(numResults);
                    sb.Append(" Error - Web site Access Denied or Website down.");
                    sb.AppendLine();
                    _BgWorker.ReportProgress(i, sb.ToString());
                    error = true;
                    resultsFound = false;
                }



                if (resultsFound)
                {
                    sb.Append("Parcel ");
                    sb.Append(parcelID);
                    sb.Append(" - ");
                    sb.Append(numResults);
                    sb.Append(" result(s) found.");
                    sb.AppendLine();
                }
                if(!error)
                    _BgWorker.ReportProgress(i, sb.ToString());
                
                ++i;
                Thread.Sleep(_Interval * 1000);
            }
            e.Result = "Scan Complete";// sb.ToString();
        }
    }
}
