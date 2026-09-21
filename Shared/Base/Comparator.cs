using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Caching;

namespace D365.Community.Shared.Base
{
    internal static class Comparator
    {
        /// <summary>
        /// Check if attributes have changed between two attribute collections. If the list of attributes is empty, a change is assumed.
        /// </summary>
        /// <param name="attributes">list of attribute names to compare</param>
        /// <param name="aCollection">collection of attributes</param>
        /// <param name="bCollection">collection of attributes</param>
        /// <param name="traces">logger</param>
        /// <returns>returns true if a change is detected, otherwise false.</returns>
        /// <exception cref="InvalidPluginExecutionException"></exception>
        internal static bool IsChanged(string[] attributes, AttributeCollection aCollection, AttributeCollection bCollection, ref List<string> traces)
        {
            var delta = attributes.Length == 0;
            if (delta) traces.Add("WARNING attributes is empty. This may result in an enforced delta!");
            foreach (var attribute in attributes)
            {
                if (!aCollection.ContainsKey(attribute) && !bCollection.ContainsKey(attribute))
                {
                    continue;//both missing, assume equal
                }
                try
                {
                    object aValue;
                    if (!aCollection.ContainsKey(attribute))
                    {
                        traces.Add($"HINT attribute '{attribute}' not in source collection. This may result in an unpredicted delta!");
                        aValue = null;
                    }
                    else
                    {
                        aValue = aCollection[attribute];
                    }
                    object bValue;
                    if (!bCollection.ContainsKey(attribute))
                    {
                        traces.Add($"HINT attribute '{attribute}' not in target collection. This may result in an unpredicted delta!");
                        bValue = null;
                    }
                    else
                    {
                        bValue = bCollection[attribute];
                    }
                    if (aValue == null && bValue == null) continue;//both null, assume equal
                    if ((aValue != null && bValue == null) || aValue == null)
                    {
                        delta = true;
                        break;//one null, assume delta 
                    }
                    if (!IsChanged(aValue, bValue, ref traces)) continue;
                    delta = true;
                    break;//delta 
                }
                catch (Exception e)
                {
                    throw new InvalidPluginExecutionException($"Compare issue; check field '{attribute}'; {e.GetBaseException().Message}!", e.GetBaseException());
                }
            }
            return delta;
        }

        /// <summary>
        /// Check if two attribute values have changed. If the types of the two values are different, an exception is thrown. If the values are of type EntityReference, Entity, or Money, their Id or Value properties are compared. Otherwise, the default equality comparer for the type is used. The result is cached for performance.
        /// </summary>
        /// <param name="aObject">attribute value</param>
        /// <param name="bObject">attribute value</param>
        /// <param name="traces">traces</param>
        /// <returns>returns true if a change is detected, otherwise false.</returns>
        internal static bool IsChanged(object aObject, object bObject, ref List<string> traces)
        {
            if (aObject == null && bObject == null) return false;
            if (aObject != null && bObject == null) return true;
            if (aObject == null) return true;

            if (aObject.GetType() != bObject.GetType()) throw new InvalidPluginExecutionException(OperationStatus.Failed, $"cannot compare different types: {aObject.GetType()} vs. {bObject.GetType()}");

            object aValue;
            object bValue;

            switch (aObject)
            {
                case EntityReference reference:
                    aValue = reference.Id;
                    bValue = ((EntityReference)bObject).Id;
                    break;
                case Entity entity:
                    aValue = entity.Id;
                    bValue = ((Entity)bObject).Id;
                    break;
                case Money money:
                    aValue = money.Value;
                    bValue = ((Money)bObject).Value;
                    break;
                default:
                    aValue = aObject;
                    bValue = bObject;
                    break;
            }

            var cacheKey = $"EqualityComparer#{aValue.GetType()}#{bValue.GetType()}";
            var cacheValue = MemoryCache.Default.Get(cacheKey);
            if (cacheValue != null)
            {
                //traces.Add($"EqualityComparer<{aValue.GetType()}> from cache!");//don't log
                var value = (Tuple<object, MethodInfo>)cacheValue;
                return !(bool)value.Item2.Invoke(value.Item1, [aValue, bValue]);
            }

            var comparerType = typeof(EqualityComparer<>).MakeGenericType(aValue.GetType());
            var comparer = comparerType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var func = comparerType.GetMethod("Equals", [aValue.GetType(), bValue.GetType()]);
            if (comparer != null && func != null)
            {
                MemoryCache.Default.Set(cacheKey, Tuple.Create(comparer, func), new CacheItemPolicy { SlidingExpiration = TimeSpan.FromHours(1) });
                return !(bool)func.Invoke(comparer, [aValue, bValue]);
            }
            traces.Add($"EqualityComparer<{aValue.GetType()}> cannot be resolved!");
            return true;//treat as delta
        }
    }
}
