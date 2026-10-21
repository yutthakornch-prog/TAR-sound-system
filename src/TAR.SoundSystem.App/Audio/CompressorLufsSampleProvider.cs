using NAudio.Dsp;
using NAudio.Wave;
namespace TAR.SoundSystem.Audio;

// Real-time stereo compressor plus EBU-style K-weighted loudness display.
// Momentary uses a 400 ms energy window; Integrated is an ungated running value in this milestone.
public sealed class CompressorLufsSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly Queue<double> momentaryBlocks = new();
    private readonly BiQuadFilter[] pre, rlb;
    private double momentarySum, integratedSum; private long integratedSamples;
    private float env;
    public WaveFormat WaveFormat => source.WaveFormat;
    public float ThresholdDb { get; set; } = -18;
    public float Ratio { get; set; } = 4;
    public float AttackMs { get; set; } = 10;
    public float ReleaseMs { get; set; } = 120;
    public float MakeupDb { get; set; } = 3;
    public bool Enabled { get; set; } = true;
    public double MomentaryLufs { get; private set; } = -70;
    public double IntegratedLufs { get; private set; } = -70;
    public float GainReductionDb { get; private set; }
    public CompressorLufsSampleProvider(ISampleProvider source)
    {
        this.source=source; int sr=WaveFormat.SampleRate, ch=WaveFormat.Channels;
        pre=Enumerable.Range(0,ch).Select(_=>BiQuadFilter.HighShelf(sr,1681.974f,0.707f,4f)).ToArray();
        rlb=Enumerable.Range(0,ch).Select(_=>BiQuadFilter.HighPassFilter(sr,38.135f,0.5f)).ToArray();
    }
    public void ResetLoudness(){momentaryBlocks.Clear();momentarySum=integratedSum=0;integratedSamples=0;MomentaryLufs=IntegratedLufs=-70;}
    public int Read(float[] buffer,int offset,int count)
    {
        int read=source.Read(buffer,offset,count); if(read==0)return 0;
        float attack=(float)Math.Exp(-1.0/(WaveFormat.SampleRate*Math.Max(1,AttackMs)/1000.0));
        float release=(float)Math.Exp(-1.0/(WaveFormat.SampleRate*Math.Max(1,ReleaseMs)/1000.0));
        float makeup=(float)Math.Pow(10,MakeupDb/20.0); double energy=0;
        for(int i=0;i<read;i++){
            float x=buffer[offset+i], level=Math.Abs(x); env=level>env?attack*env+(1-attack)*level:release*env+(1-release)*level;
            float db=20*(float)Math.Log10(Math.Max(env,1e-9f)), gr=0;
            if(Enabled&&db>ThresholdDb)gr=(ThresholdDb+(db-ThresholdDb)/Math.Max(1,Ratio))-db;
            GainReductionDb=-gr; x*=Enabled?(float)Math.Pow(10,gr/20.0)*makeup:1f; buffer[offset+i]=x;
            int c=i%WaveFormat.Channels; float k=rlb[c].Transform(pre[c].Transform(x));energy+=k*k;
        }
        double mean=energy/read; int maxBlocks=Math.Max(1,(int)Math.Ceiling(0.4*WaveFormat.SampleRate*WaveFormat.Channels/read));
        momentaryBlocks.Enqueue(mean);momentarySum+=mean;while(momentaryBlocks.Count>maxBlocks)momentarySum-=momentaryBlocks.Dequeue();
        MomentaryLufs=Math.Max(-70,-0.691+10*Math.Log10(Math.Max(momentarySum/momentaryBlocks.Count,1e-12)));
        integratedSum+=energy;integratedSamples+=read;IntegratedLufs=Math.Max(-70,-0.691+10*Math.Log10(Math.Max(integratedSum/integratedSamples,1e-12)));
        return read;
    }
}
