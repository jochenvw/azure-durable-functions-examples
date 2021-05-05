using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using TransformationFunctions.Data;
using TransformationFunctions.Data.Repositories;

[assembly: FunctionsStartup(typeof(TransformationFunctions.Startup))]

namespace TransformationFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddDbContext<FileTransformationContext>();
            builder.Services.AddTransient<ILogRepository, LogRepository>();
        }
    }
}