using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace WallpaperEngine.Common {
    public class SingleInstanceManager : IDisposable {
        private readonly string _pipeName;
        private readonly Mutex _mutex;
        private NamedPipeServerStream _pipeServer;
        private bool _isFirstInstance;
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<string[]> ArgumentsReceived;

        public SingleInstanceManager(string uniqueAppId)
        {
            _pipeName = $"SingleInstance_{uniqueAppId}";
            _mutex = new Mutex(true, uniqueAppId, out _isFirstInstance);
        }

        public bool IsFirstInstance => _isFirstInstance;

        public void StartListening()
        {
            if (!_isFirstInstance)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ListenForConnections(_cancellationTokenSource.Token));
        }

        private async Task ListenForConnections(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    _pipeServer = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await _pipeServer.WaitForConnectionAsync(cancellationToken);

                    using (var reader = new StreamReader(_pipeServer, Encoding.UTF8)) {
                        string message = await reader.ReadToEndAsync();
                        if (!string.IsNullOrEmpty(message)) {
                            string[] args = message.Split('|');
                            ArgumentsReceived?.Invoke(this, args);
                        }
                    }

                    _pipeServer.Dispose();
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"管道监听错误: {ex.Message}");
                }
            }
        }

        public static bool SendArgsToFirstInstance(string uniqueAppId, string[] args)
        {
            try {
                using (var pipeClient = new NamedPipeClientStream(
                    ".",
                    $"SingleInstance_{uniqueAppId}",
                    PipeDirection.Out)) {
                    pipeClient.Connect(1000); // 1秒超时

                    string message = string.Join("|", args);
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    pipeClient.Write(buffer, 0, buffer.Length);
                    pipeClient.Flush();
                    return true;
                }
            } catch {
                return false;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _pipeServer?.Dispose();
            if (IsFirstInstance) {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }
    }
}
