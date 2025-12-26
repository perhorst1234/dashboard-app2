using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MixerMacroPad.Models;
using WindowsInput;
using WindowsInput.Native;

namespace MixerMacroPad.Services;

public class ButtonActionService
{
    private readonly AudioService _audioService;
    private readonly InputSimulator _simulator = new();
    private readonly Dictionary<int, DateTime> _lastTriggered = new();

    public ButtonActionService(AudioService audioService)
    {
        _audioService = audioService;
    }

    public void HandleButton(ButtonMapping mapping, bool pressed)
    {
        var now = DateTime.UtcNow;
        if (_lastTriggered.TryGetValue(mapping.HardwareIndex, out var last) && (now - last).TotalMilliseconds < 30)
        {
            return;
        }

        _lastTriggered[mapping.HardwareIndex] = now;

        switch (mapping.Mode)
        {
            case ButtonMode.Toggle:
                if (pressed) TriggerAction(mapping, true);
                break;
            case ButtonMode.Momentary:
                TriggerAction(mapping, pressed);
                break;
            case ButtonMode.RepeatWhileHeld:
                if (pressed)
                {
                    TriggerAction(mapping, true);
                    _ = Repeat(mapping);
                }
                break;
        }
    }

    private async Task Repeat(ButtonMapping mapping)
    {
        while (true)
        {
            await Task.Delay(mapping.RepeatIntervalMs);
            TriggerAction(mapping, true);
            if (!_lastTriggered.ContainsKey(mapping.HardwareIndex)) break;
        }
    }

    private void TriggerAction(ButtonMapping mapping, bool active)
    {
        switch (mapping.ActionType)
        {
            case ButtonActionType.RunProcess:
                if (active) RunProcess(mapping);
                break;
            case ButtonActionType.Hotkey:
                if (active) SendHotkey(mapping.Payload);
                break;
            case ButtonActionType.MediaPlayPause:
                if (active) _simulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PLAY_PAUSE);
                break;
            case ButtonActionType.MediaNext:
                if (active) _simulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_NEXT_TRACK);
                break;
            case ButtonActionType.MediaPrevious:
                if (active) _simulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PREV_TRACK);
                break;
            case ButtonActionType.VolumeMute:
                if (active) _audioService.ToggleMuteRender();
                break;
            case ButtonActionType.ToggleMicMute:
                if (active) _audioService.ToggleMuteMic();
                break;
            case ButtonActionType.ToggleAppMute:
                if (active && !string.IsNullOrWhiteSpace(mapping.Payload))
                {
                    _audioService.ToggleMuteProcess(mapping.Payload);
                }
                break;
            case ButtonActionType.PushToTalk:
                HandlePushToTalk(mapping, active);
                break;
        }
    }

    private void RunProcess(ButtonMapping mapping)
    {
        if (string.IsNullOrWhiteSpace(mapping.Payload)) return;
        var psi = new ProcessStartInfo(mapping.Payload)
        {
            Arguments = mapping.Arguments ?? string.Empty,
            UseShellExecute = true
        };
        if (mapping.RunAsAdmin)
        {
            psi.Verb = "runas";
        }

        try { Process.Start(psi); } catch { }
    }

    private void SendHotkey(string sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence)) return;
        // Block known unsupported combinations
        if (sequence.Contains("Ctrl+Alt+Del", StringComparison.OrdinalIgnoreCase)) return;
        var keys = sequence.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var modifiers = new List<VirtualKeyCode>();
        VirtualKeyCode? final = null;
        foreach (var key in keys)
        {
            if (Enum.TryParse<VirtualKeyCode>("VK_" + key.ToUpperInvariant(), out var vk))
            {
                final = vk;
            }
            else if (Enum.TryParse<VirtualKeyCode>(key.ToUpperInvariant(), out var direct))
            {
                modifiers.Add(direct);
            }
            else
            {
                switch (key.ToLowerInvariant())
                {
                    case "ctrl": modifiers.Add(VirtualKeyCode.CONTROL); break;
                    case "alt": modifiers.Add(VirtualKeyCode.MENU); break;
                    case "shift": modifiers.Add(VirtualKeyCode.SHIFT); break;
                    case "win": modifiers.Add(VirtualKeyCode.LWIN); break;
                    default: final = null; break;
                }
            }
        }

        if (final == null) return;
        foreach (var m in modifiers) _simulator.Keyboard.KeyDown(m);
        _simulator.Keyboard.KeyPress(final.Value);
        foreach (var m in modifiers) _simulator.Keyboard.KeyUp(m);
    }

    private void HandlePushToTalk(ButtonMapping mapping, bool active)
    {
        if (string.IsNullOrWhiteSpace(mapping.Payload)) return;
        var keys = mapping.Payload.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var keyCodes = new List<VirtualKeyCode>();
        foreach (var key in keys)
        {
            if (Enum.TryParse<VirtualKeyCode>(key, true, out var vk))
            {
                keyCodes.Add(vk);
            }
        }

        if (active)
        {
            foreach (var k in keyCodes) _simulator.Keyboard.KeyDown(k);
        }
        else
        {
            foreach (var k in keyCodes) _simulator.Keyboard.KeyUp(k);
        }
    }
}
