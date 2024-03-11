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
        private float ShrubChance = 0.05f;
        private float SaplingChance = 0.04f;
        public TemperateWoods(float TreeRate = 0.03f, float Fertility = 0.5f)
        {
            this.TreeChance = TreeRate;
            this.Fertility = Fertility;
        }
        public void Generate(Chunk cell)
        {
            WorldSystem.log.Debug($"{cell} I am green and woody");
            Random r = new Random(World.SEED);

            IEntity[] trees = new IEntity[50];
            int tIndx = 0;
            foreach (var tile in cell.Tiles())
            {
                var chance = r.NextDouble();
                if (chance <= TreeChance)
                {
                    trees[tIndx++] = new Tree(tile);

                }
                else if (chance <= SaplingChance)
                {
                    trees[tIndx++] = new Sapling(tile);
                }
                else if (chance <= ShrubChance)
                {
                    if(chance >= 0.015) trees[tIndx++] = new Shrub1(tile);
                    else trees[tIndx++] = new Shrub2(tile);

                }
                if (tIndx >= 50) break;
            }

            EntityController.Get.PlaceEntitiesInWorld(trees);
        }

        public float MatchConditions(BiomeConditions conditions)
        {
            return Math.Abs(Fertility - conditions.FerilityGraph);
        }
    }
}
