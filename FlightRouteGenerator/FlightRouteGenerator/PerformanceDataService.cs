using System.Diagnostics;
using System.Text;

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
        private static string HOST_URL = $"http://{HOST_SOCKET}";
        public static async Task Initialise()
        {
            initialisationStarted = true;

            if (File.Exists("PDS.pid"))
            {
                int pdsPID;

                using (BinaryReader br = new BinaryReader(File.OpenRead("PDS.pid")))
                {
                    pdsPID = br.ReadInt32();
                }

                try
                {
                    Process.GetProcessById(pdsPID).Kill(true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Attempted to kill old process, but got error: {ex.Message}");
                }

                File.Delete("PDS.pid");
            }

            pdsProcess = new Process();
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
            using (BinaryWriter bw = new BinaryWriter(File.OpenWrite("PDS.pid")))
            {
                bw.Write(pdsProcess.Id);
            }

            bool processAlive = false;
            client = new HttpClient();

            while (!isInitialised)
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync($"{HOST_URL}");
                    response.EnsureSuccessStatusCode();
                    isInitialised = true;

                    using (StreamReader sr = new StreamReader(response.Content.ReadAsStream()))
                    {
                        Debug.WriteLine(sr.ReadLine());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType());
                    Console.WriteLine(ex.Message);
                }

                await Task.Delay(100);
            }

        }

        public static async Task<string> GetResponse(string urlPath, HttpMethod method, string serialisedJson)
        {
            string payload = "";
            string fullURI = $"{HOST_URL}/{urlPath}";
            using StringContent jsonContent = new(serialisedJson, Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage(method, fullURI)
            {
                Content = jsonContent
            };

            HttpResponseMessage response = await client.SendAsync(request);

            HttpContent content = response.Content;
            using (StreamReader sr = new StreamReader(content.ReadAsStream()))
            {
                payload += sr.ReadLine();
            }

            return payload;
        }
    }
}
