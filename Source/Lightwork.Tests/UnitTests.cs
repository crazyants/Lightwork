using System;
using System.Linq;
using System.Threading.Tasks;
using Lightwork.Core;
using Lightwork.Tests.Workflows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lightwork.Tests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public async Task UnitTestWorkflowForUpdatedArgument()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<UpdateArgumentWorkflow>();

                var args = new Argument[]
                {
                    new Argument<string>("TestArg", "This is my argument")
                };

                await instance.Start(args);
                await instance.Wait();

                Assert.AreEqual("Updated argument", args.First().ToString());
                Assert.AreEqual("Updated argument", instance.GetArgument<string>("TestArg").Value);
                Assert.AreSame(args.First(), instance.GetArgument<string>("TestArg"));
                Assert.AreEqual("This is my argument", instance.GetArgument<string>("OriginalArg").Value);
            }
        }

        [TestMethod]
        public async Task UnitTestSynchronousWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<UpdateArgumentWorkflow>();

                var args = new Argument[]
                {
                    new Argument<string>("TestArg", "This is my argument"),
                    new Argument<int>("SleepTime", UnitTestWorkflows.DefaultWaitTime)
                };

                await instance.Start(true, args);

                Assert.AreEqual("Updated argument", args.First().ToString());
            }
        }

        [TestMethod]
        public async Task UnitTestWorkflowPassedAsArgument()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<UpdateArgumentWorkflow>();

                var subArgs = new Argument[]
                {
                    new Argument<WorkflowInstance>("WorkflowArg", instance)
                };

                var subWorkflow1 = engine.CreateWorkflow<WaitForWorkflowWorkflow>();
                var subWorkflow2 = engine.CreateWorkflow<WaitForWorkflowWorkflow>();

                var args = new Argument[]
                {
                    new Argument<string>("TestArg", "This is my argument")
                };

                await instance.Start(args);

                await subWorkflow1.Start(subArgs);
                await subWorkflow2.Start(subArgs);

                await subWorkflow1.Wait();
                await subWorkflow2.Wait();

                Assert.AreEqual(args.First().ToString(), "Updated argument");
            }
        }

        [TestMethod]
        public async Task UnitTestStateWorkflowAndActions()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<PhoneCallWorkflow, PhoneStates>();

                var messageArg = new Argument<string>("Message");
                var canDialArg = new Argument<bool>("CanDial", true);
                var args = new Argument[] { messageArg, canDialArg };

                await instance.Start(args);

                // test action
                messageArg.Value = "Dialing...";
                await instance.Action("Call Dialed");

                // test asynchronous action
                await instance.Action("Connected", true);
                Assert.AreEqual("Dialing...", messageArg.Value);

                // test waiting for action to complete
                await instance.WaitAction();
                Assert.AreEqual("Connected", messageArg.Value);

                // test return value
                var response = await instance.Action<string>("Hang Up");
                Assert.AreEqual("Goodbye", response);

                // wait for workflow to complete
                await instance.Wait();

                Assert.AreEqual("Hello World!", messageArg.Value);
                Assert.AreEqual(PhoneStates.Disconnected, instance.State);
            }
        }

        [TestMethod]
        public async Task UnitTestApprovalWorkflowAssignApproveAccept()
        {
            using (var engine = new WorkflowEngine())
            {
                // without passing second generic paramter to identify state, base workflow instance type is returned
                var instance = engine.CreateWorkflow<ApprovalWorkflow>();

                await instance.Start();

                // test null wait for action
                await instance.WaitAction();

                // test action with arguments
                await instance.Action("Assign", new Argument<string>("AssignTo", "test@test.com"));
                Assert.AreEqual("test@test.com", instance.GetArgument<string>("AssignedTo").Value);

                await instance.Action("Approve");
                Assert.IsTrue(instance.GetArgument<bool>("IsApproved").Value);

                await instance.Action("Accept", new Argument<string>("AcceptedBy", "Test User"));
                Assert.AreEqual("Test User", instance.GetArgument<string>("AcceptedBy").Value);

                await instance.Wait();

                // need to use TryGetState because base workflow instance was returned during create
                Assert.AreEqual(
                    ApprovalWorkflow.ApprovalStates.Accepted,
                    (ApprovalWorkflow.ApprovalStates)instance.TryGetState());
            }
        }

        [TestMethod]
        public async Task UnitTestApprovalWorkflowAssignRejectAccept()
        {
            using (var engine = new WorkflowEngine())
            {
                // pass in workflow and state type to return stateful instance type
                var instance = engine.CreateWorkflow<ApprovalWorkflow, ApprovalWorkflow.ApprovalStates>();

                await instance.Start();

                // test action with arguments
                await instance.Action("Assign", new Argument<string>("AssignTo", "test@test.com"));
                Assert.AreEqual("test@test.com", instance.GetArgument<string>("AssignedTo").Value);

                await instance.Action("Reject");
                Assert.IsFalse(instance.GetArgument<bool>("IsApproved").Value);

                await instance.Action("Accept", new Argument<string>("AcceptedBy", "Test User"));
                Assert.AreEqual("Test User", instance.GetArgument<string>("AcceptedBy").Value);

                await instance.Wait();

                Assert.AreEqual(ApprovalWorkflow.ApprovalStates.Accepted, instance.State);
            }
        }

        [TestMethod]
        public async Task UnitTestParallelWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var parallelWorkflow = new ParallelWorkflow();

                // test parallel sequential workflow
                parallelWorkflow.Add(
                    new SequentialWorkflow(
                        async instance =>
                        {
                            // test parent id set correct
                            instance.GetArgument<Guid>("ParentWorkflowId").Value = instance.ParentId;

                            // test enter workflow using outer instance
                            await
                                instance.EnterWorkflow(
                                    Workflow.Create(
                                        internalInstance => instance.GetArgument<int>("Counter").Lock(a => a.Value += 1)));
                            instance.GetArgument<int>("Counter").Lock(a => a.Value += 1);
                        }));

                // test parallel create workflow
                parallelWorkflow.Add(
                    Workflow.Create(instance => instance.GetArgument<int>("Counter").Lock(a => a.Value += 1)));

                // test parallel delay workflow
                parallelWorkflow.Add(
                    Workflow.Create(
                        async instance => await instance.EnterWorkflow(
                            new DelayWorkflow(
                                Workflow.Create(
                                    internalInstance =>
                                        internalInstance.GetArgument<int>("Counter").Lock(a => a.Value += 1)),
                                UnitTestWorkflows.DefaultWaitTime))));

                var argCounter = new Argument<int>("Counter");

                var parallelInstance = engine.CreateWorkflow(parallelWorkflow);
                await parallelInstance.Start(
                    argCounter,
                    new Argument<Guid>("ParentWorkflowId"));

                await parallelInstance.Wait();

                Assert.AreEqual(argCounter.Value, parallelInstance.GetArgument<int>("Counter").Value);
                Assert.AreEqual(4, parallelInstance.GetArgument<int>("Counter").Value);
                Assert.AreEqual(parallelInstance.Id, parallelInstance.GetArgument<Guid>("ParentWorkflowId").Value);
            }
        }

        [TestMethod]
        public async Task UnitTestDelayUntilWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var escalationDateTime = DateTime.Now.AddSeconds(1);
                var delayUntilWorkflow = new DelayUntilWorkflow(
                    Workflow.Create(
                        instance =>
                        {
                            instance.GetArgument<bool>("HasEscalated").Value = true;
                        }),
                    escalationDateTime);

                var delayUntilInstance = engine.CreateWorkflow(delayUntilWorkflow);
                await delayUntilInstance.Start(new Argument<bool>("HasEscalated"));

                await delayUntilInstance.Wait();
                var completeTime = DateTime.Now;

                Assert.IsTrue(delayUntilInstance.GetArgument<bool>("HasEscalated").Value);
                Assert.IsTrue(completeTime >= escalationDateTime);
            }
        }

        [TestMethod]
        public async Task UnitTestDelayUntilConditionWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var argConditionMet = new Argument<bool>("ConditionMet");

                var escalationDateTime = DateTime.Now.AddSeconds(5);
                var setConditionDateTime = DateTime.Now.AddSeconds(1);
                var delayUntilWorkflow = new DelayUntilConditionWorkflow(
                    Workflow.Create(
                        instance =>
                        {
                            instance.GetArgument<bool>("HasEscalated").Value = true;
                        }),
                    () => argConditionMet.Value,
                    escalationDateTime);

                var delayUntilInstance = engine.CreateWorkflow(delayUntilWorkflow);
                await delayUntilInstance.Start(new Argument<bool>("HasEscalated"));

                var setConditionWorkflow = new DelayWorkflow(
                    Workflow.Create(
                        () =>
                        {
                            argConditionMet.Value = true;
                        }),
                    Convert.ToInt32(setConditionDateTime.Subtract(DateTime.Now).TotalMilliseconds));

                var setConditionInstance = engine.CreateWorkflow(setConditionWorkflow);
                await setConditionInstance.Start();

                await delayUntilInstance.Wait();
                await setConditionInstance.Wait();

                Assert.IsTrue(delayUntilInstance.GetArgument<bool>("HasEscalated").Value);
                Assert.IsTrue(DateTime.Now >= setConditionDateTime);
                Assert.IsFalse(DateTime.Now >= escalationDateTime);
            }
        }

        [TestMethod]
        public async Task UnitTestWhileWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var progress = 0;
                var instance = engine.CreateWorkflow(
                    new WhileWorkflow(
                        async internalInstance =>
                        {
                            await Task.Delay(100);
                            internalInstance.GetArgument<int>("Progress").Lock(
                                arg =>
                                {
                                    progress += 10;
                                    arg.Value = progress;
                                });
                        },
                        () => progress < 100));

                await instance.Start(new Argument<int>("Progress"));

                await instance.Wait();

                Assert.AreEqual(100, progress);
            }
        }

        public IfElseWorkflow CreateIfElseWorkflowBreakAfterFirstTrueMatchConditionIndex(int conditionTrueIndex)
        {
            var ifElseWorkflow = new IfElseWorkflow(true);

            // if conditionTrueIndex == 1
            ifElseWorkflow.AddBranch(
                Workflow.Create(
                    instance =>
                    {
                        instance.GetArgument<string>("ReturnValue").Value = "First condition was true";
                    }),
                () => conditionTrueIndex == 1);

            // if conditionTrueIndex == 2
            ifElseWorkflow.AddBranch(
                Workflow.Create(
                    instance =>
                    {
                        instance.GetArgument<string>("ReturnValue").Value = "Second condition was true";
                    }),
                () => conditionTrueIndex == 2);

            // else branch
            ifElseWorkflow.AddBranch(
                Workflow.Create(
                    instance =>
                    {
                        instance.GetArgument<string>("ReturnValue").Value = "Else condition was true";
                    }));

            return ifElseWorkflow;
        }

        [TestMethod]
        public async Task UnitTestIfElseWorkflowBreakAfterFirstTrueMatchFirstIf()
        {
            using (var engine = new WorkflowEngine())
            {
                var ifElseWorkflow = CreateIfElseWorkflowBreakAfterFirstTrueMatchConditionIndex(1);

                var instance = engine.CreateWorkflow(ifElseWorkflow);
                await instance.Start(new Argument<string>("ReturnValue"));
                await instance.Wait();

                Assert.AreEqual("First condition was true", instance.GetArgument<string>("ReturnValue").Value);
            }
        }

        [TestMethod]
        public async Task UnitTestIfElseWorkflowBreakAfterFirstTrueMatchSecondtIf()
        {
            using (var engine = new WorkflowEngine())
            {
                var ifElseWorkflow = CreateIfElseWorkflowBreakAfterFirstTrueMatchConditionIndex(2);

                var instance = engine.CreateWorkflow(ifElseWorkflow);
                await instance.Start(new Argument<string>("ReturnValue"));
                await instance.Wait();

                Assert.AreEqual("Second condition was true", instance.GetArgument<string>("ReturnValue").Value);
            }
        }

        [TestMethod]
        public async Task UnitTestIfElseWorkflowBreakAfterFirstTrueMatchElse()
        {
            using (var engine = new WorkflowEngine())
            {
                var ifElseWorkflow = CreateIfElseWorkflowBreakAfterFirstTrueMatchConditionIndex(3);

                var instance = engine.CreateWorkflow(ifElseWorkflow);
                await instance.Start(new Argument<string>("ReturnValue"));
                await instance.Wait();

                Assert.AreEqual("Else condition was true", instance.GetArgument<string>("ReturnValue").Value);
            }
        }

        [TestMethod]
        public async Task UnitTestNonStateWorkflowAction()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<NonStateAwaitActionWorkflow>();

                await instance.Start();
                await instance.Action("Take Action");
                await instance.Wait();

                Assert.AreEqual("Action Taken", instance.GetArgument<string>("ActionMessage").Value);
            }
        }

        [TestMethod]
        public async Task UnitTestNonStateWorkflowExitAction()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<NonStateAwaitExitActionWorkflow>();

                await instance.Start();

                var allowedActions = instance.GetAllowedActions();
                CollectionAssert.AreEqual(new[] { "Say Hello" }, allowedActions.ToList());

                await instance.Action("Say Hello");
                Assert.AreEqual("Hello World", instance.GetArgument<string>("Message").Value);

                await instance.Action("Exit Workflow");
                await instance.Wait();

                Assert.AreEqual("Awaited Exit Action", instance.GetArgument<string>("ReturnValue").Value);
            }
        }

        [TestMethod]
        public async Task UnitTestWorkflowActionWithTag()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<ActionTagWorkflow>();

                await instance.Start();

                await instance.Action("No Tag Message Action", "tag");
                Assert.IsNull(instance.GetArgumentValue<string>("Message"));

                await instance.Action("No Tag Message Action");
                Assert.AreEqual("No Tag Action Taken", instance.GetArgumentValue<string>("Message"));

                await instance.Action("Tag Exit Action");
                Assert.IsFalse(instance.IsExitState);

                await instance.Action("Tag Exit Action", "tag");
                Assert.IsTrue(instance.IsExitState);

                await instance.Wait();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "Workflow is already started")]
        public async Task UnitTestWorkflowAlreadyStartedException()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow(new DelayWorkflow(500));

                await instance.Start();

                // 2nd call to Start should throw Exception
                await instance.Start();
            }
        }

        [TestMethod]
        public async Task UnitTestAllowCancelWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<CancelWorkflow>();

                await instance.Start(new Argument<bool>("AllowCancel", true));

                await Task.Delay(50);

                var cancelled = instance.Cancel();

                await instance.Wait();

                Assert.IsTrue(instance.IsCancelled);
                Assert.AreEqual(cancelled, instance.IsCancelled);
                Assert.AreEqual("Cancelled", instance.GetArgumentValue<string>("Message"));
            }
        }

        [TestMethod]
        public async Task UnitTestDenyCancelWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<CancelWorkflow>();

                await instance.Start(new Argument<bool>("AllowCancel", false));

                await Task.Delay(50);

                var cancelled = instance.Cancel();

                await instance.Wait();

                Assert.IsFalse(instance.IsCancelled);
                Assert.AreEqual(cancelled, instance.IsCancelled);
                Assert.AreEqual("Not Yet Cancelled", instance.GetArgumentValue<string>("Message"));
            }
        }

        [TestMethod]
        public async Task UnitTestCancelStateWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var instance = engine.CreateWorkflow<ApprovalWorkflow>();

                await instance.Start();

                var cancelled = instance.Cancel();

                await instance.Wait();

                Assert.IsTrue(instance.IsCancelled);
                Assert.AreEqual(cancelled, instance.IsCancelled);
            }
        }
    }
}