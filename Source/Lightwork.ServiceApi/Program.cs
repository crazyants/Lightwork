using Topshelf;

namespace D3.Lightwork.ServiceApi
{
    public class Program
    {
        public static void Main()
        {
            HostFactory.Run(x =>
            {
                x.Service<WorkflowApiService>(s =>
                {
                    s.ConstructUsing(name => new WorkflowApiService(AppSettings.ApiAddresses));
                    s.WhenStarted(api => api.Start());
                    s.WhenStopped(api => api.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Lightwork Service Api");
                x.SetDisplayName("Lightwork.ServiceApi");
                x.SetServiceName("Lightwork.ServiceApi");
            });
        }
    }
}
