using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DATN64.Models
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiService(IConfiguration configuration)
        {
            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = false,
                Proxy = null,
                UseProxy = false
            };
            _httpClient = new HttpClient(handler);
            
            string? keySource = null;
            string? modelSource = null;

            // 1. Đọc trực tiếp từ file vật lý appsettings.json để bỏ qua đè từ Environment Variables
            try
            {
                var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                if (File.Exists(appSettingsPath))
                {
                    var json = File.ReadAllText(appSettingsPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("GeminiAI", out var geminiSection))
                    {
                        if (geminiSection.TryGetProperty("ApiKey", out var apiKeyProp))
                        {
                            keySource = apiKeyProp.GetString();
                        }
                        if (geminiSection.TryGetProperty("Model", out var modelProp))
                        {
                            modelSource = modelProp.GetString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GeminiService] Lỗi khi đọc trực tiếp appsettings.json: {ex.Message}");
            }

            // 2. Nếu không tìm thấy, fallback về IConfiguration
            if (string.IsNullOrWhiteSpace(keySource))
            {
                keySource = configuration["GeminiAI:ApiKey"] 
                            ?? configuration["GEMINI_API_KEY"] 
                            ?? configuration["GeminiAI__ApiKey"];
            }

            // 3. Fallback tiếp về file .env hoặc Environment Variables hệ thống
            if (string.IsNullOrWhiteSpace(keySource))
            {
                keySource = LoadApiKeyFromEnvFile();
            }
            if (string.IsNullOrWhiteSpace(keySource))
            {
                keySource = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                            ?? Environment.GetEnvironmentVariable("GeminiAI__ApiKey");
            }

            _apiKey = (keySource ?? "").Trim();

            // Phân giải Model
            if (string.IsNullOrWhiteSpace(modelSource))
            {
                modelSource = configuration["GeminiAI:Model"];
            }
            _model = string.IsNullOrEmpty(modelSource) ? "gemini-1.5-flash" : modelSource;

            // Log ra console để chẩn đoán chính xác key và model nào đang hoạt động
            var maskedKey = _apiKey.Length > 10 ? _apiKey.Substring(0, 10) + "..." : _apiKey;
            Console.WriteLine($"[GeminiService] Kích hoạt thành công. Key: {maskedKey}, Model: {_model}");
        }

        private string LoadApiKeyFromEnvFile()
        {
            try
            {
                // Danh sách các đường dẫn khả thi của file ApiKey.env
                var paths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "ApiKey.env"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApiKey.env"),
                    "ApiKey.env"
                };

                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        var lines = File.ReadAllLines(path);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                                continue;

                            var parts = line.Split('=', 2);
                            if (parts.Length == 2)
                            {
                                var key = parts[0].Trim();
                                var val = parts[1].Trim();
                                
                                // Hỗ trợ cả GEMINI_API_KEY hoặc GeminiAI__ApiKey
                                if (key.Equals("GEMINI_API_KEY", StringComparison.OrdinalIgnoreCase) ||
                                    key.Equals("GeminiAI__ApiKey", StringComparison.OrdinalIgnoreCase) ||
                                    key.Equals("GeminiAI:ApiKey", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Loại bỏ dấu nháy đơn hoặc nháy kép nếu có (ví dụ: KEY="value")
                                    return val.Trim('"', '\'');
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log lỗi ra Console hoặc debug, không crash ứng dụng
                Console.WriteLine($"[GeminiService] Lỗi khi đọc file ApiKey.env: {ex.Message}");
            }
            return "";
        }

        public async Task<string> GenerateResponseAsync(string systemInstruction, string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "Lỗi: API Key cho Gemini AI chưa được cấu hình. Vui lòng kiểm tra appsettings.json, ApiKey.env hoặc Environment Variables.";

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                systemInstruction = new { parts = new[] { new { text = systemInstruction } } }
            };

            return await CallGeminiAsync(url, requestBody);
        }

        /// <summary>
        /// Generates a structured agentic response. The AI is instructed to return
        /// a raw JSON object (no markdown fences) with the shape:
        /// {
        ///   "message": "...",
        ///   "hasAction": true/false,
        ///   "actionType": "CREATE_PRODUCT_AND_IMPORT" | null,
        ///   "actionPayload": { ... }
        /// }
        /// </summary>
        public async Task<string> GenerateActionResponseAsync(string systemInstruction, string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "{\"message\":\"Lỗi: API Key chưa cấu hình. Vui lòng kiểm tra appsettings.json, ApiKey.env hoặc Environment Variables.\",\"hasAction\":false}";

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            return await CallGeminiAsync(url, requestBody);
        }

        private async Task<string> CallGeminiAsync(string url, object requestBody)
        {
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // Truyền key qua query parameter ?key=... để tránh bị reverse proxy hoặc router của host lột mất custom header
                var urlWithKey = $"{url}?key={Uri.EscapeDataString(_apiKey)}";
                
                using var request = new HttpRequestMessage(HttpMethod.Post, urlWithKey);
                request.Content = content;
                
                // Clear bất kỳ Authorization header nào tự động sinh ra bởi môi trường host (vd: Azure Easy Auth, Managed Identity, IIS proxy...)
                request.Headers.Authorization = null;
                
                // Vẫn truyền kèm custom header dự phòng
                request.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);

                Console.WriteLine($"[GeminiService] CALLING URL: {url}");
                Console.WriteLine($"[GeminiService] HEADERS: {request.Headers}");
                Console.WriteLine($"[GeminiService] BODY: {json}");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"[GeminiService] RESPONSE STATUS: {response.StatusCode}");
                Console.WriteLine($"[GeminiService] RESPONSE BODY: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    return $"Lỗi API Gemini ({response.StatusCode}): {responseContent}";
                }

                var responseString = responseContent;
                using var doc = JsonDocument.Parse(responseString);

                var reply = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return reply ?? "Không nhận được nội dung phản hồi từ AI.";
            }
            catch (Exception ex)
            {
                return $"Lỗi ngoại lệ khi gọi Gemini API: {ex.Message}";
            }
        }
    }
}
