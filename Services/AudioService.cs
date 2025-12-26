using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;
using MixerPad.Models;

namespace MixerPad.Services
{
    public record AudioSessionInfo(string DisplayName, string Id, float Volume, bool IsMuted);

    public class AudioService : IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator = new();
        private readonly Dictionary<int, DateTime> _lastSliderUpdate = new();
        private readonly Dictionary<int, float> _lastVolumes = new();

        public IList<AudioSessionInfo> GetProcessSessions()
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            using var manager = device.AudioSessionManager2;
            return manager.GetSessionEnumerator()
                .Select(s => (session: s, ctrl: s.QueryInterface<AudioSessionControl2>()))
                .Select(x => new AudioSessionInfo(x.ctrl.Process?.ProcessName ?? x.ctrl.DisplayName ?? "Unknown", x.ctrl.Process?.ProcessName ?? x.ctrl.DisplayName ?? Guid.NewGuid().ToString(), x.session.SimpleAudioVolume.Volume, x.session.SimpleAudioVolume.Mute))
                .ToList();
        }

        public void SetVolume(SliderTargetType targetType, string targetId, float value)
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (targetType == SliderTargetType.Master || targetId == "master")
            {
                device.AudioEndpointVolume.MasterVolumeLevelScalar = value;
                return;
            }

            using var manager = device.AudioSessionManager2;
            foreach (var session in manager.GetSessionEnumerator())
            {
                using var control = session.QueryInterface<AudioSessionControl2>();
                var processName = control.Process?.ProcessName;
                var displayName = control.DisplayName;
                if (string.Equals(processName, targetId, StringComparison.OrdinalIgnoreCase) || string.Equals(displayName, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    session.SimpleAudioVolume.Volume = value;
                    return;
                }
            }
        }

        public void ToggleMute(string targetId)
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (targetId == "master")
            {
                device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
                return;
            }

            using var manager = device.AudioSessionManager2;
            foreach (var session in manager.GetSessionEnumerator())
            {
                using var control = session.QueryInterface<AudioSessionControl2>();
                var processName = control.Process?.ProcessName;
                var displayName = control.DisplayName;
                if (string.Equals(processName, targetId, StringComparison.OrdinalIgnoreCase) || string.Equals(displayName, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    session.SimpleAudioVolume.Mute = !session.SimpleAudioVolume.Mute;
                    return;
                }
            }
        }

        public void SetSliderVolume(int sliderIndex, SliderTargetType targetType, string targetId, float normalizedValue, TimeSpan minInterval, int threshold)
        {
            var now = DateTime.UtcNow;
            if (_lastSliderUpdate.TryGetValue(sliderIndex, out var last) && (now - last) < minInterval)
            {
                return;
            }

            if (_lastVolumes.TryGetValue(sliderIndex, out var lastVolume))
            {
                if (Math.Abs(lastVolume - normalizedValue) < (threshold / 4095f))
                {
                    return;
                }
            }

            _lastVolumes[sliderIndex] = normalizedValue;
            _lastSliderUpdate[sliderIndex] = now;
            SetVolume(targetType, targetId, normalizedValue);
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
