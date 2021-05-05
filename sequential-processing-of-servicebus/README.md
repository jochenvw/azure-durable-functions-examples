# Sequential handling of Service Bus Messages

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
