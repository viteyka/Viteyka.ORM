using System;
using System.Reflection;
using System.Linq.Expressions;

namespace Viteyka.ORM.Contexts
{
    internal class PropertyMap<T> : IPropertyMap<T>
    {
        public string Alias { get; private set; }
        public string ColumnNameOrFormula { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsColumnDiffer { get { return !Alias.Equals(ColumnNameOrFormula, StringComparison.OrdinalIgnoreCase); } }
        public Action<T, object> Setter { get; set; }
        public Func<T, object> Getter { get; set; }

        internal PropertyMap(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");

            ColumnNameOrFormula = Alias = propertyInfo.Name;

            var type = typeof(T);
            var setter = propertyInfo.GetSetMethod();
            if (setter != null)
            {
                var p1 = Expression.Parameter(type);
                var p2 = Expression.Parameter(typeof(object));
                var bind = Expression.Call(p1, setter, Expression.Convert(p2, propertyInfo.PropertyType));
                Setter = Expression.Lambda<Action<T, object>>(bind, p1, p2).Compile();
            }
            var getter = propertyInfo.GetGetMethod();
            if (getter != null)
            {
                var p1 = Expression.Parameter(type);
                var bind = Expression.Convert(Expression.Property(p1, getter), typeof(object));
                Getter = Expression.Lambda<Func<T, object>>(bind, p1).Compile();
            }
        }
    }
}
