using System;
using System.Collections.Generic;
using System.Linq;

namespace Viteyka.ORM.Contexts
{
    internal class QueryContext
    {
        private Dictionary<string, IClassMap> _classMaps;
        private int _tIndex = 0;
        private int _cIndex = 0;
        public IClassMap[] ClassMaps { get { return _classMaps.Values.ToArray(); } }

        public QueryContext(params IClassMap[] classMaps)
        {
            if (classMaps.Length == 0)
                throw new ArgumentOutOfRangeException("At least 1 class map must be provided.");

            _classMaps = classMaps.ToDictionary(it => String.IsNullOrWhiteSpace(it.TableAlias) ? String.Format("t_{0}", _tIndex++) : it.TableAlias);
        }

        public string AliasForTable(IClassMap classMap)
        {
            if (classMap != null)
                foreach (var pair in _classMaps)
                    if (pair.Value == classMap)
                        return pair.Key;

            return String.Format("t_{0}", _tIndex++);
        }

        public string AliasForColumn()
        {
            return String.Format("c_{0}", _cIndex++);
        }
    }
}
