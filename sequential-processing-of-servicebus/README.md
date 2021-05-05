# Sequential handling of Service Bus Messages

## TL;DR
Ensure the Service Bus queue is configured with sessions, and keep polling the orchestration from the run function until the orchestration finishes.
