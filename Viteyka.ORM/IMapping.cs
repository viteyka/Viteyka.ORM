using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Viteyka.ORM
{
    public interface IMapping<T>
    {
        IMapping<T> DoNotMap<TProperty>(Expression<Func<T, TProperty>> propertySelector);
        IMapping<T> DoMap<TProperty>(Expression<Func<T, TProperty>> propertySelector, string columnNameOrFormula, Action<T, object> valueProvider = null);
        IMapping<T> PrimaryKey<TProperty>(Expression<Func<T, TProperty>> propertySelector);
        IMapping<T> Identity<TProperty>(Expression<Func<T, TProperty>> propertySelector);
        IMapping<T> ReadOnly<TProperty>(Expression<Func<T, TProperty>> propertySelector);
        IMapper<T> Build();
    }
}
