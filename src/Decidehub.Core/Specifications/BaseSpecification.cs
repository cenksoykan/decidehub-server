using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Decidehub.Core.Interfaces;

namespace Decidehub.Core.Specifications
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        public BaseSpecification(Expression<Func<T, bool>> criteria, bool ignoreQueryFilters = false)
        {
            Criteria = criteria;
            IgnoreQueryFilters = ignoreQueryFilters;
        }

        public Expression<Func<T, bool>> Criteria { get; }
        public List<Expression<Func<T, object>>> Includes { get; } = new List<Expression<Func<T, object>>>();
        public List<string> IncludeStrings { get; } = new List<string>();
        public bool IgnoreQueryFilters { get; }

        protected void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }
    }
}