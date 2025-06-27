using SkiaSharp;
using SkiaSharp.Views.Maui;
using Newtonsoft.Json;
using System.Text;
using InferencingSample;
namespace MauiApp1
{
    public partial class MainPage : ContentPage, IDisposable
    {
        RetinaFace? _detector;
        CheckPose? _checker;
        Aligner? _reader;
        CheckQuality? _quality;
        Embedding? _embedding;
        FAS? _fas;
        private SKBitmap? selectedImageBitmap;
        private bool _isInitialized = false;

        public MainPage()
        {
            InitializeComponent();
            // Initialize models asynchronously to avoid blocking UI thread
            _ = InitializeModelsAsync();
        }

        private async Task InitializeModelsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        _detector = new RetinaFace();
                        System.Diagnostics.Debug.WriteLine("RetinaFace initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"RetinaFace failed: {ex.Message}");
                        throw new Exception($"RetinaFace initialization failed: {ex.Message}", ex);
                    }

                    try
                    {
                        _checker = new CheckPose();
                        System.Diagnostics.Debug.WriteLine("CheckPose initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"CheckPose failed: {ex.Message}");
                        throw new Exception($"CheckPose initialization failed: {ex.Message}", ex);
                    }

                    try
                    {
                        _reader = new Aligner();
                        System.Diagnostics.Debug.WriteLine("Aligner initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Aligner failed: {ex.Message}");
                        throw new Exception($"Aligner initialization failed: {ex.Message}", ex);
                    }

                    try
                    {
                        _quality = new CheckQuality();
                        System.Diagnostics.Debug.WriteLine("CheckQuality initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"CheckQuality failed: {ex.Message}");
                        throw new Exception($"CheckQuality initialization failed: {ex.Message}", ex);
                    }

                    try
                    {
                        _embedding = new Embedding();
                        System.Diagnostics.Debug.WriteLine("Embedding initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Embedding failed: {ex.Message}");
                        throw new Exception($"Embedding initialization failed: {ex.Message}", ex);
                    }

                    try
                    {
                        _fas = new FAS();
                        System.Diagnostics.Debug.WriteLine("FAS initialized");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"FAS failed: {ex.Message}");
                        throw new Exception($"FAS initialization failed: {ex.Message}", ex);
                    }

                    _isInitialized = true;
                    System.Diagnostics.Debug.WriteLine("All models initialized successfully");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Model initialization failed: {ex}");
                await DisplayAlert("Initialization Error", $"Failed to initialize models: {ex.Message}\n\nPlease check that all model files are present and accessible.", "OK");
            }
        }

        async Task<(SKBitmap?, string)> RunInferenceAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    return (null, "Models are still initializing. Please wait.");
                }

                if (selectedImageBitmap == null)
                    throw new InvalidOperationException("No image selected");

                if (_detector == null || _checker == null || _reader == null || _quality == null || _embedding == null || _fas == null)
                    throw new InvalidOperationException("Models not properly initialized");

                var sampleImage = selectedImageBitmap;
                var sizeImage = _detector.GetSizeImage();

                var (result_detect, resizeBitmap) = await _detector.GetDetectionAsync(sampleImage);
                var result_norm = _checker.norm_crop(result_detect);
                var result_crop = _reader.WarpAffineWithImage(sizeImage[0], sizeImage[1], resizeBitmap, result_norm.ToRowArrays());

                // Resize result_crop to 112x112 with high quality
                SKBitmap result_crop_resize;
                using (var surface = SKSurface.Create(new SKImageInfo(112, 112)))
                {
                    if (surface == null)
                        throw new InvalidOperationException("Failed to create surface for image resizing");
                        
                    var canvas = surface.Canvas;
                    canvas.Clear();
                    
                    // Calculate scale to fit 224x224 to 112x112
                    float scaleX = 112f / result_crop.Width;
                    float scaleY = 112f / result_crop.Height;
                    float scale = Math.Min(scaleX, scaleY);
                    
                    canvas.Scale(scale);
                    canvas.DrawBitmap(result_crop, 0, 0);
                    canvas.Flush();
                    
                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        result_crop_resize = SKBitmap.Decode(data) ?? throw new InvalidOperationException("Failed to decode resized image");
                    }
                }

                var gray_image = _quality.createGrayImage(result_crop);
                var result_blue = _quality.PredictIsBlurGrayLaplacian(gray_image);
                var result_brighness = _quality.Brightness(gray_image);
                if(result_blue)
                {
                    System.Diagnostics.Debug.WriteLine("Face blue");
                }
                if (result_brighness)
                {
                    System.Diagnostics.Debug.WriteLine("Environment too bright or too dark");
                }


                var test_pose = await _checker.GetPoseAsync(result_crop_resize);
                bool pose_straight = PoseCheckStraight(test_pose["Yaw"], test_pose["Pitch"]);
                bool pose_left = PoseCheckLeft(test_pose["Yaw"], test_pose["Pitch"]);
                bool pose_right = PoseCheckRight(test_pose["Yaw"], test_pose["Pitch"]);

                if(pose_straight)
                {
                    System.Diagnostics.Debug.WriteLine("Pose is straight");
                }
                if(pose_left)
                {
                    System.Diagnostics.Debug.WriteLine("Pose is left");
                }
                if(pose_right)
                {
                    System.Diagnostics.Debug.WriteLine("Pose is right");
                }

                var embt = await _embedding.GetEmbAsync(result_crop_resize);


                //var pred = await _fas.GetFasAsync(result_crop);
                
                // Force garbage collection to help with memory pressure
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var emb_float_arr_t = embt.ToArray();
     
                
                string message_t;
                try
                {
                    message_t = await PostDataAsync(emb_float_arr_t);
                }
                catch (Exception ex)
                {
                    message_t = $"API Error: {ex.Message}";
                }

                // Dispose intermediate images to free memory
                result_crop?.Dispose();
                gray_image = null; // Help GC
                
                // Ensure result message is safe for display
                var safeMessage = message_t?.Length > 500 ? 
                    message_t.Substring(0, 500) + "..." : 
                    message_t ?? "No response";

                return (result_crop_resize, safeMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RunInferenceAsync error: {ex}");
                return (null, ex.Message);
            }
        }

        bool PoseCheckStraight(float yaw, float pitch)
        {
            bool check = Math.Abs(yaw) <= 10 && Math.Abs(pitch) <= 30;
            return check;
        }

        bool PoseCheckLeft(float yaw, float pitch)
        {
            bool check = yaw <= 55 && Math.Abs(pitch) <= 30;
            return check;
        }

        bool PoseCheckRight(float yaw, float pitch)
        {
            bool check = yaw >= -55 && Math.Abs(pitch) <= 30;
            return check;
        }

        async Task<string> PostDataAsync(float[] emb)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiNjVhMmNjY2ExYmFlZDgzNjNmNDY5ZWI3In0.JcJHMpP_wdv61pjR4x_A8sKO6GGp9AaVhNGAkyELKTE");

            var payload = new
            {
                staff_code = "ABCFABCF",
                embedding = emb,
                fas_flag = true,
                report_flag = true,
            };

            var payloadJson = JsonConvert.SerializeObject(payload);
            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://6899-171-244-194-10.ngrok-free.app/api/v1/staffs/search", content);

            var responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }

        async void OnSelectImageButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync();
                if (result != null)
                {
                    using (var stream = await result.OpenReadAsync())
                    {
                        selectedImageBitmap = SKBitmap.Decode(stream);
                    }

                    if (selectedImageBitmap == null)
                    {
                        await DisplayAlert("Error", "Failed to decode the selected image.", "OK");
                    }
                    else
                    {
                        canvasView.InvalidateSurface();
                        UseImgButton.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        async void OnUseImageButtonClicked(object sender, EventArgs e)
        {
            if (selectedImageBitmap != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Starting inference process...");
                    
                    // Show loading indicator
                    UseImgButton.Text = "Processing...";
                    UseImgButton.IsEnabled = false;
                    GetImgButton.IsEnabled = false;
                    
                    // Run inference with timeout
                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                    var result = await Task.Run(async () => await RunInferenceAsync(), cancellationTokenSource.Token);
                    
                    // Always show result message on UI thread
                    if (!string.IsNullOrEmpty(result.Item2))
                    {
                        await DisplayAlert("Result", result.Item2, "OK");
                    }
                    
                    if (result.Item1 != null)
                    {
                        // Dispose old bitmap to prevent memory leaks
                        selectedImageBitmap?.Dispose();
                        selectedImageBitmap = result.Item1;
                        canvasView.InvalidateSurface();
                        System.Diagnostics.Debug.WriteLine("Inference completed successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Inference returned null result");
                        if (string.IsNullOrEmpty(result.Item2))
                        {
                            await DisplayAlert("Error", "Inference failed with no result", "OK");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    await DisplayAlert("Timeout", "The inference process took too long and was cancelled.", "OK");
                    System.Diagnostics.Debug.WriteLine("Inference timed out");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Inference error: {ex}");
                    await DisplayAlert("Error", $"An error occurred during processing: {ex.Message}", "OK");
                }
                finally
                {
                    // Reset UI
                    UseImgButton.Text = "Use Image";
                    UseImgButton.IsEnabled = true;
                    GetImgButton.IsEnabled = true;
                }
            }
            else
            {
                await DisplayAlert("Error", "Please select an image first.", "OK");
            }
        }

        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();

            if (selectedImageBitmap != null)
            {
                float scaleX = (float)canvasView.CanvasSize.Width / selectedImageBitmap.Width;
                float scaleY = (float)canvasView.CanvasSize.Height / selectedImageBitmap.Height;
                float scale = Math.Min(scaleX, scaleY);

                int newWidth = (int)(selectedImageBitmap.Width * scale);
                int newHeight = (int)(selectedImageBitmap.Height * scale);

                float x = (canvasView.CanvasSize.Width - newWidth) / 2;
                float y = (canvasView.CanvasSize.Height - newHeight) / 2;

                canvas.DrawBitmap(selectedImageBitmap, new SKRect(x, y, x + newWidth, y + newHeight));
            }
        }

        

        public void Dispose()
        {
            selectedImageBitmap?.Dispose();
            selectedImageBitmap = null;
        }
    }
}
