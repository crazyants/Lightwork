using System;
using System.Threading.Tasks;
using D3.Lightwork.Core;
using D3.Lightwork.Tests.Workflows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace D3.Lightwork.Tests
{
    [TestClass]
    public class AdvancedUnitTests
    {
        [TestMethod]
        public async Task UnitTestAssignVendorWorkflowAssignmentConfirmed()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<AssignVendorWorkflow, AssignVendorStates>();
                await instance.Start(
                    new Argument<string>("VendorName", "John Smith"),
                    new Argument<int>("EscalationTimeout", int.MaxValue)); // don't allow escalation for this workflow

                await instance.Action("Approve Recommendation");
                await instance.Action("Contact For Assignment");
                var result = await instance.Action<string>("Confirm Assignment");
                Assert.AreEqual("Vendor John Smith confirmed assignment", result);

                await instance.Wait();
                Assert.AreEqual(AssignVendorStates.AssignmentConfirmed, instance.State);
                Assert.IsTrue(instance.GetArgument<bool>("IsBooked").Value);
                Assert.IsFalse(instance.GetArgument<bool>("HasEscalated").Value);
                Assert.IsTrue(instance.GetArgument<bool>("FinishedRunning").Value);
            }
        }

        [TestMethod]
        public async Task UnitTestAssignVendorWorkflowAwaitingAssignmentEscalation()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<AssignVendorWorkflow, AssignVendorStates>();
                await instance.Start(
                    new Argument<string>("VendorName", "John Smith"),
                    new Argument<int>("EscalationTimeout", 1000)); // escalate after 1 second

                await instance.Action("Approve Recommendation");
                await instance.Action("Contact For Assignment");

                // test while workflow, waiting for the assign vendor workflow to escalate
                await instance.EnterWorkflow(new WhileWorkflow(
                    async ins => await Task.Delay(100), // run workflow to delay 50ms between loops
                    () => !instance.GetArgument<bool>("HasEscalated").Value)); // wait while HasEscalated argument is false

                // the escalation doesn't actually stop the workflow, force it to stop awaiting actions
                instance.TriggerExitState();

                // still need to wait for the workflow to complete
                await instance.Wait();

                Assert.AreEqual(AssignVendorStates.AwaitingConfirmation, instance.State);
                Assert.IsFalse(instance.GetArgument<bool>("IsBooked").Value);
                Assert.IsTrue(instance.GetArgument<bool>("HasEscalated").Value);
                Assert.IsTrue(instance.GetArgument<bool>("FinishedRunning").Value);
            }
        }

        [TestMethod]
        public async Task UnitTestPrinterWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<PrinterWorkflow, PrinterStates>();
                await instance.Start();

                await instance.Action("Turn On");
                await instance.Action("Print");

                await instance.Wait();

                Assert.AreEqual(PrinterStates.PrintEnd, instance.State);
            }
        }
    }
}