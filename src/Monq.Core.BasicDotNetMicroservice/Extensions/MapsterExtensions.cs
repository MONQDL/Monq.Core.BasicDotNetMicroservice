using Mapster;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods to work with Mapster mappings.
/// </summary>
public static class MapsterExtensions
{
    /// <summary>
    /// Do not map null strings from source object.
    /// </summary>
    /// <param name="setter">Type adapter setter.</param>
    /// <typeparam name="TSource">Type of the source object.</typeparam>
    /// <typeparam name="TDestination">Type of the destination object.</typeparam>
    /// <returns></returns>
    public static TypeAdapterSetter<TSource, TDestination> IgnoreNullStrings<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    TSource,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    TDestination>(
        this TypeAdapterSetter<TSource, TDestination> setter)
    {
        var destType = typeof(TDestination);
        foreach (var destProperty in destType.GetProperties()
                     .Where(x => x is { CanRead: true, CanWrite: true } && x.PropertyType == typeof(string)))
        {
            var srcProperty = typeof(TSource).GetProperty(destProperty.Name, destProperty.PropertyType);
            if (srcProperty is null)
                continue;

            var srcParameterExp = Expression.Parameter(typeof(TSource), "src");
            var accessSrcPropertyExp = Expression.MakeMemberAccess(srcParameterExp, srcProperty);
            var destParameterExp = Expression.Parameter(destType, "dst");
            var accessDestPropertyExp = Expression.MakeMemberAccess(destParameterExp, destProperty);

            var compareToNullExp = Expression.Equal(accessSrcPropertyExp, Expression.Constant(null, destProperty.PropertyType));
            var compareToNullLambda = Expression.Lambda<Func<TSource, TDestination, bool>>(
                compareToNullExp, srcParameterExp, destParameterExp);

            var returnDestPropertyLambda = Expression.Lambda<Func<TDestination, object>>(accessDestPropertyExp, destParameterExp);

            setter.IgnoreIf(compareToNullLambda, returnDestPropertyLambda);
        }

        return setter;
    }
}
