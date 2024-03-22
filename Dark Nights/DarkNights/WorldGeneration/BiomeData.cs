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
        private NoiseMapping MapMaker = new NoiseMapping();
        private float Fertility;
        private int TreeChance = 3;
        private int ShrubChance = 1;
        private int SaplingChance = 1;
        private int EmptyChance = 10;
        private int TotalChance;
        enum BiomeObjects
        {
            empty = 0,
            tree = 1,
            shrub = 2,
            sapling = 3
        }
        private BiomeObjects[] RandomBag;
        public TemperateWoods(float Fertility = 0.5f)
        {
            List<BiomeObjects> tmpBag = new List<BiomeObjects>();
            for (int i = 0; i < TreeChance; i++)
            {
                tmpBag.Add(BiomeObjects.tree);
            }
            for (int i = 0;i < ShrubChance; i++)
            {
                tmpBag.Add(BiomeObjects.shrub);
            }
            for (int i = 0; i <= SaplingChance; i++)
            {
                tmpBag.Add(BiomeObjects.sapling);
            }
            for(int i = 0; i < EmptyChance; i++)
            {
                tmpBag.Add(BiomeObjects.empty);
            }
            RandomBag = tmpBag.ToArray();

            this.Fertility = Fertility;
        }
        public void Generate(Chunk cell)
        {
            var Distribution = MapMaker.PerlinMap(World.SEED, cell, World.CHUNK_SIZE);
            WorldSystem.log.Debug($"{cell} I am green and woody");
            var chunkTileCount = Defs.ChunkSize * Defs.ChunkSize;
            IEntity[] Entities = new IEntity[chunkTileCount];
            int tIndx = 0;
            foreach (var tile in cell.Tiles())
            {
                var weight = (Distribution.GetValueOrDefault(tile.GetHashCode()) + 1) * .5f;
                IEntity terrain = SelectTerrain(tile, weight);
                 
                if(terrain != null) Entities[tIndx++] = terrain;
                if (tIndx >= chunkTileCount) break;
            }

            EntityController.Get.PlaceEntitiesInWorld(Entities);
        }

        private IEntity SelectTerrain(Coordinates tile, float weight)
        {
            if (weight >= 0.4f)
            {
                var val = RandomBag[WorldSystem.Get.World.RandomInteger(0, RandomBag.Length)];
                switch (val)
                {
                    case (BiomeObjects.tree):
                        {
                            return new Tree(tile);
                        }
                    case (BiomeObjects.shrub):
                        {
                            return new Shrub1(tile);
                        }
                    case (BiomeObjects.sapling): 
                        {
                            return new Sapling(tile);
                        }
                    case (BiomeObjects.empty):
                        {
                            return null;
                        }
                }
            }
            return null;
        }

        public float MatchConditions(BiomeConditions conditions)
        {
            return Math.Abs(Fertility - conditions.FerilityGraph);
        }
    }
}
