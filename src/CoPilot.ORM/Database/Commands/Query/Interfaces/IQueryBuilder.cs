using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoPilot.ORM.Common;

namespace CoPilot.ORM.Database.Commands.Query.Interfaces
{
    public interface IQueryBuilder
    {
        IQuery<T> From<T>() where T : class;
    }

    public interface IOrderedQuery<T> : IPreparedQuery<T> where T : class
    {
        IOrderedQuery<T> ThenBy(string path, Ordering ordering = Ordering.Ascending);
        IOrderedQuery<T> ThenBy(Expression<Func<T, object>> member, Ordering ordering = Ordering.Ascending);
    }

    public interface IOrderedQuery<out T, TTarget> : IPreparedQuery<T, TTarget> where T : class
    {
        IOrderedQuery<T, TTarget> ThenBy(string path, Ordering ordering = Ordering.Ascending);
        IOrderedQuery<T, TTarget> ThenBy(Expression<Func<TTarget, object>> member, Ordering ordering = Ordering.Ascending);
    }

    public interface IIncludableQuery<T> : IOrderableQuery<T> where T : class
    {
        IOrderableQuery<T> Include(params string[] paths);
    }

    public interface IOrderableQuery<T> : IPreparedQuery<T> where T : class
    {
        IOrderedQuery<T> OrderBy(string path, Ordering ordering = Ordering.Ascending);
        IOrderedQuery<T> OrderBy(Expression<Func<T, object>> member, Ordering ordering = Ordering.Ascending);
    }

    public interface IOrderableQuery<out T, TTarget> : IPreparedQuery<T, TTarget> where T : class
    {
        IOrderedQuery<T, TTarget> OrderBy(string path, Ordering ordering = Ordering.Ascending);
        IOrderedQuery<T, TTarget> OrderBy(Expression<Func<TTarget, object>> member, Ordering ordering = Ordering.Ascending);
    }

    public interface IPreparedQuery<out T> where T : class
    {
        IPreparedQuery<T> Take(int take);
        IPreparedQuery<T> Skip(int skip);
        IPreparedQuery<T> Distinct();
        T Single();
        T[] ToArray();
        IEnumerable<T> AsEnumerable();
    }

    public interface IPreparedQuery<out T, TTarget> where T : class
    {
        IPreparedQuery<T, TTarget> Take(int take);
        IPreparedQuery<T, TTarget> Skip(int skip);
        IPreparedQuery<T, TTarget> Distinct();
        TTarget Single();
        TTarget[] ToArray();
        List<TTarget> ToList();
        IEnumerable<TTarget> AsEnumerable();
    }

    public interface IFilteredQuery<T> : IIncludableQuery<T> where T : class
    {
        IIncludableQuery<T> Select();
        IOrderableQuery<T> Select(params string[] include);
        IOrderableQuery<T, TTarget> Select<TTarget>(Expression<Func<T, TTarget>> selector);
    }

    public interface IQuery<T> : IFilteredQuery<T> where T : class
    {
        IFilteredQuery<T> Where(Expression<Func<T, bool>> predicate);

    }
}