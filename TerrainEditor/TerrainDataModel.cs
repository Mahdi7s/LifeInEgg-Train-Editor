using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TerrainEditor
{
    [Serializable]
    public class TerrainPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public override bool Equals(object obj)
        {
            var tPoint = obj as TerrainPoint;
            return tPoint != null && (X == tPoint.X && Y == tPoint.Y);
        }
    }

    [Serializable]
    public class TerrainSegment
    {
        public TerrainSegment()
        {
            SegmentPoints = new List<TerrainPoint>();
        }
        public List<TerrainPoint> SegmentPoints { get; set; }

        public TerrainPoint StartPoint { get; set; }
        public TerrainPoint EndPoint { get; set; }
    }

    [Serializable]
    public class TerrainDataModel
    {
        public TerrainDataModel()
        {
            Segments = new List<TerrainSegment>();
        }

        public string Name { get; set; }
        public string ImagePath { get; set; }
        public Size TerrainSize { get; set; }

        public List<TerrainSegment> Segments { get; set; }
    }
}
