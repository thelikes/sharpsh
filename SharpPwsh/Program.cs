﻿using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

// need to add as reference:
// c:\windows\assembly\GAC_MSIL\System.Management.Automation\1.0.0.0__31bf3856ad364e35\System.Management.Automation.dll

namespace SharpPwsh
{
    class Program
    {
        static void Main(string[] args)
        {
            string cmd = "$ExecutionContext.SessionState.LanguageMode";

            Runspace rs = RunspaceFactory.CreateRunspace();
            rs.Open();

            // instantiate a PowerShell object
            PowerShell ps = PowerShell.Create();
            ps.Runspace = rs;

            while (true)
            {
                Console.Write("PS> ");
                cmd = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(cmd) || cmd == "exit" || cmd == "quit") break;
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
    }
}