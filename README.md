# LG 20MK400H Control

<p align="center">
  A lightweight Windows utility for controlling the LG 20MK400H monitor without repeatedly using its physical buttons.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/LG-20MK400H-A50034?style=for-the-badge&logo=lg&logoColor=white" height="42" alt="LG 20MK400H Control">
</p>

[![Repo Views](https://api.visitorbadge.io/api/visitors?path=https%3A%2F%2Fgithub.com%2Fpbzin%2F20MK400H-LG-Control&label=repo%20views&countColor=%230e75b6&style=flat)](https://visitorbadge.io/status?path=https%3A%2F%2Fgithub.com%2Fpbzin%2F20MK400H-LG-Control)

![Downloads](https://img.shields.io/github/downloads/pbzin/20MK400H-LG-Control/total?style=flat&color=0e75b6&label=downloads)

[![Download](https://img.shields.io/badge/Download-LG%2020MK400H%20Control-A50034?style=for-the-badge&logo=github)](https://github.com/pbzin/20MK400H-LG-Control/releases)

<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=flat-square&logo=windows" alt="Windows 10 and 11">
  <img src="https://img.shields.io/badge/.NET-Framework%204-512BD4?style=flat-square&logo=dotnet" alt=".NET Framework 4">
  <img src="https://img.shields.io/badge/Monitor-LG%2020MK400H-A50034?style=flat-square&logo=lg" alt="LG 20MK400H">
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT License"></a>
</p>

## Why this project exists

The LG 20MK400H accepts monitor-control writes through DDC/CI, but does not reliably expose its current values or capabilities to Windows. This prevents generic monitor tools—and even some versions of LG OnScreen Control—from offering the complete settings interface.

LG 20MK400H Control implements the model-specific commands discovered and tested on real hardware, with scheduling, profiles, tray controls, and an optional software night filter.

## Features

- Direct control of picture mode, brightness, contrast, sharpness, gamma, color temperature, and RGB gains.
- Confirmed LG-specific mappings for the 20MK400H.
- Four customizable profiles with independent start times.
- Optional profiles: disabled profiles remain saved but are removed from Windows Task Scheduler.
- Automatic profile switching, including after a missed schedule while the PC was off.
- Per-profile night filter from approximately 4000 K down to 1500 K.
- Global pause/resume for the night filter from the system tray.
- Optional automatic startup with Windows.
- Closing the window keeps the application running in the notification area.
- Portable: no traditional installation is required.

## Supported controls

| Control | Status |
| --- | --- |
| Picture modes | Supported |
| Brightness | Supported |
| Contrast | Supported |
| Sharpness | Supported in Custom mode |
| Gamma modes 1–3 | Supported |
| Gamma mode 4 | Monitor OSD only |
| Warm / Medium / Cool / User color | Supported |
| RGB gains | Supported in User color mode |
| Reader mode | Supported |
| Software night filter | Supported |

Reader mode is a firmware preset. It uses its own internal color processing and locks several controls. Custom + Warm is therefore not visually identical to Reader mode.

## Download and usage

1. Build `LGMonitorControl.exe` from source or download it from a future release, then keep it beside `install-lg-monitor-schedule.ps1` and the `assets` folder.
2. Run `LGMonitorControl.exe`.
3. Configure up to four profiles.
4. Check **Active** for every profile that should participate in automatic switching.
5. Click **Save and schedule** and approve the Windows administrator prompt.

The **Apply** button beside a profile only previews it immediately. **Save and schedule** saves every profile and updates all scheduled tasks.

## Night filter

The optional filter complements the monitor's own color modes using a Windows full-screen color transformation. Available presets are approximately:

- 4000 K
- 3000 K
- 2500 K
- 2000 K
- 1500 K

These values are calculated approximations, not colorimeter measurements. Use the tray command **Pause night filter** to restore normal colors globally. The paused state persists and scheduled tasks respect it.

## Portable startup behavior

The program can register itself under the current user's Windows startup entries. If the portable folder is moved, run the EXE once from its new location and the startup path will repair itself automatically.

After moving the folder, click **Save and schedule** once to update the paths stored in Windows Task Scheduler.

## Building from source

The project intentionally remains a single C# source file and uses only Windows/.NET Framework APIs.

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /nologo /target:winexe /optimize+ /platform:anycpu `
  /reference:System.dll `
  /reference:System.Drawing.dll `
  /reference:System.Windows.Forms.dll `
  /out:LGMonitorControl.exe `
  LGMonitorControl.cs
```

## Technical notes

- Monitor commands use `dxva2.dll` and `SetVCPFeature`.
- Scheduled profile changes use Windows Task Scheduler.
- The night filter uses `Magnification.dll`, with a gamma-ramp fallback.
- Startup uses the current user's `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry.
- No PawnIO driver or third-party hardware driver is required.

## Limitations

- Developed and tested specifically for the LG 20MK400H over HDMI.
- The monitor accepts writes but generally refuses DDC reads, so the app cannot reliably display the current OSD value.
- HDR, display-driver resets, resolution changes, and some exclusive full-screen applications may reset the software night filter.
- Exact color temperature requires measurement with a physical colorimeter.
- Super Resolution+, DFC, and some other firmware controls are not yet confirmed.

## Safety and disclaimer

This is an independent community project and is not affiliated with LG Electronics. Monitor-control behavior can vary by revision, connection type, GPU, and driver. Use experimental/raw commands at your own risk.

## Contributing

Bug reports and tested command mappings are welcome. When reporting an issue, include:

- Windows version;
- GPU and driver;
- HDMI or D-Sub connection;
- expected OSD value;
- value actually displayed by the monitor.

### 💖 Support My Work

<p align="center">
  <a href="https://buymeacoffee.com/pbzin">
    <img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="38" align="absmiddle">
  </a>
  <a href="https://github.com/sponsors/pbzin">
    <img src="https://img.shields.io/badge/Sponsor-💖-ea4aaa?style=for-the-badge&logo=githubsponsors&logoColor=white" alt="GitHub Sponsors" height="38" align="absmiddle">
  </a>
  <br><br>
  <img src="https://img.shields.io/badge/Pix-⚡-32BCAD?style=for-the-badge&logo=pix&logoColor=white" alt="Pix" height="30" align="absmiddle">
  <img src="https://raw.githubusercontent.com/pbzin/pbzin/main/assets/brasil-badge.png" alt="Brasil" height="30" align="absmiddle">
  <br>
  <code>5198a8b3-6b89-4475-aec1-5adcfcfd12cf</code>
  <br><br>
  <img src="https://img.shields.io/badge/Bitcoin-F7931A?style=for-the-badge&logo=bitcoin&logoColor=white" alt="Bitcoin" height="30" align="absmiddle">
  <br>
  <code>1GkpDZDHYov7WZLs54Nv19f2KUoZPcACs2</code>
  <br>
  <img src="https://raw.githubusercontent.com/pbzin/pbzin/main/assets/bitcoin-qr.png" width="150" alt="Bitcoin donation QR code">
  <br><br>
  <img src="https://img.shields.io/badge/Monero-FF6600?style=for-the-badge&logo=monero&logoColor=white" alt="Monero" height="30" align="absmiddle">
  <br>
  <code>45YtYmxUeXeFdokKPG1KWtMFLByS8nwmtiJjEiZ9LfbkNaSUCvyWWAx3VmtDKKkxPJFdQLSXxodRWMt7EBu5TmA3Qi9dgwT</code>
  <br>
  <img src="https://raw.githubusercontent.com/pbzin/pbzin/main/assets/monero-qr.png" width="150" alt="Monero donation QR code">
</p>
