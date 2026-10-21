using NAudio.Dsp;
using NAudio.Wave;
namespace TAR.SoundSystem.Audio;

public sealed class MasteringSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private BiQuadFilter[] low = [], mid = [], high = [];
    private float preGain = 1f;
    public WaveFormat WaveFormat => source.WaveFormat;
    public float LowDb { get; private set; }
    public float MidDb { get; private set; }
    public float HighDb { get; private set; }
    public float MasterDb { get; private set; }
    public float Peak { get; private set; }

    public MasteringSampleProvider(ISampleProvider source) { this.source = source; Rebuild(); }
    public void SetEq(float lowDb, float midDb, float highDb) { LowDb=lowDb; MidDb=midDb; HighDb=highDb; Rebuild(); }
    public void SetMaster(float db) { MasterDb=db; preGain=(float)Math.Pow(10, db/20.0); }
    private void Rebuild()
    {
        int sr=WaveFormat.SampleRate, ch=WaveFormat.Channels;
        low=Enumerable.Range(0,ch).Select(_=>BiQuadFilter.LowShelf(sr,120,0.8f,LowDb)).ToArray();
        mid=Enumerable.Range(0,ch).Select(_=>BiQuadFilter.PeakingEQ(sr,1200,1.0f,MidDb)).ToArray();
        high=Enumerable.Range(0,ch).Select(_=>BiQuadFilter.HighShelf(sr,8000,0.8f,HighDb)).ToArray();
    }
    public int Read(float[] buffer,int offset,int count)
    {
        var read=source.Read(buffer,offset,count); float peak=0;
        for(int i=0;i<read;i++)
        {
            int c=i%WaveFormat.Channels;
            float x=high[c].Transform(mid[c].Transform(low[c].Transform(buffer[offset+i])))*preGain;
            // soft limiter, keeps the first milestone safe from hard digital clipping
            x=(float)Math.Tanh(x*1.15)/1.05f;
            buffer[offset+i]=x; peak=Math.Max(peak,Math.Abs(x));
        }
        Peak=peak; return read;
    }
}
