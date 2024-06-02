using NHibernate.Cache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MultiThreadedWebServer
{
    internal class Server
    {
        static readonly string RootFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");
        static readonly Dictionary<string, byte[]> ResponseCache = new();
        static readonly SemaphoreSlim CacheSemaphore = new(1, 1);
        static int index = 0;

        public static async Task StartWebServerAsync(HttpListener listener)
        {
            listener.Prefixes.Add("http://localhost:5050/");
            listener.Start();
            Console.WriteLine("Listening for requests.");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context));
            }
        }

        static async Task HandleRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.Url != null)
            {
                string requestUrl = request.Url.LocalPath;
                if(request.Url.AbsolutePath == "/favicon.ico")
				{
                    return;
				}
                Console.WriteLine($"Request received: {requestUrl}");
                string filePath = Path.Combine(RootFolder, requestUrl.TrimStart('/'));

                Stopwatch stopwatch = Stopwatch.StartNew();
                if (File.Exists(filePath))
                {
                    byte[] cachedResponse;
                    await CacheSemaphore.WaitAsync();
                    try
                    {
                        if (ResponseCache.ContainsKey(requestUrl))
                        {
                            cachedResponse = ResponseCache[requestUrl];
                            Console.WriteLine("Cached response found.");
                            Console.WriteLine($"Request processed in {stopwatch.ElapsedMilliseconds} milliseconds.");

                            await response.OutputStream.WriteAsync(cachedResponse);
                            response.Close();
                            return;
                        }
                    }
                    finally
                    {
                        CacheSemaphore.Release();
                    }

                    if (FindFileType(filePath))
                    {
                        try
                        {
                            String a = (await LogicTask.ConvAsync(filePath, ++index)).ToString();
                            byte[] fileBytes = await File.ReadAllBytesAsync(a);
                            await CacheSemaphore.WaitAsync();
                            try
                            {
                                ResponseCache[requestUrl] = fileBytes;
                            }
                            finally
                            {
                                CacheSemaphore.Release();
                            }
                            response.ContentType = "image/gif";
                            response.ContentLength64 = fileBytes.Length;
                            await response.OutputStream.WriteAsync(fileBytes);
                            Console.WriteLine("A .gif file has been created.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        string errorMessage = "File not compatible";
                        byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                        await response.OutputStream.WriteAsync(errorBytes);
                        Console.WriteLine("File type not compatible.");
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    string errorMessage = $"File not found: {requestUrl}";
                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                    await response.OutputStream.WriteAsync(errorBytes);
                    Console.WriteLine($"File not found: {requestUrl}");
                }
                response.Close();
                stopwatch.Stop();
                Console.WriteLine($"Request processed in {stopwatch.ElapsedMilliseconds} milliseconds.");
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Close();
                Console.WriteLine("Request is null.");
            }
        }

        static bool FindFileType(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLower();

            List<string> Extensions = new() { ".gif", ".qoi", ".png", ".pbm", ".webp", ".tga", ".jpeg", ".jpg", ".tiff", ".bmp" };

            return Extensions.Contains(extension);
        }

        public static async Task StopWebServerAsync(HttpListener listener)
        {
            if (listener != null && listener.IsListening)
            {
                listener.Stop();
                await Task.Run(() => listener.Close());
                Console.WriteLine("Web server stopped.");
            }
        }
    }
}
