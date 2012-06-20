using System;
using System.Linq.Expressions;

namespace Viteyka.ORM.Contexts
{
    internal class PageFilter<T, TProperty>
    {
        public int From { get; set; }
        public int To { get; set; }
        public bool Desc { get; set; }
        public Expression<Func<T, TProperty>> OrderProperty { get; set; }
    }
}
