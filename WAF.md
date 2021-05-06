# Well Architected considerations

If you are considering using Azure Durable functions in your solution, here are some of the considerations you can us as input to make your
solution 'well architected' meaning not only considering technical components, but also cost effective, convenient for operations, performant, reliable and secure.

[More information on the Well-Architected framework here](https://docs.microsoft.com/en-us/azure/architecture/framework/)


## Cost optimization

- Consider using different SKUs for Dev/Test/Prod deployment of your Azure function '[hosting](https://docs.microsoft.com/en-us/azure/azure-functions/functions-scale)' options. Consumption is probably very cost effective for non-production like environments, for production you probably want to look at whether there are non-functional requirements that will limit the options. Like network integrations, maximumem execution timeouts, scale limits and perhaps things like whether you require a [managed identity](https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet) for your function app or not.
Even better: clean up non-prod environments after you stop using them and have infra-as-code deploy them as soon as you start using them again.

- Ensure you've not scaled out beyond what you need. Especially the [Elastic Premium](https://docs.microsoft.com/en-us/azure/azure-functions/functions-premium-plan?tabs=portal) plan seemed to perform well for me with regards to scaling. Out-of-the-box it scaled to 20 instances within 10 minutes, and back down to 1 when my load test stopped.

- Same considerations with regards to SKU selection go for the Azure Service Bus selection. For the [sequential processing you need Standard or Premium SKUs](https://azure.microsoft.com/en-us/pricing/details/service-bus/) but there's not need to have that non-production.

## Operational Excellence

- Instrument your functions with [Application Insights](https://docs.microsoft.com/en-us/azure/azure-functions/functions-monitoring). There is no excuse not to be able to have real-time insight in how your function is doing. Portal deployment will help you configure this, the Azure CLI also makes this very easy. I typically send application insights logs to the same workspace as my infrastructure logs - for potential future correlation.

- Use the `TelementryClient` inside your code to have [more options](https://docs.microsoft.com/en-us/azure/azure-monitor/app/api-custom-events-metrics) to write to Application Insights. Things like `TrackDependency`, `TrackEvent` are *very* powerful to decrease time to figure out what's going on in case of a problem.

- **PRO TIP**: Related to Service Bus examples - consider using [correlation](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-end-to-end-tracing?tabs=net-standard-sdk-2) to correlate telemetry over different executions triggered by the Azure Service Bus. Especially in an event-driven, loosly coupled system, it's hard (and can be very powerful) to be able to correlate all kinds of executions happening in different systems, triggered by an event.


```bash
az monitor log-analytics workspace create --resource-group "${prefix}-${project}-${postfix}" --workspace-name "${project}-logs-${postfix}"
az monitor app-insights component create --resource-group "${prefix}-${project}-${postfix}" --location westeurope --app "${project}-appinsights-${postfix}" --kind web --workspace "${project}-logs-${postfix}"
```

- Ensure your infrastructure is sending logs to the beforementioned log analytics workspace. This means configuring the '[diagnostics settings](https://docs.microsoft.com/en-us/azure/azure-monitor/essentials/diagnostic-settings?tabs=CMD)'. E.g.
```bash
az monitor diagnostic-settings create --resource-group "${prefix}-${project}-${postfix}" --name SendToLogAnalytics --resource "${project}-func-${postfix}" --resource-type Microsoft.Web/sites --logs '[{"category":"FunctionAppLogs","Enabled":true}]' --metrics '[{"category":"AllMetrics","Enabled":true}]' --workspace "${project}-logs-${postfix}"
```

- Create a dashboard to show the health of your function. Using the [Application Dashboard](https://docs.microsoft.com/en-us/azure/azure-monitor/app/overview-dashboard) feature of Application Insights is a great starting point.

- Deploy [Azure Alerts](https://docs.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-overview) to notify you when your application is not healthy and so you can respond appropriately. 

- Think of a good naming and tagging strategy before you get started so that problematic resources can be identified quickly. For inspiration, start [here](https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/naming-and-tagging)

- Ensure your infrastructure as well as your configuration is 'as code' and you can deploy programmatically. The potential tiny overhead of longer deployment time does not outweigh the benefits from having everything-as-code and deployed by a non-human.

## Performance Efficiency

- Especially with functions, this means looking into the characteristics of the service plan. Again a reference here to the [Azure Functions hosting options](https://docs.microsoft.com/en-us/azure/azure-functions/functions-scale).

## Reliability

Reliability considerations is typically a tradeoff between money and uptime SLA. Given enough money - you can deploy so many redundant infra components that you can achieve a very high theoretical uptime SLA.

- At the infrastructure or hosting level, look at the specifics of your hosting plan. Have a look at their [SLAs](https://azure.microsoft.com/en-us/support/legal/sla/functions/v1_1/). The consumption plan is at 99.95 - for super business critical apps, consider multiple [App Service Environments of multiple Availabiltit Zones](https://docs.microsoft.com/en-us/azure/app-service/environment/zone-redundancy). This prevent your application from going down in case of a zonal failure.

- In case you want to prepare for a regional outage, you're going to have to deploy multi-region and use services like Traffic Manager or Azure Frontdoor to failover in case of a failure.

At the application level:

- At the function level, I would urge you to get a real good understanding of how durable functions work and their internal queueing mechanisms. A real good starting point is [this video](https://www.youtube.com/watch?v=UQ4iBl7QMno) by [Jeff Hollan](https://www.linkedin.com/in/jeffhollan/). This will make you understand that once an orchestration is triggered, it's in fact queued on Azure Storage. So when something happens during execution (an exception, machine restart) it will retry to finish the orchestration. The same goes for the orchestrator calling activity functions! 
**Do note the impact of this** - you want to make sure things are idempotent as they may be subject to a retry! Also consider APIs that you may be calling or consuming here.

## Security

Read this comprehensive guide on '[Securing Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/security-concepts)'. My top picks:

- Use [managed identities](https://docs.microsoft.com/en-us/samples/azure-samples/functions-storage-managed-identity/using-managed-identity-between-azure-functions-and-azure-storage/) wherever possible.

- In case you do need secretes, you probably want to store them in an Azure KeyVault and [use the Azure Function's managed identity to access the KeyVault's secrets](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references).

- If you're exposing an HTTP API - lock the function behind a gateway and use the Web Application Firewall (WAF) to scan your incoming calls. As suggested [here](https://docs.microsoft.com/en-us/azure/azure-functions/security-concepts).