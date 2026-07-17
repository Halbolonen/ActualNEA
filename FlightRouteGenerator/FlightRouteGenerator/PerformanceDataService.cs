using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace FlightRouteGenerator
{
    internal class PerformanceDataService
    {
        private static string PDS_FILE_PATH = "C:\\Users\\halbo\\source\\repos\\ActualNEA\\PerformanceDataService";
        private static string PYTHON_FILE_PATH = "C:\\Users\\halbo\\source\\repos\\ActualNEA\\.venv\\Scripts\\python.exe";
        private static HttpClient client;
        public static bool initialisationStarted { get; private set; }
        public static bool isInitialised { get; private set; }
        private static Process pdsProcess;
        private static string HOST_IP = "127.0.0.1";
        private static string HOST_PORT = "8000";
        private static string HOST_SOCKET = $"{HOST_IP}:{HOST_PORT}";
        public static async Task Initialise()
        {
            initialisationStarted = true;

            using (pdsProcess = new Process())
            {
                pdsProcess.StartInfo.UseShellExecute = false;
                pdsProcess.StartInfo.FileName = PYTHON_FILE_PATH;
                pdsProcess.StartInfo.CreateNoWindow = true;
                pdsProcess.StartInfo.Arguments = $"-m uvicorn main:app --host 127.0.0.1 --port 8000";
                // start the uvicorn module as a module.
                // uvicorn is the process that exposes the PDS python script as a fastAPI endpoint
                // over http.

                pdsProcess.StartInfo.WorkingDirectory = PDS_FILE_PATH;
                pdsProcess.StartInfo.RedirectStandardOutput = true;
                pdsProcess.StartInfo.RedirectStandardError = true;

                pdsProcess.Start();

                bool processAlive = false;
                client = new HttpClient();

                while (!isInitialised)
                {
                    try
                    {
                        HttpResponseMessage response = await client.GetAsync($"http://{HOST_SOCKET}/items");
                        isInitialised = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.GetType());
                        Console.WriteLine(ex.Message);
                    }

                    await Task.Delay(100);
                }
            }
        }

        public static async Task<string> GetResponse(string urlPath)
        {
            string payload = "";

            HttpResponseMessage response = await client.GetAsync($"http://{HOST_SOCKET}/{urlPath}");
            HttpContent content = response.Content;
            using (StreamReader sr = new StreamReader(content.ReadAsStream()))
            {
                payload += sr.ReadLine();
            }

            return payload;
        }
    }
}
