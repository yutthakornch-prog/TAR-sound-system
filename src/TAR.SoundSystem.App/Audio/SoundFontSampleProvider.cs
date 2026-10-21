using MeltySynth;
using NAudio.Wave;
namespace TAR.SoundSystem.Audio;

public sealed class SoundFontSampleProvider : ISampleProvider, IDisposable
{
    private readonly object gate = new();
    private Synthesizer? synth;
    private readonly float[] left;
    private readonly float[] right;
    public WaveFormat WaveFormat { get; }
    public bool IsLoaded => synth is not null;

    public SoundFontSampleProvider(int sampleRate = 48000, int blockSize = 2048)
    {
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
        left = new float[blockSize]; right = new float[blockSize];
    }

    public void Load(string path)
    {
        var sf = new SoundFont(path);
        var settings = new SynthesizerSettings(WaveFormat.SampleRate) { MaximumPolyphony = 256 };
        lock (gate) synth = new Synthesizer(sf, settings);
    }

    public void Send(uint message)
    {
        lock (gate)
        {
            if (synth is null) return;
            var status = (int)(message & 0xFF);
            var data1 = (int)((message >> 8) & 0x7F);
            var data2 = (int)((message >> 16) & 0x7F);
            var channel = status & 0x0F;
            switch (status & 0xF0)
            {
                case 0x80: synth.NoteOff(channel, data1); break;
                case 0x90: if (data2 == 0) synth.NoteOff(channel, data1); else synth.NoteOn(channel, data1, data2); break;
                case 0xB0: synth.ProcessMidiMessage(channel, 0xB0, data1, data2); break;
                case 0xC0: synth.ProcessMidiMessage(channel, 0xC0, data1, 0); break;
                case 0xE0: synth.ProcessMidiMessage(channel, 0xE0, data1, data2); break;
            }
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, offset, count);
        var frames = count / 2;
        lock (gate)
        {
            if (synth is null) return count;
            var done = 0;
            while (done < frames)
            {
                var n = Math.Min(left.Length, frames - done);
                synth.Render(left.AsSpan(0, n), right.AsSpan(0, n));
                for (var i = 0; i < n; i++) { buffer[offset + (done + i) * 2] = left[i]; buffer[offset + (done + i) * 2 + 1] = right[i]; }
                done += n;
            }
        }
        return count;
    }

    public void AllNotesOff() { lock (gate) for (var ch = 0; ch < 16; ch++) synth?.ProcessMidiMessage(ch, 0xB0, 123, 0); }
    public void Dispose() { synth = null; }
}
