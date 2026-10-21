using NAudio.CoreAudioApi;using NAudio.Midi;using NAudio.Wave;using TAR.SoundSystem.Audio;using TAR.SoundSystem.Models;
namespace TAR.SoundSystem.Services;
public sealed class AudioOutputInfo{public required string Id{get;init;}public required string Name{get;init;}public override string ToString()=>Name;}
public sealed class EngineService:IDisposable{
 MidiIn? input;WasapiOut? output;readonly MMDeviceEnumerator devices=new();readonly SoundFontSampleProvider synth=new();readonly MasteringSampleProvider mastering;readonly CompressorLufsSampleProvider dynamics;readonly Vst3HostChain vst;
 public ChannelState[] Channels{get;}=Enumerable.Range(1,16).Select(i=>new ChannelState{Channel=i}).ToArray();public event Action<uint>? MidiReceived;public event Action<string>? StatusChanged;
 public float Peak=>mastering.Peak;public double MomentaryLufs=>dynamics.MomentaryLufs;public double IntegratedLufs=>dynamics.IntegratedLufs;public float GainReductionDb=>dynamics.GainReductionDb;
 public EngineService(){mastering=new(synth);dynamics=new(mastering);vst=new(dynamics);foreach(var c in Channels)c.PropertyChanged+=(_,_)=>ApplyMixer();}
 public IReadOnlyList<string> MidiInputs=>Enumerable.Range(0,MidiIn.NumberOfDevices).Select(i=>MidiIn.DeviceInfo(i).ProductName).ToList();
 public IReadOnlyList<AudioOutputInfo> AudioOutputs=>devices.EnumerateAudioEndPoints(DataFlow.Render,DeviceState.Active).Select(d=>new AudioOutputInfo{Id=d.ID,Name=d.FriendlyName}).ToList();
 public void LoadSoundFont(string p){synth.Load(p);ApplyMixer();StatusChanged?.Invoke($"SoundFont: {Path.GetFileName(p)}");}
 public string LoadVst3(string p){if(output!=null)throw new InvalidOperationException("หยุดระบบก่อนเพิ่ม VST3");var n=vst.LoadEffect(p);StatusChanged?.Invoke($"VST3: {n}");return n;}
 public void Start(int midi,string outputId){Stop();var dev=devices.GetDevice(outputId);output=new WasapiOut(dev,AudioClientShareMode.Shared,true,50);output.Init(vst);output.Play();input=new(midi);input.MessageReceived+=OnMidi;input.Start();StatusChanged?.Invoke("LIVE: Compressor + LUFS + VST3 ทำงาน");}
 void OnMidi(object? s,MidiInMessageEventArgs e){uint m=unchecked((uint)e.RawMessage);int st=(int)(m&255),ch=st&15,d1=(int)((m>>8)&127),d2=(int)((m>>16)&127);var c=Channels[ch];bool any=Channels.Any(x=>x.Solo);if((st&240)==0x90){if(c.Mute||(any&&!c.Solo))d2=0;d1=Math.Clamp(d1+c.Transpose,0,127);m=(uint)(st|(d1<<8)|(d2<<16));}synth.Send(m);MidiReceived?.Invoke(m);}
 public void ApplyMixer(){for(int i=0;i<16;i++){var c=Channels[i];bool any=Channels.Any(x=>x.Solo);int v=c.Mute||(any&&!c.Solo)?0:c.Volume;synth.Send((uint)((0xB0|i)|(7<<8)|(v<<16)));synth.Send((uint)((0xB0|i)|(10<<8)|(c.Pan<<16)));synth.Send((uint)((0xC0|i)|(c.Program<<8)));}}
 public void SetEq(float l,float m,float h)=>mastering.SetEq(l,m,h);public void SetMaster(float d)=>mastering.SetMaster(d);
 public void SetCompressor(bool on,float th,float ratio,float attack,float release,float makeup){dynamics.Enabled=on;dynamics.ThresholdDb=th;dynamics.Ratio=ratio;dynamics.AttackMs=attack;dynamics.ReleaseMs=release;dynamics.MakeupDb=makeup;}
 public void ResetLufs()=>dynamics.ResetLoudness();public void Panic()=>synth.AllNotesOff();public void Stop(){if(input!=null){try{input.Stop();}catch{}input.Dispose();input=null;}if(output!=null){output.Stop();output.Dispose();output=null;}}
 public void Dispose(){Stop();vst.Dispose();synth.Dispose();devices.Dispose();}
}
