using System.Threading.Tasks;
using D3.Lightwork.Core;
using D3.Lightwork.Data;

namespace D3.Lightwork.Tests.Workflows
{
    public class IntegrationTestWorkflows
    {
    }

    public class TestLoadFromStoreSyncWorkflow : Workflow
    {
        public Argument<string> InArgument { get; set; }

        public Argument<string> OutArgument { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            await instance.WithStoreSync(async storeSync =>
            {
                OutArgument.Value = "Check Sync: ";
                if (await storeSync.HasSync("Sync1"))
                {
                    OutArgument.Value += "Has Sync1: ";
                }
                else
                {
                    OutArgument.Value += "Does not have Sync1: ";
                }

                if (await storeSync.CreateSync("Sync2"))
                {
                    OutArgument.Value += "Create Sync2: ";
                }
                else
                {
                    OutArgument.Value += "Has Sync2: ";
                }
            });

            OutArgument.Value += InArgument.Value;
        }
    }
}
