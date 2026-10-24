using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace DualSpeaker {
 public sealed class StudioWordmark:Control {
  Bitmap surface;
  static readonly string family=Studio.ChooseFont("Script MT Bold","Brush Script MT","Segoe Script");
  public StudioWordmark(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);TabStop=false;AccessibleRole=AccessibleRole.StaticText;AccessibleName="Dualspeaker 双音响控制台铭牌";}
  void Clear(){if(surface!=null){surface.Dispose();surface=null;}}
  protected override void OnSizeChanged(EventArgs e){Clear();base.OnSizeChanged(e);}
  protected override void OnDpiChangedAfterParent(EventArgs e){Clear();base.OnDpiChangedAfterParent(e);}
  protected override void Dispose(bool disposing){if(disposing)Clear();base.Dispose(disposing);}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){Studio.Backdrop(e.Graphics,this);if(Width<10||Height<10)return;if(surface==null){surface=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(surface))using(var c=new Studio.PaintCoordinates(g,this)){
    int w=c.Width,h=c.Height;var outer=new Rectangle(1,2,w-3,h-5);using(var b=new SolidBrush(Color.FromArgb(10,14,11)))Studio.FillRound(g,b,new Rectangle(1,4,w-2,h-5),4);
    using(var b=new LinearGradientBrush(outer,Color.FromArgb(198,182,139),Color.FromArgb(114,103,75),90))Studio.FillRound(g,b,outer,3);
    using(var p=new Pen(Color.FromArgb(49,244,234,197),.6f))for(int y=4;y<h-5;y+=2)g.DrawLine(p,5,y,w-6,y);
    using(var p=new Pen(Color.FromArgb(224,213,174),.7f))g.DrawLine(p,5,3,w-6,3);using(var p=new Pen(Color.FromArgb(79,73,53),.7f))using(var border=Studio.Round(new Rectangle(5,6,w-11,h-14),2))g.DrawPath(p,border);
    using(var path=new GraphicsPath())using(var font=new FontFamily(family))using(var measureFont=new Font(font,64,FontStyle.Regular,GraphicsUnit.Pixel)){
     // Track the glyphs gently without stretching their handwritten outlines.
     float advance=0;foreach(char letter in "Dualspeaker"){string glyph=letter.ToString();path.AddString(glyph,font,0,64,new PointF(advance,0),StringFormat.GenericTypographic);advance+=g.MeasureString(glyph,measureFont,PointF.Empty,StringFormat.GenericTypographic).Width+2.2f;}
     var bounds=path.GetBounds();float scale=Math.Min((w-51)/bounds.Width,31/bounds.Height);
     using(var m=new Matrix()){m.Translate((w-bounds.Width*scale)/2-bounds.X*scale,(h/2f-6)-bounds.Height*scale/2-bounds.Y*scale);m.Scale(scale,scale);path.Transform(m);}
     var state=g.Save();g.TranslateTransform(0,.8f);using(var b=new SolidBrush(Color.FromArgb(113,255,241,195)))g.FillPath(b,path);g.Restore(state);using(var b=new SolidBrush(Color.FromArgb(44,45,34)))g.FillPath(b,path);
    }
    using(var font=new Font("Segoe UI",5.2f,FontStyle.Regular))Studio.TextAt(g,"D U A L   C H A N N E L   /   H I - F I",font,Color.FromArgb(57,57,42),new Rectangle(20,h-18,w-40,10),ContentAlignment.MiddleCenter);
    MetalPanel.Screw(g,12,h/2);MetalPanel.Screw(g,w-13,h/2);
   }}e.Graphics.DrawImageUnscaled(surface,0,0);}
 }
}
