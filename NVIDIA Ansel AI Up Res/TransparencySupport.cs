using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
            // Load images to memory and directly merge alpha value from grayscale into the full image.

            // Loads full image and locks data to memory
            Image fullImage_img = Image.FromFile(origin_path);
            Bitmap fullImage_bit = new Bitmap(fullImage_img);
            BitmapData fullImage_bitData = fullImage_bit.LockBits(new Rectangle(0, 0, fullImage_bit.Width, fullImage_bit.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            // Loads alpha image and locks data to memory
            Image alphaImage_img = Image.FromFile(alphaChannel_path);
            Bitmap alphaImage_bit = new Bitmap(alphaImage_img);
            BitmapData alphaImage_bitData = alphaImage_bit.LockBits(new Rectangle(0, 0, alphaImage_bit.Width, alphaImage_bit.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            int Height = fullImage_bit.Height;
            int Width = fullImage_bit.Width;

            unsafe
            {
                for (int y = 0; y < Height; ++y)
                {
                    byte* fullImage_row = (byte*)fullImage_bitData.Scan0 + (y * fullImage_bitData.Stride);
                    byte* alphaImage_row = (byte*)alphaImage_bitData.Scan0 + (y * alphaImage_bitData.Stride);
                    int columnOffset = 0;
                    for (int x = 0; x < Width; ++x)
                    {
                        // Change alpha value of a pixel within fullImage_bitData to the R value of the same location pixel in alphaImage_bitData
                        // note: R value in pixel within alphaImage_bitData will be 0-255 and represnt the alpha of the image.
                        fullImage_row[columnOffset + 3] = alphaImage_row[columnOffset + 0];
                        columnOffset += 4;
                    }
                }
            }
            // Unlocks images memory
            fullImage_bit.UnlockBits(fullImage_bitData);
            alphaImage_bit.UnlockBits(alphaImage_bitData);

            // Cleanup
            fullImage_img.Dispose();
            alphaImage_bit.Dispose();
            alphaImage_img.Dispose();

            return fullImage_bit;
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
            return (byte)((float)(source) * (float)(alpha) / (float)(byte.MaxValue) + 0.5f);
        }
   
    }
}
