using System.Diagnostics;
using System.Text;

namespace FlightRouteGenerator
{
    internal class PerformanceDataService
    {
#if DEBUG
        private static string PDS_FILE_PATH = "../../../../../PerformanceDataService";
#else
        private static string PDS_FILE_PATH = "Services";
#endif
        private static string PYTHON_FILE_PATH = "C:\\Users\\halbo\\source\\repos\\ActualNEA\\.venv\\Scripts\\python.exe";
        private static HttpClient client;
        public static bool initialisationStarted { get; private set; }
        public static bool isInitialised { get; private set; }
        private static Process pdapiProcess;
        private static Process pdcalcProcess;
        private static string HOST_IP = "127.0.0.1";
        private static string HOST_PORT = "8000";
        private static string HOST_CALC_PORT = "9000";
        private static string HOST_SOCKET = $"{HOST_IP}:{HOST_PORT}";
        private static string HOST_CALC_SOCKET = $"{HOST_IP}:{HOST_CALC_PORT}";
        private static string HOST_URL = $"http://{HOST_SOCKET}";
        private static string HOST_CALC_URL = $"http://{HOST_CALC_SOCKET}";

        private static void KillPDSProcesses()
        {
            if (File.Exists("PDS.pid"))
            {
                List<int> PIDs = new List<int>();
                using (StreamReader sr = new StreamReader(File.OpenRead("PDS.pid")))
                {
                    while (!sr.EndOfStream)
                    {
                        PIDs.Add(int.Parse(sr.ReadLine()));
                    }
                }

                try
                {
                    foreach (int pid in PIDs)
                    {
                        Process.GetProcessById(pid).Kill(true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Attempted to kill old processes, but got error: {ex.Message}");
                }

                File.Delete("PDS.pid");
            }
        }

        public static async Task Initialise()
        {
            initialisationStarted = true;

            KillPDSProcesses();

            pdapiProcess = new Process();
            pdapiProcess.StartInfo.UseShellExecute = false;
            pdapiProcess.StartInfo.CreateNoWindow = true;

#if DEBUG
            pdapiProcess.StartInfo.FileName = PYTHON_FILE_PATH;
            pdapiProcess.StartInfo.Arguments = $"-m uvicorn OpenAP_API:api --host 127.0.0.1 --port 8000";
            // start the api as a uvicorn module.
            // uvicorn is the process that exposes the PDS python script as a fastAPI endpoint
            // over http.
#else
            pdapiProcess.StartInfo.FileName = "Services/OpenAP_API.exe";
#endif

            pdcalcProcess = new Process();
            pdcalcProcess.StartInfo.UseShellExecute = false;
            pdcalcProcess.StartInfo.CreateNoWindow = true;

#if DEBUG
            pdcalcProcess.StartInfo.FileName = PYTHON_FILE_PATH;
            pdcalcProcess.StartInfo.Arguments = $"-m uvicorn PerformanceCalculator:performance_calculator --host 127.0.0.1 --port 9000";
            // same for the performance data calculator
#else
            pdcalcProcess.StartInfo.FileName = "Services/PerformanceCalculator.exe";
#endif



            pdapiProcess.StartInfo.WorkingDirectory = PDS_FILE_PATH;
            pdapiProcess.StartInfo.RedirectStandardOutput = false;
            pdapiProcess.StartInfo.RedirectStandardError = false;

            pdcalcProcess.StartInfo.WorkingDirectory = PDS_FILE_PATH;
            pdcalcProcess.StartInfo.RedirectStandardOutput = false;
            pdcalcProcess.StartInfo.RedirectStandardError = false;

            pdapiProcess.Start();
            pdcalcProcess.Start();
            using (StreamWriter sw = File.AppendText("PDS.pid"))
            {
                sw.WriteLine(pdapiProcess.Id);
                sw.WriteLine(pdcalcProcess.Id);
            }

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
                catch (HttpRequestException hEx)
                {
                    Console.Write(".");
                    Debug.WriteLine(hEx.GetType());
                    Debug.WriteLine(hEx.Message);
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

        public static async Task<string> GetCalculation(string urlPath, HttpMethod method, string serialisedJson)
        {
            string payload = "";
            string fullURI = $"{HOST_CALC_URL}/{urlPath}";
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

        public static void KillService()
        {
            KillPDSProcesses();
        }
    }
}
