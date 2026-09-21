using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Runtime.Caching;
using System.Web;

namespace D365.Community.Shared.Base
{
    internal static class UrlUtil
    {
        /// <summary>
        /// Parses a Dynamics 365 URL to extract the entity ID and entity name. If the entity name is not provided in the URL, it retrieves the logical name using the entity type code (etc) from the metadata service.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="url"></param>
        /// <param name="entityId"></param>
        /// <param name="entityName"></param>
        internal static void ParseUrl(IOrganizationService service, string url, out Guid entityId, out string entityName)
        {
            var query = HttpUtility.ParseQueryString(new Uri(url).Query);
            entityId = Guid.Parse(query.Get("id"));
            entityName = query.Get("etn") ?? GetEntityLogicalName(service, query.Get("etc"));
        }

        private static string GetEntityLogicalName(IOrganizationService service, string etc)
        {
            if (string.IsNullOrWhiteSpace(etc)) return null;

            var cacheKey = $"UrlService.etc#{etc}";
            var cacheValue = MemoryCache.Default.Get(cacheKey);
            if (cacheValue != null)
            {
                return (string)cacheValue;
            }

            var query = new EntityQueryExpression
            {
                Criteria = new MetadataFilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new MetadataConditionExpression("ObjectTypeCode", MetadataConditionOperator.Equals, int.Parse(etc))
                    }
                },
                Properties = new MetadataPropertiesExpression
                {
                    AllProperties = false,
                    PropertyNames =
                    {
                        "LogicalName"
                    }
                }
            };
            var response = (RetrieveMetadataChangesResponse)service.Execute(new RetrieveMetadataChangesRequest
            {
                Query = query
            });
            if (response.EntityMetadata.Count != 1) return null;
            var value = response.EntityMetadata[0].LogicalName;
            MemoryCache.Default.Set(cacheKey, value, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(24) });
            return value;
        }
    }
}
