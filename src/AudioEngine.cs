using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace DualSpeaker {
 public class DeviceChoice {
  public string Id,Name;
  public override string ToString(){return Name;}
 }
 public class Settings {
  public string LeftId,RightId,BusId,MicId;
  public double LeftDelayMs,RightDelayMs;
  public float LeftGain=0.8f,RightGain=0.8f;
  public float MasterGain=1;
  public bool MasterMuted=false;
  public int VolumeStepPercent=8;
  public bool ShowVolumeOverlay=true;
  public bool StereoSplit=false;
  public bool Calibrated=false;
  public bool CalibrateOnStart=true;
  public int BufferMs=120;
  public int OutputBufferMs=60;
  public double LastResidualMs;
  public string CalibrationTime="";
 }
 public class RestoreState {
  public string ConsoleId,MultimediaId,CommunicationsId,BusId;
  public float Volume; public bool Mute;
 }
 public static class Files {
  public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
  public static readonly string Config=Path.Combine(Root,"settings.json");
  public static readonly string Recovery=Path.Combine(Root,"restore-audio.json");
  static object gate=new object();
  public static void Log(string s){lock(gate){string line=DateTime.Now.ToString("HH:mm:ss.fff")+" "+s;try{File.AppendAllText(Path.Combine(Root,"application.log"),line+Environment.NewLine,System.Text.Encoding.UTF8);}catch{}try{Console.WriteLine(line);}catch{}}}
  public static void Save(string path,object value){lock(gate){File.WriteAllText(path,new JavaScriptSerializer().Serialize(value),System.Text.Encoding.UTF8);}}
  public static T Load<T>(string path) where T:class {try{return new JavaScriptSerializer().Deserialize<T>(File.ReadAllText(path,System.Text.Encoding.UTF8));}catch{return null;}}
 }
 public static class Devices {
  public static DeviceChoice[] List(DataFlow flow){using(var e=new MMDeviceEnumerator()){return e.EnumerateAudioEndPoints(flow,DeviceState.Active).Select(d=>new DeviceChoice{Id=d.ID,Name=d.FriendlyName}).ToArray();}}
  public static MMDevice Open(string id){using(var e=new MMDeviceEnumerator()){return e.GetDevice(id);}}
  public static string Default(Role role){using(var e=new MMDeviceEnumerator()){return e.GetDefaultAudioEndpoint(DataFlow.Render,role).ID;}}
  public static Settings Initial(){Settings s=Files.Load<Settings>(Files.Config)??new Settings();var ds=List(DataFlow.Render);var ms=List(DataFlow.Capture);
   if(!ds.Any(d=>d.Id==s.LeftId))s.LeftId=ds.Where(d=>d.Name.Contains("CCH")).Select(d=>d.Id).FirstOrDefault();
   if(!ds.Any(d=>d.Id==s.RightId))s.RightId=ds.Where(d=>d.Name.Contains("小爱")).Select(d=>d.Id).FirstOrDefault();
   if(!ds.Any(d=>d.Id==s.BusId))s.BusId=ds.Where(d=>d.Name.Contains("Realtek")).Select(d=>d.Id).FirstOrDefault();
   if(!ms.Any(d=>d.Id==s.MicId))s.MicId=ms.Select(d=>d.Id).FirstOrDefault();return s;
  }
 }
 public static class DefaultAudio {
  [ComImport,Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]class PolicyClient{}
  [ComImport,Guid("F8679F50-850A-41CF-9C72-430F290290C8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IPolicy {
   [PreserveSig]int GetMixFormat(string id,out IntPtr format);
   [PreserveSig]int GetDeviceFormat(string id,int mode,out IntPtr format);
   [PreserveSig]int ResetDeviceFormat(string id);
   [PreserveSig]int SetDeviceFormat(string id,IntPtr endpoint,IntPtr mix);
   [PreserveSig]int GetProcessingPeriod(string id,int mode,IntPtr period,IntPtr minimum);
   [PreserveSig]int SetProcessingPeriod(string id,IntPtr period);
   [PreserveSig]int GetShareMode(string id,IntPtr mode);
   [PreserveSig]int SetShareMode(string id,IntPtr mode);
   [PreserveSig]int GetPropertyValue(string id,IntPtr key,IntPtr value);
   [PreserveSig]int SetPropertyValue(string id,IntPtr key,IntPtr value);
   [PreserveSig]int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)]string id,int role);
   [PreserveSig]int SetEndpointVisibility(string id,int visible);
  }
  public static void Set(string id,Role role){var p=(IPolicy)new PolicyClient();try{Marshal.ThrowExceptionForHR(p.SetDefaultEndpoint(id,(int)role));}finally{Marshal.ReleaseComObject(p);}}
  public static bool Recover(){var state=Files.Load<RestoreState>(Files.Recovery);if(state==null)return false;Restore(state);return true;}
  public static void Restore(RestoreState state){
   bool ok=true;
   foreach(var pair in new[]{new{Id=state.ConsoleId,Role=Role.Console},new{Id=state.MultimediaId,Role=Role.Multimedia},new{Id=state.CommunicationsId,Role=Role.Communications}}){try{if(!String.IsNullOrEmpty(pair.Id))Set(pair.Id,pair.Role);}catch(Exception ex){ok=false;Files.Log("恢复默认设备失败: "+ex.Message);}}
   try{using(var b=Devices.Open(state.BusId)){b.AudioEndpointVolume.MasterVolumeLevelScalar=state.Volume;b.AudioEndpointVolume.Mute=state.Mute;}}catch(Exception ex){ok=false;Files.Log("恢复声卡音量失败: "+ex.Message);}
   if(ok && File.Exists(Files.Recovery))File.Delete(Files.Recovery);
  }
 }
 public class AudioRing {
  public readonly int Rate;public readonly int Capacity;
  float[] samples;object gate=new object();long written;
  public AudioRing(int rate){Rate=rate;Capacity=rate*12;samples=new float[Capacity*2];}
  public long Written{get{lock(gate)return written;}}
  public void Write(float[] data,int frames){lock(gate){for(int i=0;i<frames;i++){int k=(int)((written+i)%Capacity)*2;samples[k]=data[2*i];samples[k+1]=data[2*i+1];}written+=frames;}}
  public void ReadAt(double start,double step,float[] result,int frames,out int unavailable){unavailable=0;lock(gate){long end=written;for(int i=0;i<frames;i++){double pos=start+i*step;long p=(long)Math.Floor(pos);if(p<0||p+1>=end||p<end-Capacity){result[2*i]=result[2*i+1]=0;if(p>=0)unavailable++;continue;}int a=(int)(p%Capacity)*2,b=(int)((p+1)%Capacity)*2;float f=(float)(pos-p);result[2*i]=samples[a]+(samples[b]-samples[a])*f;result[2*i+1]=samples[a+1]+(samples[b+1]-samples[a+1])*f;}}}
 }
 public static class AudioConvert {
  public static float[] Decode(byte[] bytes,int count,WaveFormat f,bool mono){int frames=count/f.BlockAlign;int channels=f.Channels;int stride=f.BitsPerSample/8;bool ieee=f.Encoding==WaveFormatEncoding.IeeeFloat || (f is WaveFormatExtensible && ((WaveFormatExtensible)f).SubFormat.ToString().StartsWith("00000003"));var a=new float[frames*(mono?1:2)];for(int i=0;i<frames;i++){float sum=0,first=0,second=0;for(int ch=0;ch<channels;ch++){int k=i*f.BlockAlign+ch*stride;float x;
   if(ieee&&stride==4)x=BitConverter.ToSingle(bytes,k);else if(stride==2)x=BitConverter.ToInt16(bytes,k)/32768f;else if(stride==3){int v=bytes[k]|(bytes[k+1]<<8)|(bytes[k+2]<<16);if((v&0x800000)!=0)v|=unchecked((int)0xff000000);x=v/8388608f;}else if(stride==4)x=BitConverter.ToInt32(bytes,k)/2147483648f;else if(stride==1)x=(bytes[k]-128)/128f;else throw new NotSupportedException("不支持的音频格式: "+f);
   if(float.IsNaN(x)||float.IsInfinity(x))x=0;if(ch==0)first=x;if(ch==1)second=x;sum+=x;}
   if(mono)a[i]=sum/channels;else{a[2*i]=first;a[2*i+1]=channels>1?second:first;}}return a;}
 }
 public static class TestSignal {
  public static readonly double[] Offsets={0,.73,1.61,2.67,3.91,5.23};
  public const double Length=.09;
  public static double Chirp(bool left,double t){if(t<0||t>=Length)return 0;double f0=left?850:2300,f1=left?1850:3700;double w=Math.Sin(Math.PI*t/Length);return Math.Sin(2*Math.PI*(f0*t+.5*(f1-f0)*t*t/Length))*w*w;}
  public static float Value(bool left,double t){double value=0;foreach(double offset in Offsets){double x=t-offset;if(x>=0&&x<Length)value+=Chirp(left,x);}return (float)(0.16*value);}
 }
 public class OutputProvider:ISampleProvider {
  public WaveFormat WaveFormat{get;private set;}AudioRing ring;bool left;Settings settings;object gate=new object();double position=double.NaN,lastDelay;float[] scratch=new float[4096];
  public long MissingFrames;public long FramesRead;public float Peak;public double DriftPpm;public double LagMs;
  public double CalibrationStart=double.NaN;public bool CalibrationActive=false;
  public OutputProvider(AudioRing r,int outputRate,bool isLeft,Settings s){ring=r;left=isLeft;settings=s;WaveFormat=WaveFormat.CreateIeeeFloatWaveFormat(outputRate,2);}
  public void ResetTimeline(){lock(gate){position=double.NaN;}}
  public int Read(float[] buffer,int offset,int count){lock(gate){int frames=count/2;if(scratch.Length<frames*2)scratch=new float[frames*2];double delay=(left?settings.LeftDelayMs:settings.RightDelayMs);double target=(Math.Max(40,settings.BufferMs)+delay)*ring.Rate/1000.0;long written=ring.Written;
   if(double.IsNaN(position)){position=written-target;lastDelay=delay;}
   if(delay!=lastDelay){position-=(delay-lastDelay)*ring.Rate/1000.0;lastDelay=delay;}
   double lag=written-position,error=lag-target;LagMs=lag*1000/ring.Rate;
   double correction=Math.Max(-.002,Math.Min(.002,error/ring.Rate*.015));DriftPpm=correction*1000000;
   if(Math.Abs(error)>ring.Rate*2){position=written-target;Files.Log((left?"左":"右")+"输出缓冲重新定位");}
   double step=(double)ring.Rate/WaveFormat.SampleRate*(1+correction);int missing;ring.ReadAt(position,step,scratch,frames,out missing);MissingFrames+=missing;float gain=(left?settings.LeftGain:settings.RightGain)*(settings.MasterMuted?0:Math.Max(0,Math.Min(1,settings.MasterGain)));float peak=0;
   for(int i=0;i<frames;i++){float a=scratch[2*i],b=scratch[2*i+1];if(settings.StereoSplit){a=b=left?a:b;}if(CalibrationActive){a=b=TestSignal.Value(left,(position+i*step-CalibrationStart)/ring.Rate);}a*=gain;b*=gain;a=Math.Max(-1,Math.Min(1,a));b=Math.Max(-1,Math.Min(1,b));buffer[offset+2*i]=a;buffer[offset+2*i+1]=b;peak=Math.Max(peak,Math.Max(Math.Abs(a),Math.Abs(b)));}
   Peak=peak;FramesRead+=frames;position+=frames*step;return count;
  }}
 }
 class Silence:ISampleProvider {public WaveFormat WaveFormat{get;private set;}public Silence(int rate){WaveFormat=WaveFormat.CreateIeeeFloatWaveFormat(rate,2);}public int Read(float[] b,int o,int c){Array.Clear(b,o,c);return c;}}
 class CalibrationSource:ISampleProvider {long frame;public WaveFormat WaveFormat{get;private set;}public CalibrationSource(int rate){WaveFormat=WaveFormat.CreateIeeeFloatWaveFormat(rate,2);}public int Read(float[] b,int o,int c){for(int i=0;i<c/2;i++){double t=frame/(double)WaveFormat.SampleRate-1;b[o+2*i]=TestSignal.Value(true,t);b[o+2*i+1]=TestSignal.Value(false,t);frame++;}return c;}}
 public class AudioEngine:IDisposable {
  public Settings Settings;public OutputProvider Left,Right;public AudioRing Ring;public bool Running;public float InputPeak;public long InputFrames;public string Error="";public bool SpeakerMuted;public int MuteCorrections;
  MMDevice bus,leftDevice,rightDevice;ProcessAudioCapture capture;WasapiOut leftOut,rightOut,keepAlive;MediaPlaybackPresence mediaPresence;RestoreState restore;bool stopping;object stopGate=new object();volatile DemoMusic demoSource;Thread muteGuard;ManualResetEvent muteStop;
  public bool MediaPlaybackActive{get{return mediaPresence!=null&&mediaPresence.Active;}}
  public event Action<bool> MediaMuteRequested;
  public AudioEngine(Settings settings){Settings=settings;}
  public void Start(bool routeDefault){if(Running)return;if(String.IsNullOrEmpty(Settings.BusId)||String.IsNullOrEmpty(Settings.LeftId)||String.IsNullOrEmpty(Settings.RightId))throw new Exception("请选择电脑内置声卡和两个音响。");if(Settings.LeftId==Settings.RightId||Settings.BusId==Settings.LeftId||Settings.BusId==Settings.RightId)throw new Exception("音源声卡、左音响、右音响必须是三个不同设备。");
   try{bus=Devices.Open(Settings.BusId);leftDevice=Devices.Open(Settings.LeftId);rightDevice=Devices.Open(Settings.RightId);
    stopping=false;
    restore=new RestoreState{ConsoleId=Devices.Default(Role.Console),MultimediaId=Devices.Default(Role.Multimedia),CommunicationsId=Devices.Default(Role.Communications),BusId=Settings.BusId,Volume=bus.AudioEndpointVolume.MasterVolumeLevelScalar,Mute=bus.AudioEndpointVolume.Mute};Files.Save(Files.Recovery,restore);
    bus.AudioEndpointVolume.Mute=true;bus.AudioEndpointVolume.MasterVolumeLevelScalar=0;
    StartMuteGuard();
    capture=new ProcessAudioCapture();Ring=new AudioRing(capture.WaveFormat.SampleRate);
    capture.DataAvailable+=(s,e)=>{try{var a=AudioConvert.Decode(e.Buffer,e.BytesRecorded,capture.WaveFormat,false);var demo=demoSource;if(demo!=null)demo.Read(a,0,a.Length);float p=0;for(int i=0;i<a.Length;i++)p=Math.Max(p,Math.Abs(a[i]));InputPeak=p;Ring.Write(a,a.Length/2);InputFrames+=a.Length/2;}catch(Exception ex){Error=ex.Message;}};
    capture.RecordingStopped+=(s,e)=>{if(!stopping&&e.Exception!=null){Error="音源采集停止: "+e.Exception.Message;Files.Log(Error);}};
    keepAlive=new WasapiOut(bus,AudioClientShareMode.Shared,false,60);keepAlive.Init(new SampleToWaveProvider(new Silence(Ring.Rate)));
    capture.StartRecording();keepAlive.Play();Thread.Sleep(180);
    Left=new OutputProvider(Ring,leftDevice.AudioClient.MixFormat.SampleRate,true,Settings);Right=new OutputProvider(Ring,rightDevice.AudioClient.MixFormat.SampleRate,false,Settings);
    int renderBuffer=Math.Max(40,Math.Min(200,Settings.OutputBufferMs));
    leftOut=new WasapiOut(leftDevice,AudioClientShareMode.Shared,true,renderBuffer);rightOut=new WasapiOut(rightDevice,AudioClientShareMode.Shared,true,renderBuffer);
    leftOut.Init(new SampleToWaveProvider(Left));rightOut.Init(new SampleToWaveProvider(Right));
    leftOut.PlaybackStopped+=(s,e)=>{if(!stopping&&e.Exception!=null){Error="左音响断开: "+e.Exception.Message;Files.Log(Error);}};rightOut.PlaybackStopped+=(s,e)=>{if(!stopping&&e.Exception!=null){Error="右音响断开: "+e.Exception.Message;Files.Log(Error);}};
    leftOut.Play();rightOut.Play();Thread.Sleep(800);Left.ResetTimeline();Right.ResetTimeline();Running=true;if(routeDefault){DefaultAudio.Set(Settings.BusId,Role.Console);DefaultAudio.Set(Settings.BusId,Role.Multimedia);}
    mediaPresence=new MediaPlaybackPresence();mediaPresence.MuteRequested+=muted=>{var handler=MediaMuteRequested;if(handler!=null)handler(muted);else Settings.MasterMuted=muted;};try{mediaPresence.Start();}catch(Exception ex){Files.Log("蓝牙媒体状态兼容未启用: "+ex.Message);}
    Files.Log("双音响已启动 | source="+bus.FriendlyName+" | left="+leftDevice.FriendlyName+" | right="+rightDevice.FriendlyName+" | mode="+(Settings.StereoSplit?"左右声道":"双音响立体声")+" | sourceBuffer="+Settings.BufferMs+" ms | outputRequest="+renderBuffer+" ms | 静音保护已启用");
   }catch{Stop();throw;}
  }
  public void EnableCalibration(){double start=Ring.Written+Ring.Rate;Left.CalibrationStart=start;Right.CalibrationStart=start;Left.CalibrationActive=true;Right.CalibrationActive=true;}
  public void BeginDemo(){demoSource=new DemoMusic();}
  public void EndDemo(){demoSource=null;}
  void StartMuteGuard(){muteStop=new ManualResetEvent(false);muteGuard=new Thread(()=>{MMDevice endpoint=null;try{endpoint=Devices.Open(Settings.BusId);while(!muteStop.WaitOne(75)){try{var volume=endpoint.AudioEndpointVolume;if(!volume.Mute||volume.MasterVolumeLevelScalar>.0001f){volume.Mute=true;volume.MasterVolumeLevelScalar=0;MuteCorrections++;Files.Log("静音保护：内置扬声器被解除静音或调高音量，已恢复静音。");}SpeakerMuted=volume.Mute&&volume.MasterVolumeLevelScalar<.0001f;}catch(Exception ex){Error="内置扬声器静音保护失败: "+ex.Message;Files.Log(Error);break;}}}catch(Exception ex){Error="内置扬声器静音保护失败: "+ex.Message;}finally{if(endpoint!=null)endpoint.Dispose();}}){IsBackground=true,Name="Internal speaker mute guard"};muteGuard.SetApartmentState(ApartmentState.MTA);muteGuard.Start();}
  void StopMuteGuard(){if(muteStop!=null)muteStop.Set();if(muteGuard!=null){muteGuard.Join();muteGuard=null;}if(muteStop!=null){muteStop.Dispose();muteStop=null;}SpeakerMuted=false;}
  public void DisableCalibration(){if(Left!=null)Left.CalibrationActive=false;if(Right!=null)Right.CalibrationActive=false;}
  public void Stop(){lock(stopGate){stopping=true;DisableCalibration();foreach(var o in new[]{leftOut,rightOut,keepAlive})if(o!=null){try{o.Stop();o.Dispose();}catch{}}leftOut=rightOut=keepAlive=null;if(capture!=null){try{capture.StopRecording();capture.Dispose();}catch{}capture=null;}if(mediaPresence!=null){mediaPresence.Dispose();mediaPresence=null;}StopMuteGuard();if(restore!=null){DefaultAudio.Restore(restore);restore=null;}foreach(var d in new[]{bus,leftDevice,rightDevice})if(d!=null){try{d.Dispose();}catch{}}bus=leftDevice=rightDevice=null;Running=false;Files.Log("双音响已停止，恢复原来的系统输出与内置声卡音量。");}}
  public void Dispose(){Stop();}
 }
}
