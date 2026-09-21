using System;
using Microsoft.Xrm.Sdk;

namespace D365.Community.Shared.Base
{
    internal static class EntityUtil
    {
        /// <summary>
        /// Merges two entities of the same type into a new entity. The basis entity's attributes are preserved, and the other entity's attributes are added or overwrite the basis entity's attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="basis">use this entity as base for the merge</param>
        /// <param name="other">add/override values from ths entity</param>
        /// <returns></returns>
        /// <exception cref="InvalidPluginExecutionException"></exception>
        internal static T Merge<T>(T basis, T other) where T : Entity
        {
            //simple chacks
            if (basis == null) throw new InvalidPluginExecutionException(OperationStatus.Failed, "Cannot merge null basis");
            if (other == null) throw new InvalidPluginExecutionException(OperationStatus.Failed, "Cannot merge basis with null");
            if (string.Equals(basis.LogicalName, other.LogicalName, StringComparison.InvariantCulture)) throw new InvalidPluginExecutionException(OperationStatus.Failed, $"cannot merge different entities: {basis.LogicalName} vs. {other.LogicalName}");
            //merge
            var merged = new Entity(basis.LogicalName, basis.Id);
            merged.Attributes.AddRange(basis.Attributes);
            foreach (var attribute in other.Attributes)
            {
                merged[attribute.Key] = other.Attributes[attribute.Key];
            }
            return merged.ToEntity<T>();
        }

        /// <summary>
        /// Copy attributes from entity source to entity target. If overwriteMissingValuesOnly is true, only attributes that are missing in the target entity will be copied. If overwriteMissingValuesOnly is false, all attributes from the source entity will be copied to the target entity, overwriting any existing values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="overwriteMissingValuesOnly"></param>
        /// <exception cref="InvalidPluginExecutionException"></exception>
        internal static void CopyTo<T>(this T source, T target, bool overwriteMissingValuesOnly = true) where T : Entity
        {
            //simple chacks
            if (target == null) throw new InvalidPluginExecutionException(OperationStatus.Failed, "Cannot copy to null target");
            if (string.Equals(source.LogicalName, target.LogicalName, StringComparison.InvariantCulture)) throw new InvalidPluginExecutionException(OperationStatus.Failed, $"cannot merge different entities: {source.LogicalName} vs. {target.LogicalName}");
            //copy
            if (source.Id != Guid.Empty && (target.Id == Guid.Empty || !overwriteMissingValuesOnly)) target.Id = source.Id;
            foreach (var attribute in source.Attributes)
            {
                if (!target.Contains(attribute.Key) || !overwriteMissingValuesOnly) target[attribute.Key] = attribute.Value;
            }
        }
    }
}