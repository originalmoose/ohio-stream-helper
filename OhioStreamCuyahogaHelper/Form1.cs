using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;

namespace OhioStreamCuyahogaHelper
{

    public partial class Form1 : Form
    {

        private string _ParcelListFile;

        private List<string> _FormattedParcelList;
        private List<string> _ParcelList;

        public Form1()
        {
            InitializeComponent();
            _ParcelListFile = Properties.Settings.Default.ParcelListFileLocation;

            dateTimePickerStart.Value = Properties.Settings.Default.StartDate;
            dateTimePickerEnd.Value = Properties.Settings.Default.EndDate;
            mskdTxtBxIntervalSeconds.Text = "" + Properties.Settings.Default.IntervalSeconds;

            if (_ParcelListFile != string.Empty)
            {
                txtBxParcelList.Text = _ParcelListFile;
                ParseParcelListCSV();
            }
        }

        public void UpdateProgress(int percentage, string parcelID, string message)
        {
            textBox1.Text = message;
            progressBar.Value = percentage;
            lblStatus.Text = "Checking " + parcelID + " from " + dateTimePickerStart.Value.ToShortDateString() + " to " + dateTimePickerEnd.Value.ToShortDateString() + "...";
        }

        public void Complete(string results)
        {
            progressBar.Value = progressBar.Maximum;
            lblStatus.Text = "Search Complete";
            textBox1.Text = results;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Filter = "Comma Separated (*.csv)|*.csv";

            dlg.Title = "Select CSV file containing Parcel Numbers";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _ParcelListFile = dlg.FileName;
            }
            if (_ParcelListFile == string.Empty)
            {
                return;
            }
            else
            {
                txtBxParcelList.Text = _ParcelListFile;
                ParseParcelListCSV();
            }
        }

        private void ParseParcelListCSV()
        {
            string line;
            string[] row;
            _FormattedParcelList = new List<string>();
            _ParcelList = new List<string>();
            if (File.Exists(_ParcelListFile))
            {
                using (StreamReader readFile = new StreamReader(_ParcelListFile))
                {
                    line = readFile.ReadLine();

                    row = line.Split(',');
                    foreach (string s in row)
                    {
                        string parcelID = s.Trim(); ;
                        string formatedParcelID = "";
                        string[] pracelParts = s.Split('-');
                        foreach (string t in pracelParts)
                        {
                            formatedParcelID += t;
                        }
                        _ParcelList.Add(parcelID);
                        _FormattedParcelList.Add(formatedParcelID.Trim());
                    }

                    lstBxParcelNumbers.DataSource = _ParcelList;
                }
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.ParcelListFileLocation = _ParcelListFile;
            Properties.Settings.Default.EndDate = dateTimePickerEnd.Value;
            Properties.Settings.Default.StartDate = dateTimePickerStart.Value;
            try
            {
                Properties.Settings.Default.IntervalSeconds = int.Parse(mskdTxtBxIntervalSeconds.Text);
            }
            catch
            {
                Properties.Settings.Default.IntervalSeconds = 60;
            }
            Properties.Settings.Default.Save();
        }

        private void Form1_Enter(object sender, EventArgs e)
        {
            if (_ParcelListFile != string.Empty)
            {
                ParseParcelListCSV();
            }


        }

        private void dateTimePickerEnd_ValueChanged(object sender, EventArgs e)
        {
            if (dateTimePickerEnd.Value < dateTimePickerStart.Value)
            {
                dateTimePickerEnd.Value = dateTimePickerStart.Value.AddDays(1);
            }
        }


        private void btnGo_Click(object sender, EventArgs e)
        {
            progressBar.Maximum = _ParcelList.Count;

            var provider = new CuyahogaRecorderProvider(_FormattedParcelList, this, dateTimePickerStart.Value, dateTimePickerEnd.Value,int.Parse(mskdTxtBxIntervalSeconds.Text));

            provider.StartWorker();

            progressBar.Visible = true;
            lblStatus.Visible = true;
            lblStatus.Text = "Checking " + _FormattedParcelList[0] + " from " + dateTimePickerStart.Value.ToShortDateString() + " to " + dateTimePickerEnd.Value.ToShortDateString() + "...";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }
}
