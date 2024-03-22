using System;
using System.Collections.Generic;

namespace DarkNights.WorldGeneration
{
    public class NoiseMapping
    {
        public Dictionary<int, float> PerlinMap(int seed, IEnumerable<Chunk> cells, float resolution)
        {
            FastNoiseLite noise = new FastNoiseLite(seed);
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            var noiseMap = new Dictionary<int, float>();
            foreach (var cell in cells)
            {
                var fert = noise.GetNoise(cell.ChunkCoordinates.X * resolution, cell.ChunkCoordinates.Y * resolution);
                noiseMap[cell.GetHashCode()] = fert;
            }
            return noiseMap;
        }
        public Dictionary<int, float> PerlinMap(int seed, Chunk cell, float resolution)
        {
            FastNoiseLite noise = new FastNoiseLite(seed);
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            noise.SetFrequency(0.1f);

            var noiseMap = new Dictionary<int, float>();
            foreach (var tile in cell.Tiles())
            {
                var data = noise.GetNoise(tile.X, tile.Y);
                noiseMap[tile.GetHashCode()] = data;
            }
            return noiseMap;
        }
    }
}
