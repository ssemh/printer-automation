using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using PrinterAutomation.Models;

namespace PrinterAutomation.Services
{
    public class GeminiAnalysisResult
    {
        [JsonPropertyName("filamentAmount")]
        public FilamentAmount FilamentAmount { get; set; } = new FilamentAmount();
        
        [JsonPropertyName("printTime")]
        public PrintTime PrintTime { get; set; } = new PrintTime();
        
        [JsonPropertyName("costs")]
        public Costs Costs { get; set; } = new Costs();
        
        [JsonPropertyName("recommendedPrice")]
        public double RecommendedPrice { get; set; }
        
        [JsonPropertyName("analysis")]
        public string Analysis { get; set; } = string.Empty;
    }

    public class FilamentAmount
    {
        [JsonPropertyName("grams")]
        public double Grams { get; set; }
        
        [JsonPropertyName("meters")]
        public double Meters { get; set; }
    }

    public class PrintTime
    {
        [JsonPropertyName("hours")]
        public int Hours { get; set; }
        
        [JsonPropertyName("minutes")]
        public int Minutes { get; set; }
    }

    public class Costs
    {
        [JsonPropertyName("filament")]
        public double Filament { get; set; }
        
        [JsonPropertyName("electricity")]
        public double Electricity { get; set; }
        
        [JsonPropertyName("total")]
        public double Total { get; set; }
    }

    public class GeminiAnalysisService
    {
        private readonly string _apiKey;

        public GeminiAnalysisService(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        public async Task<GeminiAnalysisResult> AnalyzeModelAsync(StlModel model, double infillPercentage = 20, double layerHeight = 0.2)
        {
            try
            {
                string prompt = $@"Sen bir 3D yazdırma uzmanısın. Aşağıdaki STL model dosyasını analiz et:

📊 MODEL BİLGİLERİ:
- Dosya: {Path.GetFileName(model.FilePath)}
- Hacim: {model.Volume:F2} cm³
- Yüzey Alanı: {model.SurfaceArea:F2} cm²
- Üçgen Sayısı: {model.TriangleCount:N0}
- Boyutlar: {model.Bounds.Width:F2} x {model.Bounds.Height:F2} x {model.Bounds.Depth:F2} mm
- Doluluk Oranı: %{infillPercentage}
- Katman Yüksekliği: {layerHeight} mm

Lütfen şunları hesapla ve öner:
1. Harcanacak filament miktarı (gram ve metre cinsinden)
2. Baskı süresi tahmini (saat ve dakika cinsinden)
3. Filament maliyeti (TL cinsinden, 1kg filament = 200 TL varsayarak)
4. Elektrik maliyeti (TL cinsinden, saat başına 2 TL varsayarak)
5. Toplam üretim maliyeti
6. Önerilen satış fiyatı (kâr marjı %50-100 arası)

Yanıtını JSON formatında ver (sadece JSON, başka açıklama ekleme):
{{
  ""filamentAmount"": {{
    ""grams"": 0,
    ""meters"": 0
  }},
  ""printTime"": {{
    ""hours"": 0,
    ""minutes"": 0
  }},
  ""costs"": {{
    ""filament"": 0,
    ""electricity"": 0,
    ""total"": 0
  }},
  ""recommendedPrice"": 0,
  ""analysis"": ""Detaylı analiz açıklaması""
}}";

                // STL dosyasını Gemini File API ile yükle
                string fileUri = null;
                string mimeType = "application/octet-stream";
                
                if (model.FileData != null && model.FileData.Length > 0)
                {
                    try
                    {
                        // STL dosyasının ASCII mi binary mi olduğunu kontrol et
                        bool isAscii = IsAsciiStl(model.FileData);
                        if (isAscii)
                        {
                            mimeType = "text/plain"; // ASCII STL için text/plain
                        }
                        else
                        {
                            mimeType = "application/octet-stream"; // Binary STL için
                        }
                        
                        fileUri = await UploadFileToGeminiAsync(model.FileData, Path.GetFileName(model.FilePath), mimeType);
                        System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] STL dosyası Gemini'ye yüklendi: {fileUri}, MIME: {mimeType}");
                    }
                    catch (Exception uploadEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] Dosya yükleme hatası: {uploadEx.Message}");
                        // Dosya yüklenemezse sadece text prompt gönder
                    }
                }

                // Request body oluştur - dosya varsa file URI ile, yoksa sadece text
                object requestBody;
                if (!string.IsNullOrEmpty(fileUri))
                {
                    // Dosya URI ile birlikte gönder
                    requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new object[]
                                {
                                    new { text = prompt },
                                    new
                                    {
                                        fileData = new
                                        {
                                            mimeType = mimeType,
                                            fileUri = fileUri
                                        }
                                    }
                                }
                            }
                        }
                    };
                    System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] STL dosyası file URI ile gönderiliyor: {fileUri}, MIME: {mimeType}");
                }
                else
                {
                    // Sadece text prompt gönder
                    requestBody = new
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
                        }
                    };
                    System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] STL dosyası yüklenemedi, sadece text prompt gönderiliyor");
                }

                string json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(60);
                    
                    string apiUrl = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={_apiKey}";
                    
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                    {
                        var response = await httpClient.PostAsync(apiUrl, content, cts.Token);
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            string errorContent = await response.Content.ReadAsStringAsync();
                            throw new HttpRequestException($"API Hatası ({response.StatusCode}): {errorContent}");
                        }

                        string responseContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] Response alındı, uzunluk: {responseContent.Length}");
                        System.Console.WriteLine($"[GeminiAnalysis] Response alındı");
                        
                        using (var responseJson = JsonDocument.Parse(responseContent))
                        {
                        // Gemini yanıtını parse et
                            if (!responseJson.RootElement.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] Candidates bulunamadı. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                            throw new InvalidOperationException("API yanıtında candidates bulunamadı");
                        }

                        var candidate = candidates[0];
                            var textContent = candidate.GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                        
                        System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] Text content uzunluğu: {textContent.Length}");
                        System.Console.WriteLine($"[GeminiAnalysis] Text content alındı");

                        // JSON yanıtını çıkar (eğer markdown code block içindeyse)
                        string jsonResponse = ExtractJsonFromResponse(textContent);
                        
                        System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] Extracted JSON: {jsonResponse.Substring(0, Math.Min(500, jsonResponse.Length))}");
                        System.Console.WriteLine($"[GeminiAnalysis] JSON extracted");

                            var result = JsonSerializer.Deserialize<GeminiAnalysisResult>(jsonResponse, new JsonSerializerOptions
                        {
                                PropertyNameCaseInsensitive = true,
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        });
                        
                        if (result != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] JSON parse başarılı - Filament: {result.FilamentAmount.Grams}g, Price: {result.RecommendedPrice}TL");
                            System.Console.WriteLine($"[GeminiAnalysis] JSON parse başarılı");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] JSON parse başarısız - result null");
                            System.Console.WriteLine($"[GeminiAnalysis] JSON parse başarısız");
                        }

                        if (result == null)
                        {
                            // Eğer JSON parse edilemezse, basit bir hesaplama yap
                            result = CalculateBasicAnalysis(model, infillPercentage, layerHeight);
                            result.Analysis = textContent; // Gemini'nin açıklamasını ekle
                        }
                        else
                        {
                            // Gemini'den gelen analizi ekle
                            if (string.IsNullOrWhiteSpace(result.Analysis))
                            {
                                result.Analysis = textContent;
                            }
                        }

                        return result;
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout durumunda basit hesaplama yap
                var result = CalculateBasicAnalysis(model, infillPercentage, layerHeight);
                result.Analysis = "Hesaplama otomatik olarak yapıldı. Gemini API zaman aşımı.";
                return result;
            }
            catch (Exception ex)
            {
                // Hata durumunda basit hesaplama yap
                var result = CalculateBasicAnalysis(model, infillPercentage, layerHeight);
                result.Analysis = $"Hesaplama otomatik olarak yapıldı. Gemini API yanıtı alınamadı. (Hata: {ex.Message})";
                return result;
            }
        }

        private string ExtractJsonFromResponse(string response)
        {
            // JSON'u markdown code block'tan çıkar
            int jsonStart = response.IndexOf('{');
            int jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }
            
            return response;
        }

        private bool IsAsciiStl(byte[] fileData)
        {
            if (fileData == null || fileData.Length < 5)
                return false;
            
            // İlk birkaç byte'ı string'e çevir ve "solid" ile başlayıp başlamadığını kontrol et
            try
            {
                string firstLine = Encoding.UTF8.GetString(fileData, 0, Math.Min(100, fileData.Length));
                return firstLine.TrimStart().StartsWith("solid", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> UploadFileToGeminiAsync(byte[] fileData, string fileName, string mimeType)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(120);
                    
                    // Gemini File API için doğru format: önce metadata, sonra file data
                    // Upload URL'ye mimeType parametresi ekle
                    string uploadUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={_apiKey}&uploadType=multipart";
                    
                    // Multipart form data oluştur
                    var multipartContent = new MultipartFormDataContent();
                    
                    // Metadata JSON
                    var metadata = new
                    {
                        file = new { 
                            displayName = fileName
                        }
                    };
                    var metadataJson = JsonSerializer.Serialize(metadata);
                    multipartContent.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata");
                    
                    // File data
                    var fileContent = new ByteArrayContent(fileData);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
                    multipartContent.Add(fileContent, "file", fileName);
                    
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120)))
                    {
                        var response = await httpClient.PostAsync(uploadUrl, multipartContent, cts.Token);
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            string errorContent = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] Dosya yükleme hatası ({response.StatusCode}): {errorContent}");
                            throw new HttpRequestException($"Dosya yükleme hatası ({response.StatusCode}): {errorContent}");
                        }
                        
                        string responseContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] Upload response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                        
                        using (var responseJson = JsonDocument.Parse(responseContent))
                        {
                            string fileUri = null;
                            
                            if (responseJson.RootElement.TryGetProperty("file", out var fileElement))
                            {
                                if (fileElement.TryGetProperty("uri", out var uriElement))
                                {
                                    fileUri = uriElement.GetString();
                                }
                        
                                if (string.IsNullOrEmpty(fileUri) && fileElement.TryGetProperty("name", out var nameElement))
                        {
                            // Alternatif olarak name field'ından URI oluştur
                                    var fileNameFromResponse = nameElement.GetString();
                            if (!string.IsNullOrEmpty(fileNameFromResponse))
                            {
                                fileUri = $"gs://{fileNameFromResponse}";
                                    }
                                }
                            }
                            
                            if (string.IsNullOrEmpty(fileUri))
                            {
                                throw new InvalidOperationException("Dosya URI alınamadı");
                        }
                        
                        // Dosyanın işlenmesini bekle (Gemini dosyayı işlerken biraz zaman alabilir)
                        await Task.Delay(3000);
                        
                        return fileUri;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GeminiAnalysis] Dosya yükleme exception: {ex}");
                throw;
            }
        }

        private GeminiAnalysisResult CalculateBasicAnalysis(StlModel model, double infillPercentage, double layerHeight)
        {
            // Filament yoğunluğu: PLA için yaklaşık 1.24 g/cm³
            double filamentDensity = 1.24; // g/cm³
            
            // Filament çapı: 1.75mm
            double filamentDiameter = 0.175; // cm
            double filamentCrossSection = Math.PI * Math.Pow(filamentDiameter / 2, 2); // cm²
            
            // Daha gerçekçi hacim hesaplama
            // Shell kalınlığı: genellikle 0.4-0.8mm (ortalama 0.6mm = 0.06cm)
            double shellThickness = 0.06; // cm
            double solidVolume = model.Volume; // cm³ (gerçek hacim)
            
            // Shell hacmi (dış kabuk) - yaklaşık olarak yüzey alanı * kalınlık
            double shellVolume = (model.SurfaceArea * shellThickness); // cm³
            
            // İç hacim (infill için)
            double innerVolume = Math.Max(0, solidVolume - shellVolume);
            
            // İnfill hacmi
            double infillVolume = innerVolume * (infillPercentage / 100.0);
            
            // Toplam filament hacmi
            double totalVolume = shellVolume + infillVolume;
            
            // Filament miktarı
            double filamentGrams = totalVolume * filamentDensity;
            double filamentMeters = totalVolume / filamentCrossSection;
            
            // Baskı süresi tahmini (daha gerçekçi)
            // Katman sayısı
            double layerCount = Math.Max(1, model.Bounds.Height / layerHeight);
            
            // Baskı hızı
            double printSpeed = 50; // mm/s (ortalama)
            double infillSpeed = 80; // mm/s (infill için, daha hızlı)
            
            // Toplam yol uzunluğu (yaklaşık)
            double shellPathLength = (model.SurfaceArea * 10) * 1.5; // mm (yaklaşık, faktör ile)
            
            // İnfill için: iç hacim / filament kesit alanı
            double infillPathLength = (infillVolume * 1000) / filamentCrossSection; // mm
            
            double totalTimeSeconds = (shellPathLength / printSpeed) + (infillPathLength / infillSpeed);
            double printTimeHours = totalTimeSeconds / 3600.0;
            
            // Maliyetler
            double filamentCostPerKg = 200; // TL
            double filamentCost = (filamentGrams / 1000) * filamentCostPerKg;
            double electricityCostPerHour = 2; // TL
            double electricityCost = printTimeHours * electricityCostPerHour;
            double totalCost = filamentCost + electricityCost;
            
            // Önerilen fiyat (%75 kâr marjı)
            double recommendedPrice = totalCost * 1.75;
            
            return new GeminiAnalysisResult
            {
                FilamentAmount = new FilamentAmount
                {
                    Grams = Math.Round(filamentGrams, 2),
                    Meters = Math.Round(filamentMeters, 2)
                },
                PrintTime = new PrintTime
                {
                    Hours = (int)printTimeHours,
                    Minutes = (int)((printTimeHours - (int)printTimeHours) * 60)
                },
                Costs = new Costs
                {
                    Filament = Math.Round(filamentCost, 2),
                    Electricity = Math.Round(electricityCost, 2),
                    Total = Math.Round(totalCost, 2)
                },
                RecommendedPrice = Math.Round(recommendedPrice, 2),
                Analysis = "Hesaplama otomatik olarak yapıldı. Gemini API yanıtı alınamadı."
            };
        }
    }
}

