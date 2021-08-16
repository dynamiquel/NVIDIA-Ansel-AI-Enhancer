using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace NVIDIA_Ansel_AI_Up_Res
{
    public static class TransparencySupport
    {
        public static bool HasTransparency(Image img)
        {
            Bitmap bitmap = new Bitmap(img);
            // Not an alpha-capable color format. Note that GDI+ indexed images are alpha-capable on the palette.
            if (((ImageFlags)bitmap.Flags & ImageFlags.HasAlpha) == 0)
                return false;
            // Indexed format, and no alpha colours in the image's palette: immediate pass.
            if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0 && bitmap.Palette.Entries.All(c => c.A == 255))
                return false;
            // Get the byte data 'as 32-bit ARGB'. This offers a converted version of the image data without modifying the original image.
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            Int32 len = bitmap.Height * data.Stride;
            Byte[] bytes = new Byte[len];
            Marshal.Copy(data.Scan0, bytes, 0, len);
            bitmap.UnlockBits(data);
            bitmap.Dispose();
            // Check the alpha bytes in the data. Since the data is little-endian, the actual byte order is [BB GG RR AA]
            for (Int32 i = 3; i < len; i += 4)
                if (bytes[i] != 255)
                    return true;
            return false;
        }

        public static Bitmap UpscaleWithAlpha(string upscaled_path, string ip, double resolution, Func<string, bool, string, string> startUpscale)
        {
            Image source_img = Image.FromFile(ip);
            // Convert all image's pixel to white - preserveing only alpha values
            Bitmap alpha_img = ToAlphaChannel(new Bitmap(source_img));
            string alpha_path = ip + "_alpha";
            alpha_img.Save(alpha_path);
            alpha_img.Dispose();
            source_img.Dispose();

            // Upscale the alpha channel image (using color - as the edges looks more clean that way)
            string alphaUpscale_command = $"{alpha_path} {resolution} 2";
            string alphaUpscale_path = startUpscale(alphaUpscale_command, false, "alpha");
            Bitmap fullImage = MergeAlphaChannel(upscaled_path, alphaUpscale_path);

            // CleanUp
            File.Delete(upscaled_path);
            File.Delete(alpha_path);
            File.Delete(alphaUpscale_path);

            return fullImage;

        }

        private static Bitmap MergeAlphaChannel(string origin_path, string alphaChannel_path)
        {
            Image origin_img = Image.FromFile(origin_path),
                alphaChannel_img = Image.FromFile(alphaChannel_path);
            Bitmap origin_bit = new Bitmap(origin_img),
                alphaChannel_bit = new Bitmap(alphaChannel_img);

            Bitmap target = new Bitmap(origin_bit.Width, origin_bit.Height);
            // Paints each pixel of the image.
            // TODO: find faster method to combine "origin_img" & "alphaChannel_img"

            for (int y = 0; y < origin_bit.Height; ++y)
            {
                for (int x = 0; x < origin_bit.Width; ++x)
                {
                    Color OriginPixel = origin_bit.GetPixel(x,y);
                    Color AlphaPixel = alphaChannel_bit.GetPixel(x, y);
                    byte alpha = AlphaPixel.R;
                    Color newPixel = Color.FromArgb(alpha,
                        PremultiplyAlpha(OriginPixel.R, alpha,y),
                        PremultiplyAlpha(OriginPixel.G, alpha,y),
                        PremultiplyAlpha(OriginPixel.B, alpha,y));
                    // Creating new pixel of the final image, RGB data from original image,
                    // while alpha data is from "alpha" image(black/white)

                    target.SetPixel(x, y, newPixel);
                }
            }
            origin_img.Dispose();
            alphaChannel_img.Dispose();
            origin_bit.Dispose();
            alphaChannel_bit.Dispose();
            return target;
        }

        private static Bitmap ToAlphaChannel(Bitmap Image)
        {
            Bitmap NewBitmap = (Bitmap)Image.Clone();
            BitmapData data = NewBitmap.LockBits(
                new Rectangle(0, 0, NewBitmap.Width, NewBitmap.Height),
                ImageLockMode.ReadWrite,
                NewBitmap.PixelFormat);
            int Height = NewBitmap.Height;
            int Width = NewBitmap.Width;

            unsafe
            {
                for (int y = 0; y < Height; ++y)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    int columnOffset = 0;
                    for (int x = 0; x < Width; ++x)
                    {
                        row[columnOffset + 0] = (byte)255;
                        row[columnOffset + 1] = (byte)255;
                        row[columnOffset + 2] = (byte)255;
                        columnOffset += 4;
                    }
                }
            }
            NewBitmap.UnlockBits(data);
            return NewBitmap;
        }

        private static byte PremultiplyAlpha(byte source, byte alpha,int y)
        {
            if (y > 400)
                return source;
            return (byte)((float)(source) * (float)(alpha) / (float)(byte.MaxValue) + 0.5f);
        }
    }
}
