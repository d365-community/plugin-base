using System;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.Caching;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace D365.Community.Shared.Base
{
    internal static class WebresourceUtil
    {
        /// <summary>
        /// Get the string from resx webresource by key, using the specified name and condition operator
        /// </summary>
        /// <param name="service"></param>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="conditionOperator"></param>
        /// <returns></returns>
        /// <exception cref="InvalidPluginExecutionException"></exception>
        internal static string GetResxString(IOrganizationService service, string name, string key, ConditionOperator conditionOperator = ConditionOperator.Equal)
        {
            var cacheKey = $"Webresource#{name}";
            var cacheValue = MemoryCache.Default.Get(cacheKey);
            string content;
            if (cacheValue != null)
            {
                content = (string)cacheValue;
            }
            else
            {
                var result = service.RetrieveMultiple(new QueryExpression("webresource")
                {
                    NoLock = true,
                    ColumnSet = new ColumnSet("content"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions = {
                          new ConditionExpression("name", conditionOperator, name)
                        }
                    }
                }).Entities.ToList();

                switch (result.Count)
                {
                    case < 1:
                        throw new InvalidPluginExecutionException(OperationStatus.Failed, $"No webresource found with name: ${name}");
                    case > 1:
                        throw new InvalidPluginExecutionException(OperationStatus.Failed, $"Ambiguous webresources found with name: ${name} (operator: ${conditionOperator})");
                    default:
                        content = result.First().GetAttributeValue<string>("content");
                        MemoryCache.Default.Set(cacheKey, content, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(5) });
                        break;
                }
            }

            using var stream = new MemoryStream(Convert.FromBase64String(content));
            using var resx = new ResXResourceSet(stream);
            try
            {
                return resx.GetString(key) ?? key;
            }
            catch
            {
                return key;
            }
        }
    }
}
