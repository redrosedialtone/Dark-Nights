using DarkNights.Tasks;
using Microsoft.Xna.Framework;
using Nebula;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{


    public class Character : IEntity, IWorker, ISelectable
    {
        public string Name { get; private set; }
        public Coordinates Coordinates => Movement.Coordinates;
        public Vector2 Position { get { return Movement.Position; } set { Movement.SetPosition(value); } }
        public float Rotation { get { return Movement.Rotation; } set { Movement.SetRotation(value); } }
        public Sprite2D Sprite { get; private set; }
        public (Coordinates min, Coordinates max) Bounds => 
            (this.Coordinates, new Coordinates(Coordinates.X + Movement.Clearance - 1, Coordinates.Y + Movement.Clearance - 1));

        public EntityInventory Inventory { get; private set; }
        public EntityMovement Movement { get; private set; }

        public IWorkOrder CurrentOrder { get; private set; }
        public LinkedList<IWorkOrder> TaskQueue { get; private set; }
        public bool Available => true;

        public Character(string Name, Coordinates Coordinates)
        {
            Movement = new EntityMovement();
            Movement.SetPosition(Coordinates);
            Movement.Clearance = 2;

            Inventory = new EntityInventory(this);

            this.Name = Name;
            Sprite = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/Jerry"),
                new Rectangle(0, 0, 64, 64), new Vector2(32,32));

            TaskQueue = new LinkedList<IWorkOrder>();
        }

        public void Tick()
        {
            Movement.Tick(Time.DeltaTime);

            if (CurrentOrder == null)
            {
                if (TaskQueue.Count > 0)
                {
                    CurrentOrder = TaskQueue.First.Value;
                    TaskQueue.RemoveFirst();
                    CurrentOrder.Execute();
                }
                return;
            }
            else
                CurrentOrder.Tick();

            if ((CurrentOrder.Status & ITaskStatus.FINISHED) != 0)
            {
                CurrentOrder = null;
            }
            else if ((CurrentOrder.Status & ITaskStatus.PAUSED) != 0)
            {
                CurrentOrder = null;
            }


        }

        public void AssignTask(IWorkOrder Task, TaskAssignmentMethod AssignmentMode = TaskAssignmentMethod.DEFAULT)
        {
            switch (AssignmentMode)
            {
                case TaskAssignmentMethod.DEFAULT:
                    Task.Assign(this);
                    TaskQueue.AddLast(Task);
                    break;
                case TaskAssignmentMethod.ENQUEUE:
                    Task.Assign(this);
                    TaskQueue.AddLast(Task);
                    break;
                case TaskAssignmentMethod.INTERRUPT:
                    Task.Assign(this);
                    if (CurrentOrder != null)
                    {
                        TaskQueue.AddFirst(CurrentOrder);
                        CurrentOrder.Interrupt();
                    }
                    TaskQueue.AddFirst(Task);
                    break;
                case TaskAssignmentMethod.CLEAR:
                    TaskSystem.log.Debug("Clearing Work Queue..");
                    Task.Assign(this);
                    if (CurrentOrder != null)
                        CurrentOrder.Stop();
                    TaskQueue.Clear();
                    TaskQueue.AddFirst(Task);
                    break;
                default:
                    break;
            }
        }
    }
}
