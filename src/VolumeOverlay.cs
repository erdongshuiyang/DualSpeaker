using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DualSpeaker {
 public sealed class VolumeOverlay:Form {
  [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
  readonly System.Windows.Forms.Timer dismiss;
  const float CompactScale=.8f;
  int percent;bool muted;
  readonly Font captionFont=Studio.Font(9f,FontStyle.Regular);
  readonly Font numberFont=new Font("Segoe UI Semibold",23f,FontStyle.Regular);
  public VolumeOverlay(){Name="DualSpeakerVolumeOverlay";Text="双音响音量";AccessibleRole=AccessibleRole.Alert;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;TopMost=true;AutoScaleMode=AutoScaleMode.None;ApplyDpi(HighDpi.Dpi(null));BackColor=Studio.Background;DoubleBuffered=true;
   dismiss=new System.Windows.Forms.Timer{Interval=1800};dismiss.Tick+=(s,e)=>{dismiss.Stop();Hide();};
  }
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ExStyle|=0x08000000|0x00000080|0x00000020;return cp;}}
  protected override void WndProc(ref Message message){if(message.Msg==0x21){message.Result=new IntPtr(3);return;}if(message.Msg==0x84){message.Result=new IntPtr(-1);return;}base.WndProc(ref message);}
  void ApplyDpi(int dpi){ClientSize=new Size((int)Math.Round(360*CompactScale*dpi/96.0),(int)Math.Round(88*CompactScale*dpi/96.0));}
  protected override void OnDpiChanged(DpiChangedEventArgs e){base.OnDpiChanged(e);ApplyDpi(e.DeviceDpiNew);Invalidate();}
  protected override void OnResize(EventArgs e){base.OnResize(e);if(Width<=0||Height<=0)return;using(var path=new GraphicsPath()){int d=Math.Max(2,(int)Math.Round(22*Width/360.0));path.AddArc(0,0,d,d,180,90);path.AddArc(Width-d-1,0,d,d,270,90);path.AddArc(Width-d-1,Height-d-1,d,d,0,90);path.AddArc(0,Height-d-1,d,d,90,90);path.CloseFigure();var previous=Region;Region=new Region(path);if(previous!=null)previous.Dispose();}}
  public void Present(int value,bool isMuted){if(IsDisposed)return;percent=Math.Max(0,Math.Min(100,value));muted=isMuted;Text="双音响音量 "+percent+"%"+(muted?" · 已静音":"");AccessibleName=Text;IntPtr foreground=GetForegroundWindow();int dpi=HighDpi.MonitorDpi(foreground);ApplyDpi(dpi);Rectangle area=Screen.FromHandle(foreground).WorkingArea;Location=new Point(area.Left+(area.Width-Width)/2,area.Bottom-Height-(int)Math.Round(24*dpi/96.0));if(!Visible)Show();ApplyDpi(HighDpi.Dpi(this));Location=new Point(area.Left+(area.Width-Width)/2,area.Bottom-Height-(int)Math.Round(24*dpi/96.0));SetWindowPos(Handle,new IntPtr(-1),Left,Top,Width,Height,0x0010|0x0040);Invalidate();dismiss.Stop();dismiss.Start();}
  public void Dismiss(){dismiss.Stop();Hide();}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;using(var coordinates=new Studio.PaintCoordinates(g,this)){g.ScaleTransform(CompactScale,CompactScale);int width=360,height=88;g.SmoothingMode=SmoothingMode.AntiAlias;
   var surface=new Rectangle(0,0,width,height);using(var b=new LinearGradientBrush(surface,Color.FromArgb(47,52,44),Studio.Background,90))g.FillRectangle(b,surface);
   using(var p=new Pen(Color.FromArgb(95,99,79)))using(var path=Studio.Round(new Rectangle(0,0,width-1,height-1),11))g.DrawPath(p,path);
   using(var white=new SolidBrush(muted?Studio.Dim:Studio.Amber))using(var gray=new SolidBrush(Color.FromArgb(65,72,58)))using(var accent=new SolidBrush(Studio.Amber))using(var pen=new Pen(muted?Studio.Dim:Studio.Amber,1.7f)){
    PointF[] speaker={new PointF(22,37),new PointF(28,37),new PointF(36,30),new PointF(36,55),new PointF(28,48),new PointF(22,48)};g.FillPolygon(white,speaker);
    if(muted||percent==0){g.DrawLine(pen,43,37,52,48);g.DrawLine(pen,43,48,52,37);}else{g.DrawArc(pen,33,32,15,23,-65,130);g.DrawArc(pen,34,26,23,35,-63,126);}
    Studio.TextAt(g,muted?"双音响 · 已静音":"总音量 / MASTER",captionFont,Studio.Text,new Rectangle(77,15,190,23),ContentAlignment.MiddleLeft);
    for(int i=0;i<20;i++)g.FillRectangle(!muted&&percent>i*5?accent:gray,78+i*9,53,6,9);
    Studio.TextAt(g,percent+"%",numberFont,muted?Studio.Dim:Studio.Amber,new Rectangle(267,21,80,44),ContentAlignment.MiddleRight);
   }
  }}
  protected override void Dispose(bool disposing){if(disposing){dismiss.Dispose();captionFont.Dispose();numberFont.Dispose();}base.Dispose(disposing);}
 }
}
