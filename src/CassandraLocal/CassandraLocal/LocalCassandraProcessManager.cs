using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;

namespace SkbKontur.Cassandra.Local
{
    public static class LocalCassandraProcessManager
    {
        private const string localCassandraNodeNameMarker = "skbkontur.local.cassandra.node.name";
        private static readonly Regex anyCassandraProcessRegex = new Regex(@"org\.apache\.cassandra\.service\.CassandraDaemon", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly TimeSpan defaultWaitTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan sleepTimeout = TimeSpan.FromMilliseconds(300);

        public static string StartLocalCassandraProcess(string cassandraDirectory, TimeSpan? timeout = null)
        {
            var cassandraShellProcess = new Process
                {
                    StartInfo =
                        {
                            FileName = Path.Combine(cassandraDirectory, @"bin/cassandra.bat"),
                            WorkingDirectory = Path.Combine(cassandraDirectory, @"bin"),
                            Arguments = "LEGACY",
                            UseShellExecute = false,
                            CreateNoWindow = false,
                            WindowStyle = ProcessWindowStyle.Normal,
                        }
                };
            cassandraShellProcess.StartInfo.EnvironmentVariables["JAVA_HOME"] = JavaHomeHelpers.GetJava8Home();
            cassandraShellProcess.Start();
            WaitForCassandraToStart(cassandraDirectory, timeout);
            var localNodeName = GetLocalCassandraNodeName(cassandraShellProcess);
            return localNodeName;
        }

        public static void WaitForCassandraToStart(string cassandraDirectory, TimeSpan? timeout = null)
        {
            var waitTimeout = timeout ?? defaultWaitTimeout;
            var logFileName = Path.Combine(cassandraDirectory, @"logs/system.log");
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < waitTimeout)
            {
                if (File.Exists(logFileName))
                {
                    using (var file = new FileStream(logFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(file))
                    {
                        while (sw.Elapsed < waitTimeout)
                        {
                            var logContent = reader.ReadLine();
                            if (!string.IsNullOrEmpty(logContent) && logContent.Contains("Listening for thrift clients..."))
                                return;
                        }
                    }
                }
                Thread.Sleep(sleepTimeout);
            }
            throw new InvalidOperationException($"Failed to start cassandra from {cassandraDirectory} in {waitTimeout}");
        }

        public static void StopAllLocalCassandraProcesses(TimeSpan? timeout = null)
        {
            foreach (var cassandraPid in GetAllLocalCassandraProcessIds())
                Process.GetProcessById(cassandraPid).Kill();
            WaitFor("stop all local cassandra processes", timeout, () => !GetAllLocalCassandraProcessIds().Any());
        }

        public static void StopLocalCassandraProcess(string localNodeName, TimeSpan? timeout = null)
        {
            foreach (var cassandraPid in GetLocalCassandraProcessIds(localNodeName))
                Process.GetProcessById(cassandraPid).Kill();
            WaitFor($"stop local cassandra node {localNodeName}", timeout, () => !GetLocalCassandraProcessIds(localNodeName).Any());
        }

        private static void WaitFor(string actionDescription, TimeSpan? timeout, Func<bool> action)
        {
            var waitTimeout = timeout ?? defaultWaitTimeout;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < waitTimeout)
            {
                if (action())
                    return;
                Thread.Sleep(sleepTimeout);
            }
            throw new InvalidOperationException($"Failed to {actionDescription} in {waitTimeout}");
        }

        public static List<int> GetAllLocalCassandraProcessIds()
        {
            return DoGetLocalCassandraProcessIds(localNodeNameOrNothing: null);
        }

        public static List<int> GetLocalCassandraProcessIds(string localNodeName)
        {
            if (string.IsNullOrEmpty(localNodeName))
                throw new InvalidOperationException($"{nameof(localNodeName)} is empty");
            return DoGetLocalCassandraProcessIds(localNodeName);
        }

        private static List<int> DoGetLocalCassandraProcessIds(string localNodeNameOrNothing)
        {
            var cassandraPids = new List<int>();
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process"))
            {
                foreach (var o in searcher.Get())
                {
                    var mo = (ManagementObject)o;
                    if (mo["CommandLine"] == null)
                        continue;
                    var commandLine = mo["CommandLine"].ToString();
                    var cassandraPid = int.Parse(mo["ProcessId"].ToString());
                    if (string.IsNullOrEmpty(localNodeNameOrNothing))
                    {
                        if (anyCassandraProcessRegex.IsMatch(commandLine))
                            cassandraPids.Add(cassandraPid);
                    }
                    else
                    {
                        if (commandLine.Contains($"{localCassandraNodeNameMarker}={localNodeNameOrNothing}"))
                            cassandraPids.Add(cassandraPid);
                    }
                }
            }
            return cassandraPids;
        }

        private static string GetLocalCassandraNodeName(Process cassandraShellProcess)
        {
            using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ParentProcessId = {cassandraShellProcess.Id}"))
            {
                var commandLines = searcher.Get().Cast<ManagementObject>().Select(x => x["CommandLine"].ToString()).ToList();
                var javaCommandLine = commandLines.SingleOrDefault(x => x.IndexOf("java", StringComparison.InvariantCultureIgnoreCase) != -1);
                if (string.IsNullOrEmpty(javaCommandLine))
                    throw new InvalidOperationException($"Failed to get java command line from: {string.Join("; ", commandLines)}");
                var patternToMatch = $"{localCassandraNodeNameMarker}=";
                var startPos = javaCommandLine.IndexOf(patternToMatch, StringComparison.InvariantCultureIgnoreCase) + patternToMatch.Length;
                var endPos = javaCommandLine.IndexOf(' ', startPos);
                return javaCommandLine.Substring(startPos, endPos - startPos);
            }
        }
    }
}