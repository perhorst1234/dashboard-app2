# MixerPad (ESP32 serial deej + macro pad)

WPF .NET 8 desktopapp die de ESP32 mixer- en macro-gegevens via een seriële verbinding verwerkt. De UI combineert vier volumefaders met zestien macro-knoppen, real-time diagnosetools en configuratie-opslag.

## Bouwen en draaien
1. Installeer .NET SDK 8.0 (Windows, met desktop build tools).
2. Herstel afhankelijkheden en bouw:
   ```bash
   dotnet restore
   dotnet build
   ```
3. Start de app:
   ```bash
   dotnet run
   ```

## Afhankelijkheden
- **NAudio**: Core Audio (sessions, master/device volume, mute) via `AudioSessionManager2`.
- **WindowsInput**: simulatie van hotkeys en mediatoetsen.

## Serieel protocol
- Elke regel: `s<int>|s<int>|s<int>|s<int>|b0|...|b1` (~10 ms)
- Sliders: `s0..s4095` (4 stuks)
- Buttons: `b0` of `b1` (16 stuks). Standaard betekenen `b1` niet ingedrukt en `b0` ingedrukt; de instelling **Invert buttons** kan dit omkeren.
- Parser negeert whitespace/ongeldige regels zonder crash.

## Mapping workflow
- **Sliders**
  - Kies target type (Master, Process, DeviceSession) en target-id (procesnaam of sessie-id). Een "Master" entry is altijd aanwezig.
  - Waarde 0..4095 wordt naar 0..100% vertaald; invert-optie per slider.
  - Anti-spam: drempel (standaard 8 stappen) en minimuminterval (standaard 30 ms) voordat het volume wordt gezet.
  - "Sticky" mapping: de gekozen target-id blijft behouden, ook als het proces niet actief is; zodra het terugkeert wordt volume toegepast.
  - **Refresh sessions** knopt haalt actuele audiosessies op.
- **Buttons**
  - Acties: processen/scripts starten (met optioneel run-as-admin), hotkeys, mediatoetsen, mute toggle (master/app/mic), push-to-talk.
  - Modus: Toggle, Momentary, Repeat-while-held (met instelbaar interval).
  - Debounce op basis van edge-detectie; push-to-talk gebruikt key-down/-up events.

## Configuratie (config.json)
Bestand staat naast de executable en bevat: `comPort`, `baudRate`, `invertButtons`, `sliderMappings` (target type/id, drempel, smoothing, invert) en `buttonMappings` (actie, payload, arguments, runAsAdmin, mode, repeatIntervalMs). Gebruik **Export config** / **Import config** in de UI of kopieer het bestand handmatig.

## Troubleshooting
- **Geen COM-poorten zichtbaar**: controleer drivers/USB-kabel; kies handmatig de juiste poort en baudrate (default 9600).
- **Verbinding valt weg**: de app probeert elke 2 seconden opnieuw te verbinden; check dat de poort vrij is.
- **Audiorechten**: voor app-specifiek volume is een actief audiosessie nodig; gebruik **Refresh sessions** nadat een app geluid maakt.
- **Hotkeys**: combinaties zoals Ctrl+Alt+Del worden genegeerd door Windows; kies ondersteunde toetsen.
- **Machtigingen**: scripts/batches die adminrechten vragen moeten de optie *Run as admin* aan hebben.

## Screenshot
Een screenshot kan niet worden toegevoegd in deze omgeving; de UI biedt een donker thema met tabs voor Mixer en Diagnose.
