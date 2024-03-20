using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights.Tasks
{
    [Flags]
    public enum ITaskStatus
    {
        Nil = 0,
        Cancelled = 1,
        Pending = 2,
        Running = 4,
        Interrupted = 8,
        Success = 16,
        Failure = 32,
        Completed = 64,

        FINISHED = Success | Failure | Cancelled | Completed,
        PAUSED = Pending | Interrupted
    }

    public enum ITaskTransition
    {
        Nil = 0,
        Stopping = 1,
        Interrupting = 2,
        Finishing = 3
    }

    public enum TaskAssignmentMethod
    {
        DEFAULT = 0,
        ENQUEUE = 1,
        INTERRUPT = 2,
        CLEAR = 3,
    }

    public interface IWorker
    {
        public string Name { get; }
        IWorkOrder CurrentOrder { get; }
        LinkedList<IWorkOrder> TaskQueue { get; }
        EntityMovement Movement { get; }
        //ICreatureInventory Inventory { get; }
        bool Available { get; }

        void AssignTask(IWorkOrder Task, TaskAssignmentMethod AssignmentMode = TaskAssignmentMethod.DEFAULT);
    }

    public interface ITask
    {
        ITaskStatus Status { get; }
        ITaskTransition TransitionState { get; }

        void Tick();

        void SetStatus(ITaskStatus taskState);
        void Transition(ITaskTransition transitionState);
        void Stop();
        void Execute();
        void Complete();
        void Interrupt();
    }

    public interface IWorkOrder : ITask
    {
        IWorker Worker { get; }

        ITaskState CurrentTask { get; }
        ITaskState[] TaskList { get; }

        void Assign(IWorker Worker);
    }

    public interface ITaskState : ITask
    {
        IWorkOrder WorkOrder { get; }

        void Assign(IWorkOrder Order);
    }

    public abstract class TaskState : ITaskState
    {
        public IWorkOrder WorkOrder { get; private set; }
        public ITaskStatus Status { get; private set; }
        public ITaskTransition TransitionState { get; protected set; }

        protected bool Running => (Status & ITaskStatus.Running) != 0;
        protected IWorker Worker => WorkOrder.Worker;

        protected ITaskStatus FinishState;

        public abstract void Execute();


        public virtual void Assign(IWorkOrder Order)
        {
            this.WorkOrder = Order;
        }

        public virtual void SetStatus(ITaskStatus State)
        {
            this.Status = State;
        }

        public virtual void Tick()
        {
            UpdateState();
        }

        protected virtual void UpdateState()
        {
            if (TransitionState != ITaskTransition.Nil)
            {
                switch (TransitionState)
                {
                    case ITaskTransition.Stopping:
                        SetStatus(ITaskStatus.Cancelled);
                        break;
                    case ITaskTransition.Interrupting:
                        SetStatus(ITaskStatus.Interrupted);
                        break;
                    case ITaskTransition.Finishing:
                        SetStatus(FinishState);
                        break;
                    default:
                        break;
                }
            }
        }

        public virtual void Stop()
        {
            Transition(ITaskTransition.Stopping);
        }

        public virtual void Complete()
        {
            FinishState = ITaskStatus.Success;
            Transition(ITaskTransition.Finishing);
        }

        public virtual void Interrupt()
        {
            Transition(ITaskTransition.Interrupting);
        }

        public virtual void Transition(ITaskTransition transitionState)
        {
            this.TransitionState = transitionState;
        }
    }

    public abstract class WorkOrder : IWorkOrder
    {
        public ITaskStatus Status { get; protected set; }
        public ITaskTransition TransitionState { get; protected set; }
        public IWorker Worker { get; protected set; }
        public ITaskState CurrentTask { get; protected set; }
        public ITaskState[] TaskList { get; protected set; }

        protected int TaskIndex = 0;
        protected bool Running => (Status & ITaskStatus.Running) != 0;

        public void Assign(IWorker Worker)
        {
            this.Worker = Worker;
        }

        public virtual void SetStatus(ITaskStatus taskState)
        {
            this.Status = taskState;
            if (CurrentTask != null)
            {
                CurrentTask.SetStatus(taskState);
            }
        }

        public virtual void Transition(ITaskTransition transitionState)
        {
            this.TransitionState = transitionState;
            if (CurrentTask != null)
            {
                CurrentTask.Transition(transitionState);
            }
        }

        public virtual void Stop() 
        { 
            Transition(ITaskTransition.Stopping);
        }
        public virtual void Execute() 
        { 
            SetStatus(ITaskStatus.Running);
        }
        public virtual void Complete()
        { 
            Transition(ITaskTransition.Finishing);
        }
        public virtual void Interrupt()
        {
            Transition(ITaskTransition.Interrupting);
        }

        public void Tick()
        {
            UpdateState();
            if (Running)
            {
                bool Success = false;
                if (CurrentTask != null)
                {
                    CurrentTask.Tick();
                    Success = (CurrentTask.Status & ITaskStatus.FINISHED) != 0;
                }
                if (CurrentTask == null || Success)
                {
                    // Is there another task?
                    TaskSystem.log.Trace("Checking next task..");
                    if (Next())
                    {
                        // If so, run.
                        CurrentTask.Execute();
                    }
                    else
                    {
                        // Otherwise we're done.
                        TaskSystem.log.Trace("No Tasks; Finished!");
                        Complete();
                    }
                }
            }
        }

        protected void UpdateState()
        {
            if (TransitionState != ITaskTransition.Nil)
            {
                switch (TransitionState)
                {
                    case ITaskTransition.Stopping:
                        if ((CurrentTask.Status & ITaskStatus.FINISHED) != 0)
                        {
                            TaskSystem.log.Debug($"{CurrentTask} Cancelled:: {CurrentTask.Status}");
                            SetStatus(ITaskStatus.Cancelled);
                        }
                        break;
                    case ITaskTransition.Interrupting:
                        if ((CurrentTask.Status & ITaskStatus.PAUSED) != 0)
                        {
                            TaskSystem.log.Debug($"{CurrentTask} Interrupted:: {CurrentTask.Status}");
                            SetStatus(ITaskStatus.Interrupted);
                        }
                        break;
                    case ITaskTransition.Finishing:
                        if ((CurrentTask.Status & ITaskStatus.FINISHED) != 0)
                        {
                            TaskSystem.log.Debug($"{CurrentTask} Finished:: {CurrentTask.Status}");
                            SetStatus(ITaskStatus.Completed);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        protected bool Next()
        {

            if (TaskList != null && TaskIndex < TaskList.Length)
            {
                if (CurrentTask != null)
                {
                    CurrentTask.Complete();
                }

                CurrentTask = TaskList[TaskIndex];
                CurrentTask.Assign(this);
                TaskSystem.log.Debug($"Assigning new task:: {CurrentTask.ToString()}");
                TaskIndex++;
                return true;
            }
            return false;
        }
    }

    public class MoveToWaypointTask : WorkOrder
    {
        public Coordinates Destination { get; private set; }

        public MoveToWaypointTask(Coordinates Destination)
        {
            this.Destination = Destination;
            TaskList = new ITaskState[]
            {
                new MoveTo(Destination)
            };
        }
    }

    public class PickUpItemTask : WorkOrder
    {
        public Coordinates Destination { get; private set; }
        public IItem Item { get; private set; }
        public EntityInventory Inventory { get; set; }

        public PickUpItemTask(Coordinates Destination, IItem item, EntityInventory inventory)
        {
            this.Destination = Destination;
            Item = item;
            Inventory = inventory;

            TaskList = new ITaskState[]
            {
                new MoveTo(Destination),
                new PickUp(Inventory, item)
            };
        }
    }

    public class MoveTo : TaskState
    {
        private readonly Coordinates Destination;

        public MoveTo(Coordinates Destination)
        {
            this.Destination = Destination;
            SetStatus(ITaskStatus.Pending);
        }

        public override void Execute()
        {
            Worker.Movement.MoveTo(Destination);
            SetStatus(ITaskStatus.Running);
        }

        public override void Tick()
        {
            UpdateState();
            if (Running)
            {
                if (WorkOrder.Worker.Movement.Coordinates == Destination)
                {
                    Transition(ITaskTransition.Finishing);
                }
            }
        }

        protected override void UpdateState()
        {
            if (TransitionState != ITaskTransition.Nil)
            {
                switch (TransitionState)
                {
                    case ITaskTransition.Stopping:
                        FinishState = ITaskStatus.Cancelled;
                        Cancel();
                        break;
                    case ITaskTransition.Interrupting:
                        FinishState = ITaskStatus.Interrupted;
                        Cancel();
                        break;
                    case ITaskTransition.Finishing:
                        SetStatus(ITaskStatus.Success);
                        break;
                    default:
                        break;
                }
            }
        }

        private void Cancel()
        {
            Worker.Movement.Stop(FinaliseMovement);
        }

        private void FinaliseMovement()
        {
            SetStatus(FinishState);
        }
    }

    public class PickUp : TaskState
    {
        private readonly IItem Item;
        private readonly EntityInventory Inventory;

        public PickUp(EntityInventory Inventory, IItem item)
        {
            this.Item = item;
            this.Inventory = Inventory;
            SetStatus(ITaskStatus.Pending);
        }

        public override void Execute()
        {
            if (Inventory.AttemptItemPickup(Item))
            {
                SetStatus(ITaskStatus.Success);
            }
            else
            {
                SetStatus(ITaskStatus.Failure);
            }
        }
    }
}
