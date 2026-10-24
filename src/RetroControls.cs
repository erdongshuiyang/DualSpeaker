using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DualSpeaker {
 // All instrument surfaces are drawn locally, so they stay sharp without image assets.
 public static class Studio {
  public static readonly Color Background=Color.FromArgb(23,26,25),Panel=Color.FromArgb(37,41,39),Inset=Color.FromArgb(24,28,26);
  public static readonly Color Text=Color.FromArgb(225,220,204),Dim=Color.FromArgb(169,174,160),Amber=Color.FromArgb(218,178,106),Line=Color.FromArgb(62,67,60),Green=Color.FromArgb(159,190,143);
  static readonly string uiFamily=ChooseFont("Microsoft YaHei UI","Noto Sans SC","Segoe UI"),mediumFamily=uiFamily;
  public static string ChooseFont(params string[] names){foreach(string name in names)try{using(var family=new FontFamily(name))if(family.Name==name)return name;}catch(ArgumentException){}return FontFamily.GenericSansSerif.Name;}
  public static Font Font(float size,FontStyle style){return new Font(PixelTypography.Family,size,FontStyle.Regular);}
  public static Font Medium(float size){return new Font(PixelTypography.Family,size,FontStyle.Regular);}
  public static Font Caption(float size){return new Font("Segoe UI",size,FontStyle.Regular);}
  // Paint directly from the nearest opaque surface. Replaying transparent
  // layout parents through InvokePaint mixes nested DC offsets at high DPI.
  public static void Backdrop(Graphics g,Control owner){int x=owner.Left,y=owner.Top;Control parent=owner.Parent;while(parent!=null){var metal=parent as MetalPanel;if(metal!=null){metal.PaintBackdrop(g,x,y);return;}if(parent.BackColor.A==255){using(var b=new SolidBrush(parent.BackColor))g.FillRectangle(b,owner.ClientRectangle);return;}x+=parent.Left;y+=parent.Top;parent=parent.Parent;}using(var b=new SolidBrush(Background))g.FillRectangle(b,owner.ClientRectangle);}
  public static Color Mix(Color a,Color b,double t){t=Math.Max(0,Math.Min(1,t));return Color.FromArgb((int)Math.Round(a.A+(b.A-a.A)*t),(int)Math.Round(a.R+(b.R-a.R)*t),(int)Math.Round(a.G+(b.G-a.G)*t),(int)Math.Round(a.B+(b.B-a.B)*t));}
  public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
  public static void FillRound(Graphics g,Brush b,RectangleF r,float radius){using(var p=Round(r,radius))g.FillPath(b,p);}
  static Rectangle DeviceRectangle(Graphics g,Rectangle rect){PointF[] points={new PointF(rect.Left,rect.Top),new PointF(rect.Right,rect.Bottom)};using(var transform=g.Transform)transform.TransformPoints(points);return Rectangle.FromLTRB((int)Math.Round(points[0].X),(int)Math.Round(points[0].Y),(int)Math.Round(points[1].X),(int)Math.Round(points[1].Y));}
  // Draw hinted glyphs at integer device-pixel sizes, outside the artwork transform.
  // Native GDI text DCs can lose the clip origin during WM_PRINT on child windows.
  public static void TextAt(Graphics g,string text,Font font,Color color,Rectangle rect,ContentAlignment align,float textScale=1){
   var state=g.Save();Rectangle pixels=DeviceRectangle(g,rect);float scale;using(var transform=g.Transform){var m=transform.Elements;scale=(float)Math.Sqrt(m[2]*m[2]+m[3]*m[3]);}
   g.ResetTransform();g.TextRenderingHint=PixelTypography.Hint(font);
   using(var physicalFont=new Font(font.FontFamily,Math.Max(1,(float)Math.Round(font.SizeInPoints*96f/72f*textScale*scale)),font.Style,GraphicsUnit.Pixel)){
    using(var ink=new SolidBrush(color))using(var format=new StringFormat(StringFormat.GenericTypographic)){
     format.FormatFlags|=StringFormatFlags.NoWrap;format.Trimming=StringTrimming.EllipsisCharacter;format.LineAlignment=StringAlignment.Center;format.Alignment=align==ContentAlignment.MiddleCenter?StringAlignment.Center:align==ContentAlignment.MiddleRight?StringAlignment.Far:StringAlignment.Near;
     g.DrawString(text??"",physicalFont,ink,pixels,format);
    }
   }g.Restore(state);
  }
  public static void Focus(Graphics g,Rectangle rect,Color fore,Color back){if(rect.Width<2||rect.Height<2)return;using(var p=new Pen(Color.FromArgb(120,fore),.8f)){using(var path=Round(rect,3))g.DrawPath(p,path);}}
  public sealed class PaintCoordinates:IDisposable {
   readonly Graphics graphics;readonly GraphicsState state;public readonly int Width,Height;
   public PaintCoordinates(Graphics g,Control control){graphics=g;state=g.Save();float scale=HighDpi.UiScale(control);Width=Math.Max(1,(int)Math.Round(control.Width/scale));Height=Math.Max(1,(int)Math.Round(control.Height/scale));g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;}
   public void Dispose(){graphics.Restore(state);}
  }
 }
 public class MetalPanel:Panel {
  Bitmap surface;bool screws;
  public bool Screws{get{return screws;}set{if(screws==value)return;screws=value;ClearSurface();Invalidate();}}
  public MetalPanel(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.UserPaint,true);BackColor=Studio.Panel;ForeColor=Studio.Text;}
  void ClearSurface(){if(surface!=null){surface.Dispose();surface=null;}}
  protected override void OnSizeChanged(EventArgs e){ClearSurface();base.OnSizeChanged(e);}
  protected override void OnDpiChangedAfterParent(EventArgs e){ClearSurface();base.OnDpiChangedAfterParent(e);Invalidate();}
  protected override void Dispose(bool disposing){if(disposing)ClearSurface();base.Dispose(disposing);}
  internal void PaintBackdrop(Graphics target,int x,int y){if(Width<2||Height<2)return;if(surface==null){surface=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(surface))PaintSurface(g);}target.DrawImageUnscaled(surface,-x,-y);}
  protected override void OnPaintBackground(PaintEventArgs e){PaintBackdrop(e.Graphics,0,0);}
  void PaintSurface(Graphics g){using(var coordinates=new Studio.PaintCoordinates(g,this)){int width=coordinates.Width,height=coordinates.Height;g.Clear(Studio.Background);if(width<8||height<8)return;var shell=new Rectangle(0,0,width-1,height-1);using(var b=new LinearGradientBrush(shell,Color.FromArgb(67,71,63),Color.FromArgb(20,24,21),90))Studio.FillRound(g,b,shell,5);var face=new Rectangle(2,2,width-5,height-6);using(var b=new LinearGradientBrush(face,Color.FromArgb(44,48,44),Color.FromArgb(29,34,30),90))Studio.FillRound(g,b,face,4);
   var state=g.Save();using(var clip=Studio.Round(face,4))g.SetClip(clip,CombineMode.Intersect);using(var light=new Pen(Color.FromArgb(4,208,211,185)))using(var dark=new Pen(Color.FromArgb(7,8,13,9))){for(int y=4;y<height-3;y+=3){g.DrawLine(light,3,y,width-4,y);if(y%2==0)g.DrawLine(dark,3,y+1,width-4,y+1);}}
   using(var wash=new LinearGradientBrush(new Rectangle(0,0,width,Math.Max(1,height/2)),Color.FromArgb(9,227,213,176),Color.Transparent,90))g.FillRectangle(wash,0,0,width,height/2);g.Restore(state);
   using(var p=new Pen(Color.FromArgb(84,91,77),.7f))g.DrawLine(p,7,1,width-8,1);using(var p=new Pen(Color.FromArgb(14,18,15)))g.DrawLine(p,5,height-2,width-6,height-2);if(Screws){Screw(g,11,11);Screw(g,width-12,11);Screw(g,11,height-12);Screw(g,width-12,height-12);}}}
  internal static void Screw(Graphics g,int x,int y){using(var b=new SolidBrush(Color.FromArgb(15,19,16)))g.FillEllipse(b,x-4,y-3,8,8);using(var b=new LinearGradientBrush(new Rectangle(x-3,y-3,6,6),Color.FromArgb(119,123,103),Color.FromArgb(39,45,38),55))g.FillEllipse(b,x-3,y-3,6,6);using(var p=new Pen(Color.FromArgb(139,143,120),.6f))g.DrawArc(p,x-3,y-3,6,6,190,140);using(var p=new Pen(Color.FromArgb(13,17,13),1.2f))g.DrawLine(p,x-2,y+1,x+2,y-1);}
 }
 public class StudioLayout:TableLayoutPanel {
  internal bool FixedLayout;public override System.Windows.Forms.Layout.LayoutEngine LayoutEngine{get{return FixedLayout?FixedLayoutEngine.Instance:base.LayoutEngine;}}
  public StudioLayout(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
 }
 public class StudioSurface:Panel {
  public StudioSurface(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
 }
 public class StudioLabel:Label {
  public StudioLabel(){UseCompatibleTextRendering=false;}
  protected override void OnPaint(PaintEventArgs e){var state=e.Graphics.Save();e.Graphics.TextRenderingHint=PixelTypography.Hint(Font);base.OnPaint(e);e.Graphics.Restore(state);}
 }
 public class StudioButton:Button {
  public bool Accent{get;set;} public bool Selected{get;set;} bool down;StudioMotion hover;
  public StudioButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Standard;UseVisualStyleBackColor=false;Cursor=Cursors.Hand;BackColor=Studio.Panel;ForeColor=Studio.Text;Font=Studio.Medium(9);Size=new Size(120,36);hover=new StudioMotion(this,0,.045);}
  protected override void OnMouseEnter(EventArgs e){hover.To(1);base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover.To(0);down=false;Invalidate();base.OnMouseLeave(e);}protected override void OnMouseDown(MouseEventArgs e){down=e.Button==MouseButtons.Left;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){down=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture){down=false;Invalidate();}base.OnMouseCaptureChanged(e);}
  protected override void Dispose(bool disposing){if(disposing&&hover!=null)hover.Dispose();base.Dispose(disposing);}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){Studio.Backdrop(e.Graphics,this);var g=e.Graphics;using(var coordinates=new Studio.PaintCoordinates(g,this)){int width=coordinates.Width,height=coordinates.Height;if(width<8||height<8)return;bool lit=Enabled&&(Accent||Selected);double warmth=Enabled?hover.Value:0;int travel=down?1:0;Color a=lit?Color.FromArgb(213,185,137):Color.FromArgb(62,69,61),b=lit?Color.FromArgb(157,126,78):Color.FromArgb(38,44,39);a=Studio.Mix(a,lit?Color.FromArgb(232,205,157):Color.FromArgb(81,86,71),warmth*.55);if(down){a=Studio.Mix(a,b,.55);b=Studio.Mix(b,Color.Black,.08);}var r=new Rectangle(1,1+travel,width-3,height-5);using(var shadow=new SolidBrush(Color.FromArgb(11,15,12)))Studio.FillRound(g,shadow,new Rectangle(0,3,width-1,height-4),5);using(var fill=new LinearGradientBrush(r,a,b,90))Studio.FillRound(g,fill,r,4);using(var p=new Pen(lit?Color.FromArgb(226,201,157):Studio.Mix(Color.FromArgb(81,89,76),Studio.Amber,warmth*.35)))using(var path=Studio.Round(r,4))g.DrawPath(p,path);using(var p=new Pen(lit?Color.FromArgb(92,255,239,203):Color.FromArgb(40,205,215,187)))g.DrawLine(p,6,2+travel,width-8,2+travel);using(var p=new Pen(Color.FromArgb(45,8,14,9)))g.DrawLine(p,5,height-5+travel,width-7,height-5+travel);Studio.TextAt(g,Text,Font,!Enabled?Color.FromArgb(111,116,105):lit?Color.FromArgb(36,38,30):Studio.Text,new Rectangle(6,travel,width-12,height-4),ContentAlignment.MiddleCenter);if(Focused&&ShowFocusCues)Studio.Focus(g,new Rectangle(5,5,width-11,height-12),Studio.Amber,BackColor);}}
 }
 public class StudioCombo:ComboBox {
  public StudioCombo(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);DropDownStyle=ComboBoxStyle.DropDownList;FlatStyle=FlatStyle.Flat;DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=27;BackColor=Studio.Inset;ForeColor=Studio.Text;Font=Studio.Font(9,FontStyle.Regular);}
  public void UpdateUiScale(){ItemHeight=HighDpi.UiPixels(27,this);}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);UpdateUiScale();}
  protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);UpdateUiScale();Invalidate();}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Studio.Inset);using(var coordinates=new Studio.PaintCoordinates(g,this)){int width=coordinates.Width,height=coordinates.Height;Studio.TextAt(g,Text,Font,Enabled?Studio.Text:Studio.Dim,new Rectangle(10,1,width-36,height-2),ContentAlignment.MiddleLeft,1/HighDpi.Zoom(this));PaintFrame(g,width,height);if(Focused&&ShowFocusCues)Studio.Focus(g,new Rectangle(4,4,width-29,height-8),Studio.Dim,Studio.Inset);}}
  protected override void OnSelectedIndexChanged(EventArgs e){base.OnSelectedIndexChanged(e);Invalidate();}
  protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
  protected override void OnDrawItem(DrawItemEventArgs e){if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;using(var b=new SolidBrush(selected?Color.FromArgb(67,70,56):Studio.Inset))e.Graphics.FillRectangle(b,e.Bounds);Studio.TextAt(e.Graphics,GetItemText(Items[e.Index]),Font,Enabled?Studio.Text:Studio.Dim,new Rectangle(e.Bounds.X+HighDpi.Pixels(8,this),e.Bounds.Y,e.Bounds.Width-HighDpi.UiPixels(12,this),e.Bounds.Height),ContentAlignment.MiddleLeft,HighDpi.Scale(this));}
  void PaintFrame(Graphics g,int width,int height){using(var p=new Pen(Studio.Line,2))g.DrawRectangle(p,1,1,width-2,height-2);using(var b=new SolidBrush(Studio.Inset))g.FillRectangle(b,width-22,2,20,height-4);using(var p=new Pen(Enabled?Studio.Amber:Studio.Dim,1.3f)){int x=width-12,y=height/2;g.DrawLines(p,new Point[]{new Point(x-4,y-2),new Point(x,y+2),new Point(x+4,y-2)});}}
 }
 public class StudioNumber:NumericUpDown {
  public StudioNumber(){foreach(Control child in Controls)if(!(child is TextBox))child.Paint+=(s,e)=>PaintButtons((Control)s,e.Graphics);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(Studio.Line))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
  static void PaintButtons(Control c,Graphics g){g.Clear(Studio.Panel);using(var coordinates=new Studio.PaintCoordinates(g,c))using(var p=new Pen(Studio.Dim)){int x=coordinates.Width/2,y=coordinates.Height/4;g.DrawLines(p,new Point[]{new Point(x-3,y+1),new Point(x,y-2),new Point(x+3,y+1)});y=coordinates.Height*3/4;g.DrawLines(p,new Point[]{new Point(x-3,y-1),new Point(x,y+2),new Point(x+3,y-1)});}}
 }
 public class StudioCheck:CheckBox {
  public string Description{get;set;}bool hover;StudioMotion thumb;
  public StudioCheck(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor|ControlStyles.ResizeRedraw,true);UseVisualStyleBackColor=false;AutoSize=false;BackColor=Color.Transparent;ForeColor=Studio.Text;Font=Studio.Medium(10);Cursor=Cursors.Hand;Height=66;TabStop=true;AccessibleRole=AccessibleRole.CheckButton;thumb=new StudioMotion(this,0,.045);}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnCheckedChanged(EventArgs e){if(thumb==null)thumb=new StudioMotion(this,Checked?1:0,.045);else thumb.To(Checked?1:0);base.OnCheckedChanged(e);AccessibleDescription=(Description??"")+"；"+(Checked?"已开启":"已关闭");Invalidate();}
  protected override void Dispose(bool disposing){if(disposing&&thumb!=null)thumb.Dispose();base.Dispose(disposing);}
  protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
  protected override void OnPaintBackground(PaintEventArgs e){}
  protected override void OnPaint(PaintEventArgs e){Studio.Backdrop(e.Graphics,this);var g=e.Graphics;using(var coordinates=new Studio.PaintCoordinates(g,this)){int width=coordinates.Width,height=coordinates.Height;if(width<8||height<12)return;var row=new Rectangle(0,3,width-1,height-8);
   using(var b=new LinearGradientBrush(row,hover&&Enabled?Color.FromArgb(56,62,52):Color.FromArgb(46,51,44),Color.FromArgb(37,43,36),90))Studio.FillRound(g,b,row,5);using(var p=new Pen(hover&&Enabled?Color.FromArgb(121,111,82):Studio.Line))using(var path=Studio.Round(row,5))g.DrawPath(p,path);
   Color textColor=Enabled?Studio.Text:Studio.Dim;Studio.TextAt(g,Text,Font,textColor,new Rectangle(14,9,Math.Max(1,width-124),24),ContentAlignment.MiddleLeft);using(var small=Studio.Font(8,FontStyle.Regular))Studio.TextAt(g,Description??"",small,Studio.Dim,new Rectangle(14,34,Math.Max(1,width-90),20),ContentAlignment.MiddleLeft);
   double position=thumb==null?(Checked?1:0):thumb.Value,lit=Enabled?position:0;int x=width-62,y=(height-24)/2;var track=new Rectangle(x,y,46,23);using(var b=new LinearGradientBrush(track,Studio.Mix(Color.FromArgb(21,28,21),Color.FromArgb(166,132,75),lit),Studio.Mix(Color.FromArgb(54,64,49),Color.FromArgb(208,176,112),lit),90))Studio.FillRound(g,b,track,12);using(var p=new Pen(Studio.Mix(Color.FromArgb(90,101,79),Studio.Amber,lit)))using(var path=Studio.Round(track,12))g.DrawPath(p,path);
   var cap=new RectangleF(x+3+(float)(position*22),y+3,17,17);using(var shadow=new SolidBrush(Color.FromArgb(80,0,0,0)))g.FillEllipse(shadow,cap.X,cap.Y+2,18,18);using(var b=new LinearGradientBrush(cap,Color.FromArgb(236,232,209),Color.FromArgb(119,132,106),70))g.FillEllipse(b,cap);using(var p=new Pen(Color.FromArgb(66,73,58)))g.DrawEllipse(p,cap);using(var p=new Pen(Color.FromArgb(168,249,244,215),.7f))g.DrawArc(p,cap.X+1,cap.Y+1,15,15,190,140);using(var small=Studio.Font(8,FontStyle.Regular))Studio.TextAt(g,Checked?"开":"关",small,Enabled&&Checked?Studio.Amber:Studio.Dim,new Rectangle(width-94,y,24,23),ContentAlignment.MiddleCenter);
   if(Focused&&ShowFocusCues)Studio.Focus(g,new Rectangle(5,7,width-11,height-16),Studio.Amber,Studio.Panel);
  }}
 }
 public class StudioHeading:Control {
  public string Number{get;set;}public string Caption{get;set;}
  public StudioHeading(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.ResizeRedraw,true);DoubleBuffered=true;BackColor=Color.Transparent;}
  protected override void OnPaint(PaintEventArgs e){using(var coordinates=new Studio.PaintCoordinates(e.Graphics,this)){var g=e.Graphics;int w=coordinates.Width;using(var number=new Font("Georgia",25,FontStyle.Regular))Studio.TextAt(g,Number,number,Studio.Amber,new Rectangle(0,0,57,42),ContentAlignment.MiddleLeft);using(var title=Studio.Medium(16))Studio.TextAt(g,Text,title,Studio.Text,new Rectangle(68,0,w-69,31),ContentAlignment.MiddleLeft);using(var caption=Studio.Caption(7.5f))Studio.TextAt(g,Caption,caption,Studio.Dim,new Rectangle(70,32,w-71,18),ContentAlignment.MiddleLeft);}}
 }
 public class StudioDivider:Control {
  public StudioDivider(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.ResizeRedraw,true);DoubleBuffered=true;BackColor=Color.Transparent;Font=Studio.Font(8,FontStyle.Regular);}
  protected override void OnPaint(PaintEventArgs e){using(var coordinates=new Studio.PaintCoordinates(e.Graphics,this)){var g=e.Graphics;int width=coordinates.Width,height=coordinates.Height;int label=(int)Math.Ceiling(g.MeasureString(Text,Font).Width/HighDpi.Scale(this))+10;Studio.TextAt(g,Text,Font,Studio.Amber,new Rectangle(0,0,label,height),ContentAlignment.MiddleLeft);using(var p=new Pen(Studio.Line))g.DrawLine(p,label,height/2,width-1,height/2);}}
 }
 public class StudioValue:Control {
  int minimum,maximum=100,value;bool dragging;
  public event EventHandler ValueChanged;
  public int Minimum{get{return minimum;}set{minimum=value;Value=this.value;}}public int Maximum{get{return maximum;}set{maximum=value;Value=this.value;}}
  public int Value{get{return value;}set{int n=Math.Max(minimum,Math.Min(maximum,value));if(n==this.value)return;this.value=n;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
  public StudioValue(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable|ControlStyles.SupportsTransparentBackColor,true);TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Slider;BackColor=Color.Transparent;}
  protected double Fraction{get{return (Value-Minimum)/(double)Math.Max(1,Maximum-Minimum);}}
  protected virtual void PointValue(Point p){Value=Minimum+(int)Math.Round(Math.Max(0,Math.Min(1,(p.X-HighDpi.UiPixels(12,this))/(double)Math.Max(1,Width-HighDpi.UiPixels(24,this))))*(Maximum-Minimum));}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();dragging=true;Capture=true;PointValue(e.Location);}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)PointValue(e.Location);}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
  protected override bool IsInputKey(Keys key){if(key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||key==Keys.Home||key==Keys.End)return true;return base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down){Value--;e.Handled=true;}else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up){Value++;e.Handled=true;}else if(e.KeyCode==Keys.Home){Value=Minimum;e.Handled=true;}else if(e.KeyCode==Keys.End){Value=Maximum;e.Handled=true;}}
  protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);Value+=e.Delta/120*2;}
  protected override AccessibleObject CreateAccessibilityInstance(){return new ValueAccessibility(this);}
  class ValueAccessibility:ControlAccessibleObject {StudioValue owner;public ValueAccessibility(StudioValue c):base(c){owner=c;}public override string Value{get{return owner.Value.ToString()+"%";}set{int v;if(Int32.TryParse(value.TrimEnd('%'),out v))owner.Value=v;}}public override string DefaultAction{get{return "调节音量";}}}
 }
 public class StudioMeter:Control {
  public float Peak{get;set;} public string Channel{get;set;} public bool Active{get;set;}
  double displayed=-48;Bitmap dial;bool dialActive;string dialChannel;long lastAnimation;
  public StudioMeter(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw|ControlStyles.SupportsTransparentBackColor,true);Size=new Size(300,170);Channel="L";BackColor=Color.Transparent;AccessibleRole=AccessibleRole.Indicator;}
  // A wider/taller window gives the instrument more space; its face and type keep
  // their native DPI size and aspect ratio. Small slots fit with a uniform scale.
  public RectangleF FaceBounds{get{float scale=Math.Min(HighDpi.UiScale(this),Math.Min(Width/300f,Height/170f));float w=300*scale,h=170*scale;return new RectangleF((float)Math.Floor((Width-w)/2),(float)Math.Floor((Height-h)/2),w,h);}}
  public void Animate(){long now=System.Diagnostics.Stopwatch.GetTimestamp();double dt=lastAnimation==0?.033:Math.Min(.1,(now-lastAnimation)/(double)System.Diagnostics.Stopwatch.Frequency);lastAnimation=now;double target=Active?Math.Max(-48,Math.Min(0,20*Math.Log10(Math.Max(.00001,Peak)))):-48;double before=displayed;displayed+=(target-displayed)*(1-Math.Exp(-dt/(target>displayed?.056:.35)));if(Math.Abs(displayed-target)<.02)displayed=target;if(Math.Abs(displayed-before)>.01||dial==null||dialActive!=Active)Invalidate(Rectangle.Ceiling(FaceBounds));}
  void ClearDial(){if(dial!=null){dial.Dispose();dial=null;}}
  protected override void OnSizeChanged(EventArgs e){ClearDial();base.OnSizeChanged(e);}
  protected override void OnDpiChangedAfterParent(EventArgs e){ClearDial();base.OnDpiChangedAfterParent(e);Invalidate();}
  protected override void Dispose(bool disposing){if(disposing)ClearDial();base.Dispose(disposing);}
  static PointF Polar(float radius,double angle){double a=angle*Math.PI/180;return new PointF((float)(150+radius*Math.Cos(a)),(float)(157+radius*Math.Sin(a)));}
  protected override void OnPaint(PaintEventArgs e){var bounds=FaceBounds;if(bounds.Width<1||bounds.Height<1)return;int w=(int)Math.Ceiling(bounds.Width),h=(int)Math.Ceiling(bounds.Height);float scale=bounds.Width/300f;if(dial==null||dial.Width!=w||dial.Height!=h||dialActive!=Active||dialChannel!=Channel){ClearDial();dial=new Bitmap(w,h,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(dial)){g.ScaleTransform(scale,scale);DrawDial(g);}dialActive=Active;dialChannel=Channel;}e.Graphics.DrawImageUnscaled(dial,(int)bounds.X,(int)bounds.Y);var state=e.Graphics.Save();var canvas=e.Graphics;canvas.TranslateTransform(bounds.X,bounds.Y);canvas.ScaleTransform(scale,scale);canvas.SmoothingMode=SmoothingMode.AntiAlias;var face=new Rectangle(8,8,284,151);using(var path=Studio.Round(face,3))canvas.SetClip(path,CombineMode.Intersect);DrawNeedle(canvas);DrawGlass(canvas);e.Graphics.Restore(state);}
  void DrawDial(Graphics g){g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;using(var b=new LinearGradientBrush(new Rectangle(0,0,300,170),Color.FromArgb(14,19,15),Color.FromArgb(103,109,88),90))Studio.FillRound(g,b,new Rectangle(0,0,300,169),6);using(var p=new Pen(Color.FromArgb(17,23,17),2))using(var path=Studio.Round(new Rectangle(2,2,296,164),5))g.DrawPath(p,path);using(var b=new SolidBrush(Color.FromArgb(17,21,16)))Studio.FillRound(g,b,new Rectangle(5,5,290,156),4);var face=new Rectangle(8,8,284,151);using(var b=new LinearGradientBrush(face,Active?Color.FromArgb(241,220,172):Color.FromArgb(194,187,157),Active?Color.FromArgb(194,163,103):Color.FromArgb(146,147,120),90))Studio.FillRound(g,b,face,3);
   using(var path=new GraphicsPath()){path.AddEllipse(-15,-110,330,290);using(var glow=new PathGradientBrush(path)){glow.CenterPoint=new PointF(145,60);glow.CenterColor=Color.FromArgb(Active?37:15,255,245,205);glow.SurroundColors=new[]{Color.FromArgb(0,255,244,209)};g.FillRectangle(glow,face);}}
   var state=g.Save();using(var path=Studio.Round(face,3))g.SetClip(path);using(var p=new Pen(Color.FromArgb(66,58,43),1.2f))g.DrawArc(p,32,39,236,236,-142,104);using(var p=new Pen(Color.FromArgb(150,63,44),2.5f))g.DrawArc(p,32,39,236,236,-51,13);
   for(int i=0;i<=32;i++){double angle=-142+i*104.0/32;bool major=i%8==0;using(var p=new Pen(i>=28?Color.FromArgb(147,59,39):Color.FromArgb(63,62,46),major?1.6f:.8f))g.DrawLine(p,Polar(major?107:112,angle),Polar(118,angle));}
   using(var font=new Font("Segoe UI",8,FontStyle.Regular,GraphicsUnit.Pixel))using(var ink=new SolidBrush(Color.FromArgb(61,61,47)))using(var fmt=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}){int[] db={-48,-36,-24,-12,0};for(int i=0;i<db.Length;i++){var p=Polar(131,-142+i*26);g.DrawString(db[i].ToString(),font,ink,p,fmt);}g.DrawString("dBFS",font,ink,new RectangleF(123,64,54,16),fmt);}
   using(var f=new Font("Georgia",25,FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(Color.FromArgb(94,83,58)))using(var fmt=new StringFormat{Alignment=StringAlignment.Center})g.DrawString("VU",f,b,new RectangleF(90,88,120,36),fmt);
   using(var f=new Font("Segoe UI",9,FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(Color.FromArgb(93,82,57)))using(var fmt=new StringFormat{Alignment=StringAlignment.Center})g.DrawString(Channel+"   /   OUTPUT LEVEL",f,b,new RectangleF(44,130,212,18),fmt);
   g.Restore(state);
  }
  void DrawNeedle(Graphics g){double a=-142+(displayed+48)/48*104;PointF needle=Polar(112,a);using(var p=new Pen(Color.FromArgb(40,50,39,25),3))g.DrawLine(p,152,159,needle.X+2,needle.Y+2);using(var p=new Pen(Color.FromArgb(139,46,29),1.6f))g.DrawLine(p,150,157,needle.X,needle.Y);using(var b=new SolidBrush(Color.FromArgb(54,54,38)))g.FillEllipse(b,143,150,14,14);using(var b=new LinearGradientBrush(new Rectangle(145,152,10,10),Color.FromArgb(162,150,110),Color.FromArgb(63,64,43),70))g.FillEllipse(b,145,152,10,10);}
  static void DrawGlass(Graphics g){var face=new Rectangle(8,8,284,151);using(var shine=new LinearGradientBrush(face,Color.FromArgb(25,255,253,228),Color.FromArgb(0,255,253,228),80))g.FillPolygon(shine,new[]{new Point(8,8),new Point(292,8),new Point(292,24),new Point(8,67)});using(var p=new Pen(Color.FromArgb(18,255,253,224),.7f))g.DrawLine(p,12,67,288,24);using(var p=new Pen(Color.FromArgb(29,24,25,16),5))g.DrawRectangle(p,9,9,282,149);using(var p=new Pen(Color.FromArgb(45,255,241,201),.7f))g.DrawLine(p,13,157,287,157);}
 }
 public class StatusLamp:Control {
  bool active,warning;public bool Active{get{return active;}set{if(active==value)return;active=value;Invalidate();}}public bool Warning{get{return warning;}set{if(warning==value)return;warning=value;Invalidate();}}
  public StatusLamp(){SetStyle(ControlStyles.SupportsTransparentBackColor,true);DoubleBuffered=true;BackColor=Studio.Background;ForeColor=Studio.Dim;Font=Studio.Font(9,FontStyle.Regular);Size=new Size(190,28);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;using(var coordinates=new Studio.PaintCoordinates(g,this)){int width=coordinates.Width,height=coordinates.Height;g.SmoothingMode=SmoothingMode.AntiAlias;Color c=Warning?Studio.Amber:Active?Studio.Green:Color.FromArgb(95,105,87);using(var b=new SolidBrush(Color.FromArgb(28,c)))g.FillEllipse(b,0,height/2-8,16,16);using(var b=new SolidBrush(c))g.FillEllipse(b,5,height/2-3,6,6);Studio.TextAt(g,Text,Font,c,new Rectangle(23,0,width-24,height),ContentAlignment.MiddleLeft);}}
 }
}
