using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights.WorldGeneration
{
    public interface IWorldEntity
    {
        INavNode Nodetype { get; }
        Coordinates Coordinates { get; }
        void OnDestroy();
        void OnCreate();
    }
    public class Tree : IWorldEntity
    {
        public INavNode Nodetype { get; private set; }
        public Coordinates Coordinates { get; private set; }

        public Tree(Coordinates coordinates)
        {
            Coordinates = coordinates;
            Nodetype = new ImpassableNode(Coordinates);
        }

        public void OnCreate()
        {
            throw new NotImplementedException();
        }

        public void OnDestroy()
        {
            throw new NotImplementedException();
        }
    }
}
