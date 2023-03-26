namespace WaveFunctionCollapse
{
    public class TileData
    {
        // A list of tile names to symmetry values
        public Dictionary<string, string> tiles;

        // An adjacency list of tile faces to tile faces. These specify the faces of
        // tiles that can be connected together
        // For example "TileA North" -> "TileB West"
        // The rule would be that on top of TileA, TileB's west face could connect to it
        public Dictionary<string, HashSet<(string, string)>> neighbors;

        public TileData()
        {
            this.tiles = new Dictionary<string, string>();
            this.neighbors = new Dictionary<string, HashSet<(string, string)>>();
        }

        public void AddNeighbor(string neighborA, string neighborB)
        {
            AddToNeighbors(neighborA, neighborB);
        }

        public void AddNeighbors(HashSet<(string, string)> neighbors)
        {
            foreach ((string, string) neighborSet in neighbors)
            {
                AddNeighbor(neighborSet.Item1, neighborSet.Item2);
            }
        }

        public void AddTile(string tileName, string symmetry)
        {
            this.tiles.Add(tileName, symmetry);
        }

        public int GetRuleCount()
        {
            int count = 0;
            this.neighbors.Keys.ToList().ForEach(key => count += this.neighbors[key].Count);
            return count;
        }

        private void AddToNeighbors(string neighborA, string neighborB)
        {
            string tileTypeA = neighborA.Split()[0];

            if (this.neighbors.ContainsKey(tileTypeA))
            {
                this.neighbors[tileTypeA].Add((neighborA, neighborB));
            }
            else
            {
                this.neighbors.Add(tileTypeA, new HashSet<(string, string)>());
                this.neighbors[tileTypeA].Add((neighborA, neighborB));
            }
        }
    }
}
