using MathNet.Numerics.LinearAlgebra;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using SkiaSharp;
using MathNet.Numerics.Statistics;

namespace InferencingSample
{
    public class CheckPose
    {
        const string ModelInputName = "input0";
        const string ModelOutputMaskName = "output0";
        const string ModelOutputYawName = "209";
        const string ModelOutputPitchName = "210";
        const string ModelOutputRollName = "211";
        const float temperture = 1.0f;
        private byte[]? _model;
        private byte[]? _sampleImage;
        private InferenceSession? _session;
        private Task? _initTask;

        public CheckPose()
        {
            _initTask = InitTask();
        }

        public async Task<byte[]> GetSampleImageAsync()
        {
            await InitAsync().ConfigureAwait(false);
            return _sampleImage ?? throw new InvalidOperationException("Sample image not initialized");
        }

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

                using var modelStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.MaskPoseTest_opset13.quant.onnx") 
                    ?? throw new InvalidOperationException("Model resource not found");
            using var modelMemoryStream = new MemoryStream();

            modelStream.CopyTo(modelMemoryStream);
            _model = modelMemoryStream.ToArray();

            _session = new InferenceSession(_model);

            //Get sample image (create dummy if not found)
            //var sampleImageStream1 = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.right_img_1.jpg");
            //if (sampleImageStream1 != null)
            //{
            //    using var sampleImageMemoryStream1 = new MemoryStream();
            //    sampleImageStream1.CopyTo(sampleImageMemoryStream1);
            //    _sampleImage = sampleImageMemoryStream1.ToArray();
            //    sampleImageStream1.Dispose();
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

        public async Task<Dictionary<string, float>> GetPoseAsync(SKBitmap sourceBitmap)
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

                    channelData[rChannelIndex] = (pixelR - 127.5f) / 128.0f;
                    channelData[gChannelIndex] = (pixelG - 127.5f) / 128.0f;
                    channelData[bChannelIndex] = (pixelB - 127.5f) / 128.0f;

                    channelDataIndex++;
                }
            }


            var input = new DenseTensor<float>(channelData, new[] { 1, 3, 112, 112 });


            using var results = _session.Run(new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(ModelInputName, input) });


            var outputRawMask = results.FirstOrDefault(i => i.Name == ModelOutputMaskName);
            var outputRawYaw = results.FirstOrDefault(i => i.Name == ModelOutputYawName);
            var outputRawPitch = results.FirstOrDefault(i => i.Name == ModelOutputPitchName);
            var outputRawRoll = results.FirstOrDefault(i => i.Name == ModelOutputRollName);


            if (outputRawMask == null || outputRawYaw == null || outputRawPitch == null || outputRawRoll == null)
                return [];


            var Mask = outputRawMask.AsTensor<float>().ToList();
            var Yaw = outputRawYaw.AsTensor<float>().ToList();
            var Pitch = outputRawPitch.AsTensor<float>().ToList();
            var Roll = outputRawRoll.AsTensor<float>().ToList();

            var realYaw = ConvertToReal(Yaw, temperture);
            var realPitch = ConvertToReal(Pitch, temperture);
            var realRoll = ConvertToReal(Roll, temperture);

            var realMask = (Mask[0] > Mask[1]) ? 1f : 0f;

            Dictionary<string, float> pose = new Dictionary<string, float>

            {

                { "Mask", realMask },

                { "Yaw", realYaw },

                { "Pitch", realPitch },

                { "Roll", realRoll }

            };

            return pose;
        }
        public Matrix<float> norm_crop(List<RetinaFaceBoundingBox> results, bool estimate_scale = true)
        {
            float[,] landmark_2d_arr = new float[5, 2];
            for (var j = 0; j < 5; j++)
            {
                var list_row = results[0].Point[j];
                landmark_2d_arr[j, 0] = list_row.X;
                landmark_2d_arr[j, 1] = list_row.Y;
            }
            //Stopwatch stopwatchInNorm = new Stopwatch();
            //stopwatchInNorm.Start();
            var M = Matrix<float>.Build;
            float[,] dst = { { 38.2946f * 2f, 51.6963f * 2f }, { 73.5318f * 2f, 51.5014f * 2f }, { 56.0252f * 2f, 71.7366f * 2f }, { 41.5493f * 2f, 92.3655f * 2f }, { 70.7299f * 2f, 92.2041f * 2f } };
            //float[,] dst = { { 38.2946f, 51.6963f}, { 73.5318f, 51.5014f}, { 56.0252f, 71.7366f}, { 41.5493f, 92.3655f}, { 70.7299f, 92.2041f} };
            Matrix<float> dst_matrix = M.DenseOfArray(dst);
            //double[,] src = { { 467.32767, 533.8805 }, { 670.4482, 536.5202 }, { 632.30707, 652.0111 }, { 507.60132, 798.708 }, { 659.24936, 796.9324 } };
            Matrix<float> src_matrix = M.DenseOfArray(landmark_2d_arr);

            var onesColumn = Vector<float>.Build.Dense(src_matrix.RowCount, 1f);
            //var lmkTranMatrix = src_matrix.InsertColumn(2, onesColumn);
            //var min_M = Matrix<float>.Build.Dense(0, 0);
            //var min_index = -1;
            //var min_error = float.PositiveInfinity;

            int num = src_matrix.RowCount;
            int dim = src_matrix.ColumnCount;

            var srcMean = src_matrix.ColumnSums() / num;
            var dstMean = dst_matrix.ColumnSums() / num;

            Matrix<float> srcDemean = src_matrix.Clone();
            for (int i = 0; i < num; i++)
            {
                srcDemean.SetRow(i, srcDemean.Row(i) - srcMean);
            }
            Matrix<float> dstDemean = dst_matrix.Clone();
            for (int i = 0; i < num; i++)
            {
                dstDemean.SetRow(i, dstDemean.Row(i) - dstMean);
            }
            //var srcDemean = src_matrix.EnumerateRows().Select(row => row - srcMean).ToArray();
            //var dstDemean = dst_matrix.EnumerateRows().Select(row => row - dstMean).ToArray();

            //var srcDemean_matrix = CreateMatrixFromArrayOfVectors(srcDemean);
            //var dstDemean_matrix = CreateMatrixFromArrayOfVectors(dstDemean);
            //var srcDemean_matrix = srcDemean;
            //var dstDemean_matrix = dstDemean;

            Matrix<float> result_matrix_demean = dstDemean.Transpose().Multiply(srcDemean) / num;

            var d = Vector<float>.Build.Dense(dim, 1f);
            if (result_matrix_demean.Determinant() < 0)
                d[dim - 1] = -1;

            var T = Matrix<float>.Build.DenseIdentity(dim + 1);
            var svd = result_matrix_demean.Svd();
            var U = svd.U;
            U[0, 1] = U[0, 1] * (-1f);
            U[1, 1] = U[1, 1] * (-1f);
            var S = svd.S;
            var V = svd.VT.Transpose();
            V[0, 1] = V[0, 1] * (-1f);
            V[1, 1] = V[1, 1] * (-1f);

            int rank = result_matrix_demean.Rank();
            if (rank == 0)
            {
                var K = Matrix<float>.Build.Dense(dim + 1, dim + 1, float.NaN);
            }
            else if (rank == dim - 1)
            {
                if (U.Determinant() * V.Determinant() > 0)
                {
                    T.SetSubMatrix(0, dim, 0, dim, U * V);
                }
                else
                {
                    var s = d[dim - 1];
                    d[dim - 1] = -1f;
                    T.SetSubMatrix(0, dim, 0, dim, U * Matrix<float>.Build.DenseOfDiagonalVector(d) * V);
                    d[dim - 1] = s;

                }
            }
            else
            {
                T.SetSubMatrix(0, dim, 0, dim, U * Matrix<float>.Build.DenseOfDiagonalVector(d) * V);
            }

            float scale;
            if (estimate_scale)
            {
                var columnVariances = srcDemean.EnumerateColumns()
                      .Select(column => (float)Statistics.Variance(column))
                      .ToArray();
                columnVariances[0] = columnVariances[0] * (4f) / 5;
                columnVariances[1] = columnVariances[1] * (4f) / 5;
                var sum = columnVariances[0] + columnVariances[1];
                scale = 1f / sum * S * d;
            }
            else
            {
                scale = 1f;
            }

            T.SetSubMatrix(0, dim, dim, 1, dstMean.ToColumnMatrix() - scale * (T.SubMatrix(0, dim, 0, dim) * srcMean).ToColumnMatrix());
            T.SetSubMatrix(0, dim, 0, dim, T.SubMatrix(0, dim, 0, dim) * scale);

            //////****/////////
            //var MM = T.SubMatrix(0, dim, 0, dim + 1);
            //var results_matrix = MM.Multiply(lmkTranMatrix.Transpose()).Transpose();

            //var squaredDifferences = (results_matrix - dst_matrix).PointwisePower(2);
            //var error = squaredDifferences.RowSums().PointwiseSqrt().Sum();
            //if (error < min_error)
            //{
            //    min_error = error;
            //    min_M = MM;
            //    min_index = 0;
            //}
            var LL = T.Transpose();

            return LL;
        }
        private Vector<float> SoftmaxTemperature(Vector<float> vector, float temperature)
        {
            var exponentials = vector.PointwiseExp() / temperature;
            var result = exponentials / exponentials.Sum();
            return result;
        }

        private float ConvertToReal(List<float> yawRaw, float temperature)
        {
            // Convert yawRaw to MathNet Vector
            var tensor1 = Vector<float>.Build.DenseOfEnumerable(yawRaw);

            // Apply softmax temperature
            var tensorOut1 = SoftmaxTemperature(tensor1, temperature);

            // Generate idxTensor
            var idxTensor = Vector<float>.Build.Dense(62);
            for (int i = 0; i < 62; i++)
            {
                idxTensor[i] = i;
            }

            // Calculate yaw
            var yaw = tensorOut1.DotProduct(idxTensor) * 3 - 93;

            return yaw;
        }
    }
}
