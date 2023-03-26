using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using WaveFunctionCollapse;
using SixLabors.ImageSharp.Processing;
using Point = SixLabors.ImageSharp.Point;

class Program
{
    private static JsonArray tileNames;

    public static void Main(string[] args)
    {
        // Set working directory to project src
        Directory.SetCurrentDirectory(@"../../../src");
        string fileName = "Tilesets/Knots.json";
        string knotsJson = File.ReadAllText(fileName);

        // Read and parse json
        JsonObject json = JsonNode.Parse(knotsJson).AsObject();
        tileNames = json["tiles"].AsArray();
        JsonArray neighbors = json["neighbors"].AsArray();
        TileData tileData = new TileData();

        foreach (var tile in tileNames)
        {
            tileData.AddTile(tile["name"].ToString(), tile["symmetry"].ToString());
        }

        foreach (var neighbor in neighbors)
        {
            tileData.AddNeighbor(neighbor["left"].ToString(), neighbor["right"].ToString());
        }

        TiledWaveFunctionCollapse waveFunctionCollapse = new TiledWaveFunctionCollapse(Utils.n, Utils.m, tileData, Mode.LOWEST_ENTROPY);
        Tile[,] collapsedTiles = waveFunctionCollapse.CollapseWave();
        PrintTiles(collapsedTiles);

        //int size = Utils.n * Utils.m;
        //for (int i = 0; i < size; i++)
        //{
        //    Tile[,] collapsedTiles = waveFunctionCollapse.SingleCollapseWave();
        //    PrintTiles(collapsedTiles);
        //}
    }

    private static void PrintTiles(Tile[,] tiles)
    {
        Dictionary<string, Image<Rgba32>> tileToBitmap = LoadTileBitmaps();

        int rows = tiles.GetLength(0);
        int columns = tiles.GetLength(1);

        int tileSize = tileToBitmap.First().Value.Width;
        using Image<Rgba32> mergedImage = new Image<Rgba32>(tileSize * columns, tileSize * rows);

        var emptyImage = tileToBitmap["empty"];
        mergedImage.Mutate(x => x.DrawImage(emptyImage, new Point(0, tileSize * (rows - 1)), 1f));
        mergedImage.Mutate(x => x.DrawImage(emptyImage, new Point(tileSize * (columns - 1), 0), 1f));

        // 0, 0 is going to be 0, row-1
        // top right will be columns-1, 0
        // The tile set was created bottom up
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (tiles[i, j] == null)
                {
                    continue;
                }

                string tileName = tiles[i, j].Name;
                int rotation = (int)tiles[i, j].Direction;

                var tileImage = tileToBitmap[tileName];

                // Rotate if needed
                if (rotation > 0)
                {
                    // Rotate the image by 90 degrees clockwise
                    tileImage.Mutate(x => x.Rotate(rotation * 90));
                }

                int yPos = tileSize * (rows - i - 1);
                // Place on merged image
                mergedImage.Mutate(x => x.DrawImage(tileImage, new Point(tileSize * j, yPos), 1f));
                tileImage.Mutate(x => x.Rotate(rotation * -90));
            }
        }

        // Save created image
        string fileLocationPrefix = "Tilesets/Knots/";
        mergedImage.Save(fileLocationPrefix + "merged.png");
    }

    private static Dictionary<string, Image<Rgba32>> LoadTileBitmaps()
    {
        Dictionary<string, Image<Rgba32>> tileBitmaps = new Dictionary<string, Image<Rgba32>>();

        string fileLocationPrefix = "Tilesets/Knots/";
        foreach (var tile in tileNames)
        {
            string pngFileName = $"{tile["name"]}.png";

            if (!tileBitmaps.ContainsKey(tile["name"].ToString()))
            {
                var tileBitmap = Image.Load<Rgba32>(fileLocationPrefix + pngFileName);
                tileBitmaps.Add(tile["name"].ToString(), tileBitmap);
            }
        }

        return tileBitmaps;
    }
}
