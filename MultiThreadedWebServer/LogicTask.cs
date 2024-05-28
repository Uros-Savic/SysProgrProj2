using System;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MultiThreadedWebServer
{
    internal class LogicTask
    {
        private static readonly object _lock = new object();
        private static Image<Rgba32> gif;
        private static Image<Rgba32> image;

        public static void EditImage(Image<Rgba32> image, byte red, byte green, byte blue)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Rgba32 pixel = image[x, y];
                    if (red != 0)
                        pixel.R = red;
                    if (green != 0)
                        pixel.G = green;
                    if (blue != 0)
                        pixel.B = blue;
                    image[x, y] = pixel;
                }
            }
        }

        public static string Conv(string imagePath, int index)
        {
            image = Image.Load<Rgba32>(imagePath);
            gif = new Image<Rgba32>(image.Width, image.Height);

            Task[] taskArray = new Task[10];
            for (int i = 0; i < taskArray.Length; i++)
            {
                int localIndex = i;
                taskArray[localIndex] = Task.Factory.StartNew(() => ProcessImage(image.CloneAs<Rgba32>(), localIndex));
            }
            Task.WaitAll(taskArray);

            gif.Frames.RemoveFrame(0);

            var gifMetadata = gif.Metadata.GetGifMetadata();
            gifMetadata.RepeatCount = 0;

            foreach (var frame in gif.Frames)
            {
                var frameMetadata = frame.Metadata.GetGifMetadata();
                frameMetadata.FrameDelay = 30;
            }
            string outputPath = $"../../../output{index}.gif";
            gif.Save(outputPath, new GifEncoder());
            return outputPath;
        }

        private static void ProcessImage(Image<Rgba32> tempImage, int i)
        {
            tempImage.Mutate(x => x.Hue(70 + i * 20));

            lock (_lock)
            {
                gif.Frames.AddFrame(tempImage.Frames.RootFrame);
            }
        }
    }
}
