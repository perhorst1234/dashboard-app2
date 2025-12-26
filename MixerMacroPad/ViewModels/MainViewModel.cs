using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using MixerMacroPad.Models;
using MixerMacroPad.Services;

namespace MixerMacroPad.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _configService;
    private readonly SerialService _serialService;
    private readonly AudioService _audioService;
    private readonly ButtonActionService _buttonActions;
    private readonly bool[] _lastButtons = new bool[16];
    private readonly int[] _lastSliderValues = new int[4];

    public ObservableCollection<SliderMapping> Sliders { get; } = new();
    public ObservableCollection<ButtonMapping> Buttons { get; } = new();
    public ObservableCollection<string> ComPorts { get; } = new(SerialPort.GetPortNames());
    public ObservableCollection<AudioService.SessionInfo> Sessions { get; } = new();

    private string? _selectedComPort;
    public string? SelectedComPort
    {
        get => _selectedComPort;
        set { _selectedComPort = value; OnPropertyChanged(); }
    }

    private string _rawLine = string.Empty;
    public string RawLine
    {
        get => _rawLine;
        set { _rawLine = value; OnPropertyChanged(); }
    }

    private string _log = string.Empty;
    public string Log
    {
        get => _log;
        set { _log = value; OnPropertyChanged(); }
    }

    private bool _invertButtons;
    public bool InvertButtons
    {
        get => _invertButtons;
        set
        {
            if (_invertButtons == value) return;
            _invertButtons = value;
            _configService.Current.InvertButtons = value;
            _configService.Save();
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(ConfigService configService, SerialService serialService, AudioService audioService, ButtonActionService buttonActions)
    {
        _configService = configService;
        _serialService = serialService;
        _audioService = audioService;
        _buttonActions = buttonActions;

        _serialService.FrameReceived += HandleFrame;
        _serialService.RawLine += l => RawLine = l.Trim();
        _serialService.Log += AppendLog;

        LoadConfig();
        RefreshSessions();
    }

    private void LoadConfig()
    {
        _configService.Load();
        Sliders.Clear();
        Buttons.Clear();
        foreach (var s in _configService.Current.SliderMappings.OrderBy(x => x.HardwareIndex))
        {
            Sliders.Add(s);
        }
        foreach (var b in _configService.Current.ButtonMappings.OrderBy(x => x.HardwareIndex))
        {
            Buttons.Add(b);
        }
        InvertButtons = _configService.Current.InvertButtons;
        SelectedComPort = _configService.Current.ComPort;
    }

    public void SaveConfig()
    {
        _configService.Current.SliderMappings = Sliders.ToList();
        _configService.Current.ButtonMappings = Buttons.ToList();
        _configService.Save();
    }

    public void RefreshPorts()
    {
        ComPorts.Clear();
        foreach (var p in SerialPort.GetPortNames()) ComPorts.Add(p);
        if (SelectedComPort == null && ComPorts.Count > 0)
        {
            SelectedComPort = ComPorts[0];
        }
    }

    public void RefreshSessions()
    {
        Sessions.Clear();
        foreach (var session in _audioService.EnumerateSessions()) Sessions.Add(session);
    }

    public void ConnectSerial(string port)
    {
        _configService.Current.ComPort = port;
        _configService.Save();
        _serialService.Connect(port, _configService.Current.BaudRate);
    }

    public void DisconnectSerial() => _serialService.Disconnect();

    private void HandleFrame(int[] sliders, bool[] buttons)
    {
        for (int i = 0; i < Sliders.Count && i < sliders.Length; i++)
        {
            var mapping = Sliders[i];
            var delta = Math.Abs(sliders[i] - _lastSliderValues[i]);
            if (delta >= mapping.DeltaThreshold)
            {
                _lastSliderValues[i] = sliders[i];
                _audioService.ApplyVolume(mapping, sliders[i]);
            }
        }

        for (int i = 0; i < Buttons.Count && i < buttons.Length; i++)
        {
            var state = buttons[i];
            var last = _lastButtons[i];
            if (state != last)
            {
                _lastButtons[i] = state;
                _buttonActions.HandleButton(Buttons[i], state);
            }
        }
    }

    public void AppendLog(string message)
    {
        Log = $"{DateTime.Now:HH:mm:ss} {message}\n" + Log;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
