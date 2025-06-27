using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms.Internals;

namespace InferencingSample
{
    public class CheckQuality
    {
        public List<List<int>> createGrayImage(SKBitmap originalBitmap)
        {
            // Create a new bitmap with the same dimensions as the original image
            //SKBitmap grayBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height);
            List<List<int>> grayList = new List<List<int>>();
            // Iterate through each pixel in the original image
            for (int y = 0; y < originalBitmap.Height; y++)
            {
                List<int> row = new List<int>();
                for (int x = 0; x < originalBitmap.Width; x++)
                {
                    // Get the color of the pixel
                    SKColor color = originalBitmap.GetPixel(x, y);

                    // Calculate the grayscale value using the formula: Gray = (R + G + B) / 3
                    //byte grayValue = (byte)Math.Round(color.Red * 0.229f + color.Green * 0.587f + color.Blue * 0.114f).Clamp(0,255);
                    int intensity = (int)Math.Round(color.Red * 0.229f + color.Green * 0.587f + color.Blue * 0.114f).Clamp(0, 255);
                    row.Add(intensity);
                    // Set the grayscale value for the pixel in the new bitmap
                    //grayBitmap.SetPixel(x, y, new SKColor(grayValue, grayValue, grayValue));
                }
                grayList.Add(row);
            }
            return grayList;

        }

        public bool Brightness(List<List<int>> grayImage)
        {
            // Calculate mean brightness
            double totalBrightness = 0;
            int pixelCount = 0;

            foreach (var row in grayImage)
            {
                foreach (var pixel in row)
                {
                    totalBrightness += pixel;
                    pixelCount++;
                }
            }

            // Compute mean brightness
            double meanBrightness = totalBrightness / pixelCount;

            // Output mean brightness
            Console.WriteLine($"Brightness: {meanBrightness}");

            // Check if brightness falls within the specified range
            if (meanBrightness >= 100 && meanBrightness <= 300)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public (bool, double) PredictIsBlurGrayLaplacian(
            List<List<int>> grayImage,
            int threshold = 10)
        {
            List<List<int>> laplacian = ApplyLaplacian(grayImage);
            double variance = CalculateVariance(laplacian);
            //_logger.Info($"Variance: {variance}");
            return (variance < threshold, variance);
        }

        private double CalculateVariance(List<List<int>> matrix)
        {
            int numRows = matrix.Count;
            int numCols = matrix[0].Count;
            int totalElements = numRows * numCols;

            // Calculate the mean
            double mean = matrix.SelectMany(row => row).Sum() / (double)totalElements;

            // Calculate the variance
            double variance = matrix.SelectMany(row => row)
                                    .Aggregate(0.0, (acc, value) => acc + Math.Pow(value - mean, 2)) / totalElements;

            return variance;
        }

        private List<List<int>> PadImage(List<List<int>> image)
        {
            int numRows = image.Count;
            int numCols = image[0].Count;

            // Create a new matrix with extra padding
            List<List<int>> paddedImage = Enumerable.Range(0, numRows + 2)
                                                    .Select(i => Enumerable.Repeat(0, numCols + 2).ToList())
                                                    .ToList();

            // Copy original image into the center of the padded image
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    paddedImage[i + 1][j + 1] = image[i][j];
                }
            }

            // Reflect padding
            // Top and bottom rows
            for (int j = 1; j <= numCols; j++)
            {
                paddedImage[0][j] = paddedImage[2][j]; // Top row
                paddedImage[numRows + 1][j] = paddedImage[numRows - 1][j]; // Bottom row
            }
            // Left and right columns
            for (int i = 0; i < numRows + 2; i++)
            {
                paddedImage[i][0] = paddedImage[i][2]; // Left column
                paddedImage[i][numCols + 1] = paddedImage[i][numCols - 1]; // Right column
            }

            return paddedImage;
        }

        public List<List<int>> ApplyLaplacian(List<List<int>> image)
        {
            List<List<int>> paddedImage = PadImage(image);
            int numRows = image.Count;
            int numCols = image[0].Count;
            List<List<int>> outputImage = Enumerable.Range(0, numRows)
                                                    .Select(i => Enumerable.Repeat(0, numCols).ToList())
                                                    .ToList();

            // Define the Laplacian kernel
            List<List<int>> kernel = new List<List<int>>
            {
            new List<int> {0, 1, 0},
            new List<int> {1, -4, 1},
            new List<int> {0, 1, 0}
            };

            // Apply the kernel to each pixel
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    int sum = 0;
                    for (int ki = 0; ki < 3; ki++)
                    {
                        for (int kj = 0; kj < 3; kj++)
                        {
                            sum += paddedImage[i + ki][j + kj] * kernel[ki][kj];
                        }
                    }
                    // Adjust the output value if necessary (e.g., clipping)
                    outputImage[i][j] = sum; //.clamp(0, 255);
                }
            }

            return outputImage;
        }
    }
}
