using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Figgle;
using Microsoft.Extensions.Configuration;

namespace servicebus_loader
{
    class Program
    {
        private static string _connectionString;
        private static string _queueName;
        private static int _messageToQueue;

        /// <summary>
        /// Session ID used while queuing messages - to ensure true FIFO / sequence.
        /// <see cref="https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-sessions"/>
        /// </summary>
        private static Guid sessionId = Guid.NewGuid();

        public static async Task Main(string[] args)
        {
            Console.WriteLine(FiggleFonts.Standard.Render("Azure Service Bus"));
            Console.WriteLine(FiggleFonts.Standard.Render("Queue message sender"));
            var config = InitializeConfiguration();

            while (true)
            {
                Console.WriteLine(
                    $"Press any key to load {_messageToQueue} messages on queue '{_queueName}' ... hit 'q' to exit");
                var input = Console.ReadLine();

                if (input == "q")
                {
                    break;
                }
                else
                {
                    await QueueMessages();
                    Console.WriteLine(
                        $"Placing {_messageToQueue} messages on the service bus queue {_queueName} ... Done !");
                }
            }
        }

        private static IConfigurationRoot InitializeConfiguration()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            _connectionString = config["ServiceBusConnectionString"];
            _queueName = config["ServiceBusQueue"];
            _messageToQueue = Convert.ToInt32(config["NumberOfMessagesToPlaceOnQueue"]);
            return config;
        }

        private static async Task QueueMessages()
        {
            await using var client = new ServiceBusClient(_connectionString);
            var sender = client.CreateSender(_queueName);

            for (var i = 0; i < _messageToQueue; i++)
            {
                var message = new ServiceBusMessage($"This is message {i}") {SessionId = sessionId.ToString()};

                await sender.SendMessageAsync(message);

                if (i % 10 == 0)
                {
                    Console.WriteLine($"Number of messages queued: {i}");
                }
            }
        }
    }
}
