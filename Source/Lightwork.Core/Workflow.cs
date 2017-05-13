using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lightwork.Core
{
    public abstract class Workflow
    {
        protected Workflow()
        {
        }

        protected Workflow(params object[] parameters)
        {
            Parameters = parameters;
        }

        internal event WorkflowCompleteHandler Complete;

        internal event WorkflowExceptionHandler Exception;

        public ICollection<object> Parameters { get; set; }
        
        protected WorkflowInstance WorkflowInstance { get; set; }

        public static Workflow Create(Func<WorkflowInstance, Task> activity)
        {
            return new SequentialWorkflow(activity);
        }

        public static Workflow Create(Func<Task> activity)
        {
            return new SequentialWorkflow(instance => activity());
        }

        public static Workflow Create(Action<WorkflowInstance> activity)
        {
#pragma warning disable 1998
            return new SequentialWorkflow(async instance => activity(instance));
#pragma warning restore 1998
        }

        public static Workflow Create(Action activity)
        {
#pragma warning disable 1998
            return new SequentialWorkflow(async instance => activity());
#pragma warning restore 1998
        }

        internal virtual async Task OnBeginExecute(WorkflowInstance instance, Action beginAction = null)
        {
            SetWorkflowInstance(instance);
            beginAction?.Invoke();
            ConfigureGlobalState(instance.GlobalState);
            WorkflowInstance.AllowActions();

            var executedWithoutError = false;
            try
            {
                instance.CancellationToken.Register(
                    () =>
                    {
                        instance.IsCancelled = true;
                        OnCancelling();
                    });
                instance.AllowExecute();
                await Execute(instance);
                executedWithoutError = true;
            }
            catch (Exception ex)
            {
                OnException(ex);
            }

            if (executedWithoutError)
            {
                OnComplete();
            }
        }

        internal virtual async Task BeginExecute(WorkflowInstance instance)
        {
            await OnBeginExecute(instance);
        }

        protected internal virtual void OnStateChange(object oldState, object newState)
        {
        }

        protected internal virtual bool OnCancel()
        {
            return true;
        }

        protected internal virtual void OnCancelling()
        {
        }
        
        protected virtual void SetWorkflowInstance(WorkflowInstance instance)
        {
            WorkflowInstance = instance;
        }

        protected virtual void ConfigureGlobalState(WorkflowState<GlobalWorkflowStates> globalState)
        {
        }
        
        protected abstract Task Execute(WorkflowInstance instance);
        
        protected virtual void OnComplete()
        {
            if (Complete == null)
            {
                return;
            }

            var e = new WorkflowCompleteEventArgs
            {
                WorkflowType = GetType()
            };

            Complete(this, e);
        }

        protected virtual void OnException(Exception exeption)
        {
            if (Exception == null)
            {
                return;
            }

            var e = new WorkflowExceptionEventArgs
            {
                WorkflowType = GetType(),
                Exception = exeption
            };

            Exception(this, e);
        }
    }

    public abstract class Workflow<TState> : Workflow
    {
        protected readonly IDictionary<TState, WorkflowState<TState>> States =
            new Dictionary<TState, WorkflowState<TState>>();

        protected Workflow()
        {
        }

        protected Workflow(params object[] parameters)
            : base(parameters)
        {
        }

        internal TState InitialState { get; set; }

        protected new WorkflowInstance<TState> WorkflowInstance
        {
            get { return (WorkflowInstance<TState>)base.WorkflowInstance; }
            set { base.WorkflowInstance = value; }
        }

        internal WorkflowState<TState> GetState(TState state, WorkflowState<TState> workflowState = null)
        {
            WorkflowState<TState> result;
            if (States.TryGetValue(state, out result))
            {
                return result;
            }

            result = workflowState ?? new WorkflowState<TState>(state, WorkflowInstance);
            result.ConfigureActions();
            States.Add(state, result);
            return result;
        }

        internal override async Task BeginExecute(WorkflowInstance instance)
        {
            await base.OnBeginExecute(instance,
                () =>
                {
                    ConfigureStates();
                    WorkflowInstance.SetState(InitialState);
                });
        }

        protected internal virtual void OnStateChange(TState oldState, TState newState)
        {
            base.OnStateChange(oldState, newState);
        }

        protected abstract void ConfigureStates();

        protected override void SetWorkflowInstance(WorkflowInstance instance)
        {
            base.SetWorkflowInstance(instance);
        }

        protected sealed override async Task Execute(WorkflowInstance instance)
        {
            await Execute((WorkflowInstance<TState>)instance);
        }

        protected abstract Task Execute(WorkflowInstance<TState> instance);
        
        protected void SetInitialState(TState state)
        {
            InitialState = state;
        }

        protected WorkflowState<TState> OnState(TState state)
        {
            return GetState(state);
        }

        protected WorkflowState<TState> OnState(TState state, WorkflowState<TState> workflowState)
        {
            workflowState.WorkflowInstance = WorkflowInstance;
            return GetState(state, workflowState);
        }

        protected WorkflowState<TState> OnState<TWorkflowState>(TState state)
            where TWorkflowState : WorkflowState<TState>, new()
        {
            var workflowState = new TWorkflowState { State = state };
            return OnState(state, workflowState);
        }
    }
}