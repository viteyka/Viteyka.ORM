using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using Viteyka.ORM.Contexts;
using Viteyka.ORM.Builders;
using Viteyka.ORM.Helpers;

namespace Viteyka.ORM
{
    public class Mapping<T> : IMapping<T>, IMapper<T>, IClassMap<T>
    {
        private Func<T> _new;
        private Dictionary<string, PropertyMap<T>> _propertyMappings;
        private string _tableName;
        private string _alias;

        #region IClassMap members

        Type IClassMap.GenericType { get { return typeof(T); } }
        string IClassMap.TableName { get { return _tableName; } }
        string IClassMap.TableAlias { get { return _alias; } }
        IEnumerable<IPropertyMap> IClassMap.Properties { get { return _propertyMappings.Values; } }
        IEnumerable<IPropertyMap<T>> IClassMap<T>.Properties { get { return _propertyMappings.Values; } }

        #endregion

        public static IMapping<T> Create(string tableName, string alias = null, Func<T> constructor = null)
        {
            return new Mapping<T>(tableName, alias, constructor ?? Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile());
        }

        #region Constructor

        /// <summary>
        /// Creates mapping between T and database's table <see cref="tableName"/>
        /// </summary>
        /// <param name="constructor">delegate to create instance of T</param>
        /// <param name="tableName">Table's name in database</param>
        /// <param name="alias">alias to use in queries</param>
        private Mapping(string tableName, string alias, Func<T> constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException("constructor");
            if (tableName == null)
                throw new ArgumentNullException("tableName");

            _new = constructor;
            _tableName = tableName;
            _alias = alias;
            var type = typeof(T);
            var properties = type.GetProperties();
            _propertyMappings = new Dictionary<string, PropertyMap<T>>(properties.Length);
            foreach (var prop in properties)
                _propertyMappings.Add(prop.Name, new PropertyMap<T>(prop));
        }

        #endregion

        #region IMapping<T> members

        IMapping<T> IMapping<T>.DoNotMap<TProperty>(Expression<Func<T, TProperty>> propertySelector)
        {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");

            var propName = MemberHelper.GetName(propertySelector);
            _propertyMappings.Remove(propName);
            return this;
        }

        IMapping<T> IMapping<T>.DoMap<TProperty>(Expression<Func<T, TProperty>> propertySelector, string columnNameOrFormula, Action<T, object> valueProvider = null)
        {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");

            var prop = MemberHelper.GetProperty(propertySelector);
            var propMap = new PropertyMap<T>(prop);
            propMap.ColumnNameOrFormula = columnNameOrFormula;
            if (valueProvider != null)
            {
                propMap.Setter = valueProvider;
            }
            _propertyMappings[prop.Name] = propMap;
            return this;
        }

        IMapping<T> IMapping<T>.PrimaryKey<TProperty>(Expression<Func<T, TProperty>> propertySelector)
        {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");

            var propName = MemberHelper.GetName(propertySelector);
            _propertyMappings[propName].IsPrimaryKey = true;
            return this;
        }

        IMapping<T> IMapping<T>.Identity<TProperty>(Expression<Func<T, TProperty>> propertySelector)
        {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");

            var propName = MemberHelper.GetName(propertySelector);
            if (_propertyMappings.Values.Any(it => it.Alias != propName && it.IsIdentity))
                throw new InvalidOperationException("Identity field already mapped.");
            _propertyMappings[propName].IsIdentity = true;
            return this;
        }

        IMapping<T> IMapping<T>.ReadOnly<TProperty>(Expression<Func<T, TProperty>> propertySelector)
        {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");

            var propName = MemberHelper.GetName(propertySelector);
            _propertyMappings[propName].IsReadOnly = true;
            return this;
        }

        IMapper<T> IMapping<T>.Build()
        {
            return this;
        }

        #endregion

        #region IMapper<T> members

        T IMapper<T>.GetById(IDbConnection connection, object id)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            var primaryKeyMaps = _propertyMappings.Values.Where(it => it.IsPrimaryKey).ToList();
            if (primaryKeyMaps.Count != 1)
                throw new InvalidOperationException("This method supports only 1 primary key mapping.");

            var p1 = Expression.Parameter(typeof(T), "it");
            var predicate = Expression.Lambda<Func<T, bool>>(Expression.Equal(Expression.Property(p1, primaryKeyMaps[0].Alias), Expression.Constant(id)), p1);
            return ((IMapper<T>)this).Where(connection, predicate).FirstOrDefault();
        }

        IEnumerable<T> IMapper<T>.All(IDbConnection connection)
        {
            using (var command = CommandBuilder.BuildSelect(connection, this))
            using (var reader = command.ExecuteReader())
                return this.Map(reader).ToList();
        }

        IEnumerable<T> IMapper<T>.Where(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            using (var command = CommandBuilder.BuildSelect(connection, this, predicate))
            using (var reader = command.ExecuteReader())
                return this.Map(reader).ToList();
        }

        IEnumerable<T> IMapper<T>.Where<TProperty>(IDbConnection connection, Expression<Func<T, bool>> predicate,
            Expression<Func<T, TProperty>> orderProperty, int from, int to, bool desc = false)
        {
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            if (orderProperty == null)
                throw new ArgumentNullException("orderProperty");

            using (var command = CommandBuilder.BuildSelect(connection, this, predicate, new PageFilter<T, TProperty>
            {
                OrderProperty = orderProperty,
                From = from,
                To = to,
                Desc = desc
            }))
            using (var reader = command.ExecuteReader())
                return this.Map(reader).ToList();
        }

        IEnumerable<T> IMapper<T>.Join<T1>(IDbConnection connection, IMapper<T1> joined, Expression<Func<T, T1, bool>> predicate)
        {
            using (var command = CommandBuilder.BuildJoin(connection, this, (IClassMap<T1>)joined, predicate))
            using (var reader = command.ExecuteReader())
                return this.Map(reader).ToList();
        }

        IEnumerable<T> IMapper<T>.Join<T1, TProperty>(IDbConnection connection, IMapper<T1> joined, Expression<Func<T, T1, bool>> predicate,
            Expression<Func<T, TProperty>> orderProperty, int from, int to, bool desc = false)
        {
            if (orderProperty == null)
                throw new ArgumentNullException("orderProperty");

            using (var command = CommandBuilder.BuildJoin(connection, this, (IClassMap<T1>)joined, predicate, new PageFilter<T, TProperty>
            {
                OrderProperty = orderProperty,
                From = from,
                To = to,
                Desc = desc
            }))
            using (var reader = command.ExecuteReader())
                return this.Map(reader).ToList();
        }

        IEnumerable<T> IMapper<T>.ExecProc(IDbConnection connection, string procName, object parameters)
        {
            using (var command = CommandBuilder.BuildExecProc(connection, this, procName, parameters))
            using (var reader = command.ExecuteReader())
                return this.Map(reader).ToList();
        }

        int IMapper<T>.Count(IDbConnection connection)
        {
            using (var command = CommandBuilder.BuildCount(connection, this))
                return (int)command.ExecuteScalar();
        }

        int IMapper<T>.Count(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            using (var command = CommandBuilder.BuildCount(connection, this, predicate))
                return (int)command.ExecuteScalar();
        }

        double IMapper<T>.Sum<TProperty>(IDbConnection connection, Expression<Func<T, TProperty>> property)
        {
            using (var command = CommandBuilder.BuildSum(connection, this, property))
                return Convert.ToDouble(command.ExecuteScalar());
        }

        double IMapper<T>.Sum<TProperty>(IDbConnection connection, Expression<Func<T, TProperty>> property, Expression<Func<T, bool>> predicate)
        {
            using (var command = CommandBuilder.BuildSum(connection, this, property, predicate))
                return Convert.ToDouble(command.ExecuteScalar());
        }

        void IMapper<T>.Insert(IDbConnection connection, T item)
        {
            using (var command = CommandBuilder.BuildInsert(connection, this, item))
            using (var tran = connection.BeginTransaction())
            {
                command.Transaction = tran;
                var identityColumnMap = _propertyMappings.Values.FirstOrDefault(it => it.IsIdentity);
                if (identityColumnMap != null && identityColumnMap.Setter != null)
                {
                    command.CommandText += " select CAST(scope_identity() as INT)";
                    var id = command.ExecuteScalar();
                    identityColumnMap.Setter(item, id);
                }
                else
                {
                    command.ExecuteNonQuery();
                }
                tran.Commit();
            }
        }

        void IMapper<T>.Update(IDbConnection connection, T item)
        {
            using (var command = CommandBuilder.BuildUpdate(connection, this, item))
            {
                var cnt = command.ExecuteNonQuery();
                if (cnt != 1)
                    throw new InvalidOperationException("Operation must affect 1 row.");
            }
        }

        void IMapper<T>.Delete(IDbConnection connection, T item)
        {
            using (var command = CommandBuilder.BuildDelete(connection, this, item))
            {
                var cnt = command.ExecuteNonQuery();
                if (cnt != 1)
                    throw new InvalidOperationException("Operation must affect 1 row.");
            }
        }

        #endregion

        #region private methods

        private IEnumerable<T> Map(IDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            while (reader.Read())
            {
                var inst = _new();
                PropertyMap<T> propertyMapping;
                for (var i = 0; i < reader.FieldCount; i++)
                    if (!reader.IsDBNull(i))
                    {
                        if (_propertyMappings.TryGetValue(reader.GetName(i), out propertyMapping)
                            && propertyMapping.Setter != null)
                        {
                            propertyMapping.Setter(inst, reader.GetValue(i));
                        }
                    }
                yield return inst;
            }
        }

        #endregion
    }
}
