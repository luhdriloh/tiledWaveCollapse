namespace WaveFunctionCollapse
{
    public enum Symmetry
    {
        L,
        T,
        I,
        X
    }

    public enum CardinalDirection : ushort
    {
        N = 0,
        E,
        S,
        W
    }

    // The tile class will hold ONLY a simple tiles worth of information
    // Namely the type of tile, aka, the tile name and the direction in which it is pointing
    // All base tiles can be assumed to have a direction of 0. If rotated to the right 90 degrees (facing east)
    //  then the direction will then be said to be 1, etc for South and West directions
    public class Tile
    {
        public string Name { get; set; }
        public CardinalDirection Direction { get; set; }

        public Tile(string name, CardinalDirection direction)
        {
            this.Name = name;
            this.Direction = direction;
        }

        public Tile(string name, int direction)
        {
            this.Name = name;
            this.Direction = (CardinalDirection)direction;
        }

        public Tile(Tile other)
        {
            this.Name = other.Name;
            this.Direction = other.Direction;
        }

        public override bool Equals(object obj)
        {

            return Equals(obj as Tile);
        }

        public bool Equals(Tile other)
        {
            return other != null &&
                   Name.Equals(other.Name, StringComparison.CurrentCultureIgnoreCase) &&
                   Direction == other.Direction;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Direction);
        }

        public override string ToString()
        {
            return $"{Name} {(int)Direction}";
        }
    }
}
