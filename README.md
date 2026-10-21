# TAR-sound-system - Milestone 3

Adds to Milestone 2:

- Real-time compressor: threshold, ratio, attack, release, makeup, bypass and gain-reduction display
- LUFS display: 400 ms momentary K-weighted value and running integrated estimate, with reset
- Sound card output selector using active Windows WASAPI render endpoints
- VST3 effect host using NAudio.Vst3; supports compatible stereo input/output audio effects
- VST3 chain list and safe rule: stop playback before adding a plug-in

Signal chain:

`eXtreme Karaoke -> loopMIDI -> SoundFont -> channel mixer -> EQ -> compressor -> LUFS meter -> VST3 effects -> soft limiter/output`

## Build

Open `TAR-sound-system.sln` in Visual Studio 2022 with .NET 9 and Desktop Development. Restore NuGet packages, select x64, then build.

## Limits

- The LUFS integrated display is a real-time running estimate without the absolute and relative gating required for a fully certified EBU R128 meter.
- VST3 hosting is limited to compatible Windows x64 stereo audio effects. Instruments, sidechains and multi-bus plug-ins are not enabled in this milestone.
- Stop LIVE playback before adding a VST3 plug-in.
- Test third-party plug-ins carefully; a native plug-in can crash the host process.
