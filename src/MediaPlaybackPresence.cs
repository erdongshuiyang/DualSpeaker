using System;
using System.IO;
using System.Threading;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace DualSpeaker {
 // Some Bluetooth receivers stop rendering when the last Windows media session
 // is paused, even while WASAPI still carries another application's audio.
 public sealed class MediaPlaybackPresence:IDisposable {
  MediaPlayer player;MediaSource source;IRandomAccessStream stream;
  volatile string error="";volatile bool stopping;public bool Active{get;private set;}public event Action<bool> MuteRequested;
  public string Error{get{return error;}}
  internal static byte[] SilentWave(){using(var output=new MemoryStream())using(var writer=new BinaryWriter(output)){
   const int bytes=48000*2*2;
   writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+bytes);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)2);writer.Write(48000);writer.Write(bytes);writer.Write((short)4);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);writer.Write(new byte[bytes]);return output.ToArray();
  }}
  public void Start(){if(Active)return;stopping=false;error="";try{
   stream=new InMemoryRandomAccessStream();using(var writer=new DataWriter(stream)){writer.WriteBytes(SilentWave());var write=writer.StoreAsync();for(int i=0;write.Status==AsyncStatus.Started&&i<60;i++)Thread.Sleep(50);if(write.Status!=AsyncStatus.Completed)throw new Exception("静音媒体数据初始化失败。");write.GetResults();writer.DetachStream();}stream.Seek(0);source=MediaSource.CreateFromStream(stream,"audio/wav");
   player=new MediaPlayer();player.CommandManager.IsEnabled=false;player.IsLoopingEnabled=true;player.Volume=0;player.MediaFailed+=(s,e)=>{if(stopping)return;error=e.ErrorMessage;Active=false;Files.Log("蓝牙媒体状态维持失败: "+error);};
   var controls=player.SystemMediaTransportControls;controls.IsEnabled=true;controls.IsPlayEnabled=true;controls.IsPauseEnabled=true;controls.IsStopEnabled=false;
   controls.ButtonPressed+=(s,e)=>{var handler=MuteRequested;if(handler==null)return;if(e.Button==SystemMediaTransportControlsButton.Pause)handler(true);else if(e.Button==SystemMediaTransportControlsButton.Play)handler(false);};
   controls.DisplayUpdater.Type=MediaPlaybackType.Music;controls.DisplayUpdater.MusicProperties.Title="双音响播放";controls.DisplayUpdater.MusicProperties.Artist="DualSpeaker";controls.DisplayUpdater.Update();
   player.Source=source;player.Play();controls.PlaybackStatus=MediaPlaybackStatus.Playing;
   for(int i=0;i<60;i++){if(!String.IsNullOrEmpty(error))throw new Exception(error);if(player.PlaybackSession.PlaybackState==MediaPlaybackState.Playing){Active=true;Files.Log("蓝牙媒体播放状态已启用 | 内存静音流，不增加音源延迟");return;}Thread.Sleep(50);}
   throw new Exception("静音媒体会话启动超时。");
  }catch{Dispose();throw;}}
  public void Dispose(){stopping=true;Active=false;var p=player;player=null;if(p!=null){try{p.Pause();var controls=p.SystemMediaTransportControls;controls.PlaybackStatus=MediaPlaybackStatus.Stopped;controls.IsEnabled=false;}catch{}try{p.Dispose();}catch{}}var s=source;source=null;if(s!=null)try{s.Dispose();}catch{}var data=stream;stream=null;if(data!=null)try{data.Dispose();}catch{}}
 }
}
