using MathNet.Numerics.RootFinding;
using SkiaSharp;
using System;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xamarin.Forms;
using SkiaSharp.Views.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Essentials;
namespace InferencingSample
{
    public partial class MainPage : ContentPage
    {
        RetinaFace _detector;
        CheckPose _checker;
        Aligner _reader;
        CheckQuality _quality;
        Embedding _embedding;
        FAS _fas;
        SKBitmap selectedImageBitmap;
        public MainPage()
        {
            InitializeComponent();
            _detector = new RetinaFace();
            _checker = new CheckPose();
            _reader = new Aligner();
            _quality = new CheckQuality();
            _embedding = new Embedding();
            _fas = new FAS();
        }

        async Task<SKBitmap> RunInferenceAsync()
        {
            GetImgButton.IsEnabled = false;
            UseImgButton.IsEnabled = false;
            try
            {
                //var sampleImage = await _detector.GetSampleImageAsync();
                //var tll = SKBitmap.Decode(sampleImage);
                var sampleImage = selectedImageBitmap;

                var sizeImage = _detector.GetSizeImage();
                //Stopwatch stopwatchTotal = new Stopwatch();
                //Stopwatch stopwatchDetect = new Stopwatch();
                //Stopwatch stopwatchAlign = new Stopwatch();
                //Stopwatch stopwatchQuality = new Stopwatch();
                //Stopwatch stopwatchPose = new Stopwatch();
                //Stopwatch stopwatchEmbedding = new Stopwatch();
                //Stopwatch stopwatchNorm = new Stopwatch();
                //stopwatchTotal.Start();

                //stopwatchDetect.Start();
                var (result_detect, resizeBitmap) = await _detector.GetDetectionAsync(sampleImage);
                //stopwatchDetect.Stop();


                //stopwatchNorm.Start();
                var result_norm = _checker.norm_crop(result_detect);
                //stopwatchNorm.Stop();

                //stopwatchAlign.Start();
                var result_crop = _reader.WarpAffineWithImage(sizeImage[0], sizeImage[1], resizeBitmap, result_norm.ToRowArrays());
                //stopwatchAlign.Stop();

                // Resize result_crop to 112x112 with high quality
                var result_crop_resize = new SKBitmap(112, 112, result_crop.ColorType, result_crop.AlphaType);
                using (var surface = SKSurface.Create(new SKImageInfo(112, 112)))
                {
                    var canvas = surface.Canvas;
                    canvas.SetMatrix(SKMatrix.CreateScale(0.5f, 0.5f)); // Scale to half size (224->112)
                    canvas.DrawBitmap(result_crop, 0, 0);
                    canvas.Flush();
                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        result_crop_resize = SKBitmap.Decode(data);
                    }
                }

                //stopwatchQuality.Start();
                var gray_image = _quality.createGrayImage(result_crop);
                var result_blue = _quality.PredictIsBlurGrayLaplacian(gray_image);
                var result_brighness = _quality.Brightness(gray_image);
                //stopwatchQuality.Stop();

                var imgl = await _embedding.GetSampleImageAsync();
                var imgr = await _checker.GetSampleImageAsync();
                var imgt = await _detector.GetSampleImageAsync();
                using var imt = SKBitmap.Decode(imgt);
                using var imr = SKBitmap.Decode(imgr);
                using var iml = SKBitmap.Decode(imgl);

                //stopwatchPose.Start();
                var test_pose = await _checker.GetPoseAsync(result_crop_resize);
                //stopwatchPose.Stop();


                ////stopwatchEmbedding.Start();
                //var embt = await _embedding.GetEmbAsync(result_crop_resize);
                var embr = await _embedding.GetEmbAsync(imr);
                var embt = await _embedding.GetEmbAsync(imt);
                var embl = await _embedding.GetEmbAsync(iml);
                var distance1 = _embedding.FindCosineDistance(embt, embr);
                var distance2 = _embedding.FindCosineDistance(embl, embt);
                var distance3 = _embedding.FindCosineDistance(embl, embr);
                ////stopwatchEmbedding.Stop();

                //stopwatchTotal.Stop();

                //Console.WriteLine("Time Detect: " + stopwatchDetect.ElapsedMilliseconds + " ms");
                //Console.WriteLine("Time Norm: " + stopwatchNorm.ElapsedMilliseconds + " ms");
                //Console.WriteLine("Time Align: " + stopwatchAlign.ElapsedMilliseconds + " ms");
                //Console.WriteLine("Time Quality: " + stopwatchQuality.ElapsedMilliseconds + " ms");
                //Console.WriteLine("Time Pose: " + stopwatchPose.ElapsedMilliseconds + " ms");
                //Console.WriteLine("Time Embedding: " + stopwatchEmbedding.ElapsedMilliseconds + " ms");
                //Console.WriteLine("Time Total: " + stopwatchTotal.ElapsedMilliseconds + " ms");

                var pred = await _fas.GetFasAsync(result_crop);

                var emb_float_arr_t = embt.ToArray();
                var emb_float_arr_r = embr.ToArray();
                var emb_float_arr_l = embl.ToArray();
                var message_t = await _embedding.PostDataAsync(emb_float_arr_t);
                var message_r = await _embedding.PostDataAsync(emb_float_arr_r);
                var message_l = await _embedding.PostDataAsync(emb_float_arr_l);
                ////var distance = _embedding.FindCosineDistance(test_emb1, test_emb2);
                await DisplayAlert("Result", Convert.ToString(message_t), "OK");

                return result_crop_resize;

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
                return null;
            }
            finally
            {
                GetImgButton.IsEnabled = true;
                UseImgButton.IsEnabled = true;
            }
        }

        //void RunButton_Clicked(object sender, EventArgs e)
        //    => _ = RunInferenceAsync();

        //async void OnSelectImageButtonClicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        // Request permission to access photo gallery
        //        var status = await Permissions.RequestAsync<Permissions.Photos>();
        //        if (status != PermissionStatus.Granted)
        //        {
        //            // Permission denied, handle accordingly
        //            return;
        //        }

        //        // Launch the photo picker
        //        var result = await MediaPicker.PickPhotoAsync();

        //        // Check if a photo was selected
        //        if (result != null)
        //        {
        //            // Load the selected image into an SKBitmap
        //            using (var stream = await result.OpenReadAsync())
        //            {
        //                selectedImageBitmap = SKBitmap.Decode(stream);
        //            }

        //            // Refresh the canvas to display the selected image
        //            canvasView.InvalidateSurface();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle exceptions
        //        Console.WriteLine($"Error: {ex.Message}");
        //    }
        //}

        async void OnSelectImageButtonClicked(object sender, EventArgs e)
        {
            try
            {
                //var status = await Permissions.RequestAsync<Permissions.Photos>();
                //if (status != PermissionStatus.Granted)
                //{
                //    Console.WriteLine("status == PermissionStatus.Granted");
                //    await DisplayAlert("Error", "Permission denied. Please grant access to your photos.", "OK");
                //    return;
                //}

                var result = await MediaPicker.PickPhotoAsync();
                Console.WriteLine("continuee1");
                if (result != null)
                {
                    using (var stream = await result.OpenReadAsync())
                    {
                        selectedImageBitmap = SKBitmap.Decode(stream);
                        Console.WriteLine("continuee2");
                    }

                    if (selectedImageBitmap == null)
                    {
                        Console.WriteLine("selectedImageBitmap is null");
                        await DisplayAlert("Error", "Failed to decode the selected image.", "OK");
                    }
                    else
                    {
                        canvasView.InvalidateSurface();  // To refresh the canvas
                        UseImgButton.IsEnabled = true;  // Enable 'USE IMAGE' button
                        Console.WriteLine("continuee3");
                    }
                }
                else
                {
                    Console.WriteLine("result == null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("catch (Exception ex)");
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }


        async void OnUseImageButtonClicked(object sender, EventArgs e)
        {
            // Check if an image has been selected
            if (selectedImageBitmap != null)
            {
                // Call another function to use the selected image
                // Replace 'YourFunctionToUseImage' with the actual function name
                selectedImageBitmap = await RunInferenceAsync();
                canvasView.InvalidateSurface();
            }
            else
            {
                // Inform the user to select an image first
                await DisplayAlert("Error", "Please select an image first.", "OK");
            }
        }

        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            // Clear canvas
            canvas.Clear();

            // Draw the selected image on the canvas
            if (selectedImageBitmap != null)
            {
                //canvas.DrawBitmap(selectedImageBitmap, new SKPoint(0, 0));
                // Calculate scaling factor based on canvas size and image size
                float scaleX = (float)canvasView.CanvasSize.Width / selectedImageBitmap.Width;
                float scaleY = (float)canvasView.CanvasSize.Height / selectedImageBitmap.Height;
                float scale = Math.Min(scaleX, scaleY);

                // Calculate new dimensions for the scaled image
                int newWidth = (int)(selectedImageBitmap.Width * scale);
                int newHeight = (int)(selectedImageBitmap.Height * scale);

                // Calculate the position to center the image on the canvas
                float x = (canvasView.CanvasSize.Width - newWidth) / 2;
                float y = (canvasView.CanvasSize.Height - newHeight) / 2;

                // Draw the scaled image on the canvas
                canvas.DrawBitmap(selectedImageBitmap, new SKRect(x, y, x + newWidth, y + newHeight));
            }
        }

    }


}