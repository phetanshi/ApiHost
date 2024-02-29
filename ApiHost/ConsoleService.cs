using Microsoft.Extensions.Logging;
using System.Diagnostics;
using k8s;
using k8s.Models;

namespace ApiHost
{
    public class ConsoleService : IConsoleService
    {
        private readonly ILogger<ConsoleService> _logger;

        public ConsoleService(ILogger<ConsoleService> logger)
        {
            this._logger = logger;
        }
        public async Task<string> RunConsoleAsync()
        {
            _logger.LogInformation("Entered into DisplayPodInformation");

            var currentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            var machineName = System.Environment.MachineName;

            string returnVal = $"Pod information podName : machineName : {machineName ?? "It is null"} ; currentDateTime : {currentDateTime}";

            _logger.LogInformation("Creating k8s job...");
            await CreateK8sJob();
            _logger.LogInformation("k8s job created...");

            _logger.LogInformation(returnVal);
            _logger.LogInformation($"Date Time Stamp : {currentDateTime}");

            return returnVal;
        }

        private async Task CreateK8sJob()
        {
            try
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile("./kubedep/config");
                var client = new Kubernetes(config);
                //var namespaces = client.CoreV1.ListNamespace();
                var timeStamp = DateTime.Now.ToString("MMddHHmmss");
                var jobMetadata = new V1ObjectMeta();
                jobMetadata.Name = $"consoleapp-job-{timeStamp}";

                var template = new V1PodTemplateSpec();
                template.Spec = new V1PodSpec();
                template.Spec.RestartPolicy = "Never";

                var container = new V1Container
                {
                    Name = "consoleapp-job-container",
                    Image = "padmasekhar/consoleapp:latest",
                    //ImagePullPolicy = "Always"
                    ImagePullPolicy = "IfNotPresent"
                };
                container.Command = new List<string>();
                container.Command.Add("dotnet");
                container.Command.Add("DockerConsoleTest.dll");

                template.Spec.Containers = new List<V1Container>();
                template.Spec.Containers.Add(container);

                var jobSpec = new V1JobSpec();
                jobSpec.TtlSecondsAfterFinished = 0;
                jobSpec.Template = template;


                var job = new V1Job(metadata: jobMetadata, spec: jobSpec);

                #region CommentedCode
                //var job = new V1Job
                //{
                //    Metadata = new V1ObjectMeta
                //    {
                //        Name = "consoleapp-job",
                //    },
                //    Spec = new V1JobSpec
                //    {
                //        TtlSecondsAfterFinished = 100,
                //        Template = new V1PodTemplateSpec
                //        {
                //            Spec = new V1PodSpec
                //            {
                //                Containers = new[]
                //                {
                //                    new V1Container
                //                    {
                //                        Name = "consoleapp-job-container",
                //                        Image = "padmasekhar/consoleapp:latest",
                //                        ImagePullPolicy = "Always",
                //                        Command = { "dotnet", "DockerConsoleTest.dll" }
                //                    }
                //                },
                //                RestartPolicy = "Never",
                //            }
                //        },
                //        // Add other job configuration like backoffLimit, completions, parallelism, etc. if needed
                //    }
                //};
                #endregion
                
                var createdJob = await client.CreateNamespacedJobAsync(job, "default");
                _logger.LogInformation("Job created successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error creating job: {e.ToString()}");
                throw;
            }
        }
    }
}
