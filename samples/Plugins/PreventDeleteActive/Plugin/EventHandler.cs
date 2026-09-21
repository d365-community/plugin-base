using System;
using System.Collections.Generic;
using D365.Community.Model;
using D365.Community.Shared.Base;
using Microsoft.Xrm.Sdk;

namespace D365.Community.PreventDeleteActive.Plugin
{
    public class EventHandler : PluginListener
    {
        protected override void Execute(IServiceProvider serviceProvider, IPluginExecutionContext context, ref List<string> traces)
        {
            if (!GetPreImage<Account>(context, out var preImage)) return;

            var message = GetMessageName(context);
            switch (message)
            {
                case "delete":
                    {
                        if (preImage.StateCode == account_statecode.Active) throw new InvalidPluginExecutionException(OperationStatus.Succeeded, Translator.GetByCode(GetServiceElevated(serviceProvider), context, "PreventDeleteActive", "NotAllowedToDeleteActive"));
                    }
                    break;
                default:
                    throw new InvalidPluginExecutionException(OperationStatus.Failed, $"Message '{message}' is not supported by this plugin.");
            }
        }
    }
}
