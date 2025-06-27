using System.Drawing;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace InferencingSample
{
    public class RetinaFace
    {
        const int DimBatchSize = 1;
        const int DimNumberOfChannels = 3;
        const int ImageSizeX = 320;
        const int ImageSizeY = 320;
        const float threshold_conf = 0.8f;
        const float threshod_iou = 0.5f;
        const string ModelInputName = "input0";
        const string ModelOutputBoxName = "output0";
        const string ModelOutputConfName = "595";
        const string ModelOutputLandName = "594";
        
        private List<Tuple<float, float>> min_sizes =
        [
            Tuple.Create(16F, 32F),
            Tuple.Create(64F, 128F),
            Tuple.Create(256F, 512F)
        ];
        private float[] steps =
        [
            8F, 16F, 32F
        ];
        private float[] variance =
        [
            0.1F, 0.2F
        ];
        private List<Tuple<int, int>> feature =
        [
            Tuple.Create((int)Math.Ceiling(ImageSizeY/(8f)), (int)Math.Ceiling(ImageSizeX/(8f))),
            Tuple.Create((int)Math.Ceiling(ImageSizeY/(16f)), (int)Math.Ceiling(ImageSizeX/(16f))),
            Tuple.Create((int)Math.Ceiling(ImageSizeY/(32f)), (int)Math.Ceiling(ImageSizeX/(32f)))
        ];

        private byte[]? _model;
        private InferenceSession? _session;
        private Task? _initTask;

        public RetinaFace()
        {
            _initTask = InitTask();
        }

        //public async Task<byte[]> GetSampleImageAsync()
        //{
        //    await InitAsync().ConfigureAwait(false);
        //    return _sampleImage ?? throw new InvalidOperationException("Sample image not initialized");
        //}

        public List<int> GetSizeImage()
        {
            
            return new List<int>() {ImageSizeX,ImageSizeY};
        }

        //public async Task<SKImage> GetOrgImageAsync()
        //{
        //    await InitAsync().ConfigureAwait(false);
        //    return _orgImage ?? throw new InvalidOperationException("Original image not initialized");
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

                // Get model
                using var modelStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.FaceDetectorNewmobilenet320x320.onnx")
                    ?? throw new InvalidOperationException("Model resource not found");
                using var modelMemoryStream = new MemoryStream();

                modelStream.CopyTo(modelMemoryStream);
                _model = modelMemoryStream.ToArray();

                _session = new InferenceSession(_model);

                // Get sample image
                //using var sampleImageStream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.straight_img_1.jpg")
                //    ?? throw new InvalidOperationException("Sample image resource not found");
                //using var sampleImageMemoryStream = new MemoryStream();

                //sampleImageStream.CopyTo(sampleImageMemoryStream);
                //_sampleImage = sampleImageMemoryStream.ToArray();
                //_orgImage = SKImage.FromBitmap(SKBitmap.Decode(_sampleImage));
            });
        }

        public async Task<(List<RetinaFaceBoundingBox>, SKBitmap)> GetDetectionAsync(SKBitmap sourceBitmap)
        {
            await InitAsync().ConfigureAwait(false);
            if (_session == null)
                throw new InvalidOperationException("Session not initialized");

            // The channelData array stores the normalized RGB values sequentially one channel at a time (instead of the original RGB, RGB, ... sequence) i.e.
            // first all the R values,
            // then all the G values,
            // then all the B values

            // Determine the scale factor to fit the original bitmap within the desired size
            float scale = Math.Min((float)ImageSizeX / sourceBitmap.Width, (float)ImageSizeY / sourceBitmap.Height);

            // Calculate the new width and height maintaining the aspect ratio
            int newWidth = (int)(sourceBitmap.Width * scale);
            int newHeight = (int)(sourceBitmap.Height * scale);

            // Create a new SKBitmap with the desired size
            SKBitmap paddedBitmap = new SKBitmap(ImageSizeX, ImageSizeY);

            // Create an SKCanvas to draw on the new bitmap
            using (var canvas = new SKCanvas(paddedBitmap))
            {
                // Fill the canvas with black color (or any color representing padding)
                canvas.Clear(new SKColor(0, 0, 0));

                // Calculate the position to center the resized bitmap
                int x = (ImageSizeX - newWidth) / 2;
                int y = (ImageSizeY - newHeight) / 2;

                // Define the destination rectangle
                var destRect = new SKRect(x, y, x + newWidth, y + newHeight);

                // Draw the original bitmap onto the new bitmap at the calculated position
                canvas.DrawBitmap(sourceBitmap, destRect);
            }
            var pixels = paddedBitmap.Bytes;

            // The resulting channelData array is used to create the requisite Tensor object as input to the InferenceSession.Run method
            var bytesPerPixel = paddedBitmap.BytesPerPixel;
            var rowLength = ImageSizeX * bytesPerPixel;
            var channelLength = ImageSizeX * ImageSizeY;
            var channelData = new float[channelLength * 3];
            var channelDataIndex = 0;

            for (int y = 0; y < ImageSizeY; y++)
            {
                var rowOffset = y * rowLength;

                for (int x = 0, columnOffset = 0; x < ImageSizeX; x++, columnOffset += bytesPerPixel)
                {
                    var pixelOffset = rowOffset + columnOffset;

                    var pixelR = pixels[pixelOffset];
                    var pixelG = pixels[pixelOffset + 1];
                    var pixelB = pixels[pixelOffset + 2];

                    var rChannelIndex = channelDataIndex + (channelLength * 2);
                    var gChannelIndex = channelDataIndex + channelLength;
                    var bChannelIndex = channelDataIndex;

  
                    channelData[rChannelIndex] = (pixelR - 123f) / 1.0f;
                    channelData[gChannelIndex] = (pixelG - 117f) / 1.0f;
                    channelData[bChannelIndex] = (pixelB - 104f) / 1.0f;

                    channelDataIndex++;
                }
            }

            // Create Tensor model input
            // The model expects input to be in the shape of (N x 3 x H x W) i.e.
            // mini-batches (where N is the batch size) of 3-channel RGB images with H and W of 224
            var input = new DenseTensor<float>(channelData, new[] { DimBatchSize, DimNumberOfChannels, ImageSizeY,ImageSizeX});

            // Run inferencing
            using var results = _session.Run(new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(ModelInputName, input) });

            // Resolve model output
            var outputBox = results.FirstOrDefault(i => i.Name == ModelOutputBoxName);
            var outputConf = results.FirstOrDefault(i => i.Name == ModelOutputConfName);
            var outputLand = results.FirstOrDefault(i => i.Name == ModelOutputLandName);

            if (outputBox == null || outputConf == null || outputLand == null)
                return ([], paddedBitmap);

            // Postprocess output (get highest score and corresponding label)
            var rawBox = outputBox.AsTensor<float>().ToList();
            var rawConf = outputConf.AsTensor<float>().ToList();
            var rawLand = outputLand.AsTensor<float>().ToList();

            var boxes_before = ParseOutputs(rawBox, rawConf, rawLand, threshold_conf);
            var boxes_after = FilterBoundingBoxes(boxes_before, 1, threshod_iou);


            return (boxes_after.ToList(), paddedBitmap);
        }

        private List<Tuple<float, float>> Make_grid_func(int height, int width)
        {
            return Enumerable.Range(0, height)
                 .SelectMany(i => Enumerable.Range(0, width)
                 .Select(j => Tuple.Create((float)i, (float)j)))
                 .ToList();
        }

        private float IntersectionOverUnion(RectangleF boundingBoxA, RectangleF boundingBoxB)
        {
            var areaA = boundingBoxA.Width * boundingBoxA.Height;

            if (areaA <= 0)
                return 0;

            var areaB = boundingBoxB.Width * boundingBoxB.Height;

            if (areaB <= 0)
                return 0;

            var minX = Math.Max(boundingBoxA.Left, boundingBoxB.Left);
            var minY = Math.Max(boundingBoxA.Top, boundingBoxB.Top);
            var maxX = Math.Min(boundingBoxA.Right, boundingBoxB.Right);
            var maxY = Math.Min(boundingBoxA.Bottom, boundingBoxB.Bottom);

            var intersectionArea = Math.Max(maxY - minY, 0) * Math.Max(maxX - minX, 0);

            return intersectionArea / (areaA + areaB - intersectionArea);
        }
        private IList<RetinaFaceBoundingBox> ParseOutputs(List<float> rawBox, List<float> rawConf, List<float> rawLand, float threshold)
        {
            var boxes = new List<RetinaFaceBoundingBox>();
            int num_count = -1;
            foreach (var item in feature)
            {
                int index = feature.IndexOf(item);
                var min_size = min_sizes[index];
                foreach (var grid in Make_grid_func(item.Item1, item.Item2))
                {
                    float[] arr_minsize = new float[] { min_size.Item1, min_size.Item2 };

                    for (int i = 0; i < arr_minsize.Length; i++)
                    {
                        num_count++;
                        float confidence = rawConf[num_count * 2 + 1];
                        if (confidence < threshold)
                            continue;

                        float s_kx = arr_minsize[i] / ImageSizeX;
                        float s_ky = arr_minsize[i] / ImageSizeY;
                        float dense_cx = (grid.Item2 + .5f) * steps[index] / ImageSizeX;
                        float dense_cy = (grid.Item1 + .5f) * steps[index] / ImageSizeY;


                        float x1_box_old = dense_cx + rawBox[num_count * 4] * variance[0] * s_kx;
                        float y1_box_old = dense_cy + rawBox[num_count * 4 + 1] * variance[0] * s_ky;
                        float x2_box_old = (float)(s_kx * Math.Exp(rawBox[num_count * 4 + 2] * variance[1]));
                        float y2_box_old = (float)(s_ky * Math.Exp(rawBox[num_count * 4 + 3] * variance[1]));

                        float x1_box = x1_box_old - x2_box_old / 2;
                        float y1_box = y1_box_old - y2_box_old / 2;
                        float x2_box = x2_box_old + x1_box;
                        float y2_box = y2_box_old + y1_box;

                        float w_box = x2_box - x1_box;
                        float h_box = y2_box - y1_box;

                        float x_point1 = dense_cx + rawLand[num_count * 10] * variance[0] * s_kx;
                        float y_point1 = dense_cy + rawLand[num_count * 10 + 1] * variance[0] * s_ky;
                        float x_point2 = dense_cx + rawLand[num_count * 10 + 2] * variance[0] * s_kx;
                        float y_point2 = dense_cy + rawLand[num_count * 10 + 3] * variance[0] * s_ky;
                        float x_point3 = dense_cx + rawLand[num_count * 10 + 4] * variance[0] * s_kx;
                        float y_point3 = dense_cy + rawLand[num_count * 10 + 5] * variance[0] * s_ky;
                        float x_point4 = dense_cx + rawLand[num_count * 10 + 6] * variance[0] * s_kx;
                        float y_point4 = dense_cy + rawLand[num_count * 10 + 7] * variance[0] * s_ky;
                        float x_point5 = dense_cx + rawLand[num_count * 10 + 8] * variance[0] * s_kx;
                        float y_point5 = dense_cy + rawLand[num_count * 10 + 9] * variance[0] * s_ky;

                        //float confidence = yoloModelOutputs[1][num_count * 2 + 1];

                        //num_count++;

                        //if (confidence < threshold)
                        //    continue;


                        boxes.Add(new RetinaFaceBoundingBox()
                        {
                            Dimensions = new BoundingBoxDimensions
                            {
                                X = x1_box * ImageSizeX,
                                Y = y1_box * ImageSizeY,
                                Width = w_box * ImageSizeX,
                                Height = h_box * ImageSizeY,
                            },
                            Landmarks = new List<BoundingBoxDimensions>
                            {
                                new BoundingBoxDimensions{ X =x_point1* ImageSizeX,Y = y_point1* ImageSizeY,Width=0,Height=0 },
                                new BoundingBoxDimensions{ X =x_point2* ImageSizeX,Y = y_point2* ImageSizeY,Width=0,Height=0 },
                                new BoundingBoxDimensions{ X =x_point3* ImageSizeX,Y = y_point3* ImageSizeY,Width=0,Height=0 },
                                new BoundingBoxDimensions{ X =x_point4* ImageSizeX,Y = y_point4* ImageSizeY,Width=0,Height=0 },
                                new BoundingBoxDimensions{ X =x_point5* ImageSizeX,Y = y_point5* ImageSizeY,Width=0,Height=0 }
                            },
                            Confidence = confidence,
                            Label = "face",
                            BoxColor = System.Drawing.Color.AliceBlue
                        });
                    }
                }
            }
            return boxes;
        }

        private IList<RetinaFaceBoundingBox> FilterBoundingBoxes(IList<RetinaFaceBoundingBox> boxes, int limit, float threshold)
        {
            var activeCount = boxes.Count;
            var isActiveBoxes = new bool[boxes.Count];

            for (int i = 0; i < isActiveBoxes.Length; i++)
                isActiveBoxes[i] = true;

            var sortedBoxes = boxes.Select((b, i) => new { Box = b, Index = i })
                                .OrderByDescending(b => b.Box.Confidence)
                                .ToList();

            var results = new List<RetinaFaceBoundingBox>();

            for (int i = 0; i < boxes.Count; i++)
            {
                if (isActiveBoxes[i])
                {
                    var boxA = sortedBoxes[i].Box;
                    results.Add(boxA);

                    if (results.Count >= limit)
                        break;

                    for (var j = i + 1; j < boxes.Count; j++)
                    {
                        if (isActiveBoxes[j])
                        {
                            var boxB = sortedBoxes[j].Box;

                            if (IntersectionOverUnion(boxA.Rect, boxB.Rect) > threshold)
                            {
                                isActiveBoxes[j] = false;
                                activeCount--;

                                if (activeCount <= 0)
                                    break;
                            }
                        }
                    }

                    if (activeCount <= 0)
                        break;
                }
            }
            return results;
        }
    }


    public class BoundingBoxDimensions
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }
    }

    public class RetinaFaceBoundingBox
    {
        public required BoundingBoxDimensions Dimensions { get; set; }
        public required List<BoundingBoxDimensions> Landmarks { get; set; }
        public required string Label { get; set; }
        public float Confidence { get; set; }
        public RectangleF Rect => new RectangleF(Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height);
        public List<System.Drawing.PointF> Point => Landmarks.Select(l => new System.Drawing.PointF(l.X, l.Y)).ToList();
        public System.Drawing.Color BoxColor { get; set; }
    }
}

