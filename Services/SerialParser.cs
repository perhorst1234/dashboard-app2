using System;
using System.Globalization;
using System.Linq;

namespace MixerPad.Services
{
    public class SerialPayload
    {
        public int[] Sliders { get; } = new int[4];
        public bool[] Buttons { get; } = new bool[16];
    }

    public static class SerialParser
    {
        public static bool TryParse(string? line, bool invertButtons, out SerialPayload payload)
        {
            payload = new SerialPayload();
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            var tokens = line.Trim().Split('|');
            if (tokens.Length < 4)
            {
                return false;
            }

            try
            {
                int sliderIndex = 0;
                int buttonIndex = 0;
                foreach (var token in tokens)
                {
                    if (token.StartsWith('s') && sliderIndex < 4)
                    {
                        if (int.TryParse(token[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                        {
                            payload.Sliders[sliderIndex++] = Math.Clamp(value, 0, 4095);
                        }
                    }
                    else if (token.StartsWith('b') && buttonIndex < 16)
                    {
                        var isHigh = token.Length > 1 && token[1] == '1';
                        var isPressed = invertButtons ? isHigh : !isHigh;
                        payload.Buttons[buttonIndex++] = isPressed;
                    }
                }

                return sliderIndex == 4;
            }
            catch
            {
                return false;
            }
        }
    }
}
