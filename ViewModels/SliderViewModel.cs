using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MixerPad.Models;

namespace MixerPad.ViewModels
{
    public class SliderViewModel : INotifyPropertyChanged
    {
        private int _rawValue;
        private double _volumePercent;
        private SliderTargetType _targetType;
        private string _selectedTarget = "master";
        private bool _invert;

        public SliderViewModel(SliderMapping mapping)
        {
            Name = mapping.Name;
            TargetType = mapping.TargetType;
            SelectedTarget = mapping.TargetId;
            Invert = mapping.Invert;
            SmoothingMs = mapping.SmoothingMilliseconds;
            Threshold = mapping.Threshold;
        }

        public string Name { get; }
        public ObservableCollection<string> TargetOptions { get; } = new();
        public double SmoothingMs { get; set; }
        public int Threshold { get; set; }

        public int RawValue
        {
            get => _rawValue;
            set
            {
                if (_rawValue != value)
                {
                    _rawValue = value;
                    OnPropertyChanged();
                    VolumePercent = value / 4095.0 * 100.0;
                }
            }
        }

        public double VolumePercent
        {
            get => _volumePercent;
            private set
            {
                if (_volumePercent != value)
                {
                    _volumePercent = value;
                    OnPropertyChanged();
                }
            }
        }

        public SliderTargetType TargetType
        {
            get => _targetType;
            set
            {
                if (_targetType != value)
                {
                    _targetType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                if (_selectedTarget != value)
                {
                    _selectedTarget = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Invert
        {
            get => _invert;
            set
            {
                if (_invert != value)
                {
                    _invert = value;
                    OnPropertyChanged();
                }
            }
        }

        public SliderMapping ToMapping() => new()
        {
            Name = Name,
            TargetType = TargetType,
            TargetId = SelectedTarget,
            SmoothingMilliseconds = SmoothingMs,
            Threshold = Threshold,
            Invert = Invert
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
