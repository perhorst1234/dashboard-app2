using System.Collections.Generic;

namespace MixerPad.Models
{
    public class Config
    {
        public string? ComPort { get; set; }
        public int BaudRate { get; set; } = 9600;
        public bool InvertButtons { get; set; }
        public List<SliderMapping> SliderMappings { get; set; } = new();
        public List<ButtonMapping> ButtonMappings { get; set; } = new();
    }

    public class SliderMapping
    {
        public string Name { get; set; } = string.Empty;
        public SliderTargetType TargetType { get; set; } = SliderTargetType.Master;
        public string TargetId { get; set; } = string.Empty;
        public double SmoothingMilliseconds { get; set; } = 30;
        public int Threshold { get; set; } = 8;
        public bool Invert { get; set; }
    }

    public enum SliderTargetType
    {
        Master,
        Process,
        DeviceSession
    }

    public class ButtonMapping
    {
        public string Name { get; set; } = string.Empty;
        public ButtonActionType ActionType { get; set; } = ButtonActionType.None;
        public string Payload { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public bool RunAsAdmin { get; set; }
        public ButtonMode Mode { get; set; } = ButtonMode.Toggle;
        public int RepeatIntervalMs { get; set; } = 250;
    }

    public enum ButtonMode
    {
        Toggle,
        Momentary,
        RepeatWhileHeld
    }

    public enum ButtonActionType
    {
        None,
        RunProcess,
        SendHotkey,
        MediaPlayPause,
        MediaNext,
        MediaPrevious,
        VolumeMute,
        ToggleMicMute,
        ToggleSelectedAppMute,
        PushToTalk
    }
}
