using System;
using System.Collections.Generic;
using System.IO;
using PrinterAutomation.Models;

namespace PrinterAutomation.Services
{
    public class StlAnalyzer
    {
        public StlModel AnalyzeStlFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"STL dosyası bulunamadı: {filePath}");

            var model = new StlModel
            {
                FilePath = filePath,
                FileData = File.ReadAllBytes(filePath)
            };

            // STL dosyasını oku (ASCII veya Binary)
            bool isAscii = IsAsciiStl(filePath);
            
            if (isAscii)
            {
                AnalyzeAsciiStl(filePath, model);
            }
            else
            {
                AnalyzeBinaryStl(filePath, model);
            }

            return model;
        }

        private bool IsAsciiStl(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string firstLine = reader.ReadLine() ?? "";
                return firstLine.Trim().StartsWith("solid", StringComparison.OrdinalIgnoreCase);
            }
        }

        private void AnalyzeAsciiStl(string filePath, StlModel model)
        {
            var triangles = new List<Triangle>();
            var lines = File.ReadAllLines(filePath);
            
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            double minZ = double.MaxValue, maxZ = double.MinValue;

            var currentTriangle = new List<Vertex>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                if (line.StartsWith("vertex", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        if (double.TryParse(parts[1], out double x) &&
                            double.TryParse(parts[2], out double y) &&
                            double.TryParse(parts[3], out double z))
                        {
                            var vertex = new Vertex { X = x, Y = y, Z = z };
                            currentTriangle.Add(vertex);
                            
                            minX = Math.Min(minX, x);
                            maxX = Math.Max(maxX, x);
                            minY = Math.Min(minY, y);
                            maxY = Math.Max(maxY, y);
                            minZ = Math.Min(minZ, z);
                            maxZ = Math.Max(maxZ, z);

                            if (currentTriangle.Count == 3)
                            {
                                triangles.Add(new Triangle
                                {
                                    V1 = currentTriangle[0],
                                    V2 = currentTriangle[1],
                                    V3 = currentTriangle[2]
                                });
                                currentTriangle.Clear();
                            }
                        }
                    }
                }
                else if (line.StartsWith("endfacet", StringComparison.OrdinalIgnoreCase))
                {
                    currentTriangle.Clear();
                }
            }

            model.TriangleCount = triangles.Count;
            model.Bounds = new BoundingBox
            {
                MinX = minX,
                MaxX = maxX,
                MinY = minY,
                MaxY = maxY,
                MinZ = minZ,
                MaxZ = maxZ
            };

            // Gerçek hacim hesaplama (signed volume method)
            double volumeMm3 = 0.0;
            double surfaceAreaMm2 = 0.0;

            foreach (var triangle in triangles)
            {
                // Üçgenin hacmini hesapla (signed volume)
                double v321 = triangle.V3.X * triangle.V2.Y * triangle.V1.Z;
                double v231 = triangle.V2.X * triangle.V3.Y * triangle.V1.Z;
                double v312 = triangle.V3.X * triangle.V1.Y * triangle.V2.Z;
                double v132 = triangle.V1.X * triangle.V3.Y * triangle.V2.Z;
                double v213 = triangle.V2.X * triangle.V1.Y * triangle.V3.Z;
                double v123 = triangle.V1.X * triangle.V2.Y * triangle.V3.Z;
                
                volumeMm3 += (-v321 + v231 + v312 - v132 - v213 + v123) / 6.0;

                // Üçgenin yüzey alanını hesapla
                double a = Distance(triangle.V1, triangle.V2);
                double b = Distance(triangle.V2, triangle.V3);
                double c = Distance(triangle.V3, triangle.V1);
                double s = (a + b + c) / 2.0;
                double area = Math.Sqrt(Math.Max(0, s * (s - a) * (s - b) * (s - c)));
                surfaceAreaMm2 += area;
            }

            // mm³'den cm³'e dönüştür (1 cm³ = 1000 mm³)
            model.Volume = Math.Abs(volumeMm3) / 1000.0;
            
            // mm²'den cm²'ye dönüştür (1 cm² = 100 mm²)
            model.SurfaceArea = surfaceAreaMm2 / 100.0;
        }

        private double Distance(Vertex v1, Vertex v2)
        {
            double dx = v2.X - v1.X;
            double dy = v2.Y - v1.Y;
            double dz = v2.Z - v1.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private void AnalyzeBinaryStl(string filePath, StlModel model)
        {
            using (var reader = new BinaryReader(File.OpenRead(filePath)))
            {
                // Binary STL header (80 bytes)
                reader.ReadBytes(80);
                
                // Triangle count (4 bytes)
                uint triangleCount = reader.ReadUInt32();
                model.TriangleCount = (int)triangleCount;

                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;
                double minZ = double.MaxValue, maxZ = double.MinValue;

                var triangles = new List<Triangle>();

                for (uint i = 0; i < triangleCount; i++)
                {
                    // Normal vector (3 floats - 12 bytes) - skip
                    reader.ReadBytes(12);
                    
                    // Three vertices (3 floats each - 36 bytes)
                    var v1 = new Vertex { X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle() };
                    var v2 = new Vertex { X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle() };
                    var v3 = new Vertex { X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle() };
                    
                    triangles.Add(new Triangle { V1 = v1, V2 = v2, V3 = v3 });
                    
                    minX = Math.Min(minX, Math.Min(v1.X, Math.Min(v2.X, v3.X)));
                    maxX = Math.Max(maxX, Math.Max(v1.X, Math.Max(v2.X, v3.X)));
                    minY = Math.Min(minY, Math.Min(v1.Y, Math.Min(v2.Y, v3.Y)));
                    maxY = Math.Max(maxY, Math.Max(v1.Y, Math.Max(v2.Y, v3.Y)));
                    minZ = Math.Min(minZ, Math.Min(v1.Z, Math.Min(v2.Z, v3.Z)));
                    maxZ = Math.Max(maxZ, Math.Max(v1.Z, Math.Max(v2.Z, v3.Z)));
                    
                    // Attribute byte count (2 bytes) - skip
                    reader.ReadBytes(2);
                }

                model.Bounds = new BoundingBox
                {
                    MinX = minX,
                    MaxX = maxX,
                    MinY = minY,
                    MaxY = maxY,
                    MinZ = minZ,
                    MaxZ = maxZ
                };

                // Gerçek hacim hesaplama (signed volume method)
                double volumeMm3 = 0.0;
                double surfaceAreaMm2 = 0.0;

                foreach (var triangle in triangles)
                {
                    // Üçgenin hacmini hesapla (signed volume)
                    double v321 = triangle.V3.X * triangle.V2.Y * triangle.V1.Z;
                    double v231 = triangle.V2.X * triangle.V3.Y * triangle.V1.Z;
                    double v312 = triangle.V3.X * triangle.V1.Y * triangle.V2.Z;
                    double v132 = triangle.V1.X * triangle.V3.Y * triangle.V2.Z;
                    double v213 = triangle.V2.X * triangle.V1.Y * triangle.V3.Z;
                    double v123 = triangle.V1.X * triangle.V2.Y * triangle.V3.Z;
                    
                    volumeMm3 += (-v321 + v231 + v312 - v132 - v213 + v123) / 6.0;

                    // Üçgenin yüzey alanını hesapla
                    double a = Distance(triangle.V1, triangle.V2);
                    double b = Distance(triangle.V2, triangle.V3);
                    double c = Distance(triangle.V3, triangle.V1);
                    double s = (a + b + c) / 2.0;
                    double area = Math.Sqrt(Math.Max(0, s * (s - a) * (s - b) * (s - c)));
                    surfaceAreaMm2 += area;
                }

                // mm³'den cm³'e dönüştür (1 cm³ = 1000 mm³)
                model.Volume = Math.Abs(volumeMm3) / 1000.0;
                
                // mm²'den cm²'ye dönüştür (1 cm² = 100 mm²)
                model.SurfaceArea = surfaceAreaMm2 / 100.0;
            }
        }

        private class Vertex
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }

        private class Triangle
        {
            public Vertex V1 { get; set; }
            public Vertex V2 { get; set; }
            public Vertex V3 { get; set; }
        }
    }
}

