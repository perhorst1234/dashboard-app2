using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MixerPad.Services
{
    public class SerialLineEventArgs : EventArgs
    {
        public string RawLine { get; }
        public SerialLineEventArgs(string rawLine) => RawLine = rawLine;
    }

    public class SerialConnectionEventArgs : EventArgs
    {
        public bool Connected { get; }
        public SerialConnectionEventArgs(bool connected) => Connected = connected;
    }

    public class SerialService : IDisposable
    {
        private SerialPort? _port;
        private readonly object _sync = new();
        private CancellationTokenSource? _cts;
        private readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(2);

        public event EventHandler<SerialLineEventArgs>? LineReceived;
        public event EventHandler<SerialConnectionEventArgs>? ConnectionChanged;

        public string? PortName { get; private set; }
        public int BaudRate { get; private set; }

        public void Configure(string? portName, int baudRate)
        {
            PortName = portName;
            BaudRate = baudRate;
        }

        public void Connect()
        {
            lock (_sync)
            {
                DisposePort();
                if (string.IsNullOrWhiteSpace(PortName)) return;

                _cts = new CancellationTokenSource();
                Task.Run(() => RunAsync(_cts.Token));
            }
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var port = new SerialPort(PortName!, BaudRate)
                    {
                        Encoding = Encoding.ASCII,
                        NewLine = "\n",
                        ReadTimeout = 500
                    };
                    _port = port;
                    port.Open();
                    ConnectionChanged?.Invoke(this, new SerialConnectionEventArgs(true));

                    while (!token.IsCancellationRequested)
                    {
                        var line = port.ReadLine();
                        LineReceived?.Invoke(this, new SerialLineEventArgs(line));
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    ConnectionChanged?.Invoke(this, new SerialConnectionEventArgs(false));
                    await Task.Delay(_reconnectDelay, token).ConfigureAwait(false);
                }
            }
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                _cts?.Cancel();
                DisposePort();
                ConnectionChanged?.Invoke(this, new SerialConnectionEventArgs(false));
            }
        }

        private void DisposePort()
        {
            if (_port != null)
            {
                try
                {
                    _port.Close();
                }
                catch
                {
                    // ignored
                }
                _port.Dispose();
                _port = null;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
