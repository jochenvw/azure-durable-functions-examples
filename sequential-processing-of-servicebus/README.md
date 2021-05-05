# Sequential handling of Service Bus Messages

## TL;DR
Ensure the Service Bus queue is configured with sessions, and keep polling the orchestration from the run function until the orchestration finishes. see: 

https://github.com/jochenvw/azure-durable-functions-examples/blob/ffb6162b27b3c3de0223fe00206224e152446107/sequential-processing-of-servicebus/src/servicebus-processor-func/servicebus_processor.cs#L54-L96
