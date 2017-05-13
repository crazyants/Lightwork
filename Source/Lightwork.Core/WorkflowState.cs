using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lightwork.Core
{
    public enum GlobalWorkflowStates
    {
        DefaultState
    }

    public class WorkflowState<TState>
    {
        public TState State { get; internal set; }

        public WorkflowInstance<TState> WorkflowInstance { get; internal set; }

        public bool HasEntered { get; private set; }
        
        private readonly IDictionary<string, TState> _outcomes =
            new Dictionary<string, TState>(StringComparer.OrdinalIgnoreCase);

        private readonly IDictionary<string, Func<Task<InternalEventResult>>> _events =
            new Dictionary<string, Func<Task<InternalEventResult>>>(StringComparer.OrdinalIgnoreCase);

        private readonly IDictionary<string, Func<bool>> _conditions =
            new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase);

        private readonly IDictionary<string, string> _tags =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public WorkflowState()
        {
            
        }

        public WorkflowState(TState state)
        {
            State = state;
        }

        public WorkflowState(TState state, WorkflowInstance<TState> workflowInstance)
            : this(state)
        {
            WorkflowInstance = workflowInstance;
        }

        public bool IsExitState { get; set; }

        public WorkflowState<TState> Allow(string action, TState newState, string tag = null)
        {
            if (_outcomes.ContainsKey(action))
            {
                throw new Exception($"Action {action} already allowed for state {newState}");
            }

            _outcomes.Add(action, newState);

            if (tag != null)
            {
                _tags.Add(action, tag);
            }

            return this;
        }

        public WorkflowState<TState> Allow(string action, string tag = null)
        {
            return Allow(action, State, tag);
        }

        public WorkflowState<TState> AllowOnCondition(
            string action,
            TState newState,
            Func<bool> condition,
            string tag = null)
        {
            Allow(action, newState, tag);

            _conditions.Add(action, condition);

            return this;
        }

        public WorkflowState<TState> AllowOnCondition(string action, Func<bool> condition, string tag = null)
        {
            return AllowOnCondition(action, State, condition, tag);
        }

        public WorkflowState<TState> AllowAsync<T>(
            string action,
            TState newState,
            Func<Task<T>> onState,
            string tag = null)
        {
            Allow(action, newState, tag);

            _events.Add(
                action,
                async () =>
                {
                    var result = await onState();
                    return new InternalEventResult
                    {
                        ReturnValue = result
                    };
                });

            return this;
        }

        public WorkflowState<TState> AllowAsync<T>(string action, Func<Task<T>> onState, string tag = null)
        {
            return AllowAsync(action, State, onState, tag);
        }

        public WorkflowState<TState> AllowOnConditionAsync<T>(
            string action,
            TState newState,
            Func<bool> condition,
            Func<Task<T>> onState,
            string tag = null)
        {
            AllowAsync(action, newState, onState, tag);

            _conditions.Add(action, condition);

            return this;
        }

        public WorkflowState<TState> AllowOnConditionAsync<T>(
            string action,
            Func<bool> condition,
            Func<Task<T>> onState,
            string tag = null)
        {
            return AllowOnConditionAsync(action, State, condition, onState, tag);
        }

        public WorkflowState<TState> AllowAsync(string action, TState newState, Func<Task> onState, string tag = null)
        {
            Allow(action, newState, tag);

            _events.Add(
                action,
                async () =>
                {
                    await onState();
                    return new InternalEventResult
                    {
                        ReturnValue = true
                    };
                });

            return this;
        }

        public WorkflowState<TState> AllowAsync(string action, Func<Task> onState, string tag = null)
        {
            return AllowAsync(action, State, onState, tag);
        }

        public WorkflowState<TState> AllowOnConditionAsync(
            string action,
            TState newState,
            Func<bool> condition,
            Func<Task> onState,
            string tag = null)
        {
            AllowAsync(action, newState, onState, tag);

            _conditions.Add(action, condition);

            return this;
        }

        public WorkflowState<TState> AllowOnConditionAsync(
            string action,
            Func<bool> condition,
            Func<Task> onState,
            string tag = null)
        {
            return AllowOnConditionAsync(action, State, condition, onState, tag);
        }

        public WorkflowState<TState> Allow<T>(string action, TState newState, Func<T> onState, string tag = null)
        {
            Allow(action, newState, tag);

            _events.Add(
                action,
#pragma warning disable 1998
                async () =>
#pragma warning restore 1998
                {
                    var result = onState();
                    return new InternalEventResult
                    {
                        ReturnValue = result
                    };
                });

            return this;
        }

        public WorkflowState<TState> Allow<T>(string action, Func<T> onState, string tag = null)
        {
            return Allow(action, State, onState, tag);
        }

        public WorkflowState<TState> AllowOnCondition<T>(
            string action,
            TState newState,
            Func<bool> condition,
            Func<T> onState,
            string tag = null)
        {
            Allow(action, newState, onState, tag);

            _conditions.Add(action, condition);

            return this;
        }

        public WorkflowState<TState> AllowOnCondition<T>(
            string action,
            Func<bool> condition,
            Func<T> onState,
            string tag = null)
        {
            return AllowOnCondition(action, State, condition, onState, tag);
        }

        public WorkflowState<TState> Allow(string action, TState newState, Action onState, string tag = null)
        {
            Allow(action, newState, tag);

            _events.Add(
                action,
#pragma warning disable 1998
                async () =>
#pragma warning restore 1998
                {
                    onState();
                    return new InternalEventResult
                    {
                        ReturnValue = true
                    };
                });

            return this;
        }

        public WorkflowState<TState> Allow(string action, Action onState, string tag = null)
        {
            return Allow(action, State, onState, tag);
        }

        public WorkflowState<TState> AllowOnCondition(
            string action,
            TState newState,
            Func<bool> condition,
            Action onState,
            string tag = null)
        {
            Allow(action, newState, onState, tag);

            _conditions.Add(action, condition);

            return this;
        }

        public WorkflowState<TState> AllowOnCondition(
            string action,
            Func<bool> condition,
            Action onState,
            string tag = null)
        {
            return AllowOnCondition(action, State, condition, onState, tag);
        }

        public WorkflowState<TState> Allow(string action, Func<bool> condition, Action onState, string tag = null)
        {
            return AllowOnCondition(action, State, condition, onState, tag);
        }

        public void Remove(string action)
        {
            _outcomes.Remove(action);
            _conditions.Remove(action);
            _events.Remove(action);
            _tags.Remove(action);
        }

        public WorkflowState<TState> SetExitState(bool isExitState = true)
        {
            IsExitState = isExitState;
            return this;
        }

        public bool Allows(string action, string tag = null)
        {
            return _outcomes.ContainsKey(action) && AllowsCondition(action) && HasTag(action, tag);
        }

        public bool AllowsCondition(string action)
        {
            var condition = GetCondition(action);
            return condition == null || condition();
        }

        public IEnumerable<string> GetAllowedActions(string tag = null)
        {
            return _outcomes.Where(o => Allows(o.Key, tag)).Select(o => o.Key);
        }

        public TState GetOutcome(string action, string tag = null)
        {
            if (!Allows(action, tag))
            {
                throw new Exception(string.Format("Action {0} not allowed for current state {1}", action, State));
            }

            return _outcomes[action];
        }

        public bool EventIsEmpty(string action)
        {
            return GetEvent(action) == null;
        }

        public bool HasTag(string action, string tag)
        {
            return GetTag(action) == tag;
        }

        public async Task<T> RaiseEvent<T>(string action, string tag = null)
        {
            var func = GetEvent(action);
            if (!HasTag(action, tag) || func == null)
            {
                return default(T);
            }

            var result = await func();
            return (T)result.ReturnValue;
        }

        public async Task RaiseEvent(string action, string tag = null)
        {
            var func = GetEvent(action);
            if (!HasTag(action, tag) || func != null)
            {
                await func();
            }
        }

        private Func<Task<InternalEventResult>> GetEvent(string action)
        {
            Func<Task<InternalEventResult>> func;
            if (!_events.TryGetValue(action, out func))
            {
                return null;
            }

            return func;
        }

        private Func<bool> GetCondition(string action)
        {
            Func<bool> func;
            if (!_conditions.TryGetValue(action, out func))
            {
                return null;
            }

            return func;
        }

        private string GetTag(string action)
        {
            string tag = null;
            if (!_tags.TryGetValue(action, out tag))
            {
                return null;
            }

            return tag;
        }

        internal void EnterState()
        {
            if (!HasEntered)
            {
                HasEntered = true;
                OnEnterFirstTime();
            }

            OnEnterState();
        }

        protected virtual void OnEnterState()
        {
            
        }

        protected virtual void OnEnterFirstTime()
        {
            
        }

        internal void ExitState()
        {
            OnExitState();
        }

        protected virtual void OnExitState()
        {
            
        }
        
        public virtual void ConfigureActions()
        {
            
        }

        internal class InternalEventResult
        {
            public object ReturnValue { get; set; }
        }

        internal class EmptyInternalEventResult : InternalEventResult
        {
        }
    }
}