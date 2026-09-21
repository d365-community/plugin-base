using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace D365.Community.Shared.Base
{
    internal static class Translator
    {
        /// <summary>
        /// Get the localized string from resx webresource by code, using the user's ui language
        /// </summary>
        /// <param name="service"></param>
        /// <param name="context"></param>
        /// <param name="resx"></param>
        /// <param name="code"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static string GetByCode(IOrganizationService service, IPluginExecutionContext context, string resx, string code, params object[] args)
        {
            return GetByCode(service, resx, code, UserSettings.GetUiLanguage(service, context.UserId), args);
        }

        /// <summary>
        /// Get the localized string from resx webresource by code, using the specified lcid
        /// </summary>
        /// <param name="service"></param>
        /// <param name="resx"></param>
        /// <param name="code"></param>
        /// <param name="lcid"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static string GetByCode(IOrganizationService service, string resx, string code, int lcid, params object[] args)
        {
            var text = WebresourceUtil.GetResxString(service, $"*/{resx}.{lcid}.resx", code, ConditionOperator.Like);
            if (text.Contains("{0}") && args != null && args.Length > 0)
            {
                return string.Format(text, args);
            }
            return text;
        }
    }
}
