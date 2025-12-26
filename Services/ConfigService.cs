using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MixerPad.Models;

namespace MixerPad.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true
        };

        public ConfigService(string configPath)
        {
            _configPath = configPath;
        }

        public async Task<Config> LoadAsync()
        {
            if (!File.Exists(_configPath))
            {
                return CreateDefault();
            }

            using var stream = File.OpenRead(_configPath);
            var config = await JsonSerializer.DeserializeAsync<Config>(stream, _options).ConfigureAwait(false);
            return config ?? CreateDefault();
        }

        public async Task SaveAsync(Config config)
        {
            using var stream = File.Create(_configPath);
            await JsonSerializer.SerializeAsync(stream, config, _options).ConfigureAwait(false);
        }

        private static Config CreateDefault()
        {
            var config = new Config();
            for (int i = 0; i < 4; i++)
            {
                config.SliderMappings.Add(new SliderMapping
                {
                    Name = $"Slider {i + 1}",
                    TargetType = SliderTargetType.Master,
                    TargetId = "master",
                    Threshold = 8,
                    SmoothingMilliseconds = 30
                });
            }

            for (int i = 0; i < 16; i++)
            {
                config.ButtonMappings.Add(new ButtonMapping
                {
                    Name = $"Button {i + 1}",
                    ActionType = ButtonActionType.None,
                    Mode = ButtonMode.Toggle
                });
            }

            return config;
        }
    }
}
