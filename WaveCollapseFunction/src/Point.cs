using System.Xml.Linq;
namespace WaveFunctionCollapse
{
    public class Point
    {
        private (int, int) point;

        public int One { get { return this.point.Item1; } }
        public int Two { get { return this.point.Item2; } }

        public Point(int x, int y)
        {
            point = (x, y);
        }

        public Point Add(Point other)
        {
            return new Point(this.One + other.One, this.Two + other.Two);
        }

        public Point Subtract(Point other)
        {
            return new Point(this.One - other.One, this.Two - other.Two);
        }

        public override bool Equals(object obj)
        {

            return Equals(obj as Point);
        }

        public bool Equals(Point other)
        {
            return other != null &&
                   One == other.One &&
                   Two == other.Two;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(One, Two);
        }

        public override string ToString()
        {
            return $"({One}, {Two})";
        }
    }
}