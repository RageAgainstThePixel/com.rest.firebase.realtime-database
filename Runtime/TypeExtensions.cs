// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Firebase.RealtimeDatabase
{
    internal static class TypeExtensions
    {
        private static readonly Type[] collectionTypes =
        {
            typeof(IEnumerable<>),
            typeof(ICollection<>),
            typeof(IList<>),
            typeof(List<>),
        };

        /// <summary>
        /// Is this type a collection?
        /// https://stackoverflow.com/questions/10864611/how-to-determine-if-a-type-is-a-type-of-collection
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True, if the type is a collection.</returns>
        public static bool IsCollection(this Type type)
        {
            if (!type.GetGenericArguments().Any())
            {
                return false;
            }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            return collectionTypes.Any(collectionType => collectionType.IsAssignableFrom(genericTypeDefinition));
        }
    }
}
