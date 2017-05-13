using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using D3.Lightwork.Core;
using D3.Lightwork.Tests.Workflows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace D3.Lightwork.Tests
{
    [TestClass]
    public class PerformanceTests
    {
        private const bool RunPerformanceTests = true;
        private const int PerformanceIterations = 1000;
        private const int MaxDelay = 500;

        private readonly Random _random = new Random();

        public async Task RunPerformanceIncrementTest(
            int iterations,
            int maxDelay,
            bool useTaskDelay,
            bool useWorkflowDelay,
            bool? forceRun = null)
        {
            if (!forceRun.HasValue)
            {
                forceRun = RunPerformanceTests;
            }

            if (!forceRun.Value)
            {
                return;
            }

            using (var engine = new WorkflowEngine())
            {
                var argNumber = new Argument<int>("Number");
                var instances = new List<WorkflowInstance>();

                for (var i = 0; i < iterations; i++)
                {
                    var instance = engine.CreateWorkflow<IncrementWorkflow>();
                    instances.Add(instance);
                    await instance.Start(
                        argNumber,
                        new Argument<int>("Delay", _random.Next(maxDelay)),
                        new Argument<bool>("UseTaskDelay", useTaskDelay),
                        new Argument<bool>("UseWorkflowDelay", useWorkflowDelay));
                }

                await Task.WhenAll(instances.Select(i => i.Wait()));

                Assert.IsTrue(instances.All(i => i.IsComplete));
                Assert.AreEqual(iterations, argNumber.Value);
            }
        }

        [TestMethod]
        public async Task PerformanceTestIncrementWorkflowWithoutDelay()
        {
            await RunPerformanceIncrementTest(PerformanceIterations, 0, false, false);
        }

        [TestMethod]
        public async Task PerformanceTestIncrementWorkflowWithDelay()
        {
            // uses thread.sleep, blocks concurrent execution in context - disable test
            await RunPerformanceIncrementTest(PerformanceIterations, MaxDelay, false, false, false);
        }

        [TestMethod]
        public async Task PerformanceTestIncrementWorkflowWithTaskDelay()
        {
            await RunPerformanceIncrementTest(PerformanceIterations, MaxDelay, true, false);
        }

        [TestMethod]
        public async Task PerformanceTestIncrementWorkflowWithWorkflowDelay()
        {
            // uses thread.sleep, blocks concurrent execution in context - disable test
            await RunPerformanceIncrementTest(PerformanceIterations, MaxDelay, false, true, false);
        }

        [TestMethod]
        public async Task PerformanceTestIncrementWorkflowWithTaskAndWorkflowDelay()
        {
            await RunPerformanceIncrementTest(PerformanceIterations, MaxDelay, true, true);
        }

        [TestMethod]
        public async Task PerformanceTestDelayUntilConditionWorkflow()
        {
            using (var engine = new WorkflowEngine())
            {
                var arg = new Argument<bool>("HasCondition");
                var instances = new List<WorkflowInstance>();

                for (var i = 0; i < PerformanceIterations; i++)
                {
                    var instance =
                        engine.CreateWorkflow(new DelayUntilConditionWorkflow(new EmptyWorkflow(), () => arg.Value));
                    instances.Add(instance);
                    await instance.Start(arg);
                }

                var delayInstance = engine.CreateWorkflow(
                    new DelayWorkflow(
                        Workflow.Create(
                            instance =>
                            {
                                instance.GetArgument<bool>("HasCondition").Value = true;
                            }),
                        MaxDelay));
                await delayInstance.Start(arg);

                await Task.WhenAll(instances.Select(i => i.Wait()));
                await delayInstance.Wait();

                Assert.IsTrue(instances.All(i => i.IsComplete));
            }
        }
    }
}