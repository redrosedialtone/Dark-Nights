using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkNights;

namespace DarkNights.WorldGeneration
{
    public interface IBiome
    {
        float MatchConditions(BiomeConditions conditions);
        void Generate(Chunk cell);
    }

    public struct BiomeConditions
    {
        public float FerilityGraph;
        public float RainfallGraph;
        public float ElevationGraph;
    }
    public class TemperateGrasslands : IBiome
    {
        private float Fertility = 0.2f;

        public void Generate(Chunk cell)
        {
            WorldSystem.log.Debug($"{cell} I am green and grassy");
            return;
        }

        public float MatchConditions(BiomeConditions conditions)
        {
            return Math.Abs(Fertility - conditions.FerilityGraph);
        }
    }

    public class TemperateWoods : IBiome
    {
        private float Fertility;
        private float TreeChance;
        public TemperateWoods(float TreeRate = 0.05f, float Fertility = 0.5f)
        {
            this.TreeChance = TreeRate;
            this.Fertility = Fertility;
        }
        public void Generate(Chunk cell)
        {
            WorldSystem.log.Debug($"{cell} I am green and woody");
            var c = cell.Tiles();
            Random rand = new Random(World.SEED);
            foreach (var tile in c)
            {
                if (rand.NextDouble() < TreeChance)
                {
                    WorldSystem.Get.World.AddTree(tile);
                }
            }
        }

        public float MatchConditions(BiomeConditions conditions)
        {
            return Math.Abs(Fertility - conditions.FerilityGraph);
        }
    }
}
