using System;
using System.Threading.Tasks;

namespace Lightwork.Core
{
    [WorkflowBehaviorOnLoad(BypassWorkflow = true)]
    public class DelayUntilConditionWorkflow : Workflow
    {
        public DelayUntilConditionWorkflow()
        {
        }

        public DelayUntilConditionWorkflow(
            Workflow workflow,
            Func<bool> condition,
            int conditionCheckPeriod = WorkflowEngine.DefaultPollingInternal)
            : this(workflow, condition, DateTime.MaxValue, false, conditionCheckPeriod)
        {
        }

        public DelayUntilConditionWorkflow(
            Workflow workflow,
            Func<bool> condition,
            DateTime escalationDateTime,
            int conditionCheckPeriod = WorkflowEngine.DefaultPollingInternal)
            : this(workflow, condition, escalationDateTime, false, conditionCheckPeriod)
        {
        }

        public DelayUntilConditionWorkflow(
            Workflow workflow,
            Func<bool> condition,
            DateTime escalationDateTime,
            bool breakOnCondition,
            int conditionCheckPeriod = WorkflowEngine.DefaultPollingInternal)
        {
            Workflow = new Argument<Workflow>("Workflow", workflow);
            Condition = new Argument<Func<bool>>("Condition", condition);
            EscalationDateTime = new Argument<DateTime>("EscalationDateTime", escalationDateTime);
            BreakOnCondition = new Argument<bool>("BreakOnCondition", breakOnCondition);
            ConditionCheckPeriod = new Argument<int>("ConditionCheckPeriod", conditionCheckPeriod);
        }

        public Argument<DateTime> EscalationDateTime { get; set; }

        [InheritArgument(false)]
        public Argument<Workflow> Workflow { get; set; }

        [InheritArgument(false)]
        public Argument<Func<bool>> Condition { get; set; }

        public Argument<int> ConditionCheckPeriod { get; set; }

        public Argument<bool> BreakOnCondition { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            var trippedCondition = false;
            while (!instance.IsCancelled && !(trippedCondition = Condition.Value()) && DateTime.Now < EscalationDateTime.Value)
            {
                await Task.Delay(ConditionCheckPeriod.Value, instance.CancellationToken);
            }

            if (instance.IsCancelled || (BreakOnCondition.Value && trippedCondition))
            {
                return;
            }

            if (Workflow.Value != null)
            {
                await EnterWorkflow(instance);
            }
        }

        protected virtual async Task EnterWorkflow(WorkflowInstance instance)
        {
            await instance.EnterWorkflow(Workflow.Value);
        }
    }

    public class DelayUntilConditionWorkflow<TWorkflow> : DelayUntilConditionWorkflow where TWorkflow : Workflow
    {
        public DelayUntilConditionWorkflow()
        {
        }

        public DelayUntilConditionWorkflow(
            Func<bool> condition,
            int conditionCheckPeriod = WorkflowEngine.DefaultPollingInternal)
            : this(condition, DateTime.MaxValue, false, conditionCheckPeriod)
        {
        }

        public DelayUntilConditionWorkflow(
            Func<bool> condition,
            DateTime escalationDateTime,
            int conditionCheckPeriod = WorkflowEngine.DefaultPollingInternal)
            : this(condition, escalationDateTime, false, conditionCheckPeriod)
        {
        }

        public DelayUntilConditionWorkflow(
            Func<bool> condition,
            DateTime escalationDateTime,
            bool breakOnCondition,
            int conditionCheckPeriod = WorkflowEngine.DefaultPollingInternal)
            : base(null, condition, escalationDateTime, breakOnCondition, conditionCheckPeriod)
        {
        }

        protected override async Task EnterWorkflow(WorkflowInstance instance)
        {
            await instance.EnterWorkflow<TWorkflow>();
        }
    }
}
