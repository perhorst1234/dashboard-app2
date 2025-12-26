using System.Collections.Generic;

namespace MixerMacroPad.Models;

public class AppConfig
{
    public string ComPort { get; set; } = string.Empty;
    public int BaudRate { get; set; } = 9600;
    public bool InvertButtons { get; set; }
    public List<SliderMapping> SliderMappings { get; set; } = new();
    public List<ButtonMapping> ButtonMappings { get; set; } = new();
}

public class SliderMapping
{
    public int HardwareIndex { get; set; }
    public SliderTargetType TargetType { get; set; } = SliderTargetType.Master;
    public string TargetId { get; set; } = string.Empty;
    public double SmoothingMs { get; set; } = 30;
    public int DeltaThreshold { get; set; } = 8;
    public bool Invert { get; set; }
}

public class ButtonMapping
{
    public int HardwareIndex { get; set; }
    public ButtonActionType ActionType { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public bool RunAsAdmin { get; set; }
    public ButtonMode Mode { get; set; } = ButtonMode.Toggle;
    public int RepeatIntervalMs { get; set; } = 120;
}

public enum SliderTargetType
{
    Master,
    Process,
    DeviceSession
}

public enum ButtonActionType
{
    None,
    RunProcess,
    Hotkey,
    MediaPlayPause,
    MediaNext,
    MediaPrevious,
    VolumeMute,
    ToggleMicMute,
    ToggleAppMute,
    PushToTalk
}

public enum ButtonMode
{
    Toggle,
    Momentary,
    RepeatWhileHeld
}
