using System.Runtime.Serialization;
using System.Threading.Tasks;
using Lightwork.Core;

namespace Lightwork.Tests.Workflows
{
    [DataContract]
    public enum PhoneStates
    {
        [DataMember(Name = "Off Hook")]
        OffHook,
        [DataMember(Name = "Ringing")]
        Ringing,
        [DataMember(Name = "Connected")]
        Connected,
        [DataMember(Name = "On Hold")]
        OnHold,
        [DataMember(Name = "Disconnected")]
        Disconnected
    }

    public class UnitTestWorkflows
    {
        public const int DefaultWaitTime = 100;
    }

    public class UpdateArgumentWorkflow : Workflow
    {
        public Argument<string> TestArg { get; set; }

        public Argument<int> SleepTime { get; set; }

        public Argument<string> OriginalArg { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            OriginalArg.Value = TestArg.Value;

            if (SleepTime.Value > 0)
            {
                await Task.Delay(SleepTime.Value);
            }

            TestArg.Value = "Updated argument";
        }
    }

    public class WaitForWorkflowWorkflow : Workflow
    {
        public Argument<WorkflowInstance> WorkflowArg { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            await WorkflowArg.Value.Wait();
        }
    }
    
    public class PhoneCallWorkflow : Workflow<PhoneStates>
    {
        public Argument<string> Message { get; set; }

        public Argument<bool> CanDial { get; set; }

        protected override void ConfigureStates()
        {
            SetInitialState(PhoneStates.OffHook);

            OnState(PhoneStates.OffHook)
                .AllowOnCondition("Call Dialed", PhoneStates.Ringing, () => CanDial.Value);

            OnState(PhoneStates.Ringing)
                .Allow("Busy", PhoneStates.Disconnected)
                .Allow("No Answer", PhoneStates.Disconnected)
                .AllowAsync("Connected", PhoneStates.Connected, OnConnected);

            OnState(PhoneStates.Connected)
                .Allow<string>("Hang Up", PhoneStates.Disconnected, OnDisconnected)
                .Allow("Placed On Hold", PhoneStates.OnHold);

            OnState(PhoneStates.OnHold)
                .Allow<string>("Hang Up", PhoneStates.Disconnected, OnDisconnected);

            OnState(PhoneStates.Disconnected)
                .SetExitState();
        }

        protected override async Task Execute(WorkflowInstance<PhoneStates> instance)
        {
            await instance.AwaitAction(true);
            Message.Value = "Hello World!";
        }

        private async Task OnConnected()
        {
            await Task.Delay(1000);
            Message.Value = "Connected";
        }

        private string OnDisconnected()
        {
            return "Goodbye";
        }
    }

    public class NonStateAwaitActionWorkflow : Workflow
    {
        public Argument<string> ActionMessage { get; set; }

        [Action]
        public void OnTakeAction()
        {
            ActionMessage.Value = "Action Taken";
        }

        protected override async Task Execute(WorkflowInstance instance)
        {
            await instance.AwaitAction();
        }
    }

    public class NonStateAwaitExitActionWorkflow : Workflow
    {
        public Argument<string> ReturnValue { get; set; }

        public Argument<string> Message { get; set; }

        public void SayHelloWorld()
        {
            Message.Value = "Hello World";
        }

        [Action]
        public void OnExitWorkflow()
        {
            WorkflowInstance.SetExitState();
        }

        protected override void ConfigureGlobalState(WorkflowState<GlobalWorkflowStates> globalState)
        {
            globalState.Allow("Say Hello", SayHelloWorld);
        }

        protected override async Task Execute(WorkflowInstance instance)
        {
            await instance.AwaitAction(true);

            ReturnValue.Value = "Awaited Exit Action";
        }
    }

    public class ActionTagWorkflow : Workflow
    {
        public Argument<string> Message { get; set; }

        protected override void ConfigureGlobalState(WorkflowState<GlobalWorkflowStates> globalState)
        {
            globalState
                .Allow("No Tag Message Action", NoTagMessageAction)
                .Allow("Tag Exit Action", TagExitAction, "tag");
        }

        protected override async Task Execute(WorkflowInstance instance)
        {
            await instance.AwaitAction(true);
        }

        private void NoTagMessageAction()
        {
            Message.Value = "No Tag Action Taken";
        }

        private void TagExitAction()
        {
            WorkflowInstance.SetExitState();
        }
    }

    public class CancelWorkflow : Workflow
    {
        public Argument<bool> AllowCancel { get; set; }

        public Argument<string> Message { get; set; }

        public Task DelayTask { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            DelayTask = Task.Delay(500, instance.CancellationToken);
            await DelayTask;
        }

        protected override bool OnCancel()
        {
            Message.Value = "Not Yet Cancelled";
            return AllowCancel.Value;
        }

        protected override void OnCancelling()
        {
            Message.Value = "Cancelled";
        }
    }
}
