using System;
using System.Collections.Generic;
using System.Diagnostics;
using MixerPad.Models;
using WindowsInput;
using WindowsInput.Native;

namespace MixerPad.Services
{
    public class ButtonActionService
    {
        private readonly InputSimulator _input = new();
        private readonly AudioService _audioService;

        public ButtonActionService(AudioService audioService)
        {
            _audioService = audioService;
        }

        public void Execute(ButtonMapping mapping, string selectedAppTarget)
        {
            switch (mapping.ActionType)
            {
                case ButtonActionType.RunProcess:
                    RunProcess(mapping.Payload, mapping.Arguments, mapping.RunAsAdmin);
                    break;
                case ButtonActionType.SendHotkey:
                    SendHotkey(mapping.Payload);
                    break;
                case ButtonActionType.MediaPlayPause:
                    _input.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PLAY_PAUSE);
                    break;
                case ButtonActionType.MediaNext:
                    _input.Keyboard.KeyPress(VirtualKeyCode.MEDIA_NEXT_TRACK);
                    break;
                case ButtonActionType.MediaPrevious:
                    _input.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PREV_TRACK);
                    break;
                case ButtonActionType.VolumeMute:
                    _input.Keyboard.KeyPress(VirtualKeyCode.VOLUME_MUTE);
                    break;
                case ButtonActionType.ToggleMicMute:
                    ToggleGlobalMute(Role.Communications);
                    break;
                case ButtonActionType.ToggleSelectedAppMute:
                    _audioService.ToggleMute(string.IsNullOrWhiteSpace(mapping.Payload) ? selectedAppTarget : mapping.Payload);
                    break;
                case ButtonActionType.PushToTalk:
                    _input.Keyboard.KeyDown(VirtualKeyCode.SPACE);
                    break;
                default:
                    break;
            }
        }

        public void Release(ButtonMapping mapping)
        {
            if (mapping.ActionType == ButtonActionType.PushToTalk)
            {
                _input.Keyboard.KeyUp(VirtualKeyCode.SPACE);
            }
        }

        private void RunProcess(string? path, string? args, bool elevate)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            var psi = new ProcessStartInfo(path)
            {
                Arguments = args ?? string.Empty,
                UseShellExecute = true
            };
            if (elevate)
            {
                psi.Verb = "runas";
            }

            Process.Start(psi);
        }

        private void SendHotkey(string? hotkey)
        {
            if (string.IsNullOrWhiteSpace(hotkey)) return;
            var combos = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var keys = new List<VirtualKeyCode>();
            foreach (var combo in combos)
            {
                if (Enum.TryParse<VirtualKeyCode>("VK_" + combo.ToUpperInvariant(), out var vk))
                {
                    keys.Add(vk);
                }
                else if (Enum.TryParse<VirtualKeyCode>(combo, true, out var direct))
                {
                    keys.Add(direct);
                }
            }

            if (keys.Count == 0) return;

            _input.Keyboard.ModifiedKeyStroke(keys.GetRange(0, keys.Count - 1), keys[^1]);
        }

        private void ToggleGlobalMute(Role role)
        {
            using var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, role);
            device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
        }
    }
}
