using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Wave;
namespace DualSpeaker {
 public sealed class ProcessAudioCapture:IDisposable {
  [StructLayout(LayoutKind.Sequential)]struct Activation {public uint Type,Pid,Mode;}
  [StructLayout(LayoutKind.Explicit,Size=24)]struct Variant {[FieldOffset(0)]public ushort Type;[FieldOffset(8)]public uint Size;[FieldOffset(16)]public IntPtr Data;}
  [StructLayout(LayoutKind.Sequential,Pack=2)]struct Pcm {public ushort Tag,Channels;public uint Rate,Bytes;public ushort Align,Bits,Extra;}
  [ComImport,Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IOperation {[PreserveSig]int GetActivateResult(out int result,[MarshalAs(UnmanagedType.IUnknown)]out object client);}
  [ComVisible(true),Guid("41D949AB-9862-444A-80F6-C261334DA5EB"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface ICompletion {[PreserveSig]int ActivateCompleted(IOperation operation);}
  [ComVisible(true),Guid("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IAgile {}
  [ComImport,Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IClient {
   [PreserveSig]int Initialize(int share,uint flags,long duration,long period,IntPtr format,IntPtr session);
   [PreserveSig]int GetBufferSize(out uint frames);[PreserveSig]int GetStreamLatency(out long latency);[PreserveSig]int GetCurrentPadding(out uint frames);
   [PreserveSig]int IsFormatSupported(int mode,IntPtr format,IntPtr closest);[PreserveSig]int GetMixFormat(out IntPtr format);[PreserveSig]int GetDevicePeriod(out long a,out long b);
   [PreserveSig]int Start();[PreserveSig]int Stop();[PreserveSig]int Reset();[PreserveSig]int SetEventHandle(IntPtr handle);
   [PreserveSig]int GetService(ref Guid iid,[MarshalAs(UnmanagedType.IUnknown)]out object service);
  }
  [ComImport,Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface ICapture {
   [PreserveSig]int GetBuffer(out IntPtr data,out uint frames,out uint flags,out ulong position,out ulong qpc);
   [PreserveSig]int ReleaseBuffer(uint frames);[PreserveSig]int GetNextPacketSize(out uint frames);
  }
  [ComVisible(true),ClassInterface(ClassInterfaceType.None)]public sealed class Completion:ICompletion,IAgile {
   public ManualResetEvent Ready=new ManualResetEvent(false);public int Result;public object Client;
   public int ActivateCompleted(IOperation operation){try{int hr;object client;int r=operation.GetActivateResult(out hr,out client);Result=r<0?r:hr;Client=client;}catch(Exception ex){Result=Marshal.GetHRForException(ex);}finally{Ready.Set();}return 0;}
  }
  [DllImport("Mmdevapi.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]static extern int ActivateAudioInterfaceAsync(string path,ref Guid iid,ref Variant parameters,ICompletion completion,out IOperation operation);
  public WaveFormat WaveFormat= new WaveFormat(48000,16,2);
  public event EventHandler<WaveInEventArgs> DataAvailable;public event EventHandler<StoppedEventArgs> RecordingStopped;
  Thread thread;volatile bool stopping;ManualResetEvent started=new ManualResetEvent(false);Exception startError;public long Packets;
  public void StartRecording(){thread=new Thread(Run){IsBackground=true,Name="System process audio capture",Priority=ThreadPriority.AboveNormal};thread.SetApartmentState(ApartmentState.MTA);thread.Start();if(!started.WaitOne(12000))throw new Exception("系统音频接口启动超时。");if(startError!=null)throw startError;}
  void Run(){IClient client=null;ICapture capture=null;IOperation operation=null;Completion callback=null;IntPtr activation=IntPtr.Zero,format=IntPtr.Zero;EventWaitHandle ready=null;Exception failure=null;
   try{activation=Marshal.AllocHGlobal(12);Marshal.StructureToPtr(new Activation{Type=1,Pid=(uint)Process.GetCurrentProcess().Id,Mode=1},activation,false);var variant=new Variant{Type=65,Size=12,Data=activation};Guid iid=typeof(IClient).GUID;callback=new Completion();Marshal.ThrowExceptionForHR(ActivateAudioInterfaceAsync("VAD\\Process_Loopback",ref iid,ref variant,callback,out operation));if(!callback.Ready.WaitOne(10000))throw new Exception("进程音频捕获激活超时。");Marshal.ThrowExceptionForHR(callback.Result);client=(IClient)callback.Client;
    format=Marshal.AllocHGlobal(18);Marshal.StructureToPtr(new Pcm{Tag=1,Channels=2,Rate=48000,Bytes=192000,Align=4,Bits=16},format,false);Marshal.ThrowExceptionForHR(client.Initialize(0,0x80060000,0,0,format,IntPtr.Zero));
    ready=new EventWaitHandle(false,EventResetMode.AutoReset);Marshal.ThrowExceptionForHR(client.SetEventHandle(ready.SafeWaitHandle.DangerousGetHandle()));object service;Guid capid=typeof(ICapture).GUID;Marshal.ThrowExceptionForHR(client.GetService(ref capid,out service));capture=(ICapture)service;Marshal.ThrowExceptionForHR(client.Start());started.Set();
    while(!stopping){ready.WaitOne(100);uint next;Marshal.ThrowExceptionForHR(capture.GetNextPacketSize(out next));while(next>0){IntPtr data;uint frames,flags;ulong pos,qpc;Marshal.ThrowExceptionForHR(capture.GetBuffer(out data,out frames,out flags,out pos,out qpc));try{byte[] bytes=new byte[frames*4];if((flags&2)==0&&data!=IntPtr.Zero)Marshal.Copy(data,bytes,0,bytes.Length);var handler=DataAvailable;if(handler!=null)handler(this,new WaveInEventArgs(bytes,bytes.Length));Packets++;}finally{Marshal.ThrowExceptionForHR(capture.ReleaseBuffer(frames));}Marshal.ThrowExceptionForHR(capture.GetNextPacketSize(out next));}}
   }catch(Exception ex){failure=ex;startError=ex;started.Set();}
   finally{if(client!=null){try{client.Stop();}catch{}}if(capture!=null)Marshal.ReleaseComObject(capture);if(client!=null)Marshal.ReleaseComObject(client);if(operation!=null)Marshal.ReleaseComObject(operation);if(ready!=null)ready.Dispose();if(activation!=IntPtr.Zero)Marshal.FreeHGlobal(activation);if(format!=IntPtr.Zero)Marshal.FreeHGlobal(format);var done=RecordingStopped;if(done!=null)done(this,new StoppedEventArgs(failure));}
  }
  public void StopRecording(){stopping=true;if(thread!=null&&thread!=Thread.CurrentThread){thread.Join(4000);thread=null;}}
  public void Dispose(){StopRecording();started.Dispose();}
 }
}