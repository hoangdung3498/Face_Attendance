using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using SkiaSharp;
using System.Text;
using Newtonsoft.Json;

namespace InferencingSample
{
    public class Embedding
    {
        const string ModelInputName = "input0";
        const string ModelOutputName = "output0";
        private byte[]? _model;
        private InferenceSession? _session;
        private Task? _initTask;

        public Embedding()
        {
            _initTask = Task.Run(InitTask);
        }

        //public async Task<byte[]> GetSampleImageAsync()
        //{
        //    await InitAsync().ConfigureAwait(false);
        //    return _sampleImage ?? throw new InvalidOperationException("Sample image not initialized");
        //}

        Task InitAsync()
        {
            if (_initTask == null || _initTask.IsFaulted)
                _initTask = Task.Run(InitTask);

            return _initTask;
        }

        Task InitTask()
        {
            return Task.Run(() =>
        {
            var assembly = GetType().Assembly;

                using var modelStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.FaceFeature_opset13.quant.onnx")
                    ?? throw new InvalidOperationException("Model resource not found");
            using var modelMemoryStream = new MemoryStream();

            modelStream.CopyTo(modelMemoryStream);
            _model = modelMemoryStream.ToArray();

            _session = new InferenceSession(_model);

            // Get sample image (create dummy if not found)
            //var sampleImageStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.left_img_1.jpg");
            //if (sampleImageStream != null)
            //{
            //    using var sampleImageMemoryStream = new MemoryStream();
            //    sampleImageStream.CopyTo(sampleImageMemoryStream);
            //    _sampleImage = sampleImageMemoryStream.ToArray();
            //    sampleImageStream.Dispose();
            //}
            //else
            //{
            //    // Create a dummy sample image if not found
            //    using var dummyBitmap = new SKBitmap(112, 112);
            //    using var canvas = new SKCanvas(dummyBitmap);
            //    canvas.Clear(SKColors.Gray);
            //    using var image = SKImage.FromBitmap(dummyBitmap);
            //    using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
            //    _sampleImage = data.ToArray();
            //}
            });
        }

        float NormDirect(List<float> list)
        {
            float sumOfSquares = 0;
            foreach (float element in list)
            {
                sumOfSquares += element * element;
            }
            return (float)Math.Sqrt(sumOfSquares);
        }

        public async Task<List<float>> GetEmbAsync(SKBitmap sourceBitmap)
        {
            await InitAsync().ConfigureAwait(false);
            if (_session == null)
                throw new InvalidOperationException("Session not initialized");

            var pixels = sourceBitmap.Bytes;

            var bytesPerPixel = sourceBitmap.BytesPerPixel;
            var rowLength = 112 * bytesPerPixel;
            var channelLength = 112 * 112;
            var channelData = new float[channelLength * 3];
            var channelDataIndex = 0;

            for (int y = 0; y < 112; y++)
            {
                var rowOffset = y * rowLength;

                for (int x = 0, columnOffset = 0; x < 112; x++, columnOffset += bytesPerPixel)
                {
                    var pixelOffset = rowOffset + columnOffset;

                    var pixelR = pixels[pixelOffset];
                    var pixelG = pixels[pixelOffset + 1];
                    var pixelB = pixels[pixelOffset + 2];

                    var rChannelIndex = channelDataIndex + (channelLength * 2);
                    var gChannelIndex = channelDataIndex + channelLength;
                    var bChannelIndex = channelDataIndex;

                    channelData[rChannelIndex] = (pixelR / 255f - 0.5f) / 0.5f;
                    channelData[gChannelIndex] = (pixelG / 255f - 0.5f) / 0.5f;
                    channelData[bChannelIndex] = (pixelB / 255f - 0.5f) / 0.5f;

                    channelDataIndex++;
                }
            }

            var input = new DenseTensor<float>(channelData, new[] { 1, 3, 112, 112 });

            using var results = _session.Run(new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(ModelInputName, input) });

            var outputRaw = results.FirstOrDefault(i => i.Name == ModelOutputName);

            if (outputRaw == null)
                return [];

            var emb = outputRaw.AsTensor<float>().ToList();
            var norm = NormDirect(emb);

            for (int i = 0; i < emb.Count; i++)
            {
                emb[i] /= norm;
            }
            return emb;
        }

        public float FindCosineDistance(List<float> source_representation, List<float> test_representation)
        {
            float dotProduct = 0.0f;
            for (int i = 0; i < source_representation.Count; i++)
            {
                dotProduct += source_representation[i] * test_representation[i];
            }

            float magSource = 0.0f;
            foreach (float element in source_representation)
            {
                magSource += element * element;
            }

            float magTest = 0.0f;
            foreach (float element in test_representation)
            {
                magTest += element * element;
            }

            float distance = dotProduct / (float)(Math.Sqrt(magSource) * Math.Sqrt(magTest));
            return distance;
        }

        public async Task<string> PostDataAsync(float[] emb)
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

            var response = await client.PostAsync("https://5045-171-244-194-10.ngrok-free.app/api/v1/staffs/search", content);

            var responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }
    }
}
