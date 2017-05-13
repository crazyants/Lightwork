using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace D3.Lightwork.Core
{
    public class WorkflowEngine : IDisposable
    {
        public const int DefaultPollingInternal = 50;

        public readonly Argument<bool> IsRunning = new Argument<bool>("IsRunning");

        private readonly ConcurrentDictionary<Guid, WorkflowInstance> _loadedWorfklows =
            new ConcurrentDictionary<Guid, WorkflowInstance>();

        public WorkflowEngine()
        {
            IsRunning.Value = true;
        }

        public static WorkflowEngine Current { get; private set; }

        public bool RemoveCompletedWorkflowsImmediately { get; set; }

        public static TWorkflow CreateWorkflowType<TWorkflow>(IEnumerable<object> parameters = null)
            where TWorkflow : Workflow
        {
            return (TWorkflow)CreateWorkflowType(typeof(TWorkflow), parameters);
        }

        public static Workflow CreateWorkflowType(Type workflowType, IEnumerable<object> parameters = null)
        {
            var workflow = parameters == null
                ? (Workflow)Activator.CreateInstance(workflowType)
                : (Workflow)Activator.CreateInstance(workflowType, parameters);

            if (parameters != null)
            {
                workflow.Parameters = parameters.ToList();
            }

            return workflow;
        }

        public static Workflow CreateWorkflowType(string workflowType, IEnumerable<object> parameters = null)
        {
            var type = Type.GetType(workflowType);
            return CreateWorkflowType(type, parameters);
        }

        public static WorkflowEngine InitializeEngine(WorkflowEngine engine = null)
        {
            return Current = engine ?? new WorkflowEngine();
        }

        public virtual WorkflowInstance CreateWorkflow(
            Workflow workflow,
            Guid id = default(Guid),
            Guid parentId = default(Guid))
        {
            if (id == default(Guid))
            {
                id = Guid.NewGuid();
            }

            var instance = GetWorkflow(id);
            if (instance != null)
            {
                return instance;
            }

            instance = CreateWorkflowInstance(workflow, id, parentId);
            AddWorkflow(id, instance);

            return instance;
        }

        public virtual WorkflowInstance CreateWorkflow(
            Type workflowType,
            Guid id = default(Guid),
            Guid parentId = default(Guid))
        {
            var workflow = CreateWorkflowType(workflowType);
            return CreateWorkflow(workflow, id, parentId);
        }

        public virtual WorkflowInstance CreateWorkflow(
            string workflowType,
            Guid id = default(Guid),
            Guid parentId = default(Guid))
        {
            var workflow = CreateWorkflowType(workflowType);
            return CreateWorkflow(workflow, id, parentId);
        }

        public virtual WorkflowInstance CreateWorkflow<TWorkflow>(
            Guid id = default(Guid),
            Guid parentId = default(Guid)) where TWorkflow : Workflow
        {
            var workflow = CreateWorkflowType<TWorkflow>();
            return CreateWorkflow(workflow, id, parentId);
        }

        public virtual WorkflowInstance<TState> CreateWorkflow<TWorkflow, TState>(
            Guid id = default(Guid),
            Guid parentId = default(Guid)) where TWorkflow : Workflow
        {
            if (id == default(Guid))
            {
                id = Guid.NewGuid();
            }

            var instance = GetWorkflow<TState>(id);
            if (instance != null)
            {
                return instance;
            }

            var workflow = CreateWorkflowType<TWorkflow>();
            instance = new WorkflowInstance<TState>(this, workflow, id);
            AddWorkflow(id, instance);

            return instance;
        }

        public virtual WorkflowInstance GetWorkflow(Guid id)
        {
            if (!_loadedWorfklows.ContainsKey(id))
            {
                return null;
            }

            WorkflowInstance instance;
            _loadedWorfklows.TryGetValue(id, out instance);
            return instance;
        }

        public virtual WorkflowInstance<TState> GetWorkflow<TState>(Guid id)
        {
            return (WorkflowInstance<TState>)GetWorkflow(id);
        }

        public virtual IEnumerable<WorkflowInstance> FindWorkflows(Func<WorkflowInstance, bool> expression)
        {
            return _loadedWorfklows.Where(w => expression(w.Value)).Select(workflow => workflow.Value).ToList();
        }

        public virtual bool RemoveWorkflow(Guid id)
        {
            WorkflowInstance instance;
            return _loadedWorfklows.TryRemove(id, out instance);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected internal virtual Task StartWorkflowTask(
            WorkflowInstance instance,
            CancellationToken cancellationToken)
        {
            return Task.Run(async () => await instance.BeginExecute(), cancellationToken);
        }

        protected virtual WorkflowInstance CreateWorkflowInstance(Workflow workflow, Guid id, Guid parentId)
        {
            WorkflowInstance instance = null;
            var genericType = workflow.GetType();
            while ((genericType = genericType.BaseType) != null)
            {
                if (!genericType.GenericTypeArguments.Any())
                {
                    continue;
                }

                var workflowInstanceType =
                    typeof(WorkflowInstance<>).MakeGenericType(genericType.GenericTypeArguments.First());
                instance =
                    (WorkflowInstance)
                        Activator.CreateInstance(
                            workflowInstanceType,
                            BindingFlags.NonPublic | BindingFlags.Instance,
                            null,
                            new object[] { this, workflow, id, parentId },
                            null);
                break;
            }

            return instance ?? new WorkflowInstance(this, workflow, id, parentId);
        }

        protected virtual void AddWorkflow(Guid id, WorkflowInstance instance)
        {
            instance.WorkflowComplete += OnWorkflowComplete;
            instance.WorkflowException += OnWorkflowException;
            _loadedWorfklows.TryAdd(id, instance);
        }

        protected virtual void OnWorkflowComplete(object sender, WorkflowCompleteEventArgs e)
        {
            if (!RemoveCompletedWorkflowsImmediately)
            {
                return;
            }

            RemoveWorkflow(e.WorkflowId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var instance in _loadedWorfklows)
                {
                    instance.Value.Dispose();
                }
            }
        }

        protected virtual void OnWorkflowException(object sender, WorkflowExceptionEventArgs e)
        {
        }
    }
}