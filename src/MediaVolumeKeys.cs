using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DualSpeaker {
 // Handle only the three multimedia volume keys. Other keys pass through untouched.
 // Install on the UI thread; post volume updates and return immediately.
 public sealed class MediaVolumeKeys:IDisposable {
  delegate IntPtr HookProc(int code,IntPtr message,IntPtr data);
  [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SetWindowsHookEx(int id,HookProc callback,IntPtr module,uint thread);
  [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
  [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string module);
  readonly Func<bool> enabled;readonly Action<int> enqueue;readonly HookProc callback;readonly HashSet<int> swallowed=new HashSet<int>();IntPtr hook;
  public long HandledCommands;
  public static int AdjustVolume(Settings settings,int key){if(key==0xAD)settings.MasterMuted=!settings.MasterMuted;else if(key==0xAE||key==0xAF){int step=Math.Max(1,Math.Min(20,settings.VolumeStepPercent));int value=(int)Math.Round(settings.MasterGain*100)+(key==0xAF?step:-step);settings.MasterGain=Math.Max(0,Math.Min(100,value))/100f;settings.MasterMuted=false;}return (int)Math.Round(settings.MasterGain*100);}
  public MediaVolumeKeys(Func<bool> isEnabled,Action<int> postCommand){enabled=isEnabled;enqueue=postCommand;callback=OnKey;hook=SetWindowsHookEx(13,callback,GetModuleHandle(null),0);if(hook==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error(),"无法接入键盘音量键。");}
  IntPtr OnKey(int code,IntPtr message,IntPtr data){if(code>=0){int key=Marshal.ReadInt32(data);if(key>=0xAD&&key<=0xAF){long msg=message.ToInt64();bool down=msg==0x100||msg==0x104,up=msg==0x101||msg==0x105;if(up&&swallowed.Remove(key))return new IntPtr(1);if(down&&enabled()){bool first=swallowed.Add(key);if(key!=0xAD||first){try{enqueue(key);HandledCommands++;}catch{}}return new IntPtr(1);}}}return CallNextHookEx(hook,code,message,data);}
  public void Dispose(){if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}swallowed.Clear();}
 }
}
