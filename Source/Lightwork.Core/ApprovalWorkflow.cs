using System.Threading.Tasks;

namespace D3.Lightwork.Core
{
    public class ApprovalWorkflow : Workflow<ApprovalWorkflow.ApprovalStates>
    {
        public enum ApprovalStates
        {
            Open,
            Assigned,
            Approved,
            Rejected,
            Accepted
        }

        public Argument<bool> IsApproved { get; set; }

        public Argument<string> AssignedTo { get; set; }

        public Argument<string> AcceptedBy { get; set; }

        protected override void ConfigureStates()
        {
            SetInitialState(ApprovalStates.Open);

            OnState(ApprovalStates.Open)
                .Allow("Assign", ApprovalStates.Assigned, OnAssigned);

            OnState(ApprovalStates.Assigned)
                .Allow("Reassign", ApprovalStates.Assigned, OnAssigned)
                .Allow("Approve", ApprovalStates.Approved, OnApproved)
                .Allow("Reject", ApprovalStates.Rejected, OnRejected);

            OnState(ApprovalStates.Approved)
                .Allow("Accept", ApprovalStates.Accepted, OnAccepted);

            OnState(ApprovalStates.Rejected)
                .Allow("Accept", ApprovalStates.Accepted, OnAccepted);

            OnState(ApprovalStates.Accepted)
                .SetExitState();
        }

        protected override async Task Execute(WorkflowInstance<ApprovalStates> instance)
        {
            await instance.AwaitAction(true);
        }

        protected virtual void OnAssigned()
        {
            AssignedTo.Value = WorkflowInstance.GetActionArgument<string>("AssignTo").Value;
        }

        protected virtual void OnApproved()
        {
            IsApproved.Value = true;
        }

        protected virtual void OnRejected()
        {
            IsApproved.Value = false;
        }

        protected virtual void OnAccepted()
        {
            AcceptedBy.Value = WorkflowInstance.GetActionArgument<string>("AcceptedBy").Value;
        }
    }
}
