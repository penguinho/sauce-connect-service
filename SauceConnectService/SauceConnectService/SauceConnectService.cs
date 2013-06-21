using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Threading;

namespace SauceConnectService
{
    /// <summary>Service to start SauceConnect tunnel</summary>
    public partial class SauceConnectService : ServiceBase
    {
        /// <summary>Sauce Connect java process</summary>
        public Process SauceConnectProcess;
        /// <summary>Thread that polls to See If the tunnel is still running</summary>
        public Thread SauceConnectPollingThread;
        /// <summary>SauceLabs account ID to use for</summary>
        public string SauceConnectID;
        /// <summary>SauceLabs key corresponding to the account ID</summary>
        public string SauceConnectKey;
        /// <summary>id of the tunnel created by this service</summary>
        public string SauceConnectTunnelID;

        /// <summary>REST cleint to communicate with SauceLabs</summary>
        private WebClient _SauceRestClient;
        /// <summary>true if the service is already stopping</summary>
        private bool _IsStopping;
        /// <summary>event called when service is shutting down</summary>
        private ManualResetEvent _ShutDownEvent;

        /// <summary>contructor</summary>
        public SauceConnectService()
        {
            InitializeComponent();
            this._IsStopping = false;
            this._ShutDownEvent = new ManualResetEvent(false);
        }

        /// <summary>called when the service starts</summary>
        /// <param name="args">args</param>
        protected override void OnStart(string[] args)
        {
            // grab configuration from environment variables
            string javaHome = System.Environment.GetEnvironmentVariable("JAVA_HOME");
            string javaPath = Path.Combine(javaHome, "bin", "java.exe");
            string sauceArgs = System.Environment.GetEnvironmentVariable("SAUCE_CONNECT_ARGS");
            this.SauceConnectID = System.Environment.GetEnvironmentVariable("SAUCE_CONNECT_ID");
            this.SauceConnectKey = System.Environment.GetEnvironmentVariable("SAUCE_CONNECT_KEY");
            string sauceConnectPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Resources", "Sauce-Connect.jar");
            
            // create a rest client
            this._SauceRestClient = new WebClient();
            this._SauceRestClient.Credentials = new NetworkCredential(SauceConnectID, SauceConnectKey);
            
            // setup the java process
            this.SauceConnectProcess = new Process();
            this.SauceConnectProcess.StartInfo.FileName = javaPath;
            this.SauceConnectProcess.StartInfo.Arguments = "-jar \"" + sauceConnectPath + "\" " + (sauceArgs == null ? "" : sauceArgs + " ") + this.SauceConnectID + " " + this.SauceConnectKey;
            this.SauceConnectProcess.Exited += _ProcessExited;
            
            // grab existing tunnels
            var existingTunnels = this._GetTunnelIDs();

            // launch the java process
            this.SauceConnectProcess.Start();

            // wait until a new tunnel has launched
            DateTime startTime = DateTime.Now;
            while (DateTime.Now.Subtract(startTime).TotalMinutes < 2)
            {
                var tunnels = this._GetTunnelIDs();
                foreach(var tunnelID in tunnels)
                {
                    if (!existingTunnels.Contains(tunnelID))
                    {
                        // store tunnel id
                        this.SauceConnectTunnelID = tunnelID;
                        
                        // start polling thread
                        this.SauceConnectPollingThread = new Thread(this._PollForTunnel);
                        this.SauceConnectPollingThread.Priority = ThreadPriority.Normal;
                        this.SauceConnectPollingThread.Name = "Sauce Connect Polling Thread";
                        this.SauceConnectPollingThread.IsBackground = true;
                        this.SauceConnectPollingThread.Start();
                        return;
                    }
                }
            }

            // stop the service because it did not start
            this.Stop();
        }

        /// <summary>called when the service stops</summary>
        protected override void OnStop()
        {
            this._IsStopping = true;
            this._ShutDownEvent.Set();

            // kill the java process
            if (null != SauceConnectProcess && !SauceConnectProcess.HasExited)
            {
                this.SauceConnectProcess.Kill();
            }

            // kill the tunnel using the rest api
            if (_GetTunnelIDs().Contains(SauceConnectTunnelID))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://saucelabs.com/rest/v1/" + this.SauceConnectID + "/tunnels/" + this.SauceConnectTunnelID);
                request.Method = "DELETE";
                request.Accept = "application/json";
                request.ContentType = "application/json";
                request.Credentials = new NetworkCredential(SauceConnectID, SauceConnectKey);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (null != response)
                {
                    StreamReader sr = new StreamReader(response.GetResponseStream());
                    string responseText = sr.ReadToEnd().Trim();
                }
            }
        }

        /// <summary>called when the sauce connect java process terminates</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _ProcessExited(object sender, EventArgs e)
        {
            if (!this._IsStopping)
            {
                this.Stop();
            }
        }

        /// <summary>gets the ids of all Sauce Connect tunnels open for an account</summary>
        /// <returns>list of tunnelIDs</returns>
        private List<string> _GetTunnelIDs()
        {
            var result = this._SauceRestClient.DownloadString("https://saucelabs.com/rest/v1/" + this.SauceConnectID + "/tunnels");
            var resultArray = JArray.Parse(result);
            List<string> tunnelIDs = new List<string>();
            for (int i = 0; i < resultArray.Count; i++)
            {
                var item = (JValue)resultArray[i];
                tunnelIDs.Add((string)item);
            }
            return tunnelIDs;
        }

        /// <summary>polls to make sure the tunnel is still open</summary>
        private void _PollForTunnel()
        {
            int pollingInterval = 30000;

            // grab the polling interval from an environment variable
            try
            {
                var envPollingInterval = System.Environment.GetEnvironmentVariable("SAUCE_CONNECT_POLLING_INTERVAL");
                if (envPollingInterval != null)
                {
                    int.TryParse(envPollingInterval, out pollingInterval);
                }
            }
            catch { }

            // poll for exit
            while (!this._ShutDownEvent.WaitOne(0))
            {
                // check if tunnel is still open
                if (!this._GetTunnelIDs().Contains(this.SauceConnectTunnelID))
                {
                    // stop the service if the tunnel no longer exists
                    this.Stop();
                    return;
                }

                // sleep for polling interval
                for (int i = 0; i < pollingInterval / 1000; i++)
                {
                    if (this._ShutDownEvent.WaitOne(0))
                    {
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
