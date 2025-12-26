using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MixerMacroPad.Models;

namespace MixerMacroPad.Services;

public class SerialService : IDisposable
{
    private readonly ConfigService _configService;
    private SerialPort? _serialPort;
    private readonly CancellationTokenSource _cts = new();
    private readonly StringBuilder _buffer = new();

    public event Action<int[], bool[]>? FrameReceived;
    public event Action<string>? RawLine;
    public event Action<string>? Log;

    public SerialService(ConfigService configService)
    {
        _configService = configService;
    }

    public void Start()
    {
        Task.Run(ReadLoop, _cts.Token);
    }

    public void Connect(string portName, int baud)
    {
        Disconnect();
        try
        {
            _serialPort = new SerialPort(portName, baud)
            {
                Encoding = Encoding.ASCII,
                DtrEnable = true,
                RtsEnable = true,
                NewLine = "\n"
            };
            _serialPort.Open();
            Log?.Invoke($"Connected to {portName} @ {baud}");
        }
        catch (Exception ex)
        {
            Log?.Invoke($"Failed to open port: {ex.Message}");
            ScheduleReconnect();
        }
    }

    public void Disconnect()
    {
        if (_serialPort != null)
        {
            try { _serialPort.Close(); } catch { }
            _serialPort.Dispose();
        }
        _serialPort = null;
    }

    private async Task ReadLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                await Task.Delay(500, _cts.Token).ContinueWith(_ => { });
                continue;
            }

            try
            {
                var line = _serialPort.ReadLine();
                RawLine?.Invoke(line);
                ParseLine(line);
            }
            catch (TimeoutException) { }
            catch (Exception ex)
            {
                Log?.Invoke($"Serial error: {ex.Message}");
                ScheduleReconnect();
            }
        }
    }

    private void ScheduleReconnect()
    {
        Disconnect();
        Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ =>
        {
            if (!string.IsNullOrWhiteSpace(_configService.Current.ComPort))
            {
                Connect(_configService.Current.ComPort, _configService.Current.BaudRate);
            }
        });
    }

    private void ParseLine(string line)
    {
        try
        {
            var tokens = line.Trim().Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length < 5) return;

            var sliders = new int[4];
            var buttons = new bool[16];
            int sliderCount = 0;
            int buttonCount = 0;

            foreach (var token in tokens)
            {
                if (token.StartsWith('s') && sliderCount < 4 && int.TryParse(token.AsSpan(1), out var sv))
                {
                    sliders[sliderCount++] = Math.Clamp(sv, 0, 4095);
                }
                else if (token.StartsWith('b') && buttonCount < 16 && int.TryParse(token.AsSpan(1), out var bv))
                {
                    var pressed = _configService.Current.InvertButtons ? bv == 1 : bv == 0;
                    buttons[buttonCount++] = pressed;
                }
            }

            if (sliderCount == 4 && buttonCount == 16)
            {
                FrameReceived?.Invoke(sliders, buttons);
            }
        }
        catch
        {
            // ignore malformed line
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        Disconnect();
    }
}
