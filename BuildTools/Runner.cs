using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.IO.Compression;

namespace BuildTools
{

    /// <summary>
    /// General worker class
    /// </summary>
    public class Runner
    {
        /// <summary>
        /// Output directory for all files
        /// </summary>
        public static readonly string Dir = "BuildTools\\";
        /// <summary>
        /// GUI part of the program
        /// </summary>
        private readonly BuildTools _form;

        /// <summary>
        /// Final directory for the portable Git extract
        /// </summary>
        private const string GitDir = "BuildTools/PortableGit";

        /// <summary>
        /// Json response for the Jenkins api
        /// </summary>
        private JObject _api;
        /// <summary>
        /// Json response from my server
        /// </summary>
        private JObject _json;
        /// <summary>
        /// Keep track of current work here, so cleanup can remove them when needed
        /// </summary>
        private readonly HashSet<IDisposable> _disposables = new HashSet<IDisposable>();

        /// <summary>
        /// Whether the user had gpg signing enabled
        /// </summary>
        private bool isGpg;

        private readonly Job _job;

        /// <summary>
        /// Constructor for the worker class
        /// </summary>
        /// <param name="form">The GUI</param>
        /// <param name="job">Job Object</param>
        /// <param name="googleAnalytics">Google Analytics object</param>
        public Runner(BuildTools form, Job job)
        {
            _form = form;
            _job = job;
        }

        /// <summary>
        /// Checks if a BuildTools.jar exists, and if it exists, which version it is. It then checks the api on jenkins to see if
        /// there is a newer version available. If there is, or if a BuildTools.jar does not exist, this method will return true.
        /// Returns false if no update is needed.
        /// </summary>
        /// <returns>True if an update is needed.</returns>
        public bool CheckUpdate(out bool bad)
        {
            bad = false;
            if (!GetJson())
            {
                bad = true;
                return false;
            }
            if (File.Exists(Dir + (string)_json["buildTools"]["name"]))
            {
                int number = (int)_api["number"];
                bool success = GetBuildNumberFromJar(out int currentBuildNumber);

                if (success)
                {
                    bool update = currentBuildNumber < number;
                    if (update)
                        _form.AppendText("BuildTools is out of date");

                    return update;
                }
            }
            else
            {
                _form.AppendText("BuildTools does not exist");
            }
            return true;
        }

        /// <summary>
        /// Get the JSON response from my server and Jenkins and save them as JObject's.
        /// </summary>
        /// <returns>True if the JSON responses were parsed successfully.</returns>
        private bool GetJson()
        {
            if (_json == null)
            {
                _form.AppendText("Retrieving information from the server");

                _json = DownloadJson("http://uglylauncher.de/buildtools.json");

                if (_json == null)
                {
                    _form.AppendText("Error retrieving data, canceling");
                    return false;
                }
            }
            if (_api == null)
            {
                _api = DownloadJson((string)_json["buildTools"]["api"]);

                if (_api == null)
                {
                    _form.AppendText("Error retrieving data, canceling");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Given a URL, retrieve the JSON response from that URL and return it as a JObject
        /// </summary>
        /// <param name="url">URL to get JSON response from</param>
        /// <returns>JObject formed by the parsed JSON</returns>
        private JObject DownloadJson(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "BuildToolsGui";
                using (Stream stream = request.GetResponse().GetResponseStream())
                {
                    try
                    {
                        _disposables.Add(stream);
                        if (stream != null)
                        {
                            StreamReader reader = new StreamReader(stream);

                            string line;
                            string finalOutput = "";
                            while ((line = reader.ReadLine()) != null)
                            {
                                finalOutput += line + "\n";
                            }

                            return JObject.Parse(finalOutput);
                        }
                    }
                    catch (Exception)
                    {
                        _form.AppendText("There was an error while trying to receive data from the server");
                        return null;
                    }
                    finally
                    {
                        _disposables.Remove(stream);
                    }
                    return null;
                }
            }
            catch (Exception)
            {
                _form.AppendText("There was an error while trying to receive data from the server");
                return null;
            }
        }

        /// <summary>
        /// Check the build number of the BuildTools.jar. Set the given parameter to the build number of the BuildTools.jar.
        /// Returns whether the BuildTools.jar is valid. If this method returns false, the parameter means nothing (and will be 0).
        /// </summary>
        /// <param name="i">The build number of the jar</param>
        /// <returns>Whether the jar file is valid.</returns>
        private bool GetBuildNumberFromJar(out int i)
        {
            _form.AppendText("Checking downloaded BuildTools");
            try
            {
                using (ZipArchive zip = ZipFile.OpenRead(Dir + (string)_json["buildTools"]["name"]))
                {
                    ZipArchiveEntry entry = zip.GetEntry("META-INF/MANIFEST.MF");
                    using (StreamReader reader = new StreamReader(entry.Open()))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("Implementation-Version: "))
                            {
                                string version = line.Replace("Implementation-Version: ", "");
                                string[] split = version.Split('-');

                                // at least put some effort into making sure this is a BuildTools jar
                                if (!"git".Equals(split[0]) || !"BuildTools".Equals(split[1]))
                                {
                                    _form.AppendText("Jar is invalid");
                                    i = 0;
                                    return false;
                                }

                                i = int.Parse(split[3]);
                                return true;
                            }
                        }
                    }
                    i = 0;
                    return false;
                }
            }
            catch (Exception)
            {
                _form.AppendText("Jar is invalid");
                i = 0;
                return false;
            }
        }

        /// <summary>
        /// Download a new copy of the BuildTools jar
        /// </summary>
        /// <returns>True if the update was successful</returns>
        public bool UpdateJar()
        {
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }
            _form.AppendText("Checking for update");
            _form.ProgressShow();
            _form.ProgressIndeterminate();
            bool update = CheckUpdate(out bool bad);
            if (update)
            {
                _form.AppendText("Update needed for BuildTools");
                if (!GetJson())
                {
                    return false;
                }

                if (File.Exists(Dir + (string)_json["buildTools"]["name"]))
                {
                    _form.AppendText("Deleting current BuildTools");
                    File.Delete(Dir + (string)_json["buildTools"]["name"]);
                }

                string baseUrl = (string)_api["url"];
                string relativePath = (string)_api["artifacts"][0]["relativePath"];
                string fullUrl = baseUrl + "artifact/" + relativePath;

                _form.AppendText("Downloading BuildTools");
                if (!DownloadFile(fullUrl, Dir + (string)_json["buildTools"]["name"]))
                {
                    _form.AppendText("BuildTools failed to download");
                    return false;
                }
                _form.AppendText("Download complete");
            }
            else
            {
                if (!bad)
                {
                    _form.AppendText("BuildTools is up to date, no need to update");
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Goes through the process of checking the BuildTools environment and running it. If autoUpate is
        /// true, it will first call <see cref="UpdateJar()"/>.
        /// </summary>
        /// <param name="autoUpdate">If true, this will first call <see cref="UpdateJar()"/> Before continuing.</param>
        /// <param name="version">Version to build.</param>
        public void RunBuildTools(bool autoUpdate, string version)
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                _form.AppendText("BuildTools does not reliably work on 32 bit systems, if you have problems please re-run this on a 64 bit system.");
            }

            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }
            _form.ProgressShow();
            _form.ProgressIndeterminate();
            if (autoUpdate)
            {
                if (!UpdateJar())
                {
                    return;
                }
            }
            else
            {
                // We still need to grab data even if not updating
                if (!GetJson())
                {
                    return;
                }
            }

            // Java check
            if (!CheckJava(out bool javaInstalled))
            {
                string javaFile = Dir + (string)_json["java"]["name"];
                if (!javaInstalled)
                {
                    // Java is not installed
                    _form.AppendText("Downloading Java installer");

                    bool success = DownloadJava(javaFile);
                    if (!success)
                    {
                        _form.AppendText("Java could not be downloaded, canceling");
                    }

                    _form.ProgressIndeterminate();
                    success = InstallJava(javaFile);
                    if (!success)
                    {
                        _form.AppendText("Java could not be installed, canceling");
                    }

                    if (CheckJava())
                    {
                        _form.AppendText("Java installed successfully");
                    }
                    else
                    {
                        _form.AppendText("Java could not be installed, canceling");
                        return;
                    }
                }
                else
                {
                    // Java is installed
                    _form.AppendText("This is a 64 bit operating system, but the 32 bit JRE is installed. Downloading 64 bit JRE");
                    bool success = DownloadJava(javaFile);
                    if (!success)
                    {
                        _form.AppendText("Unable to download the 64 bit Java installer, will continue with the 32 bit JRE. This may cause the build to fail");
                    }
                    else
                    {
                        _form.AppendText("Uninstalling current 32 bit JRE");
                        success = UninstallJava();
                        if (!success)
                        {
                            _form.AppendText("There was an error while attempting to uninstall the 32 bit JRE");
                            CheckJava(out javaInstalled);
                            if (javaInstalled)
                            {
                                _form.AppendText("Java still seems to be installed, though. Will continue with the 32 bit JRE. This may cause the build to fail");
                            }
                            else
                            {
                                _form.AppendText("In spite of the error, it seems Java has been uninstalled. Will now install the 64 bit JRE");
                                success = InstallJava(javaFile);
                                if (!success)
                                {
                                    _form.AppendText("Java failed to install, canceling");
                                    return;
                                }
                                if (!FullJavaCheck())
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            _form.AppendText("Installing the 64 bit JRE");
                            success = InstallJava(javaFile);
                            if (!success)
                            {
                                _form.AppendText("Java failed to install, canceling");
                                return;
                            }
                            if (!FullJavaCheck())
                            {
                                return;
                            }
                        }
                    }

                }
            }

            // Git check
            if (!CheckGit())
            {
                string gitFile = Dir + "portable_git.7z.exe";
                _form.AppendText("Downloading portable Git");

                bool success;
                if (Environment.Is64BitOperatingSystem)
                {
                    success = DownloadFile((string)_json["git"]["64"], gitFile);
                }
                else
                {
                    success = DownloadFile((string)_json["git"]["32"], gitFile);
                }
                if (!success)
                {
                    _form.AppendText("Portable Git could not be downloaded, canceling");
                    return;
                }

                _form.ProgressIndeterminate();
                using (Process extractProcess = new Process())
                {
                    _form.AppendText("Extracting portable Git");
                    _disposables.Add(extractProcess);
                    try
                    {
                        extractProcess.StartInfo.FileName = gitFile;
                        extractProcess.StartInfo.UseShellExecute = true;
                        extractProcess.StartInfo.Arguments = "-gm1 -nr -y";
                        extractProcess.Start();
                        AddProcessToJob(extractProcess);
                        extractProcess.WaitForExit();
                        if (extractProcess.ExitCode != 0)
                            throw new Exception();
                    }
                    catch (Exception)
                    {
                        _form.AppendText("Portable Git could not be installed, canceling");
                        return;
                    }
                    finally
                    {
                        _disposables.Remove(extractProcess);
                    }
                }
                Thread.Sleep(100);
                File.Delete(gitFile);

                _form.AppendText("Checking portable Git installation (this may take a while)");
                if (CheckGit())
                {
                    _form.AppendText("Portable Git installed Successfully");
                }
                else
                {
                    _form.AppendText("Portable Git could not be installed, canceling");
                    return;
                }
            }

            // Check and fix commit.gpgpsign
            CheckGpg();
            if (isGpg)
            {
                _form.AppendText("Setting commit.gpgsign to false (needed for BuildTools, will reset once finished)");
                SetGpg(false);
            }

            _form.AppendRawText("");
            _form.AppendText("Running BuildTools.jar\n");

            // Run Build Tools
            using (Process buildProcess = new Process())
            {
                _disposables.Add(buildProcess);
                try
                {
                    buildProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    buildProcess.StartInfo.CreateNoWindow = true;
                    buildProcess.StartInfo.FileName = GitDir + "/bin/bash.exe";
                    buildProcess.StartInfo.UseShellExecute = false;
                    buildProcess.StartInfo.RedirectStandardOutput = true;
                    buildProcess.StartInfo.RedirectStandardError = true;
                    buildProcess.StartInfo.WorkingDirectory = Path.GetFullPath(Dir);
                    buildProcess.StartInfo.Arguments =
                        "--login -c \"git config --global --replace-all core.autocrlf true & java -jar " +
                        (string)_json["buildTools"]["name"] + " --rev " + version + "\"";
                    buildProcess.OutputDataReceived += (sender, args) => _form.AppendRawText(args.Data);
                    buildProcess.ErrorDataReceived += (sender, args) => _form.AppendRawText(args.Data);
                    buildProcess.Start();
                    AddProcessToJob(buildProcess);
                    buildProcess.BeginOutputReadLine();
                    buildProcess.BeginErrorReadLine();
                    buildProcess.WaitForExit();
                    if (buildProcess.ExitCode != 0)
                        throw new Exception();
                }
                catch (Exception)
                {
                    _form.AppendText("There was an error while running BuildTools");
                }
                finally
                {
                    _disposables.Remove(buildProcess);
                }
            }

            // Reset gpg if we disabled it
            if (isGpg)
            {
                _form.AppendText("Resetting commit.gpgsign");
                SetGpg(true);
            }
        }
        //******************//
        //** Java methods **//
        //******************//

        /// <summary>
        /// Download the program that will uninstall Java from this system and run it.
        /// We use a separate program for this because uninstalling requires the program to be
        /// run under an administrator, and we can't change the priviledge level of the current running process.
        /// </summary>
        /// <returns>True if Java was successfully uninstalled</returns>
        private bool UninstallJava()
        {
            string file = Dir + (string)_json["java"]["uninstaller"]["name"];
            // We can't elevate the priviledges of this process to uninstall java, so create a new process to do so
            bool success = DownloadFile((string)_json["java"]["uninstaller"]["url"], file);
            _form.ProgressIndeterminate();
            if (!success)
            {
                return false;
            }

            using (Process process = new Process())
            {
                _disposables.Add(process);
                try
                {
                    process.StartInfo.FileName = file;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    AddProcessToJob(process);
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception();
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    _disposables.Remove(process);
                }
            }
            Thread.Sleep(100);
            File.Delete(file);
            return true;
        }

        private bool CheckJava()
        {
            return CheckJava(out _);
        }

        /// <summary>
        /// Checks if Java is installed on this system, and if it's installed correctly. The only time it could be installed but not installed
        /// correctly is if this is run on a 64 bit machine, but the JRE installed is 32 bit.
        /// </summary>
        /// <param name="javaInstalled">Returns true if Java is installed at all, regardless of the system architecture.</param>
        /// <returns>True if Java is correctly installed</returns>
        private bool CheckJava(out bool javaInstalled)
        {
            string path = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "Path", "");
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);

            using (Process process = new Process())
            {
                _disposables.Add(process);
                try
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = "java";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.Arguments = "-version";
                    process.Start();
                    process.WaitForExit();

                    javaInstalled = true;

                    // Make sure a 64 bit JVM is installed on a 64 bit system
                    if (Environment.Is64BitOperatingSystem)
                    {
                        process.StartInfo.Arguments = "-d64 -version";
                        process.Start();
                        AddProcessToJob(process);
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    javaInstalled = false;
                    return false;
                }
                finally
                {
                    _disposables.Remove(process);
                }
            }
        }

        /// <summary>
        /// Given the output file, download Java using the data from my server.
        /// </summary>
        /// <param name="javaFile">The output file name to download to.</param>
        /// <returns>True if Java was downloaded correctly</returns>
        private bool DownloadJava(string javaFile)
        {
            bool success;
            if (Environment.Is64BitOperatingSystem)
            {
                success = DownloadFile((string)_json["java"]["64"], javaFile);
            }
            else
            {
                success = DownloadFile((string)_json["java"]["32"], javaFile);
            }

            return success;
        }

        /// <summary>
        /// Installs Java by running the given file. This will delete the given file after the
        /// installation has completed.
        /// </summary>
        /// <param name="javaFile">The file to run</param>
        /// <returns>True if Java was installed correctly</returns>
        private bool InstallJava(string javaFile)
        {
            using (Process installProcess = new Process())
            {
                _form.AppendText("Running Java installer");
                _disposables.Add(installProcess);
                try
                {
                    installProcess.StartInfo.FileName = javaFile;
                    installProcess.Start();
                    AddProcessToJob(installProcess);
                    installProcess.WaitForExit();

                    if (installProcess.ExitCode != 0)
                    {
                        throw new Exception();
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    _disposables.Remove(installProcess);
                }
            }
            Thread.Sleep(100);
            File.Delete(javaFile);
            return true;
        }

        /// <summary>
        /// Check if Java is installed. It will return true if Java is installed at all. It will only return false if Java is not installed.
        /// However, it will inform the user if Java is not installed correctly. This method is for one last check after every effort has
        /// been made to correct the Java issue.
        /// </summary>
        /// <returns>True if Java is not installed</returns>
        private bool FullJavaCheck()
        {
            bool correct = CheckJava(out bool javaInstalled);
            if (correct)
            {
                _form.AppendText("Java installed successfully");
                return true;
            }
            if (javaInstalled)
            {
                _form.AppendText("Java was installed, but it is still 32 bit for some reason.");
                _form.AppendText("Will continue with the 32 bit JRE. This may cause the build to fail");
                return true;
            }
            _form.AppendText("Java failed to install, canceling");
            return false;
        }

        /// <summary>
        /// Checks if portable Git is installed correctly.
        /// </summary>
        /// <returns>True if portable Git is installed correctly</returns>
        private bool CheckGit()
        {
            if (Directory.Exists(GitDir))
            {
                using (Process process = new Process())
                {
                    _disposables.Add(process);
                    try
                    {
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.FileName = GitDir + "/bin/bash.exe";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.Arguments = "\"--login\" \"-c\" \"exit\"";
                        process.Start();
                        AddProcessToJob(process);
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                            throw new Exception();

                        _disposables.Remove(process);
                        return true;
                    }
                    catch (Exception)
                    {
                        _disposables.Remove(process);
                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the user is using gpg signing and sets the flag accordingly
        /// </summary>
        private void CheckGpg()
        {
            using (Process process = new Process())
            {
                _disposables.Add(process);
                try
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = GitDir + "/bin/git.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.Arguments = "config --global commit.gpgsign";

                    Console.WriteLine("CHECKING GPG");
                    process.Start();
                    AddProcessToJob(process);
                    string stdout = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    Console.WriteLine(stdout);
                    if (stdout.Trim().ToLower() == "true")
                    {
                        isGpg = true;
                    }
                }
                catch (Exception) { }
                finally
                {
                    _disposables.Remove(process);
                }
            }
        }

        /// <summary>
        /// If isGpg flag is true, set commit.gpgpsign to the value given
        /// <param name="enable">The value to set commit.gpgsign to</param>
        /// </summary>
        private void SetGpg(bool enable)
        {
            using (Process process = new Process())
            {
                _disposables.Add(process);
                try
                {
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = GitDir + "/bin/git.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.Arguments = "config --global commit.gpgsign " + enable;
                    process.Start();
                    AddProcessToJob(process);
                    process.WaitForExit();
                }
                catch (Exception) { }
                finally
                {
                    _disposables.Remove(process);
                }
            }
        }

        /// <summary>
        /// Given a URL, download the file from that URL to a destination file. Returns whether the download
        /// was successful. This automatically adds the download to the list of disposables, and removes it
        /// when the download is completed.
        /// </summary>
        /// <param name="url">The URL to download the file from</param>
        /// <param name="dest">The relative or absolute path to the destination file to be saved</param>
        /// <returns>True if the download is successful</returns>
        private bool DownloadFile(string url, string dest)
        {
            using (WebClient client = new WebClient())
            {
                _disposables.Add(client);
                try
                {
                    client.Headers["User-Agent"] = "BuildToolsGui";
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        double bytesIn = e.BytesReceived;
                        double totalBytes = e.TotalBytesToReceive;
                        if (totalBytes < 0)
                        {
                            _form.ProgressIndeterminate();
                        }
                        else
                        {
                            _form.Progress((int)bytesIn, (int)totalBytes);
                        }
                    };

                    client.DownloadFileAsync(new Uri(url), dest);
                    // Make this thread wait for the download to finish
                    while (client.IsBusy)
                    {
                        Thread.Sleep(50);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    _disposables.Remove(client);
                }
            }
            return true;
        }

        /// <summary>
        /// If the application is closed, this will be run before exit. This will go through the list of any current
        /// work that may be running, and it will stop and dispose of them.
        /// </summary>
        public void CleanUp()
        {
            foreach (IDisposable disposable in _disposables)
            {
                if (disposable is Process process)
                {
                    process.Kill();
                }
                else if (disposable is WebClient client)
                {
                    client.CancelAsync();
                }
                disposable.Dispose();
            }
        }
        private void AddProcessToJob(Process process)
        {
            _job.AddProcess(process.Handle);
        }
    }
}