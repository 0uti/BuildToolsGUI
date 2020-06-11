using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace BuildTools {
    using System.Runtime.InteropServices;

    public partial class BuildTools : Form {
        // Delegates
        private delegate void AppendTextDelegate(string text);
        private readonly AppendTextDelegate _appendDelegate;
        private delegate void AppendRawTextDelegat(string text);
        private readonly AppendRawTextDelegat _appendRawDelegate;
        private delegate void DisableDelegate();
        private readonly DisableDelegate _disableDelegate;
        private delegate void EnableDelegate();
        private readonly EnableDelegate _enableDelegate;
        private delegate void ShowProgressDelegate();
        private readonly ShowProgressDelegate _showProgressDelegate;
        private delegate void HideProgressDelegate();
        private readonly HideProgressDelegate _hideProgressDelegate;
        private delegate void IndeterminateProgressDelegate();
        private readonly IndeterminateProgressDelegate _indeterminateProgressDelegate;
        private delegate void ProgressPercentDelegate(int place, int total);
        private readonly ProgressPercentDelegate _progressPercentDelegate;
        private delegate void UpdateVersionsDelegate();
        private readonly UpdateVersionsDelegate _updateVersionsDelegate;

        // Stuff
        private readonly Runner _runner;
        private string _lastLog = "";
        private readonly List<string> _versions = new List<string>();
        private readonly Job _job;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Constructor for the form
        /// </summary>
        public BuildTools(Guid guid, Job job) {
            InitializeComponent();
            _job = job;
            _runner = new Runner(this, _job);
            undoBT.Visible = false;
            progress.Visible = false;

            versionBox.SelectedIndex = 0;
            GetVersions();

            // delegates
            _appendDelegate = AppendText;
            _appendRawDelegate = AppendRawText;
            _disableDelegate = Disable;
            _enableDelegate = Enable;
            _showProgressDelegate = ProgressShow;
            _hideProgressDelegate = ProgressHide;
            _indeterminateProgressDelegate = ProgressIndeterminate;
            _progressPercentDelegate = Progress;
			_updateVersionsDelegate = UpdateVersions;

            if (File.Exists(Program.CheckUpdate)) {
                string text = File.ReadAllText(Program.CheckUpdate);
                if (string.IsNullOrEmpty(text) || text.Trim().ToLower() == "false") {
                    autoUpdateCB.Checked = false;
                }
            }

            Console.WriteLine(guid.ToString());
        }

        // Run BuildTools Button Clicked
        private void RunBT_Click(object sender, EventArgs e) {
            bool update = autoUpdateCB.Checked;
            string version;
            if (versionBox.SelectedIndex == 0) {
                version = "latest";
            } else {
                version = _versions[versionBox.SelectedIndex - 1];
            }
            
            Thread thread = new Thread(delegate() {
                _runner.RunBuildTools(update, version);
                Enable();
                ProgressHide();
            });
            Disable();
            thread.Start();
        }

        // Update BuildTools Button Clicked
        private void UpdateBT_Click(object sender, EventArgs e) {

            Thread thread = new Thread(delegate() {
                _runner.UpdateJar();
                Enable();
                ProgressHide();
            });
            Disable();
            thread.Start();
        }

        // Clear Log Button Clicked
        private void ClearBT_Click(object sender, EventArgs e) {
            _lastLog += outputTB.Text;
            outputTB.Text = "";
            undoBT.Visible = true;
        }

        // Undo Button Clicked
        private void UndoBT_Click(object sender, EventArgs e) {
            outputTB.Text = _lastLog + outputTB.Text;
            _lastLog = "";
            undoBT.Visible = false;
        }

        /// <summary>
        /// Append text to the output textbox. This method will ensure thread safety on the AppendText method call.
        /// This will also prefix a "--- " before the given text and automatically append the given text with a new-line character.
        /// </summary>
        /// <param name="text">Text to append to the output textbox.</param>
        public void AppendText(string text) {
            if (InvokeRequired) {
                Invoke(_appendDelegate, text);
            } else {
                outputTB.AppendText("--- " + text + "\n");
                outputTB.ScrollToCaret();
            }
        }

        /// <summary>
        /// Send one line of text to the output textbox, with no "---" prefix. A newline will be added to the end of the line.
        /// </summary>
        /// <param name="text">Text to append to the output textbox.</param>
        public void AppendRawText(string text) {
            if (InvokeRequired) {
                Invoke(_appendRawDelegate, text);
            } else {
                outputTB.AppendText(text + "\n");
                outputTB.ScrollToCaret();
            }
        }

        /// <summary>
        /// Disable the run buttons so they can't be pressed while work is being done.
        /// </summary>
        private void Disable() {
            if (InvokeRequired) {
                Invoke(_disableDelegate);
            } else {
                updateBT.Enabled = false;
                runBT.Enabled = false;
                versionBox.Enabled = false;
            }
        }

        /// <summary>
        /// Enable the run buttons so they will work after work has completed
        /// </summary>
        private void Enable() {
            if (InvokeRequired) {
                Invoke(_enableDelegate);
            } else {
                updateBT.Enabled = true;
                runBT.Enabled = true;
                versionBox.Enabled = true;
            }
        }

        /// <summary>
        /// Show the progress bar.
        /// This needs to be done before other calls are made to it.
        /// </summary>
        public void ProgressShow() {
            if (InvokeRequired) {
                Invoke(_showProgressDelegate);
            } else {
                progress.Visible = true;
            }
        }

        /// <summary>
        /// Hide the progress bar so it won't be visible
        /// </summary>
        public void ProgressHide() {
            if (InvokeRequired) {
                Invoke(_hideProgressDelegate);
            } else {
                progress.Visible = false;
            }
        }

        /// <summary>
        /// Set the progress bar's state to indeterminate.
        /// </summary>
        public void ProgressIndeterminate() {
            if (InvokeRequired) {
                Invoke(_indeterminateProgressDelegate);
            } else {
                progress.Style = ProgressBarStyle.Marquee;
            }
        }

        /// <summary>
        /// Set the progress bar to the specified place.
        /// </summary>
        /// <param name="place">Current progress to this point</param>
        /// <param name="total">Total progress to completion</param>
        public void Progress(int place, int total) {
            if (InvokeRequired) {
                Invoke(_progressPercentDelegate, place, total);
            } else {
                progress.Style = ProgressBarStyle.Continuous;
                progress.Minimum = 0;
                progress.Maximum = total;
                progress.Value = place;
            }
        }

        // Exit button pressed
        private void BuildTools_FormClosed(object sender, FormClosedEventArgs e) {
            _runner.CleanUp();
            _job.Dispose();
            Application.Exit();
            Environment.Exit(0);
        }

        // Source link clicked
        private void LinkLabel_Click(object sender, LinkLabelLinkClickedEventArgs e) {
            System.Diagnostics.Process.Start("https://github.com/0uti/BuildToolsGUI");
        }

        private void GetVersions() {
            new Thread(delegate () {
                string versionsHtml = GetHtml("https://hub.spigotmc.org/versions/");
                if (string.IsNullOrEmpty(versionsHtml))
                {
                    return;
                }

                // Convert the HTML to XHTML to use an XML parser
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(versionsHtml);
                if (doc.ParseErrors != null) {
                    while (doc.ParseErrors.GetEnumerator().MoveNext()) {
                        Console.Write(doc.ParseErrors.GetEnumerator().Current);
                    }
                }
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a");
                foreach (HtmlNode node in nodes) {
                    if (node.InnerText.EndsWith(".json") && node.InnerText.StartsWith("1.")) {
                        _versions.Add(node.InnerText.Remove(node.InnerText.Length - 5));
                    }
                }

                UpdateVersions();
            }).Start();
        }

        private static string GetHtml(string url) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            try {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
                if (response.StatusCode == HttpStatusCode.OK) {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream = null;

                    if (response.CharacterSet == null) {
                        if (receiveStream != null) {
                            readStream = new StreamReader(receiveStream);
                        }
                    } else {
                        if (receiveStream != null) {
                            readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                        }
                    }

                    string data = readStream.ReadToEnd();

                    response.Close();
                    readStream.Close();

                    return data;
            }
            } catch (Exception) {

            }
            return "";
        }

        private void UpdateVersions() {
            if (InvokeRequired) {
                Invoke(_updateVersionsDelegate);
            } else {
                ComboBox.ObjectCollection items = versionBox.Items;
                items.Clear();
                items.Add("Latest");
                _versions.Sort();
                _versions.Reverse();
                foreach (string s in _versions) {
                    items.Add(s);
                }
                versionBox.SelectedIndex = 0;
            }
        }

        private void AutoUpdateCB_Click(object sender, EventArgs e) {
            if (autoUpdateCB.Checked) {
                File.WriteAllText(Program.CheckUpdate, "true");
            } else {
                File.WriteAllText(Program.CheckUpdate, "false");
            }
        }
    }
}
