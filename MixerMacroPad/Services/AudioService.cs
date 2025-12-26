using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MixerMacroPad.Models;
using NAudio.CoreAudioApi;

namespace MixerMacroPad.Services;

public class AudioService : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly Dictionary<int, DateTime> _lastSliderUpdate = new();
    private readonly object _sessionLock = new();

    public event Action? SessionsChanged;

    public record SessionInfo(string Id, string DisplayName, SliderTargetType TargetType);

    public IEnumerable<SessionInfo> EnumerateSessions()
    {
        var sessions = new List<SessionInfo>
        {
            new("Master", "Master volume", SliderTargetType.Master)
        };

        using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var manager = device.AudioSessionManager2;
        foreach (var session in manager.Sessions.Cast<AudioSessionControl2>())
        {
            string id = session.Process?.ProcessName + ".exe" ?? session.DisplayName;
            sessions.Add(new SessionInfo(id, $"{session.Process?.ProcessName ?? session.DisplayName}", SliderTargetType.Process));
        }

        return sessions;
    }

    public void ApplyVolume(SliderMapping mapping, int sliderValue)
    {
        var now = DateTime.UtcNow;
        if (_lastSliderUpdate.TryGetValue(mapping.HardwareIndex, out var last) && (now - last).TotalMilliseconds < mapping.SmoothingMs)
        {
            return;
        }

        _lastSliderUpdate[mapping.HardwareIndex] = now;
        var clamped = Math.Clamp(sliderValue, 0, 4095);
        var normalized = (double)clamped / 4095.0;
        if (mapping.Invert)
        {
            normalized = 1 - normalized;
        }

        if (mapping.TargetType == SliderTargetType.Master)
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)normalized;
            return;
        }

        using var renderDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var manager = renderDevice.AudioSessionManager2;
        foreach (var session in manager.Sessions.Cast<AudioSessionControl2>())
        {
            var procName = session.Process?.ProcessName;
            if (string.IsNullOrWhiteSpace(procName)) continue;
            var key = procName + ".exe";
            if (mapping.TargetType == SliderTargetType.Process && string.Equals(mapping.TargetId, key, StringComparison.OrdinalIgnoreCase))
            {
                session.SimpleAudioVolume.Volume = (float)normalized;
            }
            else if (mapping.TargetType == SliderTargetType.DeviceSession && string.Equals(mapping.TargetId, session.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                session.SimpleAudioVolume.Volume = (float)normalized;
            }
        }
    }

    public void ToggleMuteMic()
    {
        using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
    }

    public void ToggleMuteRender()
    {
        using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
    }

    public void ToggleMuteProcess(string process)
    {
        using var renderDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var manager = renderDevice.AudioSessionManager2;
        foreach (var session in manager.Sessions.Cast<AudioSessionControl2>())
        {
            var procName = session.Process?.ProcessName;
            if (string.IsNullOrWhiteSpace(procName)) continue;
            var key = procName + ".exe";
            if (string.Equals(key, process, StringComparison.OrdinalIgnoreCase))
            {
                session.SimpleAudioVolume.Mute = !session.SimpleAudioVolume.Mute;
            }
        }
    }

    public void Dispose()
    {
        _enumerator.Dispose();
    }
}
