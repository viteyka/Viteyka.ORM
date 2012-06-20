using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Viteyka.ORM.Contexts;

namespace Viteyka.ORM.Builders
{
    internal class PredicateVisitor : ExpressionVisitor
    {
        private StringBuilder _bldr;
        private int _paramIndex;
        private Dictionary<string, object> _params;
        private QueryContext _context;

        public Dictionary<string, object> Parameters { get { return _params; } }

        public PredicateVisitor(QueryContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            _context = context;
            _bldr = new StringBuilder();
            _paramIndex = 0;
            _params = new Dictionary<string, object>();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            switch (node.Expression.NodeType)
            {
                case ExpressionType.Parameter:
                    var map = _context.ClassMaps.SingleOrDefault(cm => cm.GenericType.Equals(node.Expression.Type));
                    if (map == null)
                        throw new InvalidOperationException(String.Format("No map found for type: {0}", node.Expression.Type));
                    var propertyMap = map.Properties.Single(it => it.Alias == node.Member.Name);
                    if (propertyMap == null)
                        throw new InvalidOperationException(String.Format("No map found for property: {0} , type: {1}", node.Member.Name, node.Expression.Type));

                    if (propertyMap.Alias.Equals(propertyMap.ColumnNameOrFormula))
                        _bldr.AppendFormat("{0}.[{1}]", _context.AliasForTable(map), propertyMap.ColumnNameOrFormula);
                    else
                        _bldr.Append(propertyMap.ColumnNameOrFormula);
                    break;
                case ExpressionType.Constant:
                case ExpressionType.MemberAccess:
                    AddParam(Expression.Lambda(node).Compile().DynamicInvoke());
                    break;
            }
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            AddParam(node.Value);
            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            _bldr.Append("(");
            Visit(node.Left);
            _bldr.Append(" ");
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    _bldr.Append("=");
                    break;
                case ExpressionType.NotEqual:
                    _bldr.Append("!=");
                    break;
                case ExpressionType.GreaterThan:
                    _bldr.Append(">");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _bldr.Append(">=");
                    break;
                case ExpressionType.LessThan:
                    _bldr.Append("<");
                    break;
                case ExpressionType.LessThanOrEqual:
                    _bldr.Append("<=");
                    break;
                case ExpressionType.AndAlso:
                    _bldr.Append("AND");
                    break;
                case ExpressionType.OrElse:
                    _bldr.Append("OR");
                    break;
            }
            _bldr.Append(" ");
            Visit(node.Right);
            _bldr.Append(")");
            return node;
        }

        public override string ToString()
        {
            return _bldr.ToString();
        }

        private void AddParam(object value)
        {
            var paramName = string.Format("@whereParam{0}", _paramIndex++);
            _bldr.Append(paramName);
            _params.Add(paramName, value);
        }
    }
}
