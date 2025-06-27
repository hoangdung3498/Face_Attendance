using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace InferencingSample
{
    public class Aligner
    {
        public SKColor ReadPixelColor(int Width,int Height, SKBitmap bitmap, int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                // throw new ArgumentException("Invalid pixel coordinates.");
                Console.WriteLine("`ReadPixelColor`: Invalid pixel coordinates, out of bounds");
                return new SKColor(0, 0, 0, 0); // Transparent black color
            }

            return bitmap.GetPixel(x, y);
        }

        public float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
        public SKColor GetPixelBilinear(float fx, float fy, int imgWidth,int imgHeight, SKBitmap bitmap)
        {
            // Clamp to image boundaries
            fx = Clamp(fx, 0, imgWidth - 1);
            fy = Clamp(fy, 0, imgHeight - 1);

            // Get the surrounding coordinates and their weights
            int x0 = (int)Math.Floor(fx);
            int x1 = (int)Math.Ceiling(fx);
            int y0 = (int)Math.Floor(fy);
            int y1 = (int)Math.Ceiling(fy);
            float dx = fx - x0;
            float dy = fy - y0;
            float dx1 = 1.0f - dx;
            float dy1 = 1.0f - dy;

            // Get the original pixels
            SKColor pixel1 = ReadPixelColor(imgWidth, imgHeight, bitmap, x0, y0);
            SKColor pixel2 = ReadPixelColor(imgWidth, imgHeight, bitmap, x1, y0);
            SKColor pixel3 = ReadPixelColor(imgWidth, imgHeight, bitmap, x0, y1);
            SKColor pixel4 = ReadPixelColor(imgWidth, imgHeight, bitmap, x1, y1);

            int bilinear(int val1, int val2, int val3, int val4)
            {
                float result = val1 * dx1 * dy1 + val2 * dx * dy1 + val3 * dx1 * dy + val4 * dx * dy;
                return (int)Math.Round(result < 0 ? 0 : (result > 255 ? 255 : result));
            }

            // Calculate the weighted sum of pixels
            int r = bilinear(pixel1.Red, pixel2.Red, pixel3.Red, pixel4.Red);
            int g = bilinear(pixel1.Green, pixel2.Green, pixel3.Green, pixel4.Green);
            int b = bilinear(pixel1.Blue, pixel2.Blue, pixel3.Blue, pixel4.Blue);

            return new SKColor((byte)r, (byte)g, (byte)b, 255);
        }

        public SKBitmap WarpAffineWithImage(int widthImage , int heightImage, SKBitmap image_bit, float[][] affineMatrix, int width = 224, int height = 224)
        {
            Console.WriteLine($"affineMatrix: {affineMatrix[0][0]} {affineMatrix[0][1]} {affineMatrix[0][2]} {affineMatrix[1][0]} {affineMatrix[1][1]} {affineMatrix[1][2]}");

            Console.WriteLine("hahahahaha");
            //SKBitmap inputBitmap = SKBitmap.Decode(image_byte);
            SKBitmap inputBitmap = image_bit;
            // Transforming the transformation matrix for use on 112x112 images
            float[][] transformationMatrix = new float[affineMatrix.Length][];
            transformationMatrix = affineMatrix;
            //for (int i = 0; i < affineMatrix.Length; i++)
            //{
            //    transformationMatrix[i] = new float[affineMatrix[i].Length];
            //    for (int j = 0; j < affineMatrix[i].Length; j++)
            //    {
            //        transformationMatrix[i][j] = (affineMatrix[i][j] != 1.0f) ? affineMatrix[i][j] * 112 : 1.0f;
            //    }
            //}

            Console.WriteLine($"transformationMatrix: {transformationMatrix[0][0]} {transformationMatrix[0][1]} {transformationMatrix[0][2]} {transformationMatrix[1][0]} {transformationMatrix[1][1]} {transformationMatrix[1][2]} {transformationMatrix[2][0]} {transformationMatrix[2][1]} {transformationMatrix[2][2]}");

            if (width != 224 || height != 224)
            {
                throw new Exception("Width and height must be 112, other transformations are not supported yet.");
            }

            SKBitmap outputBitmap = new SKBitmap(width, height);

            SKMatrix a = new SKMatrix(transformationMatrix[0][0], transformationMatrix[0][1], transformationMatrix[0][2],
                                      transformationMatrix[1][0], transformationMatrix[1][1], transformationMatrix[1][2],
                                      0, 0, 1);

            a.TryInvert(out SKMatrix aInverse); //(6.628271102905273, -0.04164455458521843) (0.04164455458521843, 6.628271102905273)

            SKPoint b = new SKPoint(transformationMatrix[2][0], transformationMatrix[2][1]); //(11.148833274841309, 18.559925079345703)
            var b00 = b.X;
            var b10 = b.Y;
            var a00Prime = aInverse.ScaleX;
            var a01Prime = aInverse.SkewY;
            var a10Prime = aInverse.SkewX;
            var a11Prime = aInverse.ScaleY;

            for (int yTrans = 0; yTrans < height; ++yTrans)
            {
                for (int xTrans = 0; xTrans < width; ++xTrans)
                {
                    // Perform inverse affine transformation
                    //SKPoint point = new SKPoint(xTrans, yTrans);
                    //aInverse.MapPoints(new SKPoint[] { point });
                    float xOrigin = (xTrans - b00) * a00Prime + (yTrans - b10) * a01Prime;
                    float yOrigin = (xTrans - b00) * a10Prime + (yTrans - b10) * a11Prime;

                    SKColor pixel = GetPixelBilinear(xOrigin, yOrigin, widthImage, heightImage, inputBitmap);

                    outputBitmap.SetPixel(xTrans, yTrans, pixel);
                }
            }

            return outputBitmap;
        }

    }
}
