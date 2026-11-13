using SmartSoftware.DeviceAgent.Model;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartSoftware.DeviceAgent.Utils
{
    public static class CameraUtils
    {
        public static readonly string OutputDir = Path.Combine(AppContext.BaseDirectory, "CameraOutput");

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        }

        public static async Task<string> Login(string deviceType, string server, int port, string username, string password)
        {
            if (deviceType != "NX")
                throw new Exception($"Camera: дэмжигдэхгүй камерын төрөл ({deviceType})");

            var baseUrl = $"https://{server}:{port}";
            var body = JsonSerializer.Serialize(new { username, password });

            using var client = CreateClient();
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var res = await client.PostAsync($"{baseUrl}/rest/v3/login/sessions", content);
            var resBody = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new Exception($"Camera: нэвтрэхэд алдаа гарлаа. {resBody}");

            return JsonSerializer.Deserialize<NXAuthToken>(resBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Token
                ?? throw new Exception("Camera: нэвтрэх хариуг задлахад алдаа гарлаа.");
        }

        public static async Task<List<NXDevice>> GetDevicesAsync(CameraDeviceListRequest req)
        {
            if (req.DeviceType != "NX")
                return new List<NXDevice>();

            var token = await Login(req.DeviceType, req.Server, req.Port, req.Username, req.Password);

            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"https://{req.Server}:{req.Port}/rest/v3/devices?_with=id,name,status";
            using var res = await client.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new Exception($"Camera: төхөөрөмжийн жагсаалт татахад алдаа гарлаа. {body}");

            return JsonSerializer.Deserialize<List<NXDevice>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<NXDevice>();
        }

        public static async Task<string> Play(CameraPlayRequest req)
        {
            if (req.DeviceType != "NX")
                throw new Exception($"Camera: дэмжигдэхгүй камерын төрөл ({req.DeviceType})");

            var token = await Login(req.DeviceType, req.Server, req.Port, req.Username, req.Password);

            Directory.CreateDirectory(OutputDir);
            return await DownloadVideoAsync(req.Server, req.Port, token, req.DeviceId, req.StartMs, req.DurationMs, req.VideoName);
        }

        private static async Task<string> DownloadVideoAsync(string server, int port, string token, string deviceId, long posMs, long duration, string videoName)
        {
            var extensions = new[] { "mp4", "mkv" };
            var attemptErrors = new List<string>();

            foreach (var ext in extensions)
            {
                var fileName = $"{videoName}.{ext}";
                var filePath = Path.Combine(OutputDir, fileName);

                try
                {
                    var url = $"https://{server}:{port}/media/{deviceId}.{ext}?pos={posMs}&duration={duration}";

                    using var client = CreateClient();
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    using var res = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    if (!res.IsSuccessStatusCode)
                    {
                        var errBody = await res.Content.ReadAsStringAsync();
                        attemptErrors.Add($"{ext}: HTTP {(int)res.StatusCode} - {errBody}");
                        if (File.Exists(filePath)) File.Delete(filePath);
                        continue;
                    }

                    await using (var fs = File.Create(filePath))
                    await using (var stream = await res.Content.ReadAsStreamAsync())
                    {
                        await stream.CopyToAsync(fs);
                    }

                    if (!IsValidVideoFile(filePath, ext))
                    {
                        var info = new FileInfo(filePath);
                        attemptErrors.Add($"{ext}: downloaded {info.Length} bytes but failed file-signature check");
                        if (File.Exists(filePath)) File.Delete(filePath);
                        continue;
                    }

                    return fileName;
                }
                catch (Exception ex)
                {
                    attemptErrors.Add($"{ext}: {ex.Message}");
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
            }

            throw new Exception("Camera: бичлэг татахад алдаа гарлаа. " + string.Join(" | ", attemptErrors));
        }

        private static bool IsValidVideoFile(string filePath, string ext)
        {
            try
            {
                var info = new FileInfo(filePath);
                if (info.Length < 12) return false;

                using var fs = File.OpenRead(filePath);
                var header = new byte[12];
                fs.Read(header, 0, header.Length);

                if (ext == "mp4")
                    return header[4] == 'f' && header[5] == 't' && header[6] == 'y' && header[7] == 'p';

                if (ext == "mkv")
                    return header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3;
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
