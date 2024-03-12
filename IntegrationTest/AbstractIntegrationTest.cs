using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace IntegrationTest
{
    public abstract class AbstractIntegrationTest
    {
        protected static readonly ServiceClient client;
        protected static int servicePort = 8080;

        static AbstractIntegrationTest()
        {
            // Create network
            var network = new NetworkBuilder().Build();

            HttpClient? httpClient;
            if (Debugger.IsAttached)
            {
                Environment.SetEnvironmentVariable("TEST_VAR", "TEST_VARIABLE");

                var server = new WebApplicationFactory<Program>().Server;
                httpClient = server.CreateClient();
            }
            else
            {
                BuildAndStartService(network);
                httpClient = new HttpClient();
            }

            httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            client = new ServiceClient(httpClient)
            {
                BaseUrl = $"http://localhost:{servicePort}"
            };
        }

        private static void BuildAndStartService(INetwork network)
        {
            var useExistingImage = Environment.GetEnvironmentVariable("USE_EXISTING_IMAGE") ?? "false";
            if (useExistingImage != "true")
            {
                var image = new ImageFromDockerfileBuilder()
                    .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
                    .WithDockerfile("KitNugs/Dockerfile-fat")
                    .WithName("kvalitetsit/demo-net")
                    .WithCleanUp(false)
                    .Build();

                image.CreateAsync()
                    .Wait();
            }

            var service = new ContainerBuilder()
                .WithImage("kvalitetsit/demo-net:latest")
                .WithPortBinding(8080, true)
                .WithPortBinding(8081, true)
                .WithName("service-qa")
                .WithNetwork(network)
                .WithEnvironment("TEST_VAR", "TEST_VARIABLE")
                .WithEnvironment("ConnectionStrings__db", "server=db-qa,3306;user=hellouser;password=secret1234;database=hellodb")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPath("/healthz").ForPort(8081)))
                .Build();

            service.StartAsync()
                .Wait();

            servicePort = service.GetMappedPublicPort(8080);
        }
    }
}