namespace PrinterAutomation.Models
{
    public class StlModel
    {
        public string FilePath { get; set; } = string.Empty;
        public double Volume { get; set; } // cm³
        public double SurfaceArea { get; set; } // cm²
        public int TriangleCount { get; set; }
        public BoundingBox Bounds { get; set; } = new BoundingBox();
        public byte[] FileData { get; set; }
    }

    public class BoundingBox
    {
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
        public double MinZ { get; set; }
        public double MaxZ { get; set; }

        public double Width => MaxX - MinX;
        public double Height => MaxY - MinY;
        public double Depth => MaxZ - MinZ;
    }
}

