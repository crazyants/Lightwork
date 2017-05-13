using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using D3.Lightwork.Core;

namespace D3.Lightwork.ServiceApi.Client
{
    [WorkflowBehaviorOnLoad(BypassWorkflow = true)]
    public class HttpRequestWorkflow : Workflow
    {
        public HttpRequestWorkflow()
        {
        }

        public HttpRequestWorkflow(string requestUrl, Action<HttpResponseMessage> responseAction)
            : this(HttpMethod.Get, requestUrl, responseAction)
        {
        }

        public HttpRequestWorkflow(
            HttpMethod requestMethod,
            string requestUrl,
            Action<HttpResponseMessage> responseAction)
        {
            RequestUrl = requestUrl;
            RequestMethod = requestMethod;
            ResponseAction = responseAction;
        }

        public HttpRequestWorkflow(string requestUrl, object jsonData, Action<HttpResponseMessage> responseAction)
            : this(HttpMethod.Post, requestUrl, responseAction)
        {
            JsonData = jsonData;
        }

        public string RequestUrl { get; set; }

        public ICollection<KeyValuePair<string, object>> RequestParameters { get; set; }

        public object JsonData { get; set; }

        public HttpMethod RequestMethod { get; set; }

        public Action<HttpResponseMessage> ResponseAction { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            using (var client = new HttpClient())
            {
                var url = RequestParameters != null && RequestMethod == HttpMethod.Get
                    ? RequestUrl.ToUrl(RequestParameters.ToArray())
                    : RequestUrl.ToUrl();

                var request = new HttpRequestMessage(RequestMethod, url);

                if (RequestMethod == HttpMethod.Post)
                {
                    if (JsonData != null)
                    {
                        request.Content = JsonData.AsJsonContent();
                    }
                    else if (RequestParameters != null)
                    {
                        request.Content =
                            new FormUrlEncodedContent(
                                RequestParameters.ToDictionary(
                                    k => k.Key,
                                    v => v.Value?.ToString() ?? string.Empty));
                    }
                }

                var response = await client.SendAsync(request);

                ResponseAction?.Invoke(response);
            }
        }
    }
}
