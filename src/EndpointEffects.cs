using System;
using System.Reflection;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
namespace DualSpeaker {
 public static class EndpointEffects {
  static readonly PropertyKey Key=new PropertyKey(new Guid("1da5d803-d492-4edd-8c23-e0c0ffee7f0e"),5);
  static object Store(MMDevice device,out Type t){var f=typeof(MMDevice).GetField("deviceInterface",BindingFlags.Instance|BindingFlags.NonPublic);var m=f.FieldType.GetMethod("OpenPropertyStore");object[] a={Enum.ToObject(m.GetParameters()[0].ParameterType,2),null};Marshal.ThrowExceptionForHR((int)m.Invoke(f.GetValue(device),a));t=typeof(MMDevice).Assembly.GetType("NAudio.CoreAudioApi.Interfaces.IPropertyStore");return a[1];}
  public static PropVariant Get(MMDevice d){Type t;object s=Store(d,out t);try{object[] a={Key,new PropVariant()};Marshal.ThrowExceptionForHR((int)t.GetMethod("GetValue").Invoke(s,a));return (PropVariant)a[1];}finally{Marshal.ReleaseComObject(s);}}
  public static void Set(MMDevice d,short vt,uint value){Type t;object s=Store(d,out t);try{var p=new PropVariant{vt=vt,uintVal=value};object[] a={Key,p};Marshal.ThrowExceptionForHR((int)t.GetMethod("SetValue").Invoke(s,a));Marshal.ThrowExceptionForHR((int)t.GetMethod("Commit").Invoke(s,null));}finally{Marshal.ReleaseComObject(s);}}
  public static void Test(){var settings=Devices.Initial();using(var d=Devices.Open(settings.BusId)){var old=Get(d);try{Set(d,19,1);System.Threading.Thread.Sleep(500);Files.Log("FX_DISABLED");SignalChecks.Matrix();}finally{Set(d,old.vt,old.uintVal);Files.Log("FX_RESTORED");}}}
 }
}
