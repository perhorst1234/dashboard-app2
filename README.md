# MixerMacroPad (WPF, .NET 8)

Een donkere WPF desktop-app die een Deej-achtige mixer combineert met een macro-pad via een ESP32 die slider- en knoppenstatussen stuurt. Het bestaande seriële protocol wordt ongewijzigd verwerkt.

## Build

1. Installeer .NET SDK 8.0 op Windows.
2. Open een Developer Command Prompt in de repo-root.
3. Herstel packages en build:

```bash
 dotnet restore
 dotnet build
```

Gebruik Visual Studio of `dotnet run` om de app te starten.

## Afhankelijkheden

- [NAudio](https://github.com/naudio/NAudio) voor CoreAudio / audio sessie volume.
- [InputSimulatorPlus](https://github.com/michaelnoonan/inputsimulator) voor hotkeys en media keys.
- `System.IO.Ports` uit het .NET SDK voor seriële communicatie.

## Serieel protocol

- ESP32 stuurt regels zoals `s3067|s4095|s4095|s4095|b1|...` (~10ms).
- 4 sliders (`s0..4095`), 16 knoppen (`b0/b1`).
- Instelling **Invert buttons** maakt `b1` = niet ingedrukt / `b0` = ingedrukt omkeerbaar.
- Parser negeert corrupte/missende tokens en crasht niet.

## Functies

- **Mixers**: vier hardware sliders die gemapt worden naar master volume, specifieke processen of device sessions. Delta-drempel en smoothing voorkomen spam; mapping blijft bestaan ook als het proces nog niet draait.
- **Buttons**: zestien macro-knoppen met acties (process runnen, hotkey, media keys, mute toggles, push-to-talk). Debounce en hold/ repeat-modi aanwezig.
- **Diagnose**: toont raw seriële regels, parsed staten, logging en COM-status. Auto-reconnect probeert elke 2 seconden.
- **Config**: alles opgeslagen in `config.json` naast de exe. Import/Export-knoppen aanwezig.

## Mappings

- Slider-mapping: kies doel uit dropdown (master of actieve sessies), stel delta- en smoothing-waarden in en optioneel invert.
- Button-mapping: kies actie, modus (toggle/momentary/repeat), payload (pad/hotkey/procesnaam) en optionele arguments of run-as-admin.

## Troubleshooting

- **Geen COM-poorten**: druk Refresh, controleer drivers en of ESP32 verbonden is. Zet juiste COM in config.
- **Geen audio controle**: app vereist Windows met werkende CoreAudio endpoints. Voor per-app volume moet het doelproces actief zijn.
- **Hotkeys**: combinaties zoals `Ctrl+Alt+Del` worden bewust geblokkeerd. Start de app als admin als hotkeys programma's met hogere rechten moeten bedienen.
- **Microfoon/mute**: microfoon-toggle gebruikt het default capture device (Role.Communications). Kies juiste standaardapparaat in Windows als dit niet reageert.

## Screenshot

In deze omgeving kan geen WPF-venster getoond worden. Start de app lokaal om de UI te zien; de tabbladen bieden mixers, buttons en diagnose in een donker thema.
