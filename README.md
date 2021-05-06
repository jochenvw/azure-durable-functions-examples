# Azure Durable Functions Examples repository

Repository aimed at collecting some more advanced durable function scenarios.

## Well Architected Solutions

Azure Functions are typically part of a larger systems design. Good practice is to review the design of the overall solution using the `Well-Architected framework`, as described here: [https://docs.microsoft.com/en-us/azure/architecture/framework/](https://docs.microsoft.com/en-us/azure/architecture/framework/).
Input for the solution design, specifically looking at Durable Functions, can be found here: [WAF.md](WAF.md).

## Examples

- **Sequential handling of service bus messages** - not as straight forward as you may think - [link](https://github.com/jochenvw/azure-durable-functions-examples/tree/main/sequential-processing-of-servicebus)



## Reference material

- [Jeff Hollan's talk](https://youtu.be/UQ4iBl7QMno?t=1023) - talks about how the orchestration goes to 'sleep' and uses event sourcing to get back into the state it was
- [Performance and scale in Durable Functions (Azure Functions)](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-perf-and-scale) - talks about numerous performance optimizations that you can do to boost the perf of your durable function. It also provides some insights into what's happening in the underlying storage account and so why batching is a good idea.
- [Manage connections in Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/manage-connections)

## Tools

- [Azure function Core tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=windows%2Ccsharp%2Cbash) - to new up and scaffold functions
- [Azure Storage Explorer](https://azure.microsoft.com/en-us/downloads/) - to explore your storage account, in Azure or emulated
- [Azure Storage Emulator](https://azure.microsoft.com/en-us/downloads/) - to emulate Azure storage locally, must have for local dubugging, so must have :)
- [Visual Studio Code](https://code.visualstudio.com/) with [Azure Extensions](https://code.visualstudio.com/docs/azure/extensions)