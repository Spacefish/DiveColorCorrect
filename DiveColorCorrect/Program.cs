using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DiveColorCorrect
{
    class Program
    {
        static void Main(string[] args)
        {
            var wb = OpenCvSharp.XPhoto.SimpleWB.Create();
            wb.P = 0.2f;
            wb.OutputMin = 8.0f;

            var decodedFramesBlock = new TransformBlock<OpenCvSharp.Mat, OpenCvSharp.Mat>(
                mat =>
                {
                    return Task.Run(() => { wb.BalanceWhite(mat, mat); return mat; });
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 10,
                    MaxDegreeOfParallelism = 10,
                    EnsureOrdered = true,
                }
            );

            // var inputVideo = new OpenCvSharp.VideoCapture("2017_0928_105336_023.MOV");
            var inputVideo = new OpenCvSharp.VideoCapture("YI004801.mp4");
            var outputVideo = new OpenCvSharp.VideoWriter("out_lina.mp4", "avc1", inputVideo.Fps, new OpenCvSharp.Size(inputVideo.FrameWidth, inputVideo.FrameHeight));

            ConcurrentQueue<OpenCvSharp.Mat> decodedFramesQueue = new ConcurrentQueue<OpenCvSharp.Mat>();

            // decoder
            Task decodeTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var mat = inputVideo.RetrieveMat();
                    if (mat.Height == 0)
                    {
                        Console.WriteLine("DECODED");
                        decodedFramesBlock.Complete();
                        break;
                    }
                    decodedFramesBlock.SendAsync(mat).Wait();
                }
            });

            Task writeTask = Task.Factory.StartNew(() =>
            {
                int c = 0;
                OpenCvSharp.Mat mat;
                while (!decodedFramesBlock.Completion.IsCompleted)
                {
                    ++c;
                    mat = decodedFramesBlock.Receive();
                    outputVideo.Write(mat);
                    mat.Dispose();
                    Console.WriteLine("Frame " + c + " of " + inputVideo.FrameCount);
                }
            });

            decodeTask.Wait();
            writeTask.Wait();

            Console.WriteLine("ready!");
            outputVideo.Release();
            /*
            var channels = img.Split();
            channels = channels.Select(c => c.EqualizeHist()).ToArray();
            foreach(var channel in channels)
            {
                channel.ConvertTo(channel, channel.Type(), 0.9, 0);
            }
            OpenCvSharp.Cv2.Merge(channels, img);
            */
        }
    }
}
