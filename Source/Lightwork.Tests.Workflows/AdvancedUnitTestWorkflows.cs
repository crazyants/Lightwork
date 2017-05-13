using System;
using System.Threading.Tasks;
using Lightwork.Core;

namespace Lightwork.Tests.Workflows
{
    #region AssignVendorWorkflow
    public enum AssignVendorStates
    {
        Recommended,
        RecommendationApproved,
        RecommendationRejected,
        AwaitingConfirmation,
        AwaitingConfirmationEscalation,
        AssignmentConfirmed,
        AssignmentRejected
    }

    public class AdvancedUnitTestWorkflows
    {
    }

    public class AssignVendorWorkflow : Workflow<AssignVendorStates>
    {
        public Argument<string> VendorName { get; set; }

        public Argument<bool> IsBooked { get; set; }

        public Argument<bool> HasEscalated { get; set; }

        public Argument<int> EscalationTimeout { get; set; }

        public Argument<bool> FinishedRunning { get; set; }

        protected override void ConfigureStates()
        {
            SetInitialState(AssignVendorStates.Recommended);

            OnState(AssignVendorStates.Recommended)
                .Allow("Approve Recommendation", AssignVendorStates.RecommendationApproved)
                .Allow("Reject Recommendation", AssignVendorStates.RecommendationRejected);

            OnState(AssignVendorStates.RecommendationApproved)
                .AllowAsync("Contact For Assignment", AssignVendorStates.AwaitingConfirmation, OnContactForAssignment);

            OnState(AssignVendorStates.RecommendationRejected)
                .SetExitState();

            OnState(AssignVendorStates.AwaitingConfirmation)
                .Allow(
                    "Confirm Assignment",
                    AssignVendorStates.AssignmentConfirmed,
                    () => $"Vendor {VendorName.Value} confirmed assignment")
                .Allow(
                    "Reject Assignment",
                    AssignVendorStates.RecommendationRejected,
                    () => $"Vendor {VendorName.Value} rejected assignment");

            OnState(AssignVendorStates.AssignmentConfirmed)
                .SetExitState();

            OnState(AssignVendorStates.AssignmentRejected)
                .SetExitState();
        }

        protected override async Task Execute(WorkflowInstance<AssignVendorStates> instance)
        {
            if (EscalationTimeout.Value == 0)
            {
                EscalationTimeout.Value = 5000;
            }

            await instance.AwaitAction(true);

            if (instance.State == AssignVendorStates.AssignmentConfirmed)
            {
                IsBooked.Value = true;
            }

            FinishedRunning.Value = true;
        }

        private async Task OnContactForAssignment()
        {
            if (EscalationTimeout.Value == int.MaxValue)
            {
                return;
            }

            var escalationTime = DateTime.Now.AddMilliseconds(EscalationTimeout.Value);
            var delayUntilWorkflow = new DelayUntilConditionWorkflow(
                Create(
                    instance =>
                    {
                        HasEscalated.Value = true;
                    }),
                () => IsBooked.Value,
                escalationTime,
                true);

            await WorkflowInstance.EnterWorkflow(delayUntilWorkflow, false);
        }
    }
    #endregion

    #region State Design Example: PrinterWorkflow
    public enum PrinterStates
    {
        Offline,
        Ready,
        PrintStart,
        Printing,
        PrintEnd
    }

    public abstract class PrinterState : WorkflowState<PrinterStates>
    {
        
    }

    public class StatePrinterOffline : PrinterState
    {
        public override void ConfigureActions()
        {
            Allow("Turn On", PrinterStates.Ready);
        }
    }

    public class StatePrinterReady : PrinterState
    {
        public override void ConfigureActions()
        {
            Allow("Print", PrinterStates.PrintStart);
        }
    }

    public class StatePrinterPrintStart : PrinterState
    {
        protected override void OnEnterState()
        {
            WorkflowInstance.SetState(PrinterStates.Printing);
        }
    }

    public class StatePrinterPrinting : PrinterState
    {
        public override void ConfigureActions()
        {
            Allow("Done", PrinterStates.PrintEnd);
        }

        protected override async void OnEnterState()
        {
            var printingWorkflow = new DelayWorkflow(
                Workflow.Create(
                    async () =>
                    {
                        await WorkflowInstance.Action("Done");
                    }),
                500);

            await WorkflowInstance.EnterWorkflow(printingWorkflow, false);
        }
    }

    public class StatePrinterPrintEnd : PrinterState
    {
        public override void ConfigureActions()
        {
            SetExitState();
        }
    }

    public class PrinterWorkflow : Workflow<PrinterStates>
    {
        protected override void ConfigureStates()
        {
            SetInitialState(PrinterStates.Offline);

            OnState<StatePrinterOffline>(PrinterStates.Offline);
            OnState<StatePrinterReady>(PrinterStates.Ready);
            OnState<StatePrinterPrintStart>(PrinterStates.PrintStart);
            OnState<StatePrinterPrinting>(PrinterStates.Printing);
            OnState<StatePrinterPrintEnd>(PrinterStates.PrintEnd);
        }

        protected override async Task Execute(WorkflowInstance<PrinterStates> instance)
        {
            await instance.AwaitAction(true);
        }
    }

    #endregion
}