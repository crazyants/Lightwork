using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Web.Http;
using Lightwork.Core;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Newtonsoft.Json;
using Owin;

namespace Lightwork.ServiceApi
{
    public class WorkflowApiService
    {
        private readonly StartOptions _startOptions;

        private IDisposable _server;

        public WorkflowApiService(IEnumerable<string> baseAddresses)
        {
            ConfigureUnity();
            LoadedAssemblies = new List<Assembly>();

            _startOptions = new StartOptions();
            foreach (var baseAddress in baseAddresses)
            {
                _startOptions.Urls.Add(baseAddress);
            }
        }

        public static WorkflowEngine WorkflowEngine { get; private set; }

        public static IList<Assembly> LoadedAssemblies { get; private set; }

        public static IUnityContainer UnityContainer { get; set; }

        public static void RegisterAssembly(string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            LoadedAssemblies.Add(assembly);
        }

        public static Type TryGetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in LoadedAssemblies)
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    break;
                }
            }

            return type;
        }

        public void ConfigureUnity()
        {
            UnityContainer = new UnityContainer();
            var config = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            if (config != null)
            {
                config.Configure(UnityContainer);
                return;
            }

            UnityContainer.RegisterType<WorkflowEngine, WorkflowEngine>();
        }

        public void Start()
        {
            _server = WebApp.Start<Startup>(_startOptions);
            WorkflowEngine = UnityContainer.Resolve<WorkflowEngine>();
        }

        public void Stop()
        {
            if (_server != null)
            {
                WorkflowEngine.Dispose();
                _server.Dispose();
            }
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();

            // Enable attribute based routing
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                "DefaultApi",
                "{controller}/{id}",
                new { id = RouteParameter.Optional });

            config.Formatters.JsonFormatter.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;

            appBuilder.UseWebApi(config);
        }
    }
}