using KitNugs.Configuration;
using KitNugs.Services.Model;
using Microsoft.Extensions.Options;

namespace KitNugs.Services
{
    public class HelloService : IHelloService
    {
        private readonly string _configurationValue;
        private readonly ILogger<HelloService> _logger;

        public HelloService(IOptions<ServiceConfiguration> options, ILogger<HelloService> logger)
        {
            _configurationValue = options.Value.TEST_VAR;
            _logger = logger;
        }

        public async Task<HelloModel> BusinessLogic(string name)
        {
            _logger.LogDebug("Doing business logic.");

            return new HelloModel()
            {
                Name = name,
                Now = DateTime.Now,
                FromConfiguration = _configurationValue,
            };
        }
    }
}
