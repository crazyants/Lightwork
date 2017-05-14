using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lightwork.Core.Utilities;

namespace Lightwork.Core
{
    public class WorkflowInstance : IDisposable
    {
        internal readonly WorkflowState<GlobalWorkflowStates> GlobalState =
            new WorkflowState<GlobalWorkflowStates>(GlobalWorkflowStates.DefaultState);

        private readonly SemaphoreSlim _completeEvent = new SemaphoreSlim(0, 1);

        private readonly SemaphoreSlim _waitActionEvent = new SemaphoreSlim(0, 1);

        private readonly SemaphoreSlim _endWaitActionEvent = new SemaphoreSlim(0, 1);

        private readonly SemaphoreSlim _actioningEvent = new SemaphoreSlim(0, 1);

        private readonly ManualResetEvent _allowActionEvent = new ManualResetEvent(false);

        private readonly ManualResetEvent _beginExecuteEvent = new ManualResetEvent(false);

        private readonly object _waitEventLock = new object();

        private readonly object _waitActionEventLock = new object();

        private readonly object _startLock = new object();

        private bool _isRunning;

        internal WorkflowInstance(
            WorkflowEngine engine,
            Workflow workflow,
            Guid id,
            Guid parentId = default(Guid))
        {
            Arguments = new Collection<Argument>();
            ActionArguments = new Collection<Argument>();
            WorkflowEngine = engine;
            Workflow = workflow;
            Id = id;
            ParentId = parentId;
        }

        public event WorkflowCompleteHandler WorkflowComplete;

        public event WorkflowExceptionHandler WorkflowException;

        public ICollection<Argument> Arguments { get; set; }

        public ICollection<Argument> ActionArguments { get; set; }

        public WorkflowEngine WorkflowEngine { get; set; }

        public Type WorkflowType => Workflow.GetType();

        public Guid Id { get; set; }

        public Guid ParentId { get; set; }

        public bool IsRunning => _isRunning && WorkflowEngine.IsRunning.Value;

        public bool IsStarted { get; private set; }

        public bool IsComplete { get; private set; }

        public bool IsInError { get; private set; }

        public bool IsCancelled { get; internal set; }

        public bool IsInAction { get; protected set; }

        public bool IsExitState { get; protected set; }

        public string CurrentAction { get; private set; }

        public string CurrentActionTag { get; private set; }

        public CancellationToken CancellationToken { get; protected set; }

        public Task WorkflowTask { get; set; }

        protected bool IsAvailable
        {
            get { return !IsComplete && !IsExitState; }
        }

        protected Workflow Workflow { get; set; }

        protected int WaitEventCount { get; set; }

        protected int WaitActionEventCount { get; set; }

        protected CancellationTokenSource CancellationTokenSource { get; set; }

        public async Task Start(params Argument[] arguments)
        {
            await Start(false, null, arguments);
        }

        public async Task Start(object state, params Argument[] arguments)
        {
            await Start(false, state, arguments);
        }

        public async Task Start(bool synchronous, params Argument[] arguments)
        {
            await Start(synchronous, null, arguments);
        }

        public async Task Start(CancellationToken cancellationToken, params Argument[] arguments)
        {
            await Start(cancellationToken, false, null, arguments);
        }

        public async Task Start(CancellationToken cancellationToken, object state, params Argument[] arguments)
        {
            await Start(cancellationToken, false, state, arguments);
        }

        public async Task Start(CancellationToken cancellationToken, bool synchronous, params Argument[] arguments)
        {
            await Start(cancellationToken, synchronous, null, arguments);
        }

        public async Task Start(
            bool synchronous,
            object state,
            params Argument[] arguments)
        {
            await Start(CancellationToken.None, synchronous, state, arguments);
        }

        public async Task Start(
            CancellationToken cancellationToken,
            bool synchronous,
            object state,
            params Argument[] arguments)
        {
            lock (_waitEventLock)
            {
                if (cancellationToken == CancellationToken.None)
                {
                    CancellationTokenSource = new CancellationTokenSource();
                    cancellationToken = CancellationTokenSource.Token;
                }

                CancellationToken = cancellationToken;

                lock (_startLock)
                {
                    if (IsStarted)
                    {
                        throw new InvalidOperationException("Workflow is already started");
                    }

                    if (state != null)
                    {
                        TrySetState(state);
                    }

                    IsStarted = _isRunning = true;

                    AddArguments(arguments);

                    Workflow.Complete += OnWorkflowComplete;
                    Workflow.Exception += OnWorkflowException;
                }

                WorkflowTask = WorkflowEngine.StartWorkflowTask(this, CancellationToken);
            }

            if (synchronous)
            {
                await WorkflowTask;
            }
        }

        public void AddArguments(ICollection<Argument> argumentsToAdd = null)
        {
            if (argumentsToAdd != null)
            {
                foreach (var arg in argumentsToAdd)
                {
                    Arguments.Add(arg);
                }
            }

            foreach (
                var prop in
                    Workflow.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(
                            p =>
                                p.PropertyType.IsSubclassOf(typeof(Argument)) &&
                                    p.GetCustomAttributes<InheritArgumentAttribute>().All(a => a.CanInherit)))
            {
                var arg = Arguments.SingleOrDefault(x => x.Name == prop.Name);

                var argProp = prop.GetValue(Workflow) as Argument;
                if (argProp != null)
                {
                    if (arg != null)
                    {
                        Arguments.Remove(arg);
                    }

                    Arguments.Add(argProp);
                    continue;
                }

                if (arg == null)
                {
                    arg = (Argument)Activator.CreateInstance(prop.PropertyType, new object[] { prop.Name });
                    Arguments.Add(arg);
                }

                if (prop.GetValue(Workflow) == prop.GetType().GetDefaultValue())
                {
                    prop.SetValue(Workflow, arg, null);
                }
            }
        }

        public bool HasArgument(string name)
        {
            return Arguments.Any(a => a.Name == name);
        }

        public bool HasActionArgument(string name)
        {
            return ActionArguments.Any(a => a.Name == name);
        }

        public Argument<T> GetArgument<T>(string name)
        {
            return HasArgument(name) ? (Argument<T>)Arguments.SingleOrDefault(a => a.Name == name) : null;
        }

        public T GetArgumentValue<T>(string name, T defaultValue = default(T))
        {
            return GetArgument<T>(name).Return(a => a.Value, defaultValue);
        }

        public Argument<T> GetActionArgument<T>(string name)
        {
            return HasActionArgument(name) ? (Argument<T>)ActionArguments.SingleOrDefault(a => a.Name == name) : null;
        }

        public T GetActionArgumentValue<T>(string name, T defaultValue = default(T))
        {
            return GetActionArgument<T>(name).Return(a => a.Value, defaultValue);
        }

        public async Task Wait()
        {
            lock (_waitEventLock)
            {
                if (!IsStarted)
                {
                    throw new InvalidOperationException("Workflow is not started");
                }

                if (IsComplete)
                {
                    return;
                }

                WaitEventCount += 1;
            }

            await _completeEvent.WaitAsync();
        }

        public async Task WaitAction()
        {
            lock (_waitActionEventLock)
            {
                if (!IsStarted)
                {
                    throw new InvalidOperationException("Workflow is not started");
                }

                if (IsComplete || (_actioningEvent.CurrentCount == 0 && !IsInAction))
                {
                    return;
                }

                WaitActionEventCount += 1;
            }

            await _waitActionEvent.WaitAsync();
        }

        public async Task AwaitAction(bool waitForExitState = false)
        {
            while (true)
            {
                lock (_waitActionEventLock)
                {
                    WaitActionEventCount += 1;
                }

                _endWaitActionEvent.Release();
                await _waitActionEvent.WaitAsync(CancellationToken);

                if (!IsCancelled && waitForExitState && !IsExitState)
                {
                    continue;
                }

                break;
            }
        }

        public virtual IEnumerable<string> GetAllowedActions(string tag = null)
        {
            return GetAllowedActions(true, tag);
        }

        public virtual IEnumerable<string> GetAllowedActions(bool includeGlobal, string tag = null)
        {
            _allowActionEvent.WaitOne();
            return includeGlobal ? GlobalState.GetAllowedActions(tag) : new List<string>();
        }

        public async Task Action(string action, params Argument[] arguments)
        {
            await Action(action, false, arguments);
        }

        public async Task Action(string action, string tag, params Argument[] arguments)
        {
            await Action(action, tag, false, arguments);
        }

        public async Task Action(string action, bool asynchronous, params Argument[] arguments)
        {
            await Action(action, null, asynchronous, arguments);
        }

        public async Task Action(string action, string tag, bool asynchronous, params Argument[] arguments)
        {
            var actionTask = Task.Run(
                async () =>
                {
                    await BeginAction(arguments);
                    CurrentAction = action;
                    CurrentActionTag = tag;
                    await OnAction(action, tag);
                    CurrentAction = null;
                    CurrentActionTag = null;
                    await EndAction();
                });

            if (!asynchronous)
            {
                await actionTask;
            }
        }

        public async Task<T> Action<T>(string action, string tag = null, params Argument[] arguments)
        {
            await BeginAction(arguments);
            CurrentAction = action;
            CurrentActionTag = tag;
            var result = await OnAction<T>(action, tag);
            CurrentAction = null;
            CurrentActionTag = null;
            await EndAction();
            return result;
        }

        public async Task<WorkflowInstance> EnterWorkflow(
            Workflow workflow,
            params Argument[] arguments)
        {
            return await EnterWorkflow(workflow, true, arguments);
        }

        public async Task<WorkflowInstance> EnterWorkflow(
            Workflow workflow,
            bool synchronous = true,
            params Argument[] arguments)
        {
            var args = arguments != null ? Arguments.Union(arguments) : Arguments;
            var instance = WorkflowEngine.CreateWorkflow(workflow, Guid.Empty, Id);
            await instance.Start(CancellationToken, synchronous, args.ToArray());
            return instance;
        }

        public async Task<WorkflowInstance> EnterWorkflow<TWorkflow>(
            params Argument[] arguments) where TWorkflow : Workflow
        {
            return await EnterWorkflow<TWorkflow>(true, arguments);
        }

        public async Task<WorkflowInstance> EnterWorkflow<TWorkflow>(
            bool synchronous = true,
            params Argument[] arguments) where TWorkflow : Workflow
        {
            var args = arguments != null ? Arguments.Union(arguments) : Arguments;
            var instance = WorkflowEngine.CreateWorkflow<TWorkflow>(Guid.Empty, Id);
            await instance.Start(CancellationToken, synchronous, args.ToArray());
            return instance;
        }

        public void SetExitState(bool isExitState = true)
        {
            IsExitState = isExitState;
        }

        public void TriggerExitState()
        {
            SetExitState();
            _waitActionEvent.Release();
        }

        public virtual object TryGetState()
        {
            return null;
        }

        public virtual void TrySetState(object state)
        {
        }

        public virtual bool Cancel()
        {
            _beginExecuteEvent.WaitOne();

            lock (_waitEventLock)
            {
                if (IsCancelled)
                {
                    return false;
                }

                if (CancellationTokenSource == null)
                {
                    throw new InvalidOperationException("Cannot cancel workflow.");
                }

                if (!Workflow.OnCancel())
                {
                    return false;
                }

                CancellationTokenSource.Cancel();
                
                return true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal async Task BeginExecute()
        {
            await Workflow.BeginExecute(this);
        }

        internal void AllowExecute()
        {
            _beginExecuteEvent.Set();
        }

        internal void AllowActions()
        {
            _actioningEvent.Release();
            _allowActionEvent.Set();
        }

        protected async Task BeginAction(params Argument[] arguments)
        {
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Workflow is no longer available");
            }

            await _actioningEvent.WaitAsync();
            await _endWaitActionEvent.WaitAsync();

            if (IsComplete)
            {
                throw new InvalidOperationException("Workflow is no longer available");
            }

            IsInAction = true;
            ActionArguments.Clear();

            foreach (var arg in arguments)
            {
                ActionArguments.Add(arg);
            }
        }

#pragma warning disable 1998
        protected async Task EndAction()
#pragma warning restore 1998
        {
            lock (_waitActionEventLock)
            {
                IsInAction = false;
                for (var i = 0; i < WaitActionEventCount; i++)
                {
                    _waitActionEvent.Release();
                }

                WaitActionEventCount = 0;
            }

            _actioningEvent.Release();
        }

        protected virtual async Task OnAction(string action, string tag = null)
        {
            await OnAction<bool>(action, tag);
        }

        protected virtual async Task<T> OnAction<T>(string action, string tag = null)
        {
            if (!GlobalState.EventIsEmpty(action))
            {
                return await GlobalState.RaiseEvent<T>(action, tag);
            }

            return await Workflow.ActionAsync<T>(action);
        }

        protected virtual void OnWorkflowComplete(object sender, WorkflowCompleteEventArgs e)
        {
            lock (_waitEventLock)
            {
                IsComplete = true;
                e.Arguments = Arguments;
                e.WorkflowId = Id;

                WorkflowComplete?.Invoke(this, e);

                StopRunning();
            }
        }

        protected virtual void OnWorkflowException(object sender, WorkflowExceptionEventArgs e)
        {
            lock (_waitEventLock)
            {
                IsComplete = true;
                IsInError = true;
                e.Arguments = Arguments;
                e.WorkflowId = Id;

                WorkflowException?.Invoke(this, e);

                StopRunning();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _completeEvent.Dispose();
                _waitActionEvent.Dispose();
                _endWaitActionEvent.Dispose();
                _actioningEvent.Dispose();
                _allowActionEvent.Dispose();
            }
        }

        private void StopRunning()
        {
            _isRunning = false;

            for (var i = 0; i < WaitEventCount; i++)
            {
                _completeEvent.Release();
            }

            WaitEventCount = 0;

            _endWaitActionEvent.Release();
        }
    }

    public class WorkflowInstance<TState> : WorkflowInstance
    {
        internal WorkflowInstance(WorkflowEngine engine, Workflow workflow, Guid id, Guid parentId = default(Guid))
            : base(engine, workflow, id, parentId)
        {
        }

        private bool HasSetInitialState { get; set; }

        public TState State => CurrentState == null ? default(TState) : CurrentState.State;

        internal WorkflowState<TState> CurrentState { get; private set; }

        internal WorkflowState<TState> LastState { get; private set; }

        protected new Workflow<TState> Workflow
        {
            get { return (Workflow<TState>)base.Workflow; }
            set { base.Workflow = value; }
        }

        public void SetState(TState state)
        {
            if (HasSetInitialState && state.Equals(State))
            {
                return;
            }

            if (!HasSetInitialState)
            {
                HasSetInitialState = true;
            }

            LastState = CurrentState;
            CurrentState = Workflow.GetState(state);
            IsExitState = CurrentState.IsExitState;

            LastState?.ExitState();
            CurrentState?.EnterState();
            Workflow.OnStateChange(LastState, CurrentState);
        }

        public override IEnumerable<string> GetAllowedActions(string tag = null)
        {
            return GetAllowedActions(false, tag);
        }

        public override IEnumerable<string> GetAllowedActions(bool includeGlobal, string tag = null)
        {
            var globalActions = base.GetAllowedActions(includeGlobal, tag);
            return CurrentState.GetAllowedActions(tag).Union(globalActions);
        }

        public IEnumerable<string> GetGlobalActions(string tag = null)
        {
            return base.GetAllowedActions(true, tag);
        } 

        public override object TryGetState()
        {
            return State;
        }

        public override void TrySetState(object state)
        {
            SetState(TypeHelper.ChangeType<TState>(state));
        }

        protected override async Task OnAction(string action, string tag = null)
        {
            var state = OnInternalAction(action, tag);
            await RaiseInternalEvent<bool>(action, tag, state);
        }

        protected override async Task<T> OnAction<T>(string action, string tag = null)
        {
            var state = OnInternalAction(action, tag);
            return await RaiseInternalEvent<T>(action, tag, state);
        }

        private WorkflowState<TState> OnInternalAction(string action, string tag)
        {
            if (CurrentState.Allows(action, tag))
            {
                var outcome = CurrentState.GetOutcome(action, tag);
                SetState(outcome);
                return LastState;
            }

            return CurrentState;
        }

        private async Task<T> RaiseInternalEvent<T>(string action, string tag, WorkflowState<TState> state)
        {
            return !state.EventIsEmpty(action)
                ? await state.RaiseEvent<T>(action, tag)
                : (state.GetActionMethod(action) != null
                    ? await state.ActionAsync<T>(action)
                    : await base.OnAction<T>(action, tag));
        }
    }
}