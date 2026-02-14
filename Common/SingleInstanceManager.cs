using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace WallpaperEngine.Common {
    /// <summary>
    /// 单实例管理器，使用 Mutex 确保应用程序只运行一个实例，并通过命名管道在实例间传递参数
    /// </summary>
    public class SingleInstanceManager : IDisposable {
        private readonly string _pipeName;
        private readonly Mutex _mutex;
        private NamedPipeServerStream _pipeServer;
        private bool _isFirstInstance;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 当从其他实例接收到启动参数时触发
        /// </summary>
        public event EventHandler<string[]> ArgumentsReceived;

        /// <summary>
        /// 初始化单实例管理器，创建 Mutex 并判断是否为首个实例
        /// </summary>
        /// <param name="uniqueAppId">应用程序唯一标识符</param>
        public SingleInstanceManager(string uniqueAppId)
        {
            _pipeName = $"SingleInstance_{uniqueAppId}";
            _mutex = new Mutex(true, uniqueAppId, out _isFirstInstance);
        }

        /// <summary>
        /// 当前实例是否为首个运行的实例
        /// </summary>
        public bool IsFirstInstance => _isFirstInstance;

        /// <summary>
        /// 开始监听来自其他实例的命名管道连接（仅首个实例有效）
        /// </summary>
        public void StartListening()
        {
            if (!_isFirstInstance)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ListenForConnections(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 持续监听命名管道连接，接收并解析来自其他实例的参数
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
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

        /// <summary>
        /// 通过命名管道将启动参数发送给首个运行的实例
        /// </summary>
        /// <param name="uniqueAppId">应用程序唯一标识符</param>
        /// <param name="args">要发送的命令行参数</param>
        /// <returns>发送成功返回 true，失败返回 false</returns>
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

        /// <summary>
        /// 释放 Mutex、管道及取消令牌等资源
        /// </summary>
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
