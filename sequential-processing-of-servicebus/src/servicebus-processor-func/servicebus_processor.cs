using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace servicebus_processor_func
{
    public static class servicebus_processor
    {
        private static readonly int PollingInterval =
            Convert.ToInt32(Environment.GetEnvironmentVariable("OrchestrationStatePollingInterval"));

        private static readonly bool InjectErrors = 
            Convert.ToBoolean(Environment.GetEnvironmentVariable("InjectRandomErrors"));

        private static readonly int TaskExecutionDuration =
            Convert.ToInt32(Environment.GetEnvironmentVariable("TaskExecutionDurationInMs"));

        /// <summary>
        /// Runs the orchestration - in this particular case a very simple one:
        /// - A simulated activity on the message called 'HandleMessage'
        /// - Queueing of the result on a Service Bus queue
        /// </summary>
        [FunctionName("servicebus_processor")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var msg = context.GetInput<string>();
            log.LogInformation($"Processing message {msg} ...");

            await context.CallActivityAsync("HandleMessage", msg);
            await context.CallActivityAsync("QueueMessage", msg);

            log.LogInformation($"Processing message {msg} ... done!");
        }

        /// <summary>
        /// Queues a message on an Azure Service Bus queue
        /// </summary>
        /// <param name="msg">Message that will wrapped and used as message body</param>
        [FunctionName("QueueMessage")]
        public static async Task QueueMessage([ActivityTrigger] string msg, ILogger log)
        {
            await using var client = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnectionOut"));
            var sender = client.CreateSender(Environment.GetEnvironmentVariable("ServiceBusQueueToWriteTo"));
            var message = new ServiceBusMessage($"Message handled with content '{msg}'") { SessionId = "SingleSession" };
            await sender.SendMessageAsync(message);
        }

        /// <summary>
        /// Simulation of a process that 'handles' the message. Could be transform its content, perform API calls, store in a database.
        /// In order to simulate a real-world scenario, two things to notice:
        /// - The execution duration can be configured in appsettings.json and on top of that will have a random wait time
        /// - Fault injection can be configured in appsettings.json and will then occur in 20% of the time
        /// </summary>
        [FunctionName("HandleMessage")]
        public static async Task HandleMessage([ActivityTrigger] string msg, ILogger log)
        {
            log.LogInformation($"Processing message {msg}");

            if (InjectErrors && new Random().Next(0, 10) > 8)
            {
                log.LogWarning($"Intermittent error occurred !");
                throw new Exception();
            }

            await Task.Delay(TaskExecutionDuration + new Random().Next(100, 1000));
        }

        /// <summary>
        /// Entry point of the Durable functions. Starts the orchestration function 'RunOrchestrator' async and returns.
        ///
        /// This function binds to an Azure Service Bus and since we're trying to achieve that the next message
        /// is read from the service bus ONLY if the previous message has been fully handled by the orchestration,
        /// this function is kept alive until the orchestration is done.
        ///
        /// See README.md for more details
        /// </summary>
        /// <param name="msg">Incoming message from service bus</param>
        [FunctionName("servicebus_processor_HttpStart")]
        public static async Task Run(
            [ServiceBusTrigger("%ServiceBusQueueToListenTo%", Connection = "ServiceBusConnectionIn", IsSessionsEnabled = true)] string msg,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var instanceId = await starter.StartNewAsync("servicebus_processor", null, msg);

            var isProcessing = true;
            while (isProcessing == true)
            {
                var currentStatus = await starter.GetStatusAsync(instanceId);

                switch (currentStatus.RuntimeStatus)
                {
                    case OrchestrationRuntimeStatus.Pending:
                    case OrchestrationRuntimeStatus.Running:
                        // Pending and running are expected states - so continuing
                        // the polling while keeping this Run function alive - which,
                        // in turn, will hold the lock on the message
                        break;

                    case OrchestrationRuntimeStatus.Completed:
                        // Work done - break out of the loop
                        isProcessing = false;
                        break;

                    case OrchestrationRuntimeStatus.Failed:
                        var ex = new Exception(
                            $"(Hopefully) intermittent error. Orchestrator failed, message will become available on queue again for re-processing.");
                        log.LogError(ex, "Error during orchestration execution");
                        throw ex;

                    default:
                        ex = new Exception(
                            $"Unexpected state of orchestrator. Current state is {currentStatus}, expected Pending or Running");
                        log.LogError(ex, "Error during orchestration execution");
                        throw ex;
                }

                await Task.Delay(PollingInterval);
            }
        }
    }
}