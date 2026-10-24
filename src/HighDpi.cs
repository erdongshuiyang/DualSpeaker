using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DualSpeaker {
 public static class HighDpi {
  [DllImport("user32.dll")]static extern bool SetProcessDpiAwarenessContext(IntPtr context);
  [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")]static extern uint GetDpiForWindow(IntPtr window);
  [DllImport("user32.dll")]static extern uint GetDpiForSystem();
  [DllImport("user32.dll")]static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
  [DllImport("shcore.dll")]static extern int GetDpiForMonitor(IntPtr monitor,int kind,out uint x,out uint y);
  public static void Initialize(){try{SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{try{SetProcessDPIAware();}catch{}}}
  public static int Dpi(Control control){try{if(control!=null&&control.IsHandleCreated){uint dpi=GetDpiForWindow(control.Handle);if(dpi>0)return (int)dpi;}return (int)GetDpiForSystem();}catch{using(var g=Graphics.FromHwnd(IntPtr.Zero))return (int)Math.Round(g.DpiX);}}
  public static int MonitorDpi(IntPtr window){try{uint x,y;if(GetDpiForMonitor(MonitorFromWindow(window,2),0,out x,out y)==0&&x>0)return (int)x;}catch{}return Dpi(null);}
  public static float Scale(Control control){return Dpi(control)/96f;}
  public static float Zoom(Control control){var form=control==null?null:control.FindForm() as MainForm;return form==null?1:form.VisualZoom;}
  public static float UiScale(Control control){return Scale(control)*Zoom(control);}
  public static int UiPixels(float value,Control control){return (int)Math.Round(value*UiScale(control));}
  public static int Pixels(float value,Control control){return (int)Math.Round(value*Scale(control));}
  public static Rectangle Rectangle(Rectangle logical,float scale){return new Rectangle((int)Math.Round(logical.X*scale),(int)Math.Round(logical.Y*scale),(int)Math.Round(logical.Width*scale),(int)Math.Round(logical.Height*scale));}
 }
}
