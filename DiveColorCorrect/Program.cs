using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiveColorCorrect
{
    class Program
    {
        static void Main(string[] args)
        {
            var img = new OpenCvSharp.Mat("divescene.png", OpenCvSharp.ImreadModes.Color);
            var wb = OpenCvSharp.XPhoto.SimpleWB.Create();
            wb.P = 0.2f;
            wb.OutputMin = 8.0f;
            AutoResetEvent decodeQueueThrottleWaiter = new AutoResetEvent(false);
            ManualResetEvent decodeQueueFrameWaiter = new ManualResetEvent(false);


            // var inputVideo = new OpenCvSharp.VideoCapture("2017_0928_105336_023.MOV");
            var inputVideo = new OpenCvSharp.VideoCapture("YI004801.mp4");
            var outputVideo = new OpenCvSharp.VideoWriter("out_lina.mp4", "avc1", inputVideo.Fps, new OpenCvSharp.Size(inputVideo.FrameWidth, inputVideo.FrameHeight));

            ConcurrentQueue<OpenCvSharp.Mat> decodedFramesQueue = new ConcurrentQueue<OpenCvSharp.Mat>();

            // decoder
            Task decodeTask = Task.Factory.StartNew(() =>
            {
                Task processTask = null;
                while (true)
                {
                    var mat = inputVideo.RetrieveMat();
                    if (mat.Height == 0)
                    {
                        decodedFramesQueue.Enqueue(null);
                        decodeQueueFrameWaiter.Set();
                        break;
                    }
                    while(decodedFramesQueue.Count >= 10)
                    {
                        decodeQueueThrottleWaiter.WaitOne();
                    }

                    processTask?.Wait();

                    processTask = Task.Factory.StartNew(() =>
                    {
                        wb.BalanceWhite(mat, mat);
                        decodedFramesQueue.Enqueue(mat);
                        decodeQueueFrameWaiter.Set();
                    });
                }
            });

            Task writeTask = Task.Factory.StartNew(() =>
            {
                int c = 0;
                while(decodeQueueFrameWaiter.WaitOne()) {
                    OpenCvSharp.Mat frame;
                    bool success = decodedFramesQueue.TryDequeue(out frame);
                    if(success)
                    {
                        decodeQueueThrottleWaiter.Set();
                        // last frame exit!
                        if (frame == null)
                        {
                            break;
                        }

                        ++c;
                        outputVideo.Write(frame);
                        frame.Dispose();
                        Console.Write("Writer: Frame [" + c + "/" + inputVideo.FrameCount + "]\r");
                    }
                    else
                    {
                        decodeQueueFrameWaiter.Reset();
                    }
                }
            });

            decodeTask.Wait();
            writeTask.Wait();

            Console.WriteLine("\nready!");
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
            img.SaveImage("eq.png");
            
            Console.WriteLine("Hello World!");
        }
    }
}
