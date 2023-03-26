using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace WaveFunctionCollapse
{
    public enum Mode
    {
        LOWEST_ENTROPY
    }

    public class TiledWaveFunctionCollapse
    {
        private Mode selectionMode;
        private int rows, columns, collapsedTileCount;

        private Tile[,] tilesSelected;
        private HashSet<Tile> validTiles;
        private Dictionary<Point, HashSet<Tile>> tileSuperpositions;
        private Dictionary<Tile, Dictionary<int, HashSet<Tile>>> tileRules;
        private Stack<Point> toCheck;

        private Random rand;
        private Queue<Point> nextUpQueue;
        private HashSet<Point> seenOnQueue;

        public TiledWaveFunctionCollapse(int rows, int columns, TileData tileData, Mode selectionMode)
        {
            this.rows = rows;
            this.columns = columns;

            this.selectionMode = selectionMode;

            this.InitializeWave();

            // Process the tile data passed in
            this.ProcessTileData(tileData);

            for (int i = 0; i < this.rows; i++)
            {
                for (int j = 0; j < this.columns; j++)
                {
                    this.tileSuperpositions.Add(new Point(i, j), new HashSet<Tile>(this.validTiles));
                }
            }
        }

        public Tile[,] SingleCollapseWave()
        {
            if (this.collapsedTileCount >= this.tilesSelected.Length)
            {
                return this.tilesSelected;
            }

            Point nextTileToCollapse = NextTileToCollapse();
            Propogate(nextTileToCollapse);
            return this.tilesSelected;
        }

        public Tile[,] CollapseWave()
        {
            int size = this.rows * this.columns;
            while (this.collapsedTileCount < size)
            {
                Point nextTileToCollapse = NextTileToCollapse();
                Propogate(nextTileToCollapse);
            }

            return this.tilesSelected;
        }

        private void InitializeWave()
        {
            this.tilesSelected = new Tile[this.rows, this.columns];

            this.validTiles = new HashSet<Tile>();
            this.tileSuperpositions = new Dictionary<Point, HashSet<Tile>>();
            this.tileRules = new Dictionary<Tile, Dictionary<int, HashSet<Tile>>>();
            this.toCheck = new Stack<Point>();
            this.nextUpQueue = new Queue<Point>();
            this.seenOnQueue = new HashSet<Point>();

            this.collapsedTileCount = 0;
            this.rand = new Random();
        }

        private void Propogate(Point tileToCollapse)
        {
            // Search, and select tile
            Tile[] possibleTiles = this.tileSuperpositions[tileToCollapse].ToArray();

            // Collapse the tile by randomly selecting one of the possibilities
            int indexToPick = this.rand.Next(possibleTiles.Length);
            Tile selectedTile = possibleTiles[indexToPick];

            this.tileSuperpositions[tileToCollapse].Clear();
            this.tileSuperpositions[tileToCollapse].Add(selectedTile);

            Queue<Point> toCheck = new Queue<Point>();
            HashSet<Point> seen = new HashSet<Point>();

            toCheck.Enqueue(tileToCollapse);
            seen.Add(tileToCollapse);

            while (toCheck.Count > 0)
            {
                Point position = toCheck.Dequeue();
                ValidateTileSuperposition(position);

                // Collapse the tile completely
                if (this.tileSuperpositions[position].Count == 1 && this.tilesSelected[position.One, position.Two] == null)
                {
                    //Console.WriteLine($"Collapsed Tile: ({position.One}, {position.Two})");
                    this.tilesSelected[position.One, position.Two] = this.tileSuperpositions[position].ToArray()[0];
                    this.collapsedTileCount++;
                }

                // Get an adjacent node list
                HashSet<Point> adjacentNodes = GetAdjacentNodes(position);
                adjacentNodes = new HashSet<Point>(adjacentNodes.Where(node => this.tilesSelected[node.One, node.Two] == null));

                // Collapse all adjacent nodes and filter out any seen points
                CollapseAdjacentNodes(position, adjacentNodes, seen);
                IEnumerable<Point> pointsToAdd = adjacentNodes.Except(seen);

                // Add adjacent nodes to queue
                foreach (Point node in pointsToAdd)
                {
                    seen.Add(node);
                    toCheck.Enqueue(node);
                }
            }
        }

        private HashSet<Point> GetAdjacentNodes(Point location)
        {
            HashSet<Point> adjacentNodes = new HashSet<Point>();
            AddToAdjacentListIfValid(location, new Point(-1, 0), adjacentNodes);
            AddToAdjacentListIfValid(location, new Point(1, 0), adjacentNodes);
            AddToAdjacentListIfValid(location, new Point(0, -1), adjacentNodes);
            AddToAdjacentListIfValid(location, new Point(0, 1), adjacentNodes);

            return adjacentNodes;
        }

        private void AddToAdjacentListIfValid(Point position, Point add, HashSet<Point> nodes)
        {
            if (ValidateGridLocation(position.Add(add)))
            {
                nodes.Add(position.Add(add));
            }
        }

        private bool ValidateGridLocation(Point location)
        {
            return (location.One >= 0 && location.One < this.rows &&
                location.Two >= 0 && location.Two < this.columns);
        }

        private void CollapseAdjacentNodes(Point location, HashSet<Point> adjacentNodes, HashSet<Point> seen)
        {
            // For the current location
            // for each possible set of tiles in the specified direction of the adjacent node
            // Intersect with the adjacent nodes superimposed/possible tiles
            List<Tile> currentPossibleTiles = this.tileSuperpositions[location].ToList();
            foreach (Point nodePosition in adjacentNodes)
            {
                CardinalDirection cardinalDirectionOfAdjacentNode = FindCardinalDirectionToAdjacentNode(location, nodePosition);

                // Get hashset of all new possible tiles
                HashSet<Tile> newSetOfPossibleTiles = new HashSet<Tile>();
                foreach (Tile possibleTile in currentPossibleTiles)
                {
                    newSetOfPossibleTiles.UnionWith(this.tileRules[possibleTile][(int)cardinalDirectionOfAdjacentNode]);
                }

                int preCount = this.tileSuperpositions[nodePosition].Count;

                // Collapse the node with all possible tiles found
                CollapseNodeWithIncomingTiles(nodePosition, newSetOfPossibleTiles);

                // If the count has not changed set the node to seen
                if (preCount == this.tileSuperpositions[nodePosition].Count ||
                    this.tileSuperpositions[nodePosition].Count == 1)
                {
                    seen.Add(nodePosition);
                }
            }
        }

        private CardinalDirection FindCardinalDirectionToAdjacentNode(Point location, Point adjacentNode)
        {
            Point sub = adjacentNode.Subtract(location);
            if (sub.One == 1)
            {
                return CardinalDirection.N;
            }
            else if (sub.One == -1)
            {
                return CardinalDirection.S;
            }
            else if (sub.Two == -1)
            {
                return CardinalDirection.W;
            }
            else if (sub.Two == 1)
            {
                return CardinalDirection.E;
            }

            throw new Exception("CardinalDirectionFromOriginLocation: There is a problem with finding cardinality to adjacent nodes.");
        }

        private void CollapseNodeWithIncomingTiles(Point positionOfTileToCollapse, HashSet<Tile> incomingPossibleTiles)
        {
            // Do the intersection between the incoming tiles and the current possible tile in
            HashSet<Tile> tilesToCollapse = this.tileSuperpositions[positionOfTileToCollapse];
            tilesToCollapse.IntersectWith(incomingPossibleTiles);
        }

        private void ProcessTileData(TileData tileData)
        {
            ExpandTilesBasedOnSymmetry(tileData);

            // Print out expanded tile rules
            foreach (var tileType in tileData.neighbors.Keys)
            {
                foreach (var neighbors in tileData.neighbors[tileType])
                {
                    Console.WriteLine("\nleft: " + neighbors.Item1 + "\nright: " + neighbors.Item2);
                }
            }

            // Create a map of tile/direction to a set of valid
            //  tiles (in the 4 cardinal directions)
            // In the process create a set of all validTiles
            foreach (var tileType in tileData.neighbors.Keys)
            {
                Console.WriteLine($"Starting rule creation for tile: {tileType}");

                foreach (var neighbor in tileData.neighbors[tileType])
                {
                    int leftFaceIndex = FindFaceValueFromTileNeighborValue(neighbor.Item1);
                    int rightFaceIndex = FindFaceValueFromTileNeighborValue(neighbor.Item2);

                    // Find the rotation needed for the fit
                    int rightIndexRotation = FindTileRotation(leftFaceIndex, rightFaceIndex);
                    string rightTileName = neighbor.Item2.Split()[0];

                    // Fill the rule out for every rotation
                    for (int i = 0; i < 4; i++)
                    {
                        Tile tile = new Tile(tileType, i);

                        // First part of tile rule. Tile - Rotation
                        //   Add to dictionary if not there
                        if (!this.tileRules.ContainsKey(tile))
                        {
                            this.tileRules.Add(tile, new Dictionary<int, HashSet<Tile>>());
                        }

                        // Add as a valid tile
                        this.validTiles.Add(tile);

                        // Retrieve the left side and use its index for N, E, S, W + (Rotation offset)
                        int leftFaceIndexPlusRotationOffset = (leftFaceIndex + i) % 4;
                        if (!this.tileRules[tile].ContainsKey(leftFaceIndexPlusRotationOffset))
                        {
                            this.tileRules[tile].Add(leftFaceIndexPlusRotationOffset, new HashSet<Tile>());
                        }

                        // Find the right neighbor tile rotation and add as a valid tile
                        int newRightIndexRotation = (rightIndexRotation + i) % 4;
                        this.tileRules[tile][leftFaceIndexPlusRotationOffset].Add(new Tile(rightTileName, newRightIndexRotation));
                    }
                }
            }

            foreach (Tile tileVal in tileRules.Keys)
            {
                Console.WriteLine();
                foreach (int cardinalDirection in tileRules[tileVal].Keys)
                {
                    foreach (Tile validTile in tileRules[tileVal][cardinalDirection])
                    {
                        Console.WriteLine($"{tileVal} - {(CardinalDirection)cardinalDirection} - {validTile}");
                    }
                }
            }

            Console.WriteLine();
        }

        private void ExpandTilesBasedOnSymmetry(TileData tileData)
        {
            // Recursively expand tiles
            int ruleCount = tileData.GetRuleCount();

            // expand tiles depending on the symmetry value
            foreach (var tileType in tileData.neighbors.Keys)
            {
                string symmetry = tileData.tiles[tileType];
                HashSet<(string, string)> rulesToAdd = new HashSet<(string, string)>();
                foreach (var neighbors in tileData.neighbors[tileType])
                {
                    if (symmetry.Equals("L"))
                    {
                        int faceValue = FindFaceValueFromTileNeighborValue(neighbors.Item1);

                        // mod in c# is funky and returns negative when working with neg
                        int newFaceValue = (4 + (1 - faceValue)) % 4;
                        newFaceValue = newFaceValue < 0 ? newFaceValue + 4 : newFaceValue;

                        rulesToAdd.Add(($"{tileType} {newFaceValue}", neighbors.Item2));
                    }

                    if (symmetry.Equals("X"))
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            rulesToAdd.Add(($"{tileType} {i}", neighbors.Item2));
                        }
                    }

                    if (symmetry.Equals("I"))
                    {
                        int faceValue = FindFaceValueFromTileNeighborValue(neighbors.Item1);
                        int newFaceValue = (faceValue + 2) % 4;

                        rulesToAdd.Add(($"{tileType} {newFaceValue}", neighbors.Item2));
                    }

                    if (symmetry.Equals("T"))
                    {
                        int faceValue = FindFaceValueFromTileNeighborValue(neighbors.Item1);
                        if (faceValue == 0)
                        {
                            continue;
                        }

                        for (int i = 1; i < 4; i++)
                        {
                            rulesToAdd.Add(($"{tileType} {i}", neighbors.Item2));
                        }
                    }
                }

                tileData.neighbors[tileType].UnionWith(rulesToAdd);
            }

            // Add the inverse rules after expansion
            HashSet<(string, string)> newRules = new HashSet<(string, string)>();
            foreach (var tileType in tileData.neighbors.Keys)
            {
                foreach (var neighbors in tileData.neighbors[tileType])
                {
                    newRules.Add((neighbors.Item2, neighbors.Item1));
                }
            }

            tileData.AddNeighbors(newRules);

            if (tileData.GetRuleCount() > ruleCount)
            {
                Console.WriteLine("Expanding tile ruleset!");
                ExpandTilesBasedOnSymmetry(tileData);
            }
        }

        private int FindTileRotation(int faceOne, int faceTwo)
        {
            // Based on 2 tile faces that connect find the rotation needed of face 2
            // to match that of faceOne
            int inverted = (faceOne + 2) % 4;
            int tileRotationValue = (inverted - faceTwo) % 4;

            // mod in c# is funky and returns negative when working with neg nums
            return tileRotationValue < 0 ? tileRotationValue + 4 : tileRotationValue;
        }

        private int FindFaceValueFromTileNeighborValue(string tile)
        {
            return Int32.Parse(tile.Split()[1]);
        }

        private Point NextTileToCollapse()
        {
            return NextLowestEntropyTile();
        }

        private Point NextRandomNonCollapsedTile()
        {
            int randomIndex = this.rand.Next((this.rows * this.columns) - this.collapsedTileCount);
            int i = 0;

            foreach (Tile collapsed in this.tilesSelected)
            {
                if (collapsed == null)
                {
                    if (randomIndex == 0)
                    {
                        break;
                    }

                    randomIndex--;
                }

                i++;
            }

            int row = i / this.columns;
            int column = i % this.columns;

            return new Point(row, column);
        }

        private Point NextLowestEntropyTile()
        {
            if (this.nextUpQueue.Count <= 0)
            {
                Point nextPoint = NextRandomNonCollapsedTile();
                this.nextUpQueue.Enqueue(nextPoint);
                this.seenOnQueue.Add(nextPoint);
            }

            Point nextUp = this.nextUpQueue.Peek();

            while (this.nextUpQueue.Count > 0)
            {
                nextUp = this.nextUpQueue.Dequeue();
                HashSet<Point> adjNodes = GetAdjacentNodes(nextUp);

                // Filter out nodes and add them to seen set and to check queue
                adjNodes = new HashSet<Point>(adjNodes.Where(node => !this.seenOnQueue.Contains(node)));
                foreach (Point node in adjNodes)
                {
                    this.seenOnQueue.Add(node);
                    this.nextUpQueue.Enqueue(node);
                }

                // If null is true then we can select as next node to collapse
                if (this.tilesSelected[nextUp.One, nextUp.Two] == null)
                {
                    break;
                }
            }

            return nextUp;
        }

        private void ValidateTileSuperposition(Point position)
        {
            if (this.tileSuperpositions[position].Count <= 0)
            {
                Console.WriteLine($"No tiles left in position {position}, this is rare. RESTARTING!");
                InitializeWave();
            }
        }
    }
}
