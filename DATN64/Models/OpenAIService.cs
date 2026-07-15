using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DATN64.Models
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public OpenAIService(IConfiguration configuration)
        {
            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = false,
                Proxy = null,
                UseProxy = false
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(45)
            };

            var keySource = configuration["OpenAI:ApiKey"]
                            ?? configuration["OPENAI_API_KEY"]
                            ?? configuration["OpenAI__ApiKey"]
                            ?? LoadApiKeyFromEnvFile();

            _apiKey = (keySource ?? string.Empty).Trim();
            _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            Console.WriteLine($"[OpenAIService] Status: {(IsConfigured ? "Configured" : "Missing API key")}, Model: {_model}");
        }

        private string LoadApiKeyFromEnvFile()
        {
            try
            {
                var paths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "ApiKey.env"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApiKey.env"),
                    "ApiKey.env"
                };

                foreach (var path in paths)
                {
                    if (!File.Exists(path)) continue;

                    var lines = File.ReadAllLines(path);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                            continue;

                        var parts = line.Split('=', 2);
                        if (parts.Length != 2) continue;

                        var key = parts[0].Trim();
                        var val = parts[1].Trim().Trim('"', '\'');

                        if (key.Equals("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase) ||
                            key.Equals("OpenAI__ApiKey", StringComparison.OrdinalIgnoreCase) ||
                            key.Equals("OpenAI:ApiKey", StringComparison.OrdinalIgnoreCase))
                        {
                            return val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenAIService] Lỗi khi đọc file ApiKey.env: {ex.Message}");
            }

            return string.Empty;
        }

        public async Task<string> GenerateResponseAsync(string systemInstruction, string prompt)
        {
            if (!IsConfigured)
            {
                return string.Empty;
            }

            var requestBody = new
            {
                model = _model,
                instructions = systemInstruction,
                input = prompt,
                temperature = 0.4,
                max_output_tokens = 1200
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[OpenAIService] RESPONSE STATUS: {response.StatusCode}");
                    Console.WriteLine($"[OpenAIService] RESPONSE BODY: {responseContent}");
                    return $"Lỗi API OpenAI ({response.StatusCode}): {responseContent}";
                }

                return ExtractTextFromResponse(responseContent);
            }
            catch (Exception ex)
            {
                return $"Lỗi ngoại lệ khi gọi OpenAI API: {ex.Message}";
            }
        }

        private string ExtractTextFromResponse(string responseContent)
        {
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("output_text", out var outputText) &&
                outputText.ValueKind == JsonValueKind.String)
            {
                return outputText.GetString() ?? string.Empty;
            }

            var parts = new List<string>();

            if (root.TryGetProperty("output", out var output) &&
                output.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in output.EnumerateArray())
                {
                    if (!item.TryGetProperty("content", out var content) ||
                        content.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var contentItem in content.EnumerateArray())
                    {
                        if (contentItem.TryGetProperty("text", out var text) &&
                            text.ValueKind == JsonValueKind.String)
                        {
                            parts.Add(text.GetString() ?? string.Empty);
                        }
                    }
                }
            }

            return parts.Count > 0 ? string.Join("\n", parts) : string.Empty;
        }
    }
}