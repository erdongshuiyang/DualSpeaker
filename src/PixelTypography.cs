using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace DualSpeaker {
 static class PixelTypography {
  [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern int AddFontResourceEx(string name,uint flags,IntPtr reserved);
  static readonly PrivateFontCollection collection=new PrivateFontCollection();
  public static readonly FontFamily Family=Load();
  static FontFamily Load(){string path=Path.Combine(Path.GetDirectoryName(typeof(PixelTypography).Assembly.Location),"fonts","fusion-pixel-12px-proportional-zh_hans.ttf");try{if(File.Exists(path)){collection.AddFontFile(path);AddFontResourceEx(path,0x10,IntPtr.Zero);return collection.Families[0];}}catch{}return new FontFamily("Microsoft YaHei UI");}
  public static bool IsPixel(Font font){return font.FontFamily.Name.StartsWith("Fusion Pixel",StringComparison.OrdinalIgnoreCase);}
  public static TextRenderingHint Hint(Font font){return IsPixel(font)?TextRenderingHint.SingleBitPerPixelGridFit:TextRenderingHint.ClearTypeGridFit;}
 }
}
