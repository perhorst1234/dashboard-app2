using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using MixerPad.Models;
using MixerPad.Services;

namespace MixerPad.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ConfigService _configService;
        private readonly SerialService _serialService = new();
        private readonly AudioService _audioService = new();
        private readonly ButtonActionService _buttonActionService;
        private Config _config = new();
        private readonly bool[] _buttonStates = new bool[16];
        private readonly DateTime[] _buttonTimestamps = new DateTime[16];
        private string _selectedPort = string.Empty;
        private int _baudRate = 9600;
        private bool _connected;
        private bool _invertButtons;
        private string _rawLine = string.Empty;
        private readonly Dispatcher _dispatcher;

        public MainViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _configService = new ConfigService(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"));
            _buttonActionService = new ButtonActionService(_audioService);

            ConnectCommand = new RelayCommand(_ => Connect());
            DisconnectCommand = new RelayCommand(_ => _serialService.Disconnect());
            RefreshSessionsCommand = new RelayCommand(_ => RefreshSessions());
            ExportConfigCommand = new RelayCommand(async _ => await SaveConfigAsync());
            ImportConfigCommand = new RelayCommand(async _ => await LoadConfigAsync());

            _serialService.LineReceived += SerialService_LineReceived;
            _serialService.ConnectionChanged += SerialService_ConnectionChanged;
            _ = LoadConfigAsync();
        }

        public ObservableCollection<string> Ports { get; } = new(SerialPort.GetPortNames());
        public ObservableCollection<SliderViewModel> Sliders { get; } = new();
        public ObservableCollection<ButtonViewModel> Buttons { get; } = new();
        public ObservableCollection<AudioSessionInfo> Sessions { get; } = new();
        public ObservableCollection<string> Logs { get; } = new();
        public Array ActionTypes => Enum.GetValues(typeof(ButtonActionType));
        public Array ButtonModes => Enum.GetValues(typeof(ButtonMode));
        public Array SliderTargets => Enum.GetValues(typeof(SliderTargetType));

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }
        public RelayCommand RefreshSessionsCommand { get; }
        public RelayCommand ExportConfigCommand { get; }
        public RelayCommand ImportConfigCommand { get; }

        public string SelectedPort
        {
            get => _selectedPort;
            set
            {
                if (_selectedPort != value)
                {
                    _selectedPort = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BaudRate
        {
            get => _baudRate;
            set
            {
                if (_baudRate != value)
                {
                    _baudRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Connected
        {
            get => _connected;
            set
            {
                if (_connected != value)
                {
                    _connected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool InvertButtons
        {
            get => _invertButtons;
            set
            {
                if (_invertButtons != value)
                {
                    _invertButtons = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RawLine
        {
            get => _rawLine;
            set
            {
                if (_rawLine != value)
                {
                    _rawLine = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task LoadConfigAsync()
        {
            _config = await _configService.LoadAsync();
            _dispatcher.Invoke(() =>
            {
                Sliders.Clear();
                Buttons.Clear();
                foreach (var slider in _config.SliderMappings)
                {
                    Sliders.Add(new SliderViewModel(slider));
                }
                foreach (var button in _config.ButtonMappings)
                {
                    Buttons.Add(new ButtonViewModel(button));
                }
                SelectedPort = _config.ComPort ?? string.Empty;
                BaudRate = _config.BaudRate;
                InvertButtons = _config.InvertButtons;
            });

            RefreshSessions();
            Connect();
        }

        private async Task SaveConfigAsync()
        {
            _config.ComPort = SelectedPort;
            _config.BaudRate = BaudRate;
            _config.InvertButtons = InvertButtons;
            _config.SliderMappings = Sliders.Select(s => s.ToMapping()).ToList();
            _config.ButtonMappings = Buttons.Select(b => b.Mapping).ToList();
            await _configService.SaveAsync(_config);
        }

        private void Connect()
        {
            _serialService.Configure(SelectedPort, BaudRate);
            _serialService.Connect();
        }

        private void SerialService_ConnectionChanged(object? sender, SerialConnectionEventArgs e)
        {
            _dispatcher.Invoke(() => Connected = e.Connected);
            Log($"Serial {(e.Connected ? "connected" : "disconnected")}");
        }

        private void SerialService_LineReceived(object? sender, SerialLineEventArgs e)
        {
            RawLine = e.RawLine;
            if (SerialParser.TryParse(e.RawLine, InvertButtons, out var payload))
            {
                HandleSliders(payload.Sliders);
                HandleButtons(payload.Buttons);
            }
        }

        private void HandleSliders(int[] values)
        {
            for (int i = 0; i < Math.Min(values.Length, Sliders.Count); i++)
            {
                var vm = Sliders[i];
                var v = vm.Invert ? 4095 - values[i] : values[i];
                vm.RawValue = v;
                var normalized = (float)Math.Clamp(v / 4095f, 0f, 1f);
                _audioService.SetSliderVolume(i, vm.TargetType, vm.SelectedTarget, normalized, TimeSpan.FromMilliseconds(vm.SmoothingMs), vm.Threshold);
            }
        }

        private void HandleButtons(bool[] states)
        {
            for (int i = 0; i < Math.Min(states.Length, Buttons.Count); i++)
            {
                var pressed = states[i];
                var vm = Buttons[i];
                var lastState = _buttonStates[i];
                var now = DateTime.UtcNow;

                if (pressed != lastState)
                {
                    _buttonStates[i] = pressed;
                    _buttonTimestamps[i] = now;
                    vm.IsPressed = pressed;

                    if (pressed)
                    {
                        TriggerButton(vm.Mapping);
                    }
                    else
                    {
                        if (vm.Mapping.Mode == ButtonMode.Momentary || vm.Mapping.ActionType == ButtonActionType.PushToTalk)
                        {
                            _buttonActionService.Release(vm.Mapping);
                        }
                    }
                }
                else if (pressed && vm.Mapping.Mode == ButtonMode.RepeatWhileHeld)
                {
                    if ((now - _buttonTimestamps[i]).TotalMilliseconds >= vm.Mapping.RepeatIntervalMs)
                    {
                        _buttonTimestamps[i] = now;
                        TriggerButton(vm.Mapping);
                    }
                }
            }
        }

        private void TriggerButton(ButtonMapping mapping)
        {
            _buttonActionService.Execute(mapping, Sliders.FirstOrDefault()?.SelectedTarget ?? "master");
            Log($"Button {mapping.Name} -> {mapping.ActionType}");
        }

        private void RefreshSessions()
        {
            var sessions = _audioService.GetProcessSessions();
            _dispatcher.Invoke(() =>
            {
                Sessions.Clear();
                Sessions.Add(new AudioSessionInfo("Master", "master", 0, false));
                foreach (var session in sessions.OrderBy(s => s.DisplayName))
                {
                    Sessions.Add(session);
                }

                foreach (var slider in Sliders)
                {
                    slider.TargetOptions.Clear();
                    foreach (var s in Sessions)
                    {
                        slider.TargetOptions.Add(s.Id);
                    }
                }
            });
        }

        private void Log(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _dispatcher.Invoke(() =>
            {
                Logs.Insert(0, entry);
                while (Logs.Count > 200)
                {
                    Logs.RemoveAt(Logs.Count - 1);
                }
            });
        }

        public void Dispose()
        {
            _serialService.Dispose();
            _audioService.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
