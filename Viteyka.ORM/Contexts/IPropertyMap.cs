using System;

namespace Viteyka.ORM.Contexts
{
    internal interface IPropertyMap
    {
        string Alias { get; }
        string ColumnNameOrFormula { get; set; }
        bool IsPrimaryKey { get; set; }
        bool IsIdentity { get; set; }
        bool IsReadOnly { get; set; }
        bool IsColumnDiffer { get; }
    }

    internal interface IPropertyMap<T> : IPropertyMap
    {
        Action<T, object> Setter { get; set; }
        Func<T, object> Getter { get; set; }
    }
}
