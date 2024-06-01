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
        private static readonly SemaphoreSlim _lock = new(1, 1);

        public static async Task<string> ConvAsync(string imagePath, int index)
        {
            var image = Image.Load<Rgba32>(imagePath);
            var gif = new Image<Rgba32>(image.Width, image.Height);

            Task[] taskArray = new Task[10];
            for (int i = 0; i < taskArray.Length; i++)
            {
                int localIndex = i;
                taskArray[localIndex] = ProcessImageAsync(image.CloneAs<Rgba32>(), localIndex, gif);
            }
            await Task.WhenAll(taskArray);

            gif.Frames.RemoveFrame(0);

            var gifMetadata = gif.Metadata.GetGifMetadata();
            gifMetadata.RepeatCount = 0;

            foreach (var frame in gif.Frames)
            {
                var frameMetadata = frame.Metadata.GetGifMetadata();
                frameMetadata.FrameDelay = 30;
            }

            string outputPath = $"../../../output{index}.gif";
            await Task.Run(() => gif.Save(outputPath, new GifEncoder()));
            return outputPath;
        }



        private static async Task ProcessImageAsync(Image<Rgba32> tempImage, int i, Image<Rgba32> gif)
        {
            await Task.Run(() =>
            {
                tempImage.Mutate(x => x.Hue(70 + i * 20));
            });

            await _lock.WaitAsync();
            try
            {
                if (gif != null) 
                {
                    gif.Frames.AddFrame(tempImage.Frames.RootFrame);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

    }
}
