using Microsoft.Xna.Framework.Input;
using Priority_Queue;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights.WorldGeneration
{
    public class BiomeGenerator
    {
        public int Seed { get; }
        public float Resolution { get; }
        IBiome[] activeBiomes;
        public Dictionary<int, float> MyFertilityMap { get; private set; }

        public BiomeGenerator(IBiome[] activeBiomes, int Seed, float Resolution)
        {
            this.activeBiomes = activeBiomes;
            this.Seed = Seed;
            this.Resolution = Resolution;
        }
        public void Generate(IEnumerable<Chunk> cells)
        {
            MyFertilityMap = FertilityMap(cells);
            var biomeMap = new Dictionary<int, IBiome>();
            var conditionMap = new Dictionary<int, BiomeConditions>();


            foreach (var cell in cells)
            {
                SimplePriorityQueue<IBiome> weightedList = new SimplePriorityQueue<IBiome>();
                BiomeConditions conditions = new BiomeConditions()
                {
                    FerilityGraph = MyFertilityMap[cell.GetHashCode()]
                };
                conditionMap.Add(cell.GetHashCode(), conditions);
                foreach (IBiome biome in activeBiomes)
                {
                    weightedList.Enqueue(biome, biome.MatchConditions(conditions));
                }
                biomeMap.Add(cell.GetHashCode(), weightedList.First());
            }
            
            foreach (var kv in biomeMap)
            {
                kv.Value.Generate(WorldSystem.Get.World.ChunkUnsf(kv.Key));
            }
        }

        public Dictionary<int,float> FertilityMap(IEnumerable<Chunk> cells)
        {
            FastNoiseLite noise = new FastNoiseLite(Seed);
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

            var noiseMap = new Dictionary<int, float>();
            foreach (var cell in cells)
            {
                var fert = noise.GetNoise(cell.ChunkCoordinates.X * Resolution, cell.ChunkCoordinates.Y * Resolution);
                noiseMap[cell.GetHashCode()] = fert;
            }
            return noiseMap;
        }


    }
}
