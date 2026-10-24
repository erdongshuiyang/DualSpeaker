using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DualSpeaker {
 // One UI clock, active only while an interaction is moving. Audio values are
 // applied immediately; these values interpolate visual feedback only.
 sealed class StudioMotion:IDisposable {
  static readonly List<StudioMotion> moving=new List<StudioMotion>();
  static System.Windows.Forms.Timer clock;static long lastTick;
  [DllImport("user32.dll")]static extern bool SystemParametersInfo(uint action,uint param,out int value,uint flags);
  static long lastPreference;static bool animations=true;
  static bool AnimationsEnabled{get{long now=Stopwatch.GetTimestamp();if(lastPreference==0||now-lastPreference>Stopwatch.Frequency){int enabled;if(SystemParametersInfo(0x1042,0,out enabled,0))animations=enabled!=0;lastPreference=now;}return animations;}}
  readonly Control owner;readonly double duration;double value,target;bool disposed;
  public double Value{get{return value;}}
  public StudioMotion(Control control,double initial,double seconds){owner=control;value=target=initial;duration=seconds;}
  bool CanAnimate{get{var form=owner.FindForm();return !disposed&&!owner.IsDisposed&&owner.IsHandleCreated&&owner.Visible&&AnimationsEnabled&&(form==null||form.WindowState!=FormWindowState.Minimized);}}
  public void Snap(double next){target=value=next;moving.Remove(this);if(!disposed&&!owner.IsDisposed)owner.Invalidate();StopIfIdle();}
  public void To(double next){if(disposed||target==next)return;target=next;if(!CanAnimate){Snap(next);return;}if(!moving.Contains(this))moving.Add(this);if(clock==null){clock=new System.Windows.Forms.Timer{Interval=16};clock.Tick+=(s,e)=>Tick();}if(!clock.Enabled){lastTick=Stopwatch.GetTimestamp();clock.Start();}}
  static void Tick(){long now=Stopwatch.GetTimestamp();double dt=Math.Min(.06,(now-lastTick)/(double)Stopwatch.Frequency);lastTick=now;for(int i=moving.Count-1;i>=0;i--){var item=moving[i];if(!item.CanAnimate){item.value=item.target;moving.RemoveAt(i);continue;}item.value+=(item.target-item.value)*(1-Math.Exp(-dt/item.duration));if(Math.Abs(item.value-item.target)<.002){item.value=item.target;moving.RemoveAt(i);}item.owner.Invalidate();}StopIfIdle();}
  static void StopIfIdle(){if(moving.Count==0&&clock!=null)clock.Stop();}
  public void Dispose(){disposed=true;moving.Remove(this);StopIfIdle();}
 }
}
