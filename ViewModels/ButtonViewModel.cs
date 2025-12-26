using System.ComponentModel;
using System.Runtime.CompilerServices;
using MixerPad.Models;

namespace MixerPad.ViewModels
{
    public class ButtonViewModel : INotifyPropertyChanged
    {
        private bool _isPressed;
        public ButtonViewModel(ButtonMapping mapping)
        {
            Mapping = mapping;
        }

        public ButtonMapping Mapping { get; }

        public bool IsPressed
        {
            get => _isPressed;
            set
            {
                if (_isPressed != value)
                {
                    _isPressed = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
