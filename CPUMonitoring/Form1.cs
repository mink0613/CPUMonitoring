using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

// This is the code for your desktop app.
// Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.

namespace CPUMonitoring
{
    public partial class Form1 : Form
    {
        private System.Timers.Timer _checkThread;

        private DateTime _startTime;

        private PerformanceCounter cpu = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");

        private PerformanceCounter ram = new PerformanceCounter("Memory", "Available MBytes");

        private List<float> _totalCPUUsage = new List<float>();

        private int _durationMinute;

        private float _minCPU;

        private float _maxCPU;

        private float _avgCPU;

        private bool _isStart;

        private string _logFile;

        public Form1()
        {
            InitializeComponent();

            MaximizeBox = false;
            cpu.NextValue();
            Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitCPUStatistics();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string duration = txtDuration.Text;
            string interval = txtInterval.Text;

            StartCheckCPU(Convert.ToInt32(duration), Convert.ToInt32(interval));
        }

        private void StartCheckCPU(int duration, int interval)
        {
            StopThread();

            if (_isStart)
            {
                _isStart = !_isStart;
                return;
            }

            InitCPUStatistics();
            _isStart = !_isStart;

            _startTime = DateTime.Now;
            _durationMinute = duration;
            _logFile = "log_" + _startTime.ToString("yyyyMMddHHmmss") + ".txt";

            btnStart.BeginInvoke(new Action(() => btnStart.Text = "Stop"));

            _checkThread = new System.Timers.Timer();
            _checkThread.Interval = interval * 1000;
            _checkThread.Elapsed += CheckCPU;
            _checkThread.Start();
        }

        private double TimeDiffInMinute()
        {
            return DateTime.Now.Subtract(_startTime).TotalMinutes;
        }

        private void StopThread()
        {
            if (_checkThread != null)
            {
                _checkThread.Dispose();
                _checkThread = null;
            }

            _totalCPUUsage.Clear();
            btnStart.BeginInvoke(new Action(() => btnStart.Text = "Start"));
        }

        object syncWrite = new object();

        public bool WriteLog(string text)
        {
            StringBuilder sbMsg = new StringBuilder();
            try
            {
                sbMsg.Append(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff\t"));
                sbMsg.AppendFormat(text);

                lock (syncWrite)
                {
                    StreamWriter sw = new StreamWriter(_logFile, true, Encoding.Default);
                    sw.WriteLine(sbMsg.ToString());
                    sw.Close();
                }

                return true;
            }
            catch (Exception E)
            {
                return false;
            }
        }

        private void CheckCPU(object sender, System.Timers.ElapsedEventArgs e)
        {
            float cpuUsage = cpu.NextValue();

            Console.WriteLine(cpuUsage);
            WriteLog(cpuUsage.ToString());

            _totalCPUUsage.Add(cpuUsage);
            UpdateCPUStatistics(cpuUsage);

            if (TimeDiffInMinute() > _durationMinute)
            {
                StopThread();
            }
        }

        private void InitCPUStatistics()
        {
            _minCPU = float.MaxValue;
            _maxCPU = 0;
            _avgCPU = 0;

            lbCPUUtil.BeginInvoke(new Action(() => lbCPUUtil.Text = "0"));
            lbMin.BeginInvoke(new Action(() => lbMin.Text = "0"));
            lbMax.BeginInvoke(new Action(() => lbMax.Text = "0"));
            lbAvg.BeginInvoke(new Action(() => lbAvg.Text = "0"));
        }

        private void UpdateCPUStatistics(float cpuUsage)
        {
            lbCPUUtil.BeginInvoke(new Action(() => lbCPUUtil.Text = cpuUsage.ToString()));

            if (_minCPU > cpuUsage)
            {
                _minCPU = cpuUsage;
                lbMin.BeginInvoke(new Action(() => lbMin.Text = cpuUsage.ToString()));
            }

            if (_maxCPU < cpuUsage)
            {
                _maxCPU = cpuUsage;
                lbMax.BeginInvoke(new Action(() => lbMax.Text = cpuUsage.ToString()));
            }

            _avgCPU = 0;
            foreach (float cpu in _totalCPUUsage)
            {
                _avgCPU += cpu;
            }

            _avgCPU /= _totalCPUUsage.Count;
            lbAvg.BeginInvoke(new Action(() => lbAvg.Text = _avgCPU.ToString()));
        }
    }
}
