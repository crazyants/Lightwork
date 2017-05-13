using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Lightwork.Core;
using Lightwork.Core.Utilities;
using Lightwork.Data;
using Lightwork.ServiceApi;
using Lightwork.ServiceApi.Client;
using Lightwork.Tests.Workflows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lightwork.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private const string BaseApiAddress = "http://localhost:9876/";
        private static WorkflowApiService _apiService;
        private static TestDbContext _dbContext;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            SetupDbContext();
            _apiService = new WorkflowApiService(new[] { BaseApiAddress });
            _apiService.Start();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _apiService.Stop();
            _dbContext.Dispose();
        }

        public async Task TestWorkflowStoreInterfaceSyncState<TStore>(WorkflowEngine<TStore> engine)
            where TStore : IWorkflowStore
        {
            var storeWorkflow = new SequentialWorkflow(
                async instance =>
                {
                    var store = WorkflowStore.TryGetStore(instance);

                    var argCounter = instance.GetArgument<int>("Counter");
                    var argInternalCounter = instance.GetArgument<int>("InternalCounter");

                    var parallelWorkflow = new ParallelWorkflow();

                    parallelWorkflow.Add(
                        Workflow.Create(
                            async internalInstance =>
                            {
                                // only one sync state should make it through for outer instance
                                if (!await store.SyncState("SyncIncrement", instance))
                                {
                                    argCounter.Value += 1;
                                }

                                // both parallel workflows should be able to enter their internal instance sync state
                                if (!await store.SyncState("SyncIncrement", internalInstance))
                                {
                                    argInternalCounter.Value += 1;
                                }
                            }));

                    parallelWorkflow.Add(
                        Workflow.Create(
                            async internalInstance =>
                            {
                                // only one sync state should make it through for outer instance
                                if (!await store.SyncState("SyncIncrement", instance))
                                {
                                    argCounter.Value += 1;
                                }

                                // both parallel workflows should be able to enter their internal instance sync state
                                if (!await store.SyncState("SyncIncrement", internalInstance))
                                {
                                    argInternalCounter.Value += 1;
                                }
                            }));

                    await instance.EnterWorkflow(parallelWorkflow);
                });

            var workflowInstance = engine.CreateWorkflow(storeWorkflow);

            await workflowInstance.Start(new Argument<int>("Counter"), new Argument<int>("InternalCounter"));
            await workflowInstance.Wait();

            Assert.AreEqual(1, workflowInstance.GetArgument<int>("Counter").Value);
            Assert.AreEqual(2, workflowInstance.GetArgument<int>("InternalCounter").Value);
        }

        [TestMethod]
        public async Task IntegrationTestWorkflowDbStoreSyncState()
        {
            using (var engine = new DbStoreWorkflowEngine(true))
            {
                await TestWorkflowStoreInterfaceSyncState(engine);
            }
        }

        [TestMethod]
        public async Task IntegrationTestWorkflowMemoryStoreSyncState()
        {
            using (var engine = new MemoryStoreWorkflowEngine())
            {
                await TestWorkflowStoreInterfaceSyncState(engine);
            }
        }

        [TestMethod]
        public async Task IntegrationTestWorkflowNonStoreFail()
        {
            using (var engine = new WorkflowEngine())
            {
#pragma warning disable 1998
                var storeWorkflow = new SequentialWorkflow(
                    async instance =>
#pragma warning restore 1998
                    {
                        var store = WorkflowStore.TryGetStore(instance);
                        var argStoreLoaded = instance.GetArgument<bool>("StoreLoaded");

                        if (store != null)
                        {
                            argStoreLoaded.Value = true;
                        }
                    });

                var workflowInstance = engine.CreateWorkflow(storeWorkflow);

                await workflowInstance.Start(new Argument<bool>("StoreLoaded"));
                await workflowInstance.Wait();

                Assert.AreEqual(false, workflowInstance.GetArgument<bool>("StoreLoaded").Value);
            }
        }

        [TestMethod]
        public async Task IntegrationTestWorkflowDbStoreLoadFromStore()
        {
            var workflowId = Guid.NewGuid();
            var workflowType = typeof(UpdateArgumentWorkflow);

            var dbStore = new WorkflowDbStore();
            using (var engine = new DbStoreWorkflowEngine(dbStore, true))
            {
                // manually add the create event
                engine.Store.Context.WorkflowSyncEvents.Add(
                    new WorkflowSyncEventEntity
                    {
                        WorkflowId = workflowId,
                        StoreEvent = StoreEvents.CreateEvent,
                        WorkflowType = workflowType.AssemblyQualifiedName,
                        State = JsonHelper.Serialize(new WorkflowStoreState()),
                        DateCreated = DateTime.UtcNow
                    });

                // manually add the start event
                engine.Store.Context.WorkflowSyncEvents.Add(
                    new WorkflowSyncEventEntity
                    {
                        WorkflowId = workflowId,
                        StoreEvent = StoreEvents.StartEvent,
                        WorkflowType = workflowType.AssemblyQualifiedName,
                        State = JsonHelper.Serialize(
                            new WorkflowStoreState
                            {
                                Arguments = new Dictionary<string, object> { { "TestArg", string.Empty } }
                            }),
                        DateCreated = DateTime.UtcNow
                    });

                await engine.Store.Context.SaveChangesAsync();

                // the manually added events shouldn't be loaded yet
                var instance = engine.GetWorkflow(workflowId);
                Assert.IsNull(instance);
            }

            // restart the engine without resetting the context so it will load the workflow
            using (var engine = new DbStoreWorkflowEngine(dbStore))
            {
                await dbStore.LoadFromStore();

                var instance = engine.GetWorkflow(workflowId);
                Assert.IsNotNull(instance);
                Assert.IsTrue(instance.IsStarted);

                await instance.Wait();
                Assert.AreEqual("Updated argument", instance.GetArgument<string>("TestArg").Value);
            }

            dbStore.Dispose();
        }

        [TestMethod]
        public async Task IntegrationTestWorkflowDbStoreLoadFromStoreCreateOnly()
        {
            var workflowId = Guid.NewGuid();
            var workflowType = typeof(UpdateArgumentWorkflow);

            var dbStore = new WorkflowDbStore();
            using (var engine = new DbStoreWorkflowEngine(dbStore, true))
            {
                // manually add the create event
                engine.Store.Context.WorkflowSyncEvents.Add(
                    new WorkflowSyncEventEntity
                    {
                        WorkflowId = workflowId,
                        StoreEvent = StoreEvents.CreateEvent,
                        WorkflowType = workflowType.AssemblyQualifiedName,
                        State = JsonHelper.Serialize(new WorkflowStoreState()),
                        DateCreated = DateTime.UtcNow
                    });

                await engine.Store.Context.SaveChangesAsync();

                // the manually added events shouldn't be loaded yet
                var instance = engine.GetWorkflow(workflowId);
                Assert.IsNull(instance);
            }

            // restart the engine without resetting the context so it will load the workflow
            using (var engine = new DbStoreWorkflowEngine(dbStore))
            {
                await dbStore.LoadFromStore();

                var instance = engine.GetWorkflow(workflowId);
                Assert.IsNotNull(instance);
                Assert.IsFalse(instance.IsStarted);
            }

            dbStore.Dispose();
        }

        [TestMethod]
        public async Task IntegrationTestWorkflowDbStoreLoadFromStoreWithSync1()
        {
            var workflowId = Guid.NewGuid();
            var workflowType = typeof(TestLoadFromStoreSyncWorkflow);

            var dbStore = new WorkflowDbStore();
            using (var engine = new DbStoreWorkflowEngine(dbStore, true))
            {
                // manually add the create event
                engine.Store.Context.WorkflowSyncEvents.Add(
                    new WorkflowSyncEventEntity
                    {
                        WorkflowId = workflowId,
                        StoreEvent = StoreEvents.CreateEvent,
                        WorkflowType = workflowType.AssemblyQualifiedName,
                        State = JsonHelper.Serialize(new WorkflowStoreState()),
                        DateCreated = DateTime.UtcNow
                    });

                // manually add the start event
                engine.Store.Context.WorkflowSyncEvents.Add(
                    new WorkflowSyncEventEntity
                    {
                        WorkflowId = workflowId,
                        StoreEvent = StoreEvents.StartEvent,
                        WorkflowType = workflowType.AssemblyQualifiedName,
                        State = JsonHelper.Serialize(
                            new WorkflowStoreState
                            {
                                Arguments = new Dictionary<string, object> { { "InArgument", "Hello World!" } }
                            }),
                        DateCreated = DateTime.UtcNow
                    });

                // manually add the sync state
                engine.Store.Context.WorkflowSyncStates.Add(
                    new WorkflowSyncStateEntity
                    {
                        SyncId = "Sync1",
                        WorkflowId = workflowId,
                        WorkflowType = workflowType.AssemblyQualifiedName,
                        State = JsonHelper.Serialize(new WorkflowStoreState()),
                        DateCreated = DateTime.UtcNow
                    });

                await engine.Store.Context.SaveChangesAsync();

                // the manually added events shouldn't be loaded yet
                var instance = engine.GetWorkflow(workflowId);
                Assert.IsNull(instance);
            }

            // restart the engine without resetting the context so it will load the workflow
            using (var engine = new DbStoreWorkflowEngine(dbStore))
            {
                await dbStore.LoadFromStore();

                var instance = engine.GetWorkflow(workflowId);
                Assert.IsNotNull(instance);

                await instance.Wait();
                Assert.AreEqual(
                    "Check Sync: Has Sync1: Create Sync2: Hello World!",
                    instance.GetArgument<string>("OutArgument").Value);
            }

            dbStore.Dispose();
        }

        [TestMethod]
        public async Task IntegrationTestWorkflowDbStoreLoadFromStoreWithSync2()
        {
            var workflowId = Guid.NewGuid();
            var workflowType = typeof(TestLoadFromStoreSyncWorkflow);

            var dbStore = new WorkflowDbStore();
            using (var engine = new DbStoreWorkflowEngine(dbStore, true))
            {
                // manually add the create event
                engine.Store.Context.WorkflowSyncEvents.Add(
                    new WorkflowSyncEventEntity
                    {
                        WorkflowId = workflowId,
                        StoreEvent = StoreEvents.CreateEvent,
                        WorkflowType = workflowType.AssemblyQualifiedName,
                        State = JsonHelper.Serialize(new WorkflowStoreState()),
                        DateCreated = DateTime.UtcNow
                    });

                // manually add the start event
                engine.Store.Context.WorkflowSyncEvents.Add(
                    new WorkflowSyncEventEntity
                    {
                        WorkflowId = workflowId,
                        StoreEvent = StoreEvents.StartEvent,
                        WorkflowType = workflowType.AssemblyQualifiedName,
                        State = JsonHelper.Serialize(
                            new WorkflowStoreState
                            {
                                Arguments = new Dictionary<string, object> { { "InArgument", "My Argument" } }
                            }),
                        DateCreated = DateTime.UtcNow
                    });

                // manually add the sync state
                engine.Store.Context.WorkflowSyncStates.Add(
                    new WorkflowSyncStateEntity
                    {
                        SyncId = "Sync2",
                        WorkflowId = workflowId,
                        WorkflowType = workflowType.AssemblyQualifiedName,
                        State = JsonHelper.Serialize(new WorkflowStoreState()),
                        DateCreated = DateTime.UtcNow
                    });

                await engine.Store.Context.SaveChangesAsync();

                // the manually added events shouldn't be loaded yet
                var instance = engine.GetWorkflow(workflowId);
                Assert.IsNull(instance);
            }

            // restart the engine without resetting the context so it will load the workflow
            using (var engine = new DbStoreWorkflowEngine(dbStore))
            {
                await dbStore.LoadFromStore();

                var instance = engine.GetWorkflow(workflowId);
                Assert.IsNotNull(instance);

                await instance.Wait();
                Assert.AreEqual(
                    "Check Sync: Does not have Sync1: Has Sync2: My Argument",
                    instance.GetArgument<string>("OutArgument").Value);
            }

            dbStore.Dispose();
        }

        [TestMethod]
        public async Task IntegrationTestApiServiceGetWorkflowNotFound()
        {
            using (var client = new HttpClient())
            {
                var url = "Workflow".ToApiUrl(BaseApiAddress, new KeyValuePair<string, object>("id", Guid.NewGuid()));
                var response = await client.GetAsync(url);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [TestMethod]
        public async Task IntegrationTestApiServiceGetWorkflow()
        {
            var engine = WorkflowApiService.WorkflowEngine;

            var workflowInstance = engine.CreateWorkflow(
                Workflow.Create(
                    instance =>
                    {
                        instance.GetArgument<string>("Message").Value = "Hello World!";
                    }));

            await workflowInstance.Start(new Argument<string>("Message"));
            await workflowInstance.Wait();

            using (var client = new HttpClient())
            {
                var url = "Workflow".ToApiUrl(
                    BaseApiAddress,
                    new KeyValuePair<string, object>("id", workflowInstance.Id));
                var response = await client.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();
                Assert.IsTrue(response.IsSuccessStatusCode);

                var contract = JsonHelper.Deserialize<GetWorkflowResponseContract>(responseString);
                Assert.AreEqual(workflowInstance.Id, contract.WorkflowId);
                Assert.AreEqual("Hello World!", contract.GetArgument<string>("Message"));
            }
        }

        [TestMethod]
        public async Task IntegrationTestApiServiceCreateWorkflow()
        {
            using (var client = new HttpClient())
            {
                var workflowId = Guid.NewGuid();
                var requestContract = new CreateWorkflowRequestContract<UpdateArgumentWorkflow>
                {
                    WorkflowId = workflowId
                };

                var url = "Workflow/Create".ToApiUrl(BaseApiAddress);
                var response = await client.PostAsync(url, requestContract.AsJsonContent());
                var responseString = await response.Content.ReadAsStringAsync();
                Assert.IsTrue(response.IsSuccessStatusCode);

                var contract = JsonHelper.Deserialize<GetWorkflowResponseContract>(responseString);
                Assert.AreEqual(workflowId, contract.WorkflowId);
            }
        }

        [TestMethod]
        public async Task IntegrationTestApiServiceCreateWorkflowWithArguments()
        {
            using (var client = new HttpClient())
            {
                var workflowId = Guid.NewGuid();
                var requestContract = new CreateWorkflowRequestContract<UpdateArgumentWorkflow>(
                    ArgumentContract.Create("TestArg", "Initial message"),
                    ArgumentContract.Create("SleepTime", 1000))
                {
                    WorkflowId = workflowId
                };

                var url = "Workflow/Create".ToApiUrl(BaseApiAddress);
                var response = await client.PostAsync(url, requestContract.AsJsonContent());
                var responseString = await response.Content.ReadAsStringAsync();
                Assert.IsTrue(response.IsSuccessStatusCode);

                var contract = JsonHelper.Deserialize<CreateWorkflowResponseContract>(responseString);
                Assert.AreEqual(workflowId, contract.WorkflowId);

                // try getting workflow until it's complete
                var getUrl = "Workflow".ToApiUrl(BaseApiAddress, new KeyValuePair<string, object>("id", workflowId));

                GetWorkflowResponseContract getContract;
                var triesLeft = 100;
                do
                {
                    await Task.Delay(250);
                    var getResponse = await client.GetAsync(getUrl);
                    var getResponseString = await getResponse.Content.ReadAsStringAsync();
                    getContract = JsonHelper.Deserialize<GetWorkflowResponseContract>(getResponseString);
                }
                while (--triesLeft > 0 && !getContract.IsComplete);

                Assert.IsTrue(triesLeft > 0);
                Assert.AreEqual("Initial message", getContract.GetArgument<string>("OriginalArg"));
                Assert.AreEqual("Updated argument", getContract.GetArgument<string>("TestArg"));
            }
        }

        [TestMethod]
        public async Task IntegrationTestApiServiceCreateWorkflowWithArguments10Times()
        {
            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(IntegrationTestApiServiceCreateWorkflowWithArguments());
            }

            await Task.WhenAll(tasks);
        }

        [TestMethod]
        public async Task IntegrationTestApiServiceWorkflowClientCreateAndAction()
        {
            using (var client = new WorkflowClient(BaseApiAddress))
            {
                var createRequest =
                    new CreateWorkflowRequestContract<PhoneCallWorkflow>(ArgumentContract.Create("CanDial", true));

                var createResponse = await client.CreateAsync(createRequest);
                var workflowId = createResponse.WorkflowId;

                await client.ActionAsync(new ActionWorkflowRequestContract(workflowId, "Call Dialed"));

                await client.ActionAsync(new ActionWorkflowRequestContract(workflowId, "No Answer"));

                var waitResponse = await client.WaitAsync(workflowId);

                Assert.AreEqual(
                    PhoneStates.Disconnected,
                    TypeHelper.ChangeType<PhoneStates>(waitResponse.WorkflowState));
                Assert.AreEqual("Hello World!", waitResponse.GetArgument<string>("Message"));
            }
        }
        
        [TestMethod]
        public async Task IntegrationTestApiServiceWorkflowClientCreateAndGetActions()
        {
            using (var client = new WorkflowClient(BaseApiAddress))
            {
                var createRequest =
                    new CreateWorkflowRequestContract<PhoneCallWorkflow>(ArgumentContract.Create("CanDial", true));

                var createResponse = await client.CreateAsync(createRequest);
                var workflowId = createResponse.WorkflowId;

                var offHookActions = (await client.GetAllowedActionsAsync(workflowId)).Actions;

                CollectionAssert.AreEqual(new[] { "Call Dialed" }, offHookActions.ToArray());

                await client.ActionAsync(new ActionWorkflowRequestContract(workflowId, "Call Dialed"));

                var waitActionResponse = await client.WaitActionAsync(workflowId);

                Assert.AreEqual(
                    PhoneStates.Ringing,
                    TypeHelper.ChangeType<PhoneStates>(waitActionResponse.WorkflowState));

                var ringingActions = (await client.GetAllowedActionsAsync(workflowId)).Actions;

                CollectionAssert.AreEqual(new[] { "Busy", "No Answer", "Connected" }, ringingActions.ToArray());

                await client.ActionAsync(new ActionWorkflowRequestContract(workflowId, "No Answer"));

                var waitResponse = await client.WaitAsync(workflowId);

                Assert.AreEqual(
                    PhoneStates.Disconnected,
                    TypeHelper.ChangeType<PhoneStates>(waitResponse.WorkflowState));
                Assert.AreEqual("Hello World!", waitResponse.GetArgument<string>("Message"));
            }
        }

        [TestMethod]
        public async Task IntegrationTestHttpRequestWorkflowGetUrl()
        {
            using (var engine = new WorkflowEngine())
            {
                var responseSuccess = false;
                var workflow = new HttpRequestWorkflow(
                    "http://www.google.com",
                    response =>
                    {
                        responseSuccess = response.IsSuccessStatusCode;
                    });

                var instance = engine.CreateWorkflow(workflow);
                await instance.Start();
                await instance.Wait();

                Assert.IsTrue(responseSuccess);
            }
        }

        [TestMethod]
        public async Task IntegrationTestDbContextWorkflow()
        {
            var testGuid = Guid.NewGuid();
            using (var engine = new WorkflowEngine())
            {
                var instance1 = engine.CreateWorkflow(CreateWriteDbContextWorkflow());
                var instance2 = engine.CreateWorkflow(CreateWriteDbContextWorkflow());

                await instance1.Start(
                    new Argument<string>("StringArgument", "Instance 1 String: " + testGuid),
                    new Argument<int>("IntArgument", 101),
                    new Argument<string>("ReturnValue"));

                await instance2.Start(
                    new Argument<string>("StringArgument", "Instance 2 String: " + testGuid),
                    new Argument<int>("IntArgument", 303),
                    new Argument<string>("ReturnValue"));

                await instance1.Wait();
                await instance2.Wait();

                Assert.AreEqual("Complete", instance1.GetArgument<string>("ReturnValue").Value);
                Assert.AreEqual("Complete", instance2.GetArgument<string>("ReturnValue").Value);

                await AssertReadDbContextWorkflow(engine, instance1);
                await AssertReadDbContextWorkflow(engine, instance2);
            }
        }

        [TestMethod]
        public async Task IntegrationTestDbContextWorkflow10Times()
        {
            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(IntegrationTestDbContextWorkflow());
            }

            await Task.WhenAll(tasks);
        }

        private static void SetupDbContext()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<TestDbContext>());
            _dbContext = new TestDbContext();
            _dbContext.Database.Initialize(true);
        }
        
        private async Task AssertReadDbContextWorkflow(WorkflowEngine engine, WorkflowInstance dbInstance)
        {
            var isComplete = false;
            var workflow = new DbContextWorkflow<TestDbContext>(
                _dbContext,
                async context =>
                {
                    var argString = dbInstance.GetArgument<string>("StringArgument").Value;
                    var argInt = dbInstance.GetArgument<int>("IntArgument").Value;
                    var row1 = await context.TestTable.SingleAsync(x => x.StringColumn1 == argString);
                    Assert.AreEqual(argInt, row1.IntColumn1);
                    isComplete = true;
                });

            var readInstance = engine.CreateWorkflow(workflow);
            await readInstance.Start();
            await readInstance.Wait();

            Assert.IsTrue(isComplete);
        }

        private Workflow CreateWriteDbContextWorkflow()
        {
            return Workflow.Create(
                async instance =>
                {
                    await instance.EnterWorkflow(
                        new DbContextWorkflow<TestDbContext>(
                            _dbContext,
                            async context =>
                            {
                                context.TestTable.Add(
                                    new TestTable
                                    {
                                        StringColumn1 = instance.GetArgument<string>("StringArgument").Value,
                                        IntColumn1 = instance.GetArgument<int>("IntArgument").Value
                                    });

                                await context.SaveChangesAsync();
                            }));

                    instance.GetArgument<string>("ReturnValue").Value = "Complete";
                });
        }
    }
}