using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace D365.Community.Shared.Base
{
    internal static class UserSettings
    {
        private const int English = 1033;

        /// <summary>
        /// Get ui language code of the user. If not found, return English (1033)
        /// </summary>
        /// <param name="service"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        internal static int GetUiLanguage(IOrganizationService service, Guid userId)
        {
            var lcid = service.RetrieveMultiple(new QueryExpression("usersettings")
            {
                ColumnSet = new ColumnSet("uilanguageid"),
                NoLock = true,
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                      new ConditionExpression("systemuserid", ConditionOperator.Equal, userId)
                    }
                }
            }).Entities.FirstOrDefault()?.GetAttributeValue<int?>("uilanguageid") ?? English;
            if (lcid < 1000) lcid = English;
            return lcid;
        }
    }
}
