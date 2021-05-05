# Sequential handling of Service Bus Messages with Azure Durable Functions

## TL;DR
Ensure the Service Bus queue is configured with sessions, and keep polling the orchestration from the run function until the orchestration finishes. 

[link to relevant code](https://github.com/jochenvw/azure-durable-functions-examples/blob/ffb6162b27b3c3de0223fe00206224e152446107/sequential-processing-of-servicebus/src/servicebus-processor-func/servicebus_processor.cs#L54-L96)

```C#
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
```

## Huh? Service bus guarantees ordered delivery - so what's the problem?

Correct, Azure Service bus can be configured to delivery messages in order they were received (using [sessions](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-sessions)) - so First-In-First-Out (FIFO). Which means a durable function can be triggered in the order the messages were received. Then things become a bit more complicated. Observe for instance this code snippet:

```C#
[FunctionName(nameof(ServiceBusTrigger))]
public async Task ServiceBusTrigger(
    [ServiceBusTrigger(topicName: "topic", subscriptionName: "sub", Connection = "connection")],
    string message
    [DurableClient] IDurableOrchestrationClient starter,
    ILogger log)
{
    var instanceId = await starter
        .StartNewAsync("ImportTransactionOrchestrator", null, msg);
}
```
This is the minimal example of an Azure Service Bus trigger on an entry point of a durable function. What will happen here is the following:
- ServiceBusTrigger binding '[Peeks and Locks](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock)' the message from the Service Bus, meaning that it locks the message on the queue as long as it's being processed. As soon as `MessageReceiver.Complete()` is called on the message, the message is removed from the queue.
- Then, the orchestration is started to handles the message, but this happens in a fire-and-forget way! So it merely enqueues the execution of `ImportTransactionOrchestrator` and that's it.
- The execution of `ServiceBusTrigger` finishes and since there were no errors, `Complete()` is called on the receiver because `autoCompleteMessages` defaults to `true`, unless specified different in the [host.json](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus#additional-settings-for-version-5x)

So messages are being dequeued in quick successsion from the service bus (where order was guaranteed) and executions of the `ImportTransactionOrchestrator` are enqueued in the storage queue on the storage account. **And that's where you potentially lose sequentiality of execution**. Becauase in case of parallel executions - if one orchestration takes longer than the another, the executions will be out of order.

## Singleton execution then?

Although this sounded promising when reading up on it, this approach has a couple of drawbacks. An example implementation of the [singleton](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-singletons?tabs=csharp) looks like this:

```C#
[FunctionName("HttpStartSingle")]
public static async Task<HttpResponseMessage> RunSingle(
    [HttpTrigger(AuthorizationLevel.Function, methods: "post", Route = "orchestrators/{functionName}/{instanceId}")] HttpRequestMessage req,
    [DurableClient] IDurableOrchestrationClient starter,
    string functionName,
    string instanceId,
    ILogger log)
{
    // Check if an instance with the specified ID already exists or an existing one stopped running(completed/failed/terminated).
    var existingInstance = await starter.GetStatusAsync(instanceId);
    if (existingInstance == null 
    || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed 
    || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed 
    || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
    {
        // An instance with the specified ID doesn't exist or an existing one stopped running, create one.
        await starter.StartNewAsync(functionName, instanceId, eventData);
        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        return starter.CreateCheckStatusResponse(req, instanceId);
    }
    else
    {
        // An instance with the specified ID exists or an existing one still running, don't create one.
        return new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent($"An instance with ID '{instanceId}' already exists."),
        };
    }
}
```
This is an HTTP trigger durable function and it checks whether a certain instanceId is already running. If not - it returns an HTTP 409 telling the caller to back off basically because it's executing. 
Were we to use this with a service bus trigger, this would mean the following:
- First message comes in, start the orchestration - trigger function finishes, message is 'completed'.
- Second message comes in, orchestration can't start - message is abandoned.
- Second message comes in again, orchestration can't start - message is abandoned again
- etc.

So a message will be offered multiple times potentially, until the orchestration of the previous one finishes and is then processed. So you have to configure the deadletter queue, think about retry counts etc. Although it may work, it's certainly not an elegant solution.

## Preventing Service Bus from offering a new message, until orchestration of previous message is done

The approach used in this example reads a message from the queue and keeps the trigger function alive until the orchestration is finishes. It's done by polling the `IDurableOrchestrationClient` for the status of the orchestration and waiting until it's in the `OrchestrationRuntimeStatus.Completed` state. Then the trigger function finishes, `autoCompleteMessages` will 'complete()' the message and only then a new trigger function is called.

**Please note:** The following:
- This means keeping the trigger function alive a bit longer than it's intended probably. So running multi-minute orchestrations with this pattern is probably a bad idea.
- It also means that if the orchestration function finishes, there can be a bit of time lost until the next poll from the trigger finds it in `OrchestrationRuntimeStatus.Completed` state. Of course, one can lower the polling interval but this comes at a trade of where the storage account is hit more.

