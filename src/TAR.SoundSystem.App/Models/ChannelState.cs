using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace TAR.SoundSystem.Models;
public sealed class ChannelState:INotifyPropertyChanged{
 int volume=100,pan=64,program,transpose;bool mute,solo;
 public int Channel{get;init;} public string Name=>Channel==10?"Drums":$"MIDI {Channel}";
 public int Volume{get=>volume;set=>Set(ref volume,Math.Clamp(value,0,127));}
 public int Pan{get=>pan;set=>Set(ref pan,Math.Clamp(value,0,127));}
 public int Program{get=>program;set=>Set(ref program,Math.Clamp(value,0,127));}
 public int Transpose{get=>transpose;set=>Set(ref transpose,Math.Clamp(value,-24,24));}
 public bool Mute{get=>mute;set=>Set(ref mute,value);} public bool Solo{get=>solo;set=>Set(ref solo,value);}
 public event PropertyChangedEventHandler? PropertyChanged;
 void Set<T>(ref T f,T v,[CallerMemberName]string? n=null){if(EqualityComparer<T>.Default.Equals(f,v))return;f=v;PropertyChanged?.Invoke(this,new(n));}
}