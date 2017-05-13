using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Lightwork.Core;
using Lightwork.Core.Utilities;

namespace Lightwork.ServiceApi.Client
{
    public class WorkflowClient : IDisposable
    {
        private readonly HttpClient _client;

        public WorkflowClient(string baseApiAddress)
        {
            _client = new HttpClient();
            BaseApiAddress = baseApiAddress;
            WaitTimeout = 30 * 1000;
            WaitPollingInterval = 500;
        }

        public int WaitTimeout { get; set; }

        public int WaitPollingInterval { get; set; }

        public string BaseApiAddress { get; set; }

        public async Task<GetWorkflowResponseContract> GetAsync(Guid workflowId)
        {
            var url = "Workflow".ToApiUrl(BaseApiAddress, new KeyValuePair<string, object>("id", workflowId));
            var response = await _client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(responseString);
            }

            return JsonHelper.Deserialize<GetWorkflowResponseContract>(responseString);
        }

        public async Task<GetWorkflowResponseContract> GetAsync(GetWorkflowRequestContract request)
        {
            var url = "Workflow/Get".ToApiUrl(BaseApiAddress);
            var response = await _client.PostAsync(url, request.AsJsonContent());
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(responseString);
            }

            return JsonHelper.Deserialize<CreateWorkflowResponseContract>(responseString);
        }

        public async Task<GetWorkflowResponseContract> WaitAsync(Guid workflowId)
        {
            return await WaitAsync(workflowId, c => c.IsComplete);
        }

        public async Task<GetWorkflowResponseContract> WaitActionAsync(Guid workflowId)
        {
            return await WaitAsync(workflowId, c => !c.IsInAction);
        }

        public async Task<GetWorkflowResponseContract> WaitAsync(
            Guid workflowId,
            Func<GetWorkflowResponseContract, bool> onCondition)
        {
            return await WaitAsync(workflowId, WaitTimeout, WaitPollingInterval, onCondition);
        }

        public async Task<GetWorkflowResponseContract> WaitAsync(
            Guid workflowId,
            int timeout,
            int pollingInterval,
            Func<GetWorkflowResponseContract, bool> onCondition)
        {
            var timeoutTime = DateTime.Now.AddMilliseconds(timeout);
            while (DateTime.Now < timeoutTime)
            {
                var responseContract = await GetAsync(workflowId);
                if (onCondition(responseContract))
                {
                    return responseContract;
                }

                await Task.Delay(pollingInterval);
            }

            throw new TimeoutException();
        }

        public async Task<CreateWorkflowResponseContract> CreateAsync(CreateWorkflowRequestContract request)
        {
            var url = "Workflow/Create".ToApiUrl(BaseApiAddress);
            var response = await _client.PostAsync(url, request.AsJsonContent());
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(responseString);
            }

            return JsonHelper.Deserialize<CreateWorkflowResponseContract>(responseString);
        }

        public async Task<CreateWorkflowResponseContract> CreateAsync<TWorkflow>(params ArgumentContract[] arguments)
            where TWorkflow : Workflow
        {
            return await CreateAsync(
                new CreateWorkflowRequestContract
                {
                    WorkflowType = typeof(TWorkflow).AssemblyQualifiedName,
                    Arguments = arguments
                });
        }

        public async Task<StartWorkflowResponseContract> StartAsync(StartWorkflowRequestContract request)
        {
            var url = "Workflow/Start".ToApiUrl(BaseApiAddress);
            var response = await _client.PostAsync(url, request.AsJsonContent());
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(responseString);
            }

            return JsonHelper.Deserialize<StartWorkflowResponseContract>(responseString);
        }

        public async Task<StartWorkflowResponseContract> CreateAsync(
            Guid workflowId,
            params ArgumentContract[] arguments)
        {
            return await StartAsync(
                new StartWorkflowRequestContract
                {
                    WorkflowId = workflowId,
                    Arguments = arguments
                });
        }

        public async Task<ActionWorkflowResponseContract> ActionAsync(ActionWorkflowRequestContract request)
        {
            var url = "Workflow/Action".ToApiUrl(BaseApiAddress);
            var response = await _client.PostAsync(url, request.AsJsonContent());
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(responseString);
            }

            return JsonHelper.Deserialize<ActionWorkflowResponseContract>(responseString);
        }

        public async Task<ActionWorkflowResponseContract> ActionAsync(
            Guid workflowId,
            string action,
            params ArgumentContract[] arguments)
        {
            return await ActionAsync(new ActionWorkflowRequestContract(workflowId, action, arguments));
        }

        public async Task<ActionWorkflowResponseContract> ActionAsync(
            Guid workflowId,
            string action,
            string tag,
            params ArgumentContract[] arguments)
        {
            return await ActionAsync(new ActionWorkflowRequestContract(workflowId, action, tag, arguments));
        }

        public async Task<AllowedActionsResponseContract> GetAllowedActionsAsync(Guid workflowId, string tag = null)
        {
            var url = "Workflow/Actions".ToApiUrl(
                BaseApiAddress,
                new KeyValuePair<string, object>("id", workflowId),
                new KeyValuePair<string, object>("tag", tag));
            var response = await _client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(responseString);
            }

            return JsonHelper.Deserialize<AllowedActionsResponseContract>(responseString);
        }

        public async Task<AllowedActionsResponseContract> GetAllowedActionsAsync(AllowedActionsRequestContract request)
        {
            return await GetAllowedActionsAsync(request.WorkflowId, request.Tag);
        }

        public async Task<GetWorkflowResponseContract> CancelAsync(Guid workflowId)
        {
            var url = "Workflow/Cancel".ToApiUrl(BaseApiAddress, new KeyValuePair<string, object>("id", workflowId));
            var response = await _client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(responseString);
            }

            return JsonHelper.Deserialize<GetWorkflowResponseContract>(responseString);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
            }
        }
    }
}