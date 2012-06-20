using System;
using System.Reflection;
using System.Linq.Expressions;

namespace Viteyka.ORM.Helpers
{
    internal static class MemberHelper
    {
        public static string GetName<T>(Expression<Action<T>> member)
        {
            if (member == null)
                throw new ArgumentNullException("member");
            var body = member.Body;
            if (body is MethodCallExpression)
            {
                return (body as MethodCallExpression).Method.Name;
            }
            else
                return null;
        }

        public static string GetName(LambdaExpression member)
        {
            if (member == null)
                throw new ArgumentNullException("member");
            var body = member.Body;
            if (body is MemberExpression)
            {
                return (body as MemberExpression).Member.Name;
            }
            else if (body is MethodCallExpression)
            {
                return (body as MethodCallExpression).Method.Name;
            }
            else
                return null;
        }

        public static string GetName<T1, R>(Expression<Func<T1, R>> member)
        {
            if (member == null)
                throw new ArgumentNullException("member");
            var body = member.Body;
            if (body is MemberExpression)
            {
                return (body as MemberExpression).Member.Name;
            }
            else if (body is MethodCallExpression)
            {
                return (body as MethodCallExpression).Method.Name;
            }
            else
                return null;
        }

        public static PropertyInfo GetProperty<T1, R>(Expression<Func<T1, R>> member)
        {
            if (member == null)
                throw new ArgumentNullException("member");
            var body = member.Body;
            if (body is MemberExpression)
            {
                return (body as MemberExpression).Member as PropertyInfo;
            }
            else
                return null;
        }
    }
}
