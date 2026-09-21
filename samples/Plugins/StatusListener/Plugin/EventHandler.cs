using System;
using System.Collections.Generic;
using D365.Community.Shared.Base;
using Microsoft.Xrm.Sdk;

namespace D365.Community.StatusListener.Plugin
{
    public class EventHandler : PluginListener
    {
        protected override void Execute(IServiceProvider serviceProvider, IPluginExecutionContext context, ref List<string> traces)
        {
            var operation = GetFullOperation(context);
            switch (operation)
            {
                case "create.pre-operation.synchronous":
                    {

                    }
                    break;
                case "update.pre-operation.synchronous":
                    {

                    }
                    break;
                default:
                    throw new InvalidPluginExecutionException(OperationStatus.Failed, $"Operation '{operation}' is not supported by this plugin.");
            }
        }
    }
}
