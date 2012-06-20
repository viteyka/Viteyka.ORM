using System;
using System.Data;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Viteyka.ORM
{
    public interface IMapper<T>
    {
        IEnumerable<T> All(IDbConnection connection);
        IEnumerable<T> Where(IDbConnection connection, Expression<Func<T, bool>> predicate);
        IEnumerable<T> Where<TProperty>(IDbConnection connection, Expression<Func<T, bool>> predicate, Expression<Func<T, TProperty>> orderProperty, int from, int to, bool desc = false);
        IEnumerable<T> Join<T1>(IDbConnection connection, IMapper<T1> joined, Expression<Func<T, T1, bool>> predicate);
        IEnumerable<T> Join<T1, TProperty>(IDbConnection connection, IMapper<T1> joined, Expression<Func<T, T1, bool>> predicate, Expression<Func<T, TProperty>> orderProperty, int from, int to, bool desc = false);
        IEnumerable<T> ExecProc(IDbConnection connection, string procName, object parameters);

        int Count(IDbConnection connection);
        int Count(IDbConnection connection, Expression<Func<T, bool>> predicate);
        double Sum<TProperty>(IDbConnection connection, Expression<Func<T, TProperty>> property);
        double Sum<TProperty>(IDbConnection connection, Expression<Func<T, TProperty>> property, Expression<Func<T, bool>> predicate);
        T GetById(IDbConnection connection, object id);

        void Insert(IDbConnection connection, T item);
        void Update(IDbConnection connection, T item);
        void Delete(IDbConnection connection, T item);
    }
}
