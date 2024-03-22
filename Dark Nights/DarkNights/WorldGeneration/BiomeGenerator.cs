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
        private NoiseMapping MapMaker = new NoiseMapping();

        public int Seed => World.SEED;
        public float Resolution { get; }
        IBiome[] activeBiomes;
        public Dictionary<int, float> MyFertilityMap { get; private set; }

        public BiomeGenerator(IBiome[] activeBiomes, float Resolution)
        {
            this.activeBiomes = activeBiomes;
            this.Resolution = Resolution;
        }
        public void Generate(IEnumerable<Chunk> cells)
        {
            MyFertilityMap = MapMaker.PerlinMap(Seed, cells, Resolution);
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

    }
}
