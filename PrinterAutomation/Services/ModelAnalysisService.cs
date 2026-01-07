using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;
using PrinterAutomation.Models;

namespace PrinterAutomation.Services
{
    public class ModelAnalysisResult
    {
        public string ModelName { get; set; }
        public double EstimatedFilamentGrams { get; set; }
        public double EstimatedFilamentMeters { get; set; }
        public double EstimatedPrintTimeHours { get; set; }
        public decimal FilamentCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal RecommendedPrice { get; set; }
        public decimal ProfitMargin { get; set; }
        public string AnalysisDetails { get; set; }
        public string GeminiAnalysis { get; set; }
        public bool UsedAI { get; set; }
    }

    public class ModelAnalysisService
    {
        // Filament maliyeti (gram başına TL)
        private const decimal FilamentCostPerGram = 0.15m; // 1kg filament = 150 TL varsayımı
        private const decimal ElectricityCostPerHour = 2.0m; // Saat başına elektrik maliyeti
        private const decimal LaborCostPerHour = 25.0m; // Saat başına işçilik maliyeti
        private const decimal ProfitMarginPercent = 50.0m; // %50 kar marjı
        
        // Varsayılan baskı parametreleri
        private const double DefaultInfillPercentage = 20.0;
        private const double DefaultLayerHeight = 0.2;
        
        private readonly string _geminiApiKey;
        private readonly StlAnalyzer _stlAnalyzer;
        
        public ModelAnalysisService()
        {
            _geminiApiKey = ConfigurationManager.AppSettings["GeminiApiKey"] ?? "";
            _stlAnalyzer = new StlAnalyzer();
        }

        public ModelAnalysisResult AnalyzeModel(string stlFilePath)
        {
            var result = new ModelAnalysisResult
            {
                ModelName = Path.GetFileName(stlFilePath),
                UsedAI = false
            };

            try
            {
                if (!File.Exists(stlFilePath))
                {
                    result.AnalysisDetails = "Dosya bulunamadı";
                    return result;
                }

                System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] STL dosyası analiz ediliyor: {stlFilePath}");
                System.Console.WriteLine($"[ModelAnalysis] STL dosyası analiz ediliyor...");

                // Gelişmiş STL analizi yap
                StlModel stlModel = null;
                try
                {
                    stlModel = _stlAnalyzer.AnalyzeStlFile(stlFilePath);
                    System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] STL analizi tamamlandı - Hacim: {stlModel.Volume:F2} cm³, Yüzey: {stlModel.SurfaceArea:F2} cm²");
                    System.Console.WriteLine($"[ModelAnalysis] STL analizi tamamlandı");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] STL analiz hatası: {ex.Message}");
                    System.Console.WriteLine($"[ModelAnalysis] STL analiz hatası: {ex.Message}");
                    // Eski yönteme geri dön
                    return AnalyzeModelLegacy(stlFilePath);
                }

                // Gemini AI ile analiz yap (eğer API key varsa)
                GeminiAnalysisResult geminiResult = null;
                if (!string.IsNullOrEmpty(_geminiApiKey) && stlModel != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] Gemini API çağrısı başlatılıyor...");
                        System.Console.WriteLine($"[ModelAnalysis] Gemini API çağrısı başlatılıyor...");
                        
                        var geminiService = new GeminiAnalysisService(_geminiApiKey);
                        
                        // Deadlock'u önlemek için Task.Run içinde çalıştır
                        var task = Task.Run(async () => await geminiService.AnalyzeModelAsync(stlModel, DefaultInfillPercentage, DefaultLayerHeight).ConfigureAwait(false));
                        
                        try
                        {
                            // Timeout ile bekle
                            bool completed = task.Wait(TimeSpan.FromSeconds(60));
                            
                            System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] Task durumu - Completed: {completed}, IsCompleted: {task.IsCompleted}, IsFaulted: {task.IsFaulted}, IsCanceled: {task.IsCanceled}");
                            System.Console.WriteLine($"[ModelAnalysis] Task durumu - Completed: {completed}, IsCompleted: {task.IsCompleted}");
                            
                            if (completed && task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                            {
                                geminiResult = task.Result;
                                System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] GeminiResult null mu? {geminiResult == null}");
                                System.Console.WriteLine($"[ModelAnalysis] GeminiResult null mu? {geminiResult == null}");
                                
                                if (geminiResult != null)
                                {
                                    result.UsedAI = true;
                                    result.EstimatedFilamentGrams = geminiResult.FilamentAmount.Grams;
                                    result.EstimatedFilamentMeters = geminiResult.FilamentAmount.Meters;
                                    result.EstimatedPrintTimeHours = geminiResult.PrintTime.Hours + (geminiResult.PrintTime.Minutes / 60.0);
                                    result.GeminiAnalysis = geminiResult.Analysis;
                        
                                    // Gemini'den gelen maliyet değerlerini kullan
                                    result.FilamentCost = (decimal)geminiResult.Costs.Filament;
                                    
                                    System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] Gemini API başarılı - UsedAI: {result.UsedAI}, Filament: {result.EstimatedFilamentGrams}g, Cost: {result.FilamentCost}TL, Price: {geminiResult.RecommendedPrice}TL");
                                    System.Console.WriteLine($"[ModelAnalysis] Gemini API başarılı - UsedAI: {result.UsedAI}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] GeminiResult null!");
                                    System.Console.WriteLine($"[ModelAnalysis] GeminiResult null!");
                                }
                            }
                            else if (task.IsFaulted)
                            {
                                result.GeminiAnalysis = $"Gemini AI hatası: {task.Exception?.GetBaseException()?.Message ?? "Bilinmeyen hata"}";
                                System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] Gemini API exception: {task.Exception}");
                                System.Console.WriteLine($"[ModelAnalysis] Gemini API exception: {task.Exception?.GetBaseException()?.Message}");
                            }
                            else if (!completed)
                            {
                                result.GeminiAnalysis = "Gemini API zaman aşımı (60 saniye)";
                                System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] Gemini API timeout");
                                System.Console.WriteLine($"[ModelAnalysis] Gemini API timeout");
                            }
                        }
                        catch (Exception taskEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] Task exception: {taskEx}");
                            System.Console.WriteLine($"[ModelAnalysis] Task exception: {taskEx.Message}");
                            result.GeminiAnalysis = $"Gemini AI analizi yapılamadı: {taskEx.Message}";
                        }
                    }
                    catch (Exception ex)
                    {
                        result.GeminiAnalysis = $"Gemini AI analizi yapılamadı: {ex.Message}";
                        System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] Gemini API genel hata: {ex}");
                        System.Console.WriteLine($"[ModelAnalysis] Gemini API genel hata: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] Gemini API key bulunamadı veya STL model null");
                    System.Console.WriteLine($"[ModelAnalysis] Gemini API key bulunamadı");
                }

                // Eğer Gemini sonucu yoksa, STL modelinden hesapla
                if (!result.UsedAI && stlModel != null)
                {
                    // Fallback: STL modelinden hesaplama
                    var fallbackResult = CalculateFromStlModel(stlModel, DefaultInfillPercentage, DefaultLayerHeight);
                    result.EstimatedFilamentGrams = fallbackResult.FilamentGrams;
                    result.EstimatedFilamentMeters = fallbackResult.FilamentMeters;
                    result.EstimatedPrintTimeHours = fallbackResult.PrintTimeHours;
                }

                // Maliyet hesaplama
                decimal electricityCost;
                decimal laborCost;
                decimal totalCost;
                
                if (geminiResult != null && result.UsedAI)
                {
                    // Gemini'den gelen maliyet değerlerini kullan
                    electricityCost = (decimal)geminiResult.Costs.Electricity;
                    totalCost = (decimal)geminiResult.Costs.Total;
                    laborCost = totalCost - result.FilamentCost - electricityCost; // Labor cost'u hesapla
                    
                    // Gemini'den gelen fiyat önerisini kullan
                    result.RecommendedPrice = (decimal)geminiResult.RecommendedPrice;
                    result.ProfitMargin = result.RecommendedPrice - totalCost;
                    result.TotalCost = totalCost; // TotalCost'u kaydet
                }
                else
                {
                    // Fallback: Manuel hesaplama
                    if (result.FilamentCost == 0)
                    {
                result.FilamentCost = (decimal)result.EstimatedFilamentGrams * FilamentCostPerGram;
                    }
                    electricityCost = (decimal)result.EstimatedPrintTimeHours * ElectricityCostPerHour;
                    laborCost = (decimal)result.EstimatedPrintTimeHours * LaborCostPerHour;
                    totalCost = result.FilamentCost + electricityCost + laborCost;

                // Kar marjı ile fiyat önerisi
                result.ProfitMargin = totalCost * (ProfitMarginPercent / 100.0m);
                result.RecommendedPrice = totalCost + result.ProfitMargin;
                    result.TotalCost = totalCost; // TotalCost'u kaydet
                }

                // Analiz detayları
                result.AnalysisDetails = $"📊 MODEL BİLGİLERİ:\n" +
                    $"   • Hacim: {stlModel?.Volume:F2} cm³\n" +
                    $"   • Yüzey Alanı: {stlModel?.SurfaceArea:F2} cm²\n" +
                    $"   • Üçgen Sayısı: {stlModel?.TriangleCount:N0}\n" +
                    $"   • Boyutlar: {stlModel?.Bounds.Width:F2} x {stlModel?.Bounds.Height:F2} x {stlModel?.Bounds.Depth:F2} mm\n\n" +
                    $"📦 FİLAMENT TAHMİNİ:\n" +
                    $"   • Miktar: {result.EstimatedFilamentGrams:F1} g ({result.EstimatedFilamentMeters:F1} m)\n\n" +
                    $"⏱️  BASKI SÜRESİ:\n" +
                    $"   • Tahmini Süre: {result.EstimatedPrintTimeHours:F2} saat\n\n" +
                    $"💰 MALİYET ANALİZİ:\n" +
                    $"   • Filament Maliyeti: {result.FilamentCost:F2} TL\n" +
                    $"   • Elektrik Maliyeti: {electricityCost:F2} TL\n" +
                    $"   • İşçilik Maliyeti: {laborCost:F2} TL\n" +
                    $"   • Toplam Maliyet: {totalCost:F2} TL\n" +
                    $"   • Kar Marjı (%{ProfitMarginPercent}): {result.ProfitMargin:F2} TL\n\n" +
                    $"💵 ÖNERİLEN SATIŞ FİYATI:\n" +
                    $"   • {result.RecommendedPrice:F2} TL";
                
                if (!string.IsNullOrEmpty(result.GeminiAnalysis) && result.UsedAI)
                {
                    result.AnalysisDetails += $"\n\n📋 DETAYLI ANALİZ:\n{result.GeminiAnalysis}";
                }
            }
            catch (Exception ex)
            {
                result.AnalysisDetails = $"Analiz hatası: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[ModelAnalysis] Genel hata: {ex}");
                System.Console.WriteLine($"[ModelAnalysis] Genel hata: {ex.Message}");
            }

            return result;
        }

        private (double FilamentGrams, double FilamentMeters, double PrintTimeHours) CalculateFromStlModel(
            StlModel model, double infillPercentage, double layerHeight)
        {
            // FilamentAnalyzer'daki hesaplama mantığı
            double filamentDensity = 1.24; // g/cm³
            double filamentDiameter = 0.175; // cm
            double filamentCrossSection = Math.PI * Math.Pow(filamentDiameter / 2, 2); // cm²
            double shellThickness = 0.06; // cm
            
            double solidVolume = model.Volume;
            double shellVolume = (model.SurfaceArea * shellThickness);
            double innerVolume = Math.Max(0, solidVolume - shellVolume);
            double infillVolume = innerVolume * (infillPercentage / 100.0);
            double totalVolume = shellVolume + infillVolume;
            
            double filamentGrams = totalVolume * filamentDensity;
            double filamentMeters = totalVolume / filamentCrossSection;
            
            double layerCount = Math.Max(1, model.Bounds.Height / layerHeight);
            double printSpeed = 50; // mm/s
            double infillSpeed = 80; // mm/s
            double shellPathLength = (model.SurfaceArea * 10) * 1.5; // mm
            double infillPathLength = (infillVolume * 1000) / filamentCrossSection; // mm
            double totalTimeSeconds = (shellPathLength / printSpeed) + (infillPathLength / infillSpeed);
            double printTimeHours = totalTimeSeconds / 3600.0;
            
            return (filamentGrams, filamentMeters, printTimeHours);
        }

        private ModelAnalysisResult AnalyzeModelLegacy(string stlFilePath)
        {
            // Eski yöntem (fallback)
            var result = new ModelAnalysisResult
            {
                ModelName = Path.GetFileName(stlFilePath),
                UsedAI = false
            };

            var fileInfo = new FileInfo(stlFilePath);
            double fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
            string fileName = Path.GetFileNameWithoutExtension(stlFilePath).ToLower();
            string folderName = Path.GetDirectoryName(stlFilePath)?.Split(Path.DirectorySeparatorChar).LastOrDefault()?.ToLower() ?? "";

            var analysis = AnalyzeSTLFile(stlFilePath, fileName, folderName, fileSizeMB);
            result.EstimatedFilamentGrams = analysis.FilamentGrams;
            result.EstimatedFilamentMeters = analysis.FilamentMeters;
            result.EstimatedPrintTimeHours = analysis.PrintTimeHours;

            result.FilamentCost = (decimal)result.EstimatedFilamentGrams * FilamentCostPerGram;
            decimal electricityCost = (decimal)result.EstimatedPrintTimeHours * ElectricityCostPerHour;
            decimal laborCost = (decimal)result.EstimatedPrintTimeHours * LaborCostPerHour;
            decimal totalCost = result.FilamentCost + electricityCost + laborCost;
            result.TotalCost = totalCost; // TotalCost'u kaydet
            result.ProfitMargin = totalCost * (ProfitMarginPercent / 100.0m);
            result.RecommendedPrice = totalCost + result.ProfitMargin;

            result.AnalysisDetails = $"Dosya Boyutu: {fileSizeMB:F2} MB\n" +
                $"Tahmini Filament: {result.EstimatedFilamentGrams:F1} g ({result.EstimatedFilamentMeters:F1} m)\n" +
                $"Tahmini Süre: {result.EstimatedPrintTimeHours:F2} saat\n" +
                $"Toplam Maliyet: {totalCost:F2} TL\n" +
                $"Önerilen Fiyat: {result.RecommendedPrice:F2} TL";

            return result;
        }


        private (double FilamentGrams, double FilamentMeters, double PrintTimeHours) AnalyzeSTLFile(
            string filePath, string fileName, string folderName, double fileSizeMB)
        {
            double filamentGrams = 0;
            double filamentMeters = 0;
            double printTimeHours = 0;

            // Dosya boyutuna göre temel tahmin
            // STL dosya boyutu genellikle model karmaşıklığı ile ilişkilidir
            double baseFilamentGrams = fileSizeMB * 2.5; // MB başına yaklaşık 2.5 gram
            double basePrintHours = fileSizeMB * 0.15; // MB başına yaklaşık 0.15 saat

            // Model tipine göre ayarlamalar
            double complexityFactor = 1.0;
            double timeFactor = 1.0;

            // Klasör adına göre
            if (folderName.Contains("octo"))
            {
                complexityFactor = 1.2; // Octo daha karmaşık
                timeFactor = 1.3;
            }
            else if (folderName.Contains("shark"))
            {
                if (fileName.Contains("body"))
                {
                    complexityFactor = 1.5; // Body daha büyük
                    timeFactor = 1.8;
                }
                else if (fileName.Contains("head"))
                {
                    complexityFactor = 0.8; // Head daha küçük
                    timeFactor = 0.9;
                }
            }
            else if (folderName.Contains("whist"))
            {
                complexityFactor = 1.1;
                timeFactor = 1.2;
            }

            // Dosya adına göre ek ayarlamalar
            if (fileName.Contains("articulated") || fileName.Contains("cute"))
            {
                complexityFactor *= 1.3; // Hareketli parçalar daha fazla filament
                timeFactor *= 1.4;
            }

            if (fileName.Contains("easy") || fileName.Contains("hard"))
            {
                complexityFactor *= 0.9; // Press-in parçalar genelde daha basit
            }

            // STL dosyasını oku ve daha detaylı analiz yap
            try
            {
                var detailedAnalysis = ReadSTLFileDetails(filePath);
                if (detailedAnalysis.VertexCount > 0)
                {
                    // Vertex sayısına göre daha hassas tahmin
                    double vertexFactor = Math.Min(2.0, detailedAnalysis.VertexCount / 10000.0);
                    complexityFactor *= (1.0 + vertexFactor * 0.3);
                    timeFactor *= (1.0 + vertexFactor * 0.4);
                }
            }
            catch
            {
                // STL okuma hatası olursa basit tahmin kullan
            }

            filamentGrams = baseFilamentGrams * complexityFactor;
            printTimeHours = basePrintHours * timeFactor;

            // Filament metreyi hesapla (1.75mm filament, yoğunluk 1.24 g/cm³)
            // 1 gram ≈ 0.26 metre (1.75mm filament için)
            filamentMeters = filamentGrams * 0.26;

            // Minimum değerler
            filamentGrams = Math.Max(5, filamentGrams);
            filamentMeters = Math.Max(1, filamentMeters);
            printTimeHours = Math.Max(0.5, printTimeHours);

            return (filamentGrams, filamentMeters, printTimeHours);
        }

        private (int VertexCount, int FaceCount) ReadSTLFileDetails(string filePath)
        {
            int vertexCount = 0;
            int faceCount = 0;

            try
            {
                // STL dosyasını oku (ASCII veya Binary)
                using (var reader = new StreamReader(filePath))
                {
                    string firstLine = reader.ReadLine()?.Trim() ?? "";
                    
                    // ASCII STL kontrolü
                    if (firstLine.StartsWith("solid", StringComparison.OrdinalIgnoreCase))
                    {
                        // ASCII format
                        string content = File.ReadAllText(filePath);
                        var lines = content.Split('\n');
                        
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("vertex", StringComparison.OrdinalIgnoreCase))
                            {
                                vertexCount++;
                            }
                            else if (trimmed.StartsWith("facet", StringComparison.OrdinalIgnoreCase))
                            {
                                faceCount++;
                            }
                        }
                    }
                    else
                    {
                        // Binary format - basit tahmin
                        var fileInfo = new FileInfo(filePath);
                        // Binary STL: 80 byte header + 4 byte face count + (50 byte per face)
                        if (fileInfo.Length > 84)
                        {
                            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                            {
                                fs.Seek(80, SeekOrigin.Begin);
                                byte[] faceCountBytes = new byte[4];
                                fs.Read(faceCountBytes, 0, 4);
                                faceCount = BitConverter.ToInt32(faceCountBytes, 0);
                                vertexCount = faceCount * 3; // Her face 3 vertex
                            }
                        }
                    }
                }
            }
            catch
            {
                // Hata durumunda 0 döndür
            }

            return (vertexCount, faceCount);
        }
    }
}

