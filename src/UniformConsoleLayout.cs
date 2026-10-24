using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Layout;

namespace DualSpeaker {
 sealed class FixedLayoutEngine:LayoutEngine {
  internal static readonly FixedLayoutEngine Instance=new FixedLayoutEngine();
  public override bool Layout(object container,LayoutEventArgs e){return false;}
 }
 public class StudioFlowLayout:FlowLayoutPanel {
  internal bool FixedLayout;
  public override LayoutEngine LayoutEngine{get{return FixedLayout?FixedLayoutEngine.Instance:base.LayoutEngine;}}
 }
 // Record the already laid-out native controls once, then fit that composition
 // uniformly. Each control paints at its new pixel size; no screenshot scaling.
 sealed class UniformConsoleLayout:IDisposable {
  sealed class Item {public Control Control;public Rectangle Bounds;public Font Font;public bool NativeFont;public Padding Padding;}
  readonly MainForm form;readonly Control root;readonly Size baseline;readonly float initialDpi;
  readonly List<Item> items=new List<Item>();Dictionary<Font,Font> fonts=new Dictionary<Font,Font>();
  bool applying;float previousZoom=-1;Size previousSize;
  public float Zoom{get;private set;}
  public UniformConsoleLayout(MainForm owner,Control shell){form=owner;root=shell;baseline=shell.Size;initialDpi=HighDpi.Dpi(form);Zoom=1;Capture(shell);foreach(var item in items){var c=item.Control;c.SuspendLayout();var table=c as StudioLayout;if(table!=null)table.FixedLayout=true;var flow=c as StudioFlowLayout;if(flow!=null)flow.FixedLayout=true;c.Dock=DockStyle.None;c.Anchor=AnchorStyles.Top|AnchorStyles.Left;c.MinimumSize=Size.Empty;c.MaximumSize=Size.Empty;}foreach(var item in items)item.Control.ResumeLayout(false);}
  void Capture(Control c){items.Add(new Item{Control=c,Bounds=c.Bounds,Font=c.Font,NativeFont=c is Label||c is ComboBox||c is TextBox||c is NumericUpDown,Padding=c.Padding});if(c is ComboBox||c is TextBox||c is NumericUpDown)return;foreach(Control child in c.Controls)Capture(child);}
  public void Apply(){if(applying||form.IsDisposed||form.WindowState==FormWindowState.Minimized||form.ClientSize.Width<1||form.ClientSize.Height<1)return;
   float scale=Math.Min(form.ClientSize.Width/(float)baseline.Width,form.ClientSize.Height/(float)baseline.Height);float zoom=scale*initialDpi/HighDpi.Dpi(form);if(previousSize==form.ClientSize&&Math.Abs(previousZoom-zoom)<.0001)return;applying=true;Zoom=zoom;var nextFonts=new Dictionary<Font,Font>();bool changedFont=Math.Abs(previousZoom-zoom)>.0001;
   form.SuspendLayout();foreach(var item in items)item.Control.SuspendLayout();
   try{foreach(var item in items){var c=item.Control;if(item.NativeFont&&changedFont){Font font;if(!nextFonts.TryGetValue(item.Font,out font)){font=new Font(item.Font.FontFamily,Math.Max(1,item.Font.SizeInPoints*zoom),item.Font.Style,GraphicsUnit.Point);nextFonts.Add(item.Font,font);}c.Font=font;}
     if(c==root){int w=(int)Math.Round(baseline.Width*scale),h=(int)Math.Round(baseline.Height*scale);c.SetBounds((form.ClientSize.Width-w)/2,(form.ClientSize.Height-h)/2,w,h);}else{var r=item.Bounds;int x=(int)Math.Round(r.X*scale),y=(int)Math.Round(r.Y*scale);c.SetBounds(x,y,Math.Max(1,(int)Math.Round(r.Right*scale)-x),Math.Max(1,(int)Math.Round(r.Bottom*scale)-y));}
     var p=item.Padding;c.Padding=new Padding((int)Math.Round(p.Left*scale),(int)Math.Round(p.Top*scale),(int)Math.Round(p.Right*scale),(int)Math.Round(p.Bottom*scale));var combo=c as StudioCombo;if(combo!=null)combo.UpdateUiScale();c.Invalidate();
    }previousSize=form.ClientSize;previousZoom=zoom;
   }finally{for(int i=items.Count-1;i>=0;i--)items[i].Control.ResumeLayout(false);form.ResumeLayout(false);applying=false;}
   if(changedFont){foreach(var font in fonts.Values)font.Dispose();fonts=nextFonts;}
  }
  public void Dispose(){foreach(var font in fonts.Values)font.Dispose();fonts.Clear();}
 }
}
