using DarkNights.Tasks;
using Nebula.Main;
using Nebula.Systems;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class TaskSystem : Manager
    {
        #region Static
        private static TaskSystem instance;
        public static TaskSystem Get => instance;
        public static readonly NLog.Logger log = NLog.LogManager.GetLogger("TASKS");
        #endregion

        public Queue<IWorkOrder> TaskQueue = new Queue<IWorkOrder>();
        private List<IWorker> workers = new List<IWorker>();

        public override void Init()
        {
            log.Info("> ...<");
            instance = this;
            ApplicationController.Get.Initiate(this);
        }

        public static void Delegate(IWorkOrder Order)
        {
            instance.TaskDelegated(Order);
        }

        private void TaskDelegated(IWorkOrder Order)
        {
            log.Debug($"Delegating New {Order}..");
            foreach (var worker in workers)
            {
                if (worker.Available)
                {
                    AssignTaskTo(Order, worker, TaskAssignmentMethod.DEFAULT);
                    return;
                }
            }
            TaskQueue.Enqueue(Order);
        }

        public static void Assign(IWorkOrder Task, IWorker Worker, TaskAssignmentMethod AssignmentMode = TaskAssignmentMethod.DEFAULT)
        {
            instance.AssignTaskTo(Task, Worker, AssignmentMode);
        }

        private void AssignTaskTo(IWorkOrder Task, IWorker Worker, TaskAssignmentMethod AssignmentMode)
        {
            log.Debug($"Assigning {Task} to {Worker.Name}..");
            Worker.AssignTask(Task, AssignmentMode);
        }

        public static void Worker(IWorker Worker)
        {
            instance.NewWorker(Worker);
        }

        private void NewWorker(IWorker Worker)
        {
            log.Debug($"Available Worker {Worker.Name} Added!");
            workers.Add(Worker);
        }
    }
}
