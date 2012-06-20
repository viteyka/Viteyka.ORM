using System;
using System.Collections.Generic;

namespace Viteyka.ORM.Contexts
{
    internal interface IClassMap
    {
        Type GenericType { get; }
        string TableName { get; }
        string TableAlias { get; }
        IEnumerable<IPropertyMap> Properties { get; }
    }

    internal interface IClassMap<T> : IClassMap
    {
        IEnumerable<IPropertyMap<T>> Properties { get; }
    }
}
