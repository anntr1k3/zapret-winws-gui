using System.Diagnostics;

namespace ZapretWinwsGui.Core.Process;

public class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new System.Diagnostics.Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessRunResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask
        );
    }

    public IActiveProcess Start(string fileName, string arguments, string workingDirectory = "")
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? AppContext.BaseDirectory : workingDirectory,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = new System.Diagnostics.Process { StartInfo = psi, EnableRaisingEvents = true };
        process.Start();

        return new SystemActiveProcess(process);
    }

    public bool IsProcessRunning(string processName)
    {
        var cleanName = Path.GetFileNameWithoutExtension(processName);
        var processes = System.Diagnostics.Process.GetProcessesByName(cleanName);
        return processes.Length > 0;
    }

    public void KillProcessesByName(string processName)
    {
        var cleanName = Path.GetFileNameWithoutExtension(processName);
        var processes = System.Diagnostics.Process.GetProcessesByName(cleanName);
        foreach (var p in processes)
        {
            try
            {
                p.Kill(entireProcessTree: true);
                p.WaitForExit(1000);
            }
            catch
            {
                // Ignore processes that cannot be killed or already terminated
            }
        }
    }

    private sealed class SystemActiveProcess : IActiveProcess
    {
        private readonly System.Diagnostics.Process _process;

        public int Id => _process.Id;
        public bool HasExited => _process.HasExited;

        public event Action<string>? OutputDataReceived;
        public event Action<string>? ErrorDataReceived;
        public event Action<int>? Exited;

        public SystemActiveProcess(System.Diagnostics.Process process)
        {
            _process = process;

            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    OutputDataReceived?.Invoke(e.Data);
            };

            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    ErrorDataReceived?.Invoke(e.Data);
            };

            _process.Exited += (_, _) =>
            {
                Exited?.Invoke(_process.ExitCode);
            };

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Kill()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Ignore if already dead
            }
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
