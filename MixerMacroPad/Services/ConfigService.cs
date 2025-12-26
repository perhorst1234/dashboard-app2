using System;
using System.IO;
using System.Text.Json;
using MixerMacroPad.Models;

namespace MixerMacroPad.Services;

public class ConfigService
{
    private readonly string _configPath;
    public AppConfig Current { get; private set; }

    public ConfigService()
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        Current = new AppConfig();
    }

    public void Load()
    {
        if (!File.Exists(_configPath))
        {
            InitializeDefaults();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });
            if (config != null)
            {
                Current = config;
                EnsureDefaults();
            }
        }
        catch
        {
            InitializeDefaults();
        }
    }

    private void EnsureDefaults()
    {
        if (Current.SliderMappings.Count == 0)
        {
            InitializeDefaultSliders();
        }
        if (Current.ButtonMappings.Count == 0)
        {
            InitializeDefaultButtons();
        }
    }

    private void InitializeDefaults()
    {
        Current = new AppConfig();
        InitializeDefaultSliders();
        InitializeDefaultButtons();
    }

    private void InitializeDefaultSliders()
    {
        Current.SliderMappings.Clear();
        for (int i = 0; i < 4; i++)
        {
            Current.SliderMappings.Add(new SliderMapping
            {
                HardwareIndex = i,
                TargetType = SliderTargetType.Master,
                TargetId = "Master"
            });
        }
    }

    private void InitializeDefaultButtons()
    {
        Current.ButtonMappings.Clear();
        for (int i = 0; i < 16; i++)
        {
            Current.ButtonMappings.Add(new ButtonMapping
            {
                HardwareIndex = i,
                ActionType = ButtonActionType.None
            });
        }
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(Current, options);
        File.WriteAllText(_configPath, json);
    }

    public void Export(string filePath) => File.Copy(_configPath, filePath, true);

    public void Import(string filePath)
    {
        File.Copy(filePath, _configPath, true);
        Load();
    }
}
