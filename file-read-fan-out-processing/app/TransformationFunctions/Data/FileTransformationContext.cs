using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TransformationFunctions.Data.Model;

namespace TransformationFunctions.Data
{
    public interface IFileTransformationContext
    {
    }

    public class FileTransformationContext : DbContext, IFileTransformationContext
    {
        public DbSet<Log> Log { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var sqlConnectionString = Environment.GetEnvironmentVariable("SQLDBConnectionString");
            var isRunningLocally = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                .Contains("UseDevelopmentStorage=true");

            if (!isRunningLocally)
            {
                optionsBuilder.AddInterceptors(new AADInterceptor());
            }

            optionsBuilder.UseSqlServer(sqlConnectionString);
        }
    }

    public class AADInterceptor : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            var sqlConnection = (SqlConnection)connection;
            sqlConnection.AccessToken = new AzureServiceTokenProvider()
                .GetAccessTokenAsync("https://database.windows.net/").Result;
            return base.ConnectionOpening(connection, eventData, result);
        }
    }
}