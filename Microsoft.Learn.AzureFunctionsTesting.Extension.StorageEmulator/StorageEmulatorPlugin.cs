using Microsoft.Learn.AzureFunctionsTesting.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.StorageEmulator
{
    public class StorageEmulatorPlugin : IFunctionTestPlugin
    {
        private const int BlobPort = 10000;
        private const int StartupTimeoutSeconds = 30;
        private const int StartupPollingIntervalMs = 500;

        internal const string DefaultName = "STORAGE_EMULATOR_DEFAULT_NAME";
        internal const string ConnectionString = "UseDevelopmentStorage=true";

        private Process? azuriteProcess;
        private string? workingDirectory;
        private readonly bool autoStart;

        public StorageEmulatorPlugin(string name, bool autoStart)
        {
            Name = name;
            this.autoStart = autoStart;
        }

        public string Name { get; }

        public async Task InitializeAsync(Dictionary<string, string> environmentVars)
        {
            if (!autoStart)
                return;

            if (IsPortInUse(BlobPort))
                return;

            var azuritePath = FindAzuritePath()
                ?? throw new Exception("Azurite is not installed or could not be found. Install it globally with 'npm install -g azurite', or set the AzuriteExePath environment variable.");

            workingDirectory = Path.Combine(Path.GetTempPath(), $"azurite-{Guid.NewGuid()}");
            Directory.CreateDirectory(workingDirectory);

            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            if (azuritePath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
            {
                processInfo.FileName = "cmd.exe";
                processInfo.Arguments = $"/C \"{azuritePath}\" --location \"{workingDirectory}\" --silent";
            }
            else
            {
                processInfo.FileName = azuritePath;
                processInfo.Arguments = $"--location \"{workingDirectory}\" --silent";
            }

            azuriteProcess = Process.Start(processInfo)
                ?? throw new Exception("Failed to start Azurite process.");

            var waitUntil = DateTime.UtcNow.AddSeconds(StartupTimeoutSeconds);
            while (!IsPortInUse(BlobPort) && DateTime.UtcNow < waitUntil)
            {
                await Task.Delay(StartupPollingIntervalMs);
            }

            if (!IsPortInUse(BlobPort))
            {
                throw new Exception($"Azurite did not start within {StartupTimeoutSeconds} seconds. Verify that Azurite is properly installed.");
            }
        }

        public Task DisposeAsync()
        {
            if (azuriteProcess != null)
            {
                if (!azuriteProcess.HasExited)
                {
                    azuriteProcess.Kill(entireProcessTree: true);
                    azuriteProcess.WaitForExit();
                }
                azuriteProcess.Dispose();
                azuriteProcess = null;
            }

            if (workingDirectory != null && Directory.Exists(workingDirectory))
            {
                try
                {
                    Directory.Delete(workingDirectory, recursive: true);
                }
                catch (IOException)
                {
                    // Best-effort cleanup; the OS will reclaim temp files eventually
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort cleanup; the OS will reclaim temp files eventually
                }
            }

            return Task.CompletedTask;
        }

        private static bool IsPortInUse(int port)
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipGlobalProperties.GetActiveTcpListeners();
            return listeners.Any(endpoint => endpoint.Port == port);
        }

        private static string? FindAzuritePath()
        {
            var envPath = Environment.GetEnvironmentVariable("AzuriteExePath");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return envPath;

            var pathsToTry = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pathsToTry.Add(Environment.ExpandEnvironmentVariables("%APPDATA%\\npm\\azurite.cmd"));
                pathsToTry.Add("C:\\npm\\prefix\\azurite.cmd");
            }
            else
            {
                pathsToTry.Add("/usr/local/bin/azurite");
                pathsToTry.Add("/opt/homebrew/bin/azurite");
                pathsToTry.Add("/usr/bin/azurite");
            }

            return pathsToTry.FirstOrDefault(File.Exists);
        }
    }
}
