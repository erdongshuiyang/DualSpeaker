using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace DualSpeaker {
 public sealed class StudioReadout:Label {
  bool muted;public bool Muted{get{return muted;}set{if(muted==value)return;muted=value;Invalidate();}}
  public StudioReadout(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AutoSize=false;TabStop=false;AccessibleRole=AccessibleRole.StaticText;AccessibleName="双音响总音量";}
  protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);AccessibleDescription="总音量 "+Text;Invalidate();}
  protected override void OnPaintBackground(PaintEventArgs e){}
  static readonly int[] masks={0x3f,0x06,0x5b,0x4f,0x66,0x6d,0x7d,0x07,0x7f,0x6f};
  static GraphicsPath Segment(float x1,float y1,float x2,float y2){float t=1.4f;var p=new GraphicsPath();if(y1==y2)p.AddPolygon(new[]{new PointF(x1,y1),new PointF(x1+t,y1-t),new PointF(x2-t,y2-t),new PointF(x2,y2),new PointF(x2-t,y2+t),new PointF(x1+t,y1+t)});else p.AddPolygon(new[]{new PointF(x1,y1),new PointF(x1+t,y1+t),new PointF(x2+t,y2-t),new PointF(x2,y2),new PointF(x2-t,y2-t),new PointF(x1-t,y1+t)});return p;}
  internal static void Digit(Graphics g,int number,float x,float y,bool dim){int mask=number<0?0:masks[number];float[,] lines={{3,1,17,1},{19,3,19,15},{19,19,19,31},{3,33,17,33},{1,19,1,31},{1,3,1,15},{3,17,17,17}};var state=g.Save();g.TranslateTransform(x,y);for(int i=0;i<7;i++)using(var path=Segment(lines[i,0],lines[i,1],lines[i,2],lines[i,3])){bool lit=(mask&(1<<i))!=0;using(var b=new SolidBrush(lit?(dim?Color.FromArgb(113,77,38):Color.FromArgb(255,181,83)):Color.FromArgb(44,35,23)))g.FillPath(b,path);if(lit&&!dim){using(var p=new Pen(Color.FromArgb(24,255,164,61),2.8f))g.DrawPath(p,path);using(var p=new Pen(Color.FromArgb(110,255,222,146),.4f))g.DrawPath(p,path);}}g.Restore(state);}
  protected override void OnPaint(PaintEventArgs e){Studio.Backdrop(e.Graphics,this);using(var c=new Studio.PaintCoordinates(e.Graphics,this)){var g=e.Graphics;int w=c.Width,h=c.Height;if(w<15||h<15)return;var outer=new Rectangle(1,1,w-3,h-3);using(var b=new LinearGradientBrush(outer,Color.FromArgb(12,17,13),Color.FromArgb(99,104,81),90))Studio.FillRound(g,b,outer,5);var face=new Rectangle(5,5,w-11,h-11);using(var b=new LinearGradientBrush(face,Color.FromArgb(15,21,17),Color.FromArgb(29,29,19),90))Studio.FillRound(g,b,face,3);
    using(var f=new Font("Segoe UI",6.3f,FontStyle.Regular))Studio.TextAt(g,Muted?"M U T E D":"M A S T E R",f,Color.FromArgb(149,132,91),new Rectangle(12,9,w-24,12),ContentAlignment.MiddleLeft);
    int value;if(!Int32.TryParse(Text.TrimEnd('%'),out value))value=0;value=Math.Max(0,Math.Min(100,value));string digits=value.ToString().PadLeft(3,' ');float x=(w-106)/2f,y=26;
    for(int i=0;i<3;i++)Digit(g,digits[i]==' '?-1:digits[i]-'0',x+i*25,y,Muted);
    using(var f=new Font("Consolas",13,FontStyle.Regular))Studio.TextAt(g,"%",f,Muted?Studio.Dim:Color.FromArgb(231,169,88),new Rectangle((int)x+80,35,25,25),ContentAlignment.MiddleCenter);
    using(var b=new LinearGradientBrush(face,Color.FromArgb(16,243,230,183),Color.FromArgb(0,243,230,183),90))g.FillPolygon(b,new[]{new Point(6,6),new Point(w-7,6),new Point(w-7,18),new Point(6,31)});
   }}
 }
}
