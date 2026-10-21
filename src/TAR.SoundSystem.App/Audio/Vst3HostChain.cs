using NAudio.Vst3;
using NAudio.Wave;
namespace TAR.SoundSystem.Audio;

public sealed class Vst3HostChain : ISampleProvider, IDisposable
{
    private ISampleProvider current;
    private readonly List<Vst3Module> modules=[];
    private readonly List<Vst3Plugin> plugins=[];
    public WaveFormat WaveFormat=>current.WaveFormat;
    public IReadOnlyList<string> LoadedNames=>plugins.Select(p=>p.ClassInfo.Name).ToList();
    public Vst3HostChain(ISampleProvider source){current=source;}
    public string LoadEffect(string path)
    {
        var module=Vst3Module.Load(path);
        try{
            Exception? last=null;
            foreach(var info in module.GetClasses())try{
                var plugin=module.CreatePlugin(info,WaveFormat.SampleRate,2048);
                current=new Vst3EffectSampleProvider(current,plugin){RenderTail=false};modules.Add(module);plugins.Add(plugin);return info.Name;
            }catch(Exception ex){last=ex;}
            throw new InvalidOperationException("ไม่พบ VST3 stereo audio effect ที่รองรับ",last);
        }catch{module.Dispose();throw;}
    }
    public int Read(float[] buffer,int offset,int count)=>current.Read(buffer,offset,count);
    public void Dispose(){for(int i=plugins.Count-1;i>=0;i--)plugins[i].Dispose();for(int i=modules.Count-1;i>=0;i--)modules[i].Dispose();}
}
