using System;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using NAudio.CoreAudioApi;
namespace DualSpeaker {
 public static class CommandLine {
  public static int Execute(string[] args){Console.OutputEncoding=Encoding.UTF8;try{
   string action=args.Length>0?args[0]:"--devices";
   if(action=="--selftest"){Calibration.SelfTest();return 0;}
   if(action=="--volume-selftest"){SignalChecks.MasterSelfTest();return 0;}
   if(action=="--matrix"){SignalChecks.Matrix();return 0;}
   if(action=="--process-probe"){SignalChecks.ProcessProbe();return 0;}
   if(action=="--regression"){SignalChecks.Regression();return 0;}
   if(action=="--source-tone"){SignalChecks.Source(false);return 0;}
   if(action=="--source-demo"){SignalChecks.Source(true);return 0;}
   if(action=="--fxprobe"){SignalChecks.FxProbe();return 0;}
   if(action=="--fxtest"){EndpointEffects.Test();return 0;}
   if(action=="--devices"){foreach(var flow in new[]{DataFlow.Render,DataFlow.Capture})foreach(var d in Devices.List(flow))Console.WriteLine(flow+" | "+d.Name+" | "+d.Id);return 0;}
   if(action=="--recover"){DefaultAudio.Recover();Console.WriteLine("恢复完成。");return 0;}
   if(action=="--watch"){int pid=int.Parse(args[1]);try{System.Diagnostics.Process.GetProcessById(pid).WaitForExit();}catch{}Thread.Sleep(600);bool take;using(var guard=new Mutex(true,"Local\\DualSpeakerAudioEngine",out take)){if(take&&System.IO.File.Exists(Files.Recovery))DefaultAudio.Recover();}return 0;}
   bool created;using(var mutex=new Mutex(true,"Local\\DualSpeakerAudioEngine",out created)){
    if(!created)throw new Exception("双音响程序已经在运行，请先在窗口中停止播放。");
    if(System.IO.File.Exists(Files.Recovery))DefaultAudio.Recover();var settings=Devices.Initial();
    using(var engine=new AudioEngine(settings)){
     engine.Start(true);
     if(action=="--calibrate"){var m=Calibration.Auto(engine,Files.Log);Console.WriteLine(new JavaScriptSerializer().Serialize(m));return 0;}
     if(action=="--measure"){var m=Calibration.Measure(engine,"measurement");Console.WriteLine(new JavaScriptSerializer().Serialize(m));return m.Valid?0:2;}
     if(action=="--stability"){for(int i=0;i<3;i++){var m=Calibration.Measure(engine,"stability-"+i);if(!m.Valid)throw new Exception(m.Message);Thread.Sleep(1500);}return 0;}
     throw new Exception("未知命令: "+action);
    }
   }
  }catch(Exception e){Files.Log("ERROR | "+e);return 1;}}
  public static int Main(string[] args){return Execute(args);}
 }
}
