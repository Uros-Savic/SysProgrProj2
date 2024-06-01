using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MultiThreadedWebServer
{
    class Program
    {
        static readonly string RootFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");
        protected static HttpListener listener;

        static async Task Main()
        {
            if (!Directory.Exists(RootFolder))
                Directory.CreateDirectory(RootFolder);
            try
            {
                listener = new HttpListener();
				await Server.StartWebServerAsync(listener);
                Console.WriteLine("Web server started.");
                await Task.Delay(-1);
            }
            catch (Exception ex)
			{
                Console.WriteLine(ex.ToString());
			}
			finally
			{
				await Server.StopWebServerAsync(listener);
            }
        }
    }
}
