using System;
using System.Collections.Generic;
using System.Data;
using Viteyka.ORM.Contexts;

namespace Viteyka.ORM.Builders
{
    internal abstract class CommandBuilderBase
    {

        protected const string SELECTALL = "select {0} from [{1}] {2}";
        protected const string SELECT = "select {0} from [{1}] {2} where {3}";
        protected const string SELECTJOIN = "select {0} from [{1}] {2} inner join [{3}] {4} on {5}";
        protected const string INSERT = "insert into [{0}]({1}) values({2})";
        protected const string UPDATE = "update [{0}] set {1} where {2}";
        protected const string DELETE = "delete from [{0}] where {1}";

        protected static Func<string, IPropertyMap, string> GetColumnListAggregator(string tableAlias)
        {
            return (accum, propertyMap) =>
            {
                var equality = propertyMap.Alias.Equals(propertyMap.ColumnNameOrFormula, StringComparison.OrdinalIgnoreCase);
                var formatString = (String.IsNullOrWhiteSpace(accum)) ?
                    (equality ? "{3}.[{1}]" : "{1} as {2}") :
                    (equality ? "{0}, {3}.[{1}]" : "{0}, {1} as {2}");
                return String.Format(formatString, accum, propertyMap.ColumnNameOrFormula, propertyMap.Alias, tableAlias);
            };
        }

        protected static Func<string, IPropertyMap, string> GetPropListAggregator(string tableAlias)
        {
            return (accum, propertyMap) => String.Format((String.IsNullOrWhiteSpace(accum)) ? "{2}.[{1}]" : "{0}, {2}.[{1}]",
                accum, propertyMap.Alias, tableAlias);
        }

        protected static Func<string, KeyValuePair<string, string>, string> GetWhereAggregator()
        {
            return (accum, pair) =>
            {
                return String.Format(String.IsNullOrWhiteSpace(accum) ? "[{1}] = {2}" : "{0} AND [{1}] = {2}", accum, pair.Key, pair.Value);
            };
        }
    }
}
