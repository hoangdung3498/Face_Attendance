using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using SkiaSharp;

namespace InferencingSample
{
    public class FAS
    {
        const string ModelInputName = "input0";
        const string ModelOutputName = "output0";
        private byte[]? _model;
        private InferenceSession? _session;
        private Task? _initTask;

        public FAS()
        {
            _initTask = Task.Run(InitTask);
        }

        //public async Task<byte[]> GetSampleImageAsync()
        //{
        //    await InitAsync().ConfigureAwait(false);
        //    return _sampleImage;
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

                using var modelStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.FAS_small_Final.onnx")
                    ?? throw new InvalidOperationException("Model resource not found");
            using var modelMemoryStream = new MemoryStream();

            modelStream.CopyTo(modelMemoryStream);
            _model = modelMemoryStream.ToArray();

            _session = new InferenceSession(_model);

            //using var sampleImageStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.straight_res.jpg");
            //using var sampleImageMemoryStream = new MemoryStream();
            //sampleImageStream.CopyTo(sampleImageMemoryStream);
            //_sampleImage = sampleImageMemoryStream.ToArray();
            });
        }



        //float NormWithVector(List<float> list)
        //{
        //    Vector<float> vector = Vector<float>.Build.DenseOfEnumerable(list);
        //    return (float)vector.Norm(2);
        //}
        public async Task<List<float>> GetFasAsync(SKBitmap sourceBitmap)
        {
            await InitAsync().ConfigureAwait(false);
            if (_session == null)
                throw new InvalidOperationException("Session not initialized");

            var pixels = sourceBitmap.Bytes;

            var bytesPerPixel = sourceBitmap.BytesPerPixel;
            var rowLength = 224 * bytesPerPixel;
            var channelLength = 224 * 224;
            var channelData = new float[channelLength * 3];
            var channelDataIndex = 0;

            for (int y = 0; y < 224; y++)
            {
                var rowOffset = y * rowLength;

                for (int x = 0, columnOffset = 0; x < 224; x++, columnOffset += bytesPerPixel)
                {
                    var pixelOffset = rowOffset + columnOffset;

                    var pixelR = pixels[pixelOffset];
                    var pixelG = pixels[pixelOffset + 1];
                    var pixelB = pixels[pixelOffset + 2];

                    var rChannelIndex = channelDataIndex ;
                    var gChannelIndex = channelDataIndex + channelLength;
                    var bChannelIndex = channelDataIndex + (channelLength * 2);

                    channelData[rChannelIndex] = (pixelR - 0.485f * 255.0f) / (0.229f * 255.0f);
                    channelData[gChannelIndex] = (pixelG - 0.456f * 255.0f) / (0.224f * 255.0f);
                    channelData[bChannelIndex] = (pixelB - 0.406f * 255.0f) / (0.225f * 255.0f);

                    //channelData[rChannelIndex] = pixelR;
                    //channelData[gChannelIndex] = pixelG;
                    //channelData[bChannelIndex] = pixelB; 

                    channelDataIndex++;
                }
            }


            var input = new DenseTensor<float>(channelData, new[] { 1, 3,224 , 224 });


            using var results = _session.Run(new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(ModelInputName, input) });


            var outputRaw = results.FirstOrDefault(i => i.Name == ModelOutputName);

            if (outputRaw == null)
                return [];

            var pred = outputRaw.AsTensor<float>().ToList();
            //var result = Softmax(pred);
            return pred;


        }

        private List<float> Softmax(List<float> input)
        {
            List<float> output = new List<float>(input.Count);

            float max = input.Max(); // Find the maximum value for numerical stability
            float sumExp = 0.0f;

            // Calculate the sum of the exponentials
            foreach (var value in input)
            {
                sumExp += (float)Math.Exp(value - max);
            }

            // Calculate the softmax values
            foreach (var value in input)
            {
                output.Add((float)Math.Exp(value - max) / sumExp);
            }

            return output;
        }
    }
}
