using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Figgle;
using Microsoft.Extensions.Configuration;

namespace servicebus_logger
{
    class Program
    {
        private static string _connectionString;
        private static string _queueName;

        public static async Task Main(string[] args)
        {
            Console.WriteLine(FiggleFonts.Standard.Render("Azure Service Bus"));
            Console.WriteLine(FiggleFonts.Standard.Render("Queue reader"));
            InitializeConfiguration();
            await ReceiveMessagesAsync();
        }

        private static async Task ProcessorOnProcessMessageAsync(ProcessSessionMessageEventArgs arg)
        {
            var body = arg.Message.Body.ToString();
            Console.WriteLine($"Message received: {body}");

            var sequenceNumberFromBody = Convert.ToInt32(Regex.Match(body, @"\d+").Value);
            await arg.CompleteMessageAsync(arg.Message);
        }
        private static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        static async Task ReceiveMessagesAsync()
        {
            await using var client = new ServiceBusClient(_connectionString);
            var processor = client.CreateSessionProcessor(_queueName, new ServiceBusSessionProcessorOptions());
            processor.ProcessMessageAsync += ProcessorOnProcessMessageAsync;
            processor.ProcessErrorAsync += ErrorHandler;
            await processor.StartProcessingAsync();

            Console.WriteLine($"Listening to service bus queue '{_queueName}' for messages ... Press any key to quit");
            Console.ReadKey();

            // stop processing 
            Console.WriteLine("\nStopping the receiver...");
            await processor.StopProcessingAsync();
            Console.WriteLine("Stopped receiving messages");
        }

        private static IConfigurationRoot InitializeConfiguration()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            _connectionString = config["ServiceBusConnectionString"];
            _queueName = config["ServiceBusQueue"];
            return config;
        }
    }
}
