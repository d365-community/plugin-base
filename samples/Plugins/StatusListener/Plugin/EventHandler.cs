using D365.Community.Model;
using D365.Community.Shared.Base;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace D365.Community.StatusListener.Plugin
{
    public class EventHandler : PluginListener
    {
        protected override void Execute(IServiceProvider serviceProvider, IPluginExecutionContext context, ref List<string> traces)
        {
            if (!GetTarget<Account>(context, out var target)) return;

            Account entity;

            var operation = GetFullOperation(context);
            switch (operation)
            {
                case "create.pre-operation.synchronous":
                    {
                        entity = target;
                    }
                    break;
                case "update.pre-operation.synchronous":
                    {
                        if (!GetPreImage<Account>(context, out var preImage)) return;

                        //no touch updates!
                        if (!Comparator.IsChanged(preImage.StatusCode, target.StatusCode, ref traces)) return;

                        //merge to get additional values from pre-image
                        entity = EntityUtil.Merge(preImage, target);
                    }
                    break;
                default:
                    throw new InvalidPluginExecutionException(OperationStatus.Failed, $"Operation '{operation}' is not supported by this plugin.");
            }

            if (entity.PrimaryContactId == null) return;

            //[...] the logic to handle the event when the PrimaryContactId is not null would go here. This could involve updating related records, sending notifications, or any other business logic that needs to be executed when the event is triggered.
        }
    }
}
