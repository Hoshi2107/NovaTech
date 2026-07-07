using System;
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
            
            // Lấy API Key từ nhiều nguồn: appsettings, biến môi trường (cả dạng dấu hai chấm và dấu gạch dưới đúp, hoặc chuẩn GEMINI_API_KEY)
            _apiKey = (configuration["GeminiAI:ApiKey"] 
                      ?? configuration["GeminiAI__ApiKey"] 
                      ?? configuration["GEMINI_API_KEY"] 
                      ?? Environment.GetEnvironmentVariable("GeminiAI__ApiKey") 
                      ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                      ?? "").Trim();

            _model = string.IsNullOrEmpty(configuration["GeminiAI:Model"])
                ? "gemini-1.5-flash"
                : configuration["GeminiAI:Model"];
        }

        public async Task<string> GenerateResponseAsync(string systemInstruction, string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return "Lỗi: API Key cho Gemini AI chưa được cấu hình. Vui lòng kiểm tra appsettings.json hoặc Environment Variables.";

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
                return "{\"message\":\"Lỗi: API Key chưa cấu hình. Vui lòng kiểm tra appsettings.json hoặc Environment Variables.\",\"hasAction\":false}";

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

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Lỗi API Gemini ({response.StatusCode}): {errorContent}";
                }

                var responseString = await response.Content.ReadAsStringAsync();
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
