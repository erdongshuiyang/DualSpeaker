using System;
using System.Reflection;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
namespace DualSpeaker {
 public static class RawMicrophone {
  [StructLayout(LayoutKind.Sequential)]struct Properties {public uint Size;[MarshalAs(UnmanagedType.Bool)]public bool Offload;public int Category;public uint Options;}
  [ComImport,Guid("726778CD-F60A-4EDA-82DE-E47610CD78AA"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IClient2 {
   [PreserveSig]int Initialize();[PreserveSig]int GetBufferSize();[PreserveSig]int GetStreamLatency();[PreserveSig]int GetCurrentPadding();[PreserveSig]int IsFormatSupported();[PreserveSig]int GetMixFormat();[PreserveSig]int GetDevicePeriod();[PreserveSig]int Start();[PreserveSig]int Stop();[PreserveSig]int Reset();[PreserveSig]int SetEventHandle();[PreserveSig]int GetService();
   [PreserveSig]int IsOffloadCapable();[PreserveSig]int SetClientProperties(ref Properties properties);[PreserveSig]int GetBufferSizeLimits();
  }
  public static bool Configure(WasapiCapture capture){try{
   var ac=(AudioClient)typeof(WasapiCapture).GetField("audioClient",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(capture);
   var com=typeof(AudioClient).GetField("audioClientInterface",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ac);var c=(IClient2)com;var p=new Properties{Size=(uint)Marshal.SizeOf(typeof(Properties)),Category=0,Options=1};int hr=c.SetClientProperties(ref p);Files.Log("麦克风 RAW 模式 result=0x"+hr.ToString("X8"));return hr>=0;
  }catch(Exception ex){Files.Log("麦克风 RAW 模式不可用: "+ex.Message);return false;}}
 }
}
