using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using Viteyka.ORM.Contexts;
using Viteyka.ORM.Helpers;

namespace Viteyka.ORM.Builders
{
    internal class CommandBuilder : CommandBuilderBase
    {
        public static IDbCommand BuildSelect<T>(IDbConnection connection, IClassMap<T> classMap)
        {
            var ctx = new QueryContext(classMap);
            var columnList = classMap.Properties.Aggregate(string.Empty, GetColumnListAggregator(ctx.AliasForTable(classMap)));

            return DbCommandDecorator.Create(connection, String.Format(SELECTALL, columnList, classMap.TableName, ctx.AliasForTable(classMap)));
        }

        public static IDbCommand BuildSelect<T>(IDbConnection connection, IClassMap<T> classMap, Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            var ctx = new QueryContext(classMap);
            var columnList = classMap.Properties.Aggregate(string.Empty, GetColumnListAggregator(ctx.AliasForTable(classMap)));

            var command = DbCommandDecorator.Create(connection);
            var visitor = new PredicateVisitor(ctx);
            visitor.Visit(predicate.Body);
            foreach (var pair in visitor.Parameters)
                command.AddParam(pair.Key, pair.Value);
            command.CommandText = String.Format(SELECT, columnList, classMap.TableName, ctx.AliasForTable(classMap), visitor.ToString());
            return command;
        }

        public static IDbCommand BuildSelect<T, TProperty>(IDbConnection connection, IClassMap<T> classMap, Expression<Func<T, bool>> predicate, PageFilter<T, TProperty> pageFilter)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (pageFilter == null)
                throw new ArgumentNullException("pageFilter");

            var ctx = new QueryContext(classMap);
            var filterColumn = ctx.AliasForColumn();
            var columnList = String.Format("row_number() over (order by [{0}] {1}) {2}, {3}",
                    MemberHelper.GetName(pageFilter.OrderProperty), pageFilter.Desc ? "desc" : "asc", filterColumn,
                    classMap.Properties.Aggregate(string.Empty, GetColumnListAggregator(ctx.AliasForTable(classMap))));
            var command = DbCommandDecorator.Create(connection);
            if (predicate != null)
            {
                var visitor = new PredicateVisitor(ctx);
                visitor.Visit(predicate.Body);
                foreach (var pair in visitor.Parameters)
                    command.AddParam(pair.Key, pair.Value);
                command.CommandText = String.Format(SELECT, columnList, classMap.TableName, ctx.AliasForTable(classMap), visitor.ToString());
            }
            else
                command.CommandText = String.Format(SELECTALL, columnList, classMap.TableName, ctx.AliasForTable(classMap));

            var fromParam = command.AddParam(null, pageFilter.From);
            var toParam = command.AddParam(null, pageFilter.To);
            command.CommandText = String.Format("select {4} from ({0}) d where d.[{1}] between {2} and {3} order by d.[{1}] asc",
                command.CommandText, filterColumn, fromParam.ParameterName, toParam.ParameterName,
                classMap.Properties.Aggregate(string.Empty, GetPropListAggregator("d")));
            return command;
        }

        public static IDbCommand BuildJoin<T, T1>(IDbConnection connection, IClassMap<T> classMap, IClassMap<T1> joinMap, Expression<Func<T, T1, bool>> predicate)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (joinMap == null)
                throw new ArgumentNullException("joinMap");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            var ctx = new QueryContext(classMap, joinMap);
            var columnList = classMap.Properties.Aggregate(string.Empty, GetColumnListAggregator(ctx.AliasForTable(classMap)));
            var command = DbCommandDecorator.Create(connection);
            var visitor = new PredicateVisitor(ctx);
            visitor.Visit(predicate.Body);
            foreach (var pair in visitor.Parameters)
                command.AddParam(pair.Key, pair.Value);
            command.CommandText = String.Format(SELECTJOIN, columnList, classMap.TableName, ctx.AliasForTable(classMap), joinMap.TableName, ctx.AliasForTable(joinMap), visitor.ToString());
            return command;
        }

        public static IDbCommand BuildJoin<T, T1, TProperty>(IDbConnection connection, IClassMap<T> classMap, IClassMap<T1> joinMap, Expression<Func<T, T1, bool>> predicate, PageFilter<T, TProperty> pageFilter)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (joinMap == null)
                throw new ArgumentNullException("joinMap");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            var ctx = new QueryContext(classMap, joinMap);
            var filterColumn = ctx.AliasForColumn();
            var columnList = String.Format("row_number() over (order by [{0}] {1}) {2}, {3}",
                    MemberHelper.GetName(pageFilter.OrderProperty), pageFilter.Desc ? "desc" : "asc", filterColumn,
                    classMap.Properties.Aggregate(string.Empty, GetColumnListAggregator(ctx.AliasForTable(classMap))));
            var command = DbCommandDecorator.Create(connection);
            var visitor = new PredicateVisitor(ctx);
            visitor.Visit(predicate.Body);
            foreach (var pair in visitor.Parameters)
                command.AddParam(pair.Key, pair.Value);
            command.CommandText = String.Format(SELECTJOIN, columnList, classMap.TableName, ctx.AliasForTable(classMap), joinMap.TableName, ctx.AliasForTable(joinMap), visitor.ToString());

            var fromParam = command.AddParam(null, pageFilter.From);
            var toParam = command.AddParam(null, pageFilter.To);
            command.CommandText = String.Format("select {4} from ({0}) d where d.[{1}] between {2} and {3} order by d.[{1}] asc",
                command.CommandText, filterColumn, fromParam.ParameterName, toParam.ParameterName,
                classMap.Properties.Aggregate(string.Empty, GetPropListAggregator("d")));
            return command;
        }

        public static IDbCommand BuildExecProc<T>(IDbConnection connection, IClassMap<T> classMap, string procName, object parameters)
        {
            if (procName == null)
                throw new ArgumentNullException("procName");

            var command = DbCommandDecorator.Create(connection, procName, CommandType.StoredProcedure);
            if (parameters != null)
            {
                foreach (var prop in parameters.GetType().GetProperties())
                    if (prop.CanRead)
                        command.AddParam("@" + prop.Name, prop.GetValue(parameters, null));
            }
            return command;
        }

        public static IDbCommand BuildCount<T>(IDbConnection connection, IClassMap<T> classMap)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");

            var ctx = new QueryContext(classMap);
            var command = DbCommandDecorator.Create(connection);
            command.CommandText = String.Format(SELECTALL, "count(1)", classMap.TableName, ctx.AliasForTable(classMap));
            return command;
        }

        public static IDbCommand BuildCount<T>(IDbConnection connection, IClassMap<T> classMap, Expression<Func<T, bool>> predicate)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            var ctx = new QueryContext(classMap);
            var command = DbCommandDecorator.Create(connection);
            var visitor = new PredicateVisitor(ctx);
            visitor.Visit(predicate.Body);
            foreach (var pair in visitor.Parameters)
                command.AddParam(pair.Key, pair.Value);
            command.CommandText = String.Format(SELECT, "count(1)", classMap.TableName, ctx.AliasForTable(classMap), visitor.ToString());
            return command;
        }

        public static IDbCommand BuildSum<T, TProperty>(IDbConnection connection, IClassMap<T> classMap, Expression<Func<T, TProperty>> property)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (property == null)
                throw new ArgumentNullException("property");

            var ctx = new QueryContext(classMap);
            var command = DbCommandDecorator.Create(connection);
            var propertyMap = classMap.Properties.Single(it => it.Alias == MemberHelper.GetName(property));
            var sumStr = String.Format(propertyMap.IsColumnDiffer ? "sum({0})" : "sum({1}.[{0}])", propertyMap.ColumnNameOrFormula, ctx.AliasForTable(classMap));
            command.CommandText = String.Format(SELECTALL, sumStr, classMap.TableName, ctx.AliasForTable(classMap));
            return command;
        }

        public static IDbCommand BuildSum<T, TProperty>(IDbConnection connection, IClassMap<T> classMap, Expression<Func<T, TProperty>> property, Expression<Func<T, bool>> predicate)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (property == null)
                throw new ArgumentNullException("property");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            var ctx = new QueryContext(classMap);
            var command = DbCommandDecorator.Create(connection);
            var visitor = new PredicateVisitor(ctx);
            visitor.Visit(predicate.Body);
            foreach (var pair in visitor.Parameters)
                command.AddParam(pair.Key, pair.Value);
            var propertyMap = classMap.Properties.Single(it => it.Alias == MemberHelper.GetName(property));
            var sumStr = String.Format(propertyMap.IsColumnDiffer ? "sum({0})" : "sum({1}.[{0}])", propertyMap.ColumnNameOrFormula, ctx.AliasForTable(classMap));
            command.CommandText = String.Format(SELECT, sumStr, classMap.TableName, ctx.AliasForTable(classMap), visitor.ToString());
            return command;
        }

        public static IDbCommand BuildInsert<T>(IDbConnection connection, IClassMap<T> classMap, T item)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (item == null)
                throw new ArgumentNullException("item");

            var command = DbCommandDecorator.Create(connection);
            var columnList = classMap.Properties.Where(it => !it.IsIdentity && !it.IsReadOnly)
                .Aggregate(string.Empty, (accum, val) => String.Format("{0}, [{1}]", accum, val.ColumnNameOrFormula)).Trim(',').Trim();
            foreach (var columnMap in classMap.Properties.Where(it => !it.IsIdentity && !it.IsReadOnly))
                command.AddParam(null, GetPropertyValue(item, columnMap));
            var paramList = command.Parameters.OfType<IDbDataParameter>().Aggregate(string.Empty, (accum, val) => String.Format("{0}, {1}", accum, val)).Trim(',').Trim();
            command.CommandText = String.Format(INSERT, classMap.TableName, columnList, paramList);
            return command;
        }

        public static IDbCommand BuildUpdate<T>(IDbConnection connection, IClassMap<T> classMap, T item)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (item == null)
                throw new ArgumentNullException("item");

            if (!classMap.Properties.Any(it => it.IsPrimaryKey))
                throw new InvalidOperationException("Primary key is not configured.");

            var command = DbCommandDecorator.Create(connection);
            var where = new Dictionary<string, string>();
            foreach (var primaryKeyMap in classMap.Properties.Where(it => it.IsPrimaryKey))
            {
                var param = command.AddParam(null, GetPropertyValue(item, primaryKeyMap));
                where.Add(primaryKeyMap.ColumnNameOrFormula, param.ParameterName);
            }
            var set = new Dictionary<string, string>();
            foreach (var columnMap in classMap.Properties.Where(it => !it.IsIdentity && !it.IsReadOnly))
            {
                var param = command.AddParam(null, GetPropertyValue(item, columnMap));
                set.Add(columnMap.ColumnNameOrFormula, param.ParameterName);
            }
            command.CommandText = String.Format(UPDATE, classMap.TableName,
                set.Aggregate(string.Empty, (accum, value) => String.Format("{0}, [{1}] = {2}", accum, value.Key, value.Value)).Trim(',').Trim(),
                where.Aggregate(string.Empty, GetWhereAggregator()));
            return command;
        }

        public static IDbCommand BuildDelete<T>(IDbConnection connection, IClassMap<T> classMap, T item)
        {
            if (classMap == null)
                throw new ArgumentNullException("classMap");
            if (item == null)
                throw new ArgumentNullException("item");

            if (!classMap.Properties.Any(it => it.IsPrimaryKey))
                throw new InvalidOperationException("Primary key is not configured.");

            var command = DbCommandDecorator.Create(connection);
            var where = new Dictionary<string, string>();
            foreach (var primaryKeyMap in classMap.Properties.Where(it => it.IsPrimaryKey))
            {
                var param = command.AddParam(null, GetPropertyValue(item, primaryKeyMap));
                where.Add(primaryKeyMap.ColumnNameOrFormula, param.ParameterName);
            }
            command.CommandText = String.Format(DELETE, classMap.TableName, where.Aggregate(string.Empty, GetWhereAggregator()));
            return command;
        }

        #region Private methods

        private static object GetPropertyValue<T>(T item, IPropertyMap<T> propertyMap)
        {
            if (item != null && propertyMap != null && propertyMap.Getter != null)
                return propertyMap.Getter(item) ?? System.DBNull.Value;
            else
                return System.DBNull.Value;
        }

        #endregion
    }
}
