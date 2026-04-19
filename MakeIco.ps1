$code = @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public class IcoGenerator
{
    public static void Generate(string pngPath, string icoPath)
    {
        using (var bitmap = new Bitmap(pngPath))
        {
            using (var resized = new Bitmap(256, 256))
            {
                using (var g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bitmap, 0, 0, 256, 256);
                }
                
                using (var ms = new MemoryStream())
                {
                    resized.Save(ms, ImageFormat.Png);
                    byte[] pngBytes = ms.ToArray();
                    
                    using (var fs = new FileStream(icoPath, FileMode.Create))
                    using (var bw = new BinaryWriter(fs))
                    {
                        bw.Write((short)0); // reserved
                        bw.Write((short)1); // type
                        bw.Write((short)1); // count
                        bw.Write((byte)0); // width 256
                        bw.Write((byte)0); // height 256
                        bw.Write((byte)0); // colors
                        bw.Write((byte)0); // reserved
                        bw.Write((short)1); // planes
                        bw.Write((short)32); // bpp
                        bw.Write(pngBytes.Length); // size
                        bw.Write((int)22); // offset
                        bw.Write(pngBytes);
                    }
                }
            }
        }
    }
}
"@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
[IcoGenerator]::Generate("$PWD\probeta.png", "$PWD\probeta.ico")
