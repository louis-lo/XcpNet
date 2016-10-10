using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.ServiceProcess;

namespace MoneyClient
{
    public static class SvcController
    {
        private sealed class ServiceProcess : IDisposable
        {
            private Process _process;
            private StringBuilder _output;
            private bool disposed;

            public ServiceProcess(string filename, string arguments)
            {
                disposed = false;
                _output = new StringBuilder();
                _process = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        Arguments = arguments,
                        CreateNoWindow = true,
                        FileName = filename,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                _process.OutputDataReceived += OutputDataReceived;
            }

            public string Output
            {
                get { return _output.ToString(); }
            }

            public bool Run()
            {
                try
                {
                    if (_process.Start())
                    {
                        _process.BeginOutputReadLine();
                        _process.WaitForExit();
                        return true;
                    }
                }
                catch (Exception) { }
                return false;
            }

            private void OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                _output.AppendLine(e.Data);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            private void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        if (_process != null)
                        {
                            _process.Close();
                            _process = null;
                        }
                    }
                    disposed = true;
                }
            }
            ~ServiceProcess()
            {
                Dispose(false);
            }
        }

        private static string GetDotNetFrameWorkPath()
        {
            return Path.Combine(Environment.GetEnvironmentVariable("windir"), "Microsoft.Net", "Framework", string.Concat('v', Environment.Version.ToString(3)));
        }

        private static ServiceController Get(string name)
        {
            ServiceController ret = null;
            ServiceController[] svcs = ServiceController.GetServices();
            foreach (ServiceController svc in svcs)
            {
                if (svc.ServiceName.Equals(name))
                    ret = svc;
                else
                    svc.Dispose();
            }
            return ret;
        }
        public static bool Exists(string name)
        {
            ServiceController svc = Get(name);
            if (svc != null)
            {
                svc.Dispose();
                svc = null;
                return true;
            }
            return false;
        }
        public static bool Start(string name)
        {
            ServiceController svc = Get(name);
            if (svc != null)
            {
                try
                {
                    svc.Refresh();
                    if (svc.Status == ServiceControllerStatus.StopPending)
                    {
                        svc.WaitForStatus(ServiceControllerStatus.Stopped);
                        svc.Start();
                    }
                    else if (svc.Status == ServiceControllerStatus.PausePending)
                    {
                        svc.WaitForStatus(ServiceControllerStatus.Paused);
                        svc.Continue();
                    }
                    svc.WaitForStatus(ServiceControllerStatus.Running);
                    svc.Refresh();
                    return svc.Status == ServiceControllerStatus.Running;
                }
                catch (Exception) { }
                finally
                {
                    svc.Dispose();
                    svc = null;
                }
            }
            return false;
        }
        public static bool Stop(string name)
        {
            ServiceController svc = Get(name);
            if (svc != null)
            {
                try
                {
                    svc.Refresh();
                    if (svc.Status == ServiceControllerStatus.StartPending || svc.Status == ServiceControllerStatus.ContinuePending)
                    {
                        svc.WaitForStatus(ServiceControllerStatus.Running);
                        svc.Stop();
                    }
                    else if (svc.Status == ServiceControllerStatus.PausePending)
                    {
                        svc.WaitForStatus(ServiceControllerStatus.Paused);
                        svc.Stop();
                    }
                    svc.WaitForStatus(ServiceControllerStatus.Stopped);
                    svc.Refresh();
                    return svc.Status == ServiceControllerStatus.Stopped;
                }
                catch (Exception) { }
                finally
                {
                    svc.Dispose();
                    svc = null;
                }
            }
            return false;
        }
        public static string Install(string filename)
        {
            using (ServiceProcess p = new ServiceProcess(Path.Combine(GetDotNetFrameWorkPath(), "InstallUtil.exe"), string.Concat('"', filename, '"')))
            {
                if (p.Run())
                    return p.Output;
            }
            return null;
        }
        public static string Uninstall(string filename)
        {
            using (ServiceProcess p = new ServiceProcess(Path.Combine(GetDotNetFrameWorkPath(), "InstallUtil.exe"), string.Concat("/u \"", filename, '"')))
            {
                if (p.Run())
                    return p.Output;
            }
            return null;
        }
    }
}
