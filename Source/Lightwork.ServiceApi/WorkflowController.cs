using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Lightwork.Core;
using Lightwork.ServiceApi.Client;

#pragma warning disable 4014
#pragma warning disable 1998

namespace Lightwork.ServiceApi
{
    [RoutePrefix("Workflow")]
    public class WorkflowController : ApiController
    {
        private readonly WorkflowEngine _engine;

        public WorkflowController()
        {
            _engine = WorkflowApiService.WorkflowEngine;
        }

        public WorkflowController(WorkflowEngine engine)
        {
            _engine = engine;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetWorkflowAsync(Guid id)
        {
            return await GetWorkflowAsyncCore(id);
        }

        [HttpPost]
        [Route("Get")]
        public async Task<IHttpActionResult> GetWorkflowAsync([FromBody] GetWorkflowRequestContract request)
        {
            return await GetWorkflowAsyncCore(request.WorkflowId);
        }

        [HttpPost]
        [Route("Create")]
        public async Task<IHttpActionResult> CreateWorkflowAsync([FromBody] CreateWorkflowRequestContract request)
        {
            var workflow = WorkflowEngine.CreateWorkflowType(request.WorkflowType, request.Parameters);

            var instance = _engine.CreateWorkflow(workflow, request.WorkflowId);

            if (request.StartImmediately)
            {
                await Task.Run(async () => await instance.Start(request.GetArguments()));
            }

            if (request.WaitOnComplete)
            {
                await instance.Wait();
            }

            var contract = new CreateWorkflowResponseContract(instance);
            return Json(contract);
        }

        [HttpPost]
        [Route("Start")]
        public async Task<IHttpActionResult> StartWorkflowAsync([FromBody] StartWorkflowRequestContract request)
        {
            var instance = _engine.GetWorkflow(request.WorkflowId);
            if (instance == null)
            {
                return NotFound();
            }

            await Task.Run(async () => await instance.Start(request.GetArguments()));

            if (request.WaitOnComplete)
            {
                await instance.Wait();
            }

            var contract = new StartWorkflowResponseContract(instance);
            return Json(contract);
        }

        [HttpPost]
        [Route("Action")]
        public async Task<IHttpActionResult> ActionWorkflowAsync([FromBody] ActionWorkflowRequestContract request)
        {
            var instance = _engine.GetWorkflow(request.WorkflowId);
            if (instance == null)
            {
                return NotFound();
            }

            await Task.Run(async () => await instance.Action(request.Action, request.Tag, !request.WaitOnAction, request.GetArguments()));

            var contract = new ActionWorkflowResponseContract(instance, request.Action, request.Tag);
            return Json(contract);
        }

        [HttpGet]
        [Route("Actions")]
        public async Task<IHttpActionResult> GetAllowedActionsAsync(Guid id, string tag = null)
        {
            var instance = _engine.GetWorkflow(id);
            if (instance == null)
            {
                return NotFound();
            }

            var contract = new AllowedActionsResponseContract(instance, instance.GetAllowedActions(tag).ToList());
            return Json(contract);
        }

        [HttpGet]
        [Route("Cancel")]
        public async Task<IHttpActionResult> CancelAsync(Guid id, string tag = null)
        {
            var instance = _engine.GetWorkflow(id);
            if (instance == null)
            {
                return NotFound();
            }

            instance.Cancel();
            await instance.Wait();

            var contract = new GetWorkflowResponseContract(instance);
            return Json(contract);
        }

        private async Task<IHttpActionResult> GetWorkflowAsyncCore(Guid id)
        {
            var instance = _engine.GetWorkflow(id);
            if (instance == null)
            {
                return NotFound();
            }

            var contract = new GetWorkflowResponseContract(instance);
            return Json(contract);
        }
    }
}