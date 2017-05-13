using System;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using Lightwork.Core;

namespace Lightwork.Data
{
    [WorkflowBehaviorOnLoad(BypassWorkflow = true)]
    public class DbContextWorkflow<TContext> : Workflow where TContext : DbContext, new()
    {
        private static readonly SemaphoreSlim ContextLock = new SemaphoreSlim(1, 1);

        private readonly bool _disposeContext;

        public DbContextWorkflow()
        {
        }

        public DbContextWorkflow(TContext context, Func<TContext, Task> action)
        {
            Context = new Argument<TContext>("Context", context);
            Action = new Argument<Func<TContext, Task>>("Action", action);
        }

        public DbContextWorkflow(Func<TContext, Task> action)
            : this(null, action)
        {
            _disposeContext = true;
        }

        public Argument<TContext> Context { get; set; }

        [InheritArgument(false)]
        public Argument<Func<TContext, Task>> Action { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            await ContextLock.WaitAsync();
            if (Context.Value == null)
            {
                Context.Value = new TContext();
            }

            await Action.Value(Context.Value);

            if (_disposeContext)
            {
                Context.Value.Dispose();
            }

            ContextLock.Release();
        }
    }
}