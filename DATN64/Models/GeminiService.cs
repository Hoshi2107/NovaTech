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
            _httpClient = new HttpClient();
            _apiKey = configuration["GeminiAI:ApiKey"] ?? "";
            // Default to gemini-1.5-flash if not specified or empty
            _model = string.IsNullOrEmpty(configuration["GeminiAI:Model"]) 
                ? "gemini-1.5-flash" 
                : configuration["GeminiAI:Model"];
        }

        public async Task<string> GenerateResponseAsync(string systemInstruction, string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "Lỗi: API Key cho Gemini AI chưa được cấu hình trong appsettings.json.";
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = systemInstruction }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
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
