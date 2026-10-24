using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DualSpeaker {
 public class StudioFader:StudioValue {
  StudioMotion hover;
  public StudioFader(){Height=34;hover=new StudioMotion(this,0,.045);}
  protected override void OnMouseEnter(EventArgs e){hover.To(1);base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover.To(0);base.OnMouseLeave(e);}
  protected override void Dispose(bool disposing){if(disposing&&hover!=null)hover.Dispose();base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;using(var coordinates=new Studio.PaintCoordinates(g,this)){int width=coordinates.Width,height=coordinates.Height;if(width<28||height<14)return;int y=height/2,w=width-24;double hot=Enabled?Math.Max(hover.Value,Capture?1:0):0;
   using(var p=new Pen(Color.FromArgb(87,94,79),.7f)){for(int i=0;i<=20;i++){int x=12+i*w/20;g.DrawLine(p,x,y+11,x,y+(i%5==0?15:13));}}
   using(var p=new Pen(Color.FromArgb(69,77,64),1))g.DrawLine(p,13,y+4,width-13,y+4);
   using(var b=new SolidBrush(Color.FromArgb(10,14,11)))Studio.FillRound(g,b,new Rectangle(10,y-4,w+4,8),4);
   using(var b=new SolidBrush(Color.FromArgb(39,47,35)))Studio.FillRound(g,b,new Rectangle(12,y-2,w,3),1.5f);
   float fill=Math.Max(0,(float)(Fraction*w));if(fill>0){using(var b=new LinearGradientBrush(new Rectangle(12,y-2,w,4),Enabled?Color.FromArgb(197,160,97):Studio.Dim,Enabled?Color.FromArgb(233,200,138):Studio.Dim,0f))Studio.FillRound(g,b,new RectangleF(12,y-2,fill,3),1.5f);}
   int cx=12+(int)Math.Round(Fraction*w);var cap=new Rectangle(cx-8,y-10,16,21);
   using(var b=new SolidBrush(Color.FromArgb(100,0,0,0)))Studio.FillRound(g,b,new Rectangle(cx-9,y-8,19,23),3);
   using(var b=new LinearGradientBrush(cap,Color.FromArgb(178,183,162),Color.FromArgb(61,71,59),0f)){b.InterpolationColors=new ColorBlend{Colors=new[]{Color.FromArgb(90,102,83),Color.FromArgb(191,195,174),Color.FromArgb(137,147,126),Color.FromArgb(65,78,62)},Positions=new[]{0f,.18f,.70f,1f}};Studio.FillRound(g,b,cap,3);}
   using(var p=new Pen(Studio.Mix(Color.FromArgb(157,168,140),Studio.Amber,hot),.8f))using(var path=Studio.Round(cap,3))g.DrawPath(p,path);
   using(var p=new Pen(Color.FromArgb(68,34,43,31),.6f)){for(int i=0;i<3;i++)g.DrawLine(p,cx-4,y-5+i*5,cx+4,y-5+i*5);}
   using(var p=new Pen(Color.FromArgb(15,23,16),2))g.DrawLine(p,cx,y-6,cx,y+6);using(var p=new Pen(Enabled?Color.FromArgb(245,215,157):Studio.Dim,.9f))g.DrawLine(p,cx-.2f,y-6,cx-.2f,y+6);
   if(Focused&&ShowFocusCues)Studio.Focus(g,new Rectangle(0,0,width-1,height-1),Studio.Amber,Studio.Panel);
  }}
 }

 public class StudioKnob:StudioValue {
  Point dragOrigin;int dragValue;bool dragging,muted;Bitmap body;
  StudioMotion position,hover,light;
  public bool Muted{get{return muted;}set{if(muted==value)return;muted=value;light.To(value?0:1);Invalidate();}}
  public StudioKnob(){Size=new Size(190,190);AccessibleName="双音响总音量";position=new StudioMotion(this,0,.04);hover=new StudioMotion(this,0,.06);light=new StudioMotion(this,1,.07);ValueChanged+=(s,e)=>{if(dragging)position.Snap(Fraction);else position.To(Fraction);};}
  protected override void OnMouseEnter(EventArgs e){hover.To(1);base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover.To(0);base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;Focus();Capture=true;dragging=true;dragOrigin=e.Location;dragValue=Value;Invalidate();}
  protected override void OnMouseMove(MouseEventArgs e){if(dragging)Value=dragValue+(int)Math.Round((dragOrigin.Y-e.Y+(e.X-dragOrigin.X)*.4)*.65/HighDpi.UiScale(this));}
  protected override void OnMouseUp(MouseEventArgs e){dragging=false;Capture=false;Invalidate();}
  protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture)dragging=false;base.OnMouseCaptureChanged(e);}
  void ClearBody(){if(body!=null){body.Dispose();body=null;}}
  protected override void OnSizeChanged(EventArgs e){ClearBody();base.OnSizeChanged(e);}
  protected override void OnDpiChangedAfterParent(EventArgs e){ClearBody();base.OnDpiChangedAfterParent(e);Invalidate();}
  protected override void Dispose(bool disposing){if(disposing){ClearBody();if(position!=null)position.Dispose();if(hover!=null)hover.Dispose();if(light!=null)light.Dispose();}base.Dispose(disposing);}
  static PointF Polar(double radius,double degrees){double angle=degrees*Math.PI/180;return new PointF((float)(95+radius*Math.Cos(angle)),(float)(95+radius*Math.Sin(angle)));}
  void DrawBody(Graphics g){g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
   for(int i=0;i<=40;i++){double a=135+i*6.75;using(var p=new Pen(Color.FromArgb(85,93,78),i%4==0?1.5f:.7f))g.DrawLine(p,Polar(i%4==0?80:82,a),Polar(87,a));}
   using(var b=new SolidBrush(Color.FromArgb(95,0,0,0)))g.FillEllipse(b,23,29,146,146);using(var b=new SolidBrush(Color.FromArgb(13,18,14)))g.FillEllipse(b,22,22,146,146);
   using(var p=new Pen(Color.FromArgb(100,108,90),.7f))g.DrawArc(p,22,22,146,146,190,140);
   var rim=new Rectangle(27,27,136,136);using(var b=new LinearGradientBrush(rim,Color.FromArgb(188,196,171),Color.FromArgb(46,57,44),45))g.FillEllipse(b,rim);
   for(int i=0;i<104;i++){double a=i*360.0/104;double reflect=.5+.5*Math.Cos((a+135)*Math.PI/180);using(var p=new Pen(Studio.Mix(Color.FromArgb(40,52,37),Color.FromArgb(155,166,137),reflect),.9f))g.DrawLine(p,Polar(64,a),Polar(67,a));}
   using(var b=new SolidBrush(Color.FromArgb(30,40,29)))g.FillEllipse(b,34,34,122,122);
   var face=new Rectangle(36,36,118,118);using(var b=new LinearGradientBrush(face,Color.FromArgb(174,183,159),Color.FromArgb(67,82,64),55)){b.InterpolationColors=new ColorBlend{Colors=new[]{Color.FromArgb(177,185,160),Color.FromArgb(127,143,116),Color.FromArgb(77,94,72),Color.FromArgb(106,121,94)},Positions=new[]{0f,.28f,.72f,1f}};g.FillEllipse(b,face);}
   for(int i=0;i<53;i++){float d=114-i*2.1f;using(var p=new Pen(Color.FromArgb(i%3==0?18:8,228,233,204),.5f))g.DrawEllipse(p,95-d/2,95-d/2,d,d);}
   using(var path=new GraphicsPath()){path.AddEllipse(face);using(var sheen=new PathGradientBrush(path)){sheen.CenterPoint=new PointF(68,55);sheen.CenterColor=Color.FromArgb(36,242,239,204);sheen.SurroundColors=new[]{Color.FromArgb(0,200,211,180)};g.FillEllipse(sheen,face);}}
   using(var p=new Pen(Color.FromArgb(204,213,182),.65f))g.DrawArc(p,36,36,118,118,193,129);using(var p=new Pen(Color.FromArgb(34,46,32),1))g.DrawArc(p,36,36,118,118,10,125);
   using(var font=new Font("Segoe UI",7,FontStyle.Regular,GraphicsUnit.Pixel))using(var ink=new SolidBrush(Studio.Dim)){g.DrawString("MIN",font,ink,21,164);g.DrawString("MAX",font,ink,153,164);}
  }
  protected override void OnPaint(PaintEventArgs e){int size=Math.Min(Width,Height);if(size<2)return;if(body==null||body.Width!=size){ClearBody();body=new Bitmap(size,size,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(body)){g.ScaleTransform(size/190f,size/190f);DrawBody(g);}}
   var canvas=e.Graphics;canvas.DrawImageUnscaled(body,(Width-size)/2,(Height-size)/2);var state=canvas.Save();canvas.TranslateTransform((Width-size)/2f,(Height-size)/2f);canvas.ScaleTransform(size/190f,size/190f);canvas.SmoothingMode=SmoothingMode.AntiAlias;
   double pos=position.Value,lit=Enabled?light.Value:0;for(int i=0;i<=40;i++){if(i>pos*40)break;double a=135+i*6.75;using(var p=new Pen(Studio.Mix(Color.FromArgb(85,93,78),Color.FromArgb(230,188,113),lit),i%4==0?1.7f:.8f))canvas.DrawLine(p,Polar(i%4==0?80:82,a),Polar(87,a));}
   double hot=Math.Max(hover.Value,dragging?1:0);if(hot>.01){for(int i=0;i<3;i++)using(var p=new Pen(Color.FromArgb((int)((16-i*4)*hot),225,183,105),2+i*2))canvas.DrawArc(p,23,23,144,144,135,270);}
   double angle=135+pos*270;var start=Polar(43,angle);var end=Polar(55,angle);using(var p=new Pen(Color.FromArgb(98,11,19,11),5)){p.StartCap=p.EndCap=LineCap.Round;canvas.DrawLine(p,start.X+1,start.Y+1,end.X+1,end.Y+1);}using(var p=new Pen(Studio.Mix(Color.FromArgb(95,111,83),Color.FromArgb(255,225,161),lit),3.2f)){p.StartCap=p.EndCap=LineCap.Round;canvas.DrawLine(p,start,end);}
   if(lit>.01)using(var p=new Pen(Color.FromArgb((int)(20*lit),255,209,126),6)){p.StartCap=p.EndCap=LineCap.Round;canvas.DrawLine(p,start,end);}
   if(Focused&&ShowFocusCues)using(var p=new Pen(Studio.Amber,.8f))canvas.DrawEllipse(p,4,4,182,182);canvas.Restore(state);
  }
 }

 public class SpeakerGrille:Control {
  Bitmap mesh;
  public SpeakerGrille(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;TabStop=false;AccessibleRole=AccessibleRole.Graphic;AccessibleName="音响金属网罩装饰";}
  void ClearMesh(){if(mesh!=null){mesh.Dispose();mesh=null;}}
  protected override void OnSizeChanged(EventArgs e){ClearMesh();base.OnSizeChanged(e);}
  protected override void OnDpiChangedAfterParent(EventArgs e){ClearMesh();base.OnDpiChangedAfterParent(e);Invalidate();}
  protected override void Dispose(bool disposing){if(disposing)ClearMesh();base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){if(Width<4||Height<4)return;if(mesh==null){mesh=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(mesh))using(var coordinates=new Studio.PaintCoordinates(g,this)){int w=coordinates.Width,h=coordinates.Height;if(w>6&&h>6){var face=new Rectangle(1,1,w-3,h-3);using(var b=new SolidBrush(Color.FromArgb(24,29,25)))Studio.FillRound(g,b,face,3);using(var path=Studio.Round(face,3))g.SetClip(path);g.SmoothingMode=SmoothingMode.None;using(var hole=new SolidBrush(Color.FromArgb(11,16,12)))using(var lip=new SolidBrush(Color.FromArgb(43,50,40))){for(int y=4,row=0;y<h-2;y+=5,row++)for(int x=4+(row%2)*3;x<w-2;x+=6){g.FillEllipse(lip,x,y+1,2.2f,2.2f);g.FillEllipse(hole,x,y,2.2f,2.2f);}}g.ResetClip();g.SmoothingMode=SmoothingMode.AntiAlias;using(var p=new Pen(Color.FromArgb(14,20,15)))g.DrawLine(p,4,1,w-5,1);using(var p=new Pen(Color.FromArgb(60,67,54),.6f))g.DrawLine(p,4,h-2,w-5,h-2);}}}e.Graphics.DrawImageUnscaled(mesh,0,0);}
 }
}
