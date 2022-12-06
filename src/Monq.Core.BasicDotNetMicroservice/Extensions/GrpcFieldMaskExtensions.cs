using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Google.Protobuf.WellKnownTypes.FieldMask;

namespace Monq.Core.BasicDotNetMicroservice.Extensions
{
    public static class GrpcFieldMaskExtensions
    {
        static readonly MergeOptions _defaultMergeOptions = new()
        {
            // Нужно true для слияния StringValue и т.п., иначе возникает ошибка - похоже на баг библиотеки.
            ReplaceMessageFields = false,
        };

        /// <summary>
        /// Apply <paramref name="fieldMask"/> on the <paramref name="value"/>.
        /// If <paramref name="fieldMask"/> is null, then non FieldMask Paths will be applied.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="value"/>.</typeparam>
        /// <param name="value">The protobuf object.</param>
        /// <param name="fieldMask"><see cref="FieldMask"/> with configured Paths property.</param>
        /// <param name="options">The FieldMask merge options.</param>
        /// <returns></returns>
        public static T ApplyFieldMask<T>(this T value,
            FieldMask? fieldMask, MergeOptions? options = null)
            where T : IMessage, new()
        {
            if (fieldMask is null)
                return value;

            var mergedItem = new T();
            fieldMask.Merge(value, mergedItem, options ?? _defaultMergeOptions);
            return mergedItem;
        }

        /// <summary>
        /// Apply <paramref name="fieldMask"/> on each element of the collection <paramref name="values"/>.
        /// If <paramref name="fieldMask"/> is null, then non FieldMask Paths will be applied.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="values"/> collection member.</typeparam>
        /// <param name="values">The protobuf objects collection.</param>
        /// <param name="fieldMask"><see cref="FieldMask"/> with configured Paths property.</param>
        /// <param name="options">The FieldMask merge options.</param>
        /// <returns></returns>
        public static IEnumerable<T> ApplyFieldMask<T>(this IEnumerable<T> values,
            FieldMask? fieldMask, MergeOptions? options = null)
            where T : IMessage, new()
        {
            if (fieldMask is null)
                return values;

            return MergeCollection(values, fieldMask, options);
        }

        /// <summary>
        /// Apply <paramref name="fieldMask"/> on each element of the collection <paramref name="values"/> 
        /// and use <paramref name="convert"/> function at each element.
        /// If <paramref name="fieldMask"/> is null, then non FieldMask Paths will be applied.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="values">Collection.</param>
        /// <param name="fieldMask"><see cref="FieldMask"/>.</param>
        /// <param name="options">Merge options.</param>
        /// <param name="convert">Convert function.</param>
        /// <returns></returns>
        public static IEnumerable<TResult> ApplyFieldMask<TSource, TResult>(this IEnumerable<TSource> values,
            FieldMask? fieldMask, Func<TSource, TResult> convert, MergeOptions? options = null)
            where TResult : IMessage, new()
        {
            if (fieldMask is null)
                return ConvertWithoutFieldMask(values, convert);

            return MergeCollectionWithConvertion(values, fieldMask, convert, options);
        }

        /// <summary>
        /// True, if <paramref name="fieldMask"/> is not null and Paths contains <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="fieldMask"><see cref="FieldMask"/> from GRPC request.</param>
        /// <param name="fieldName">The name of a model property that 
        /// can be included in the mask's Paths array.</param>
        /// <returns></returns>
        public static bool ShouldIncludeField(this FieldMask? fieldMask, string fieldName) =>
            fieldMask == null || fieldMask?.Paths?.Contains(fieldName) == true;

        /// <summary>
        /// Get gRPC field mask for an object.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns></returns>
        public static FieldMask GetFieldMask<T>(this T obj) where T : class
        {
            var props = GetNotNullPropertiesNames(obj);
            return FromString(string.Join(",", props));
        }

        /// <summary>
        /// Get gRPC field mask for an object type.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns></returns>
        public static FieldMask GetFieldMask(this System.Type type)
        {
            var props = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => x.Name)
                .ToList();
            return FromString(string.Join(",", props));
        }

        static IEnumerable<string> GetNotNullPropertiesNames(object obj)
        {
            var props = obj.GetType().GetProperties();
            var result = props
                .Where(x => x.GetValue(obj) != null)
                .Select(x => x.Name);

            return result;
        }

        static IEnumerable<TResult> ConvertWithoutFieldMask<TSource, TResult>(
            IEnumerable<TSource> values, Func<TSource, TResult> convert)
        {
            foreach (var item in values)
            {
                yield return convert(item);
            }
        }

        static IEnumerable<TSource> MergeCollection<TSource>(IEnumerable<TSource> values,
            FieldMask fieldMask,
            MergeOptions? options = null)
            where TSource : IMessage, new()
        {
            foreach (var item in values)
            {
                var mergedItem = new TSource();
                fieldMask.Merge(item, mergedItem, options ?? _defaultMergeOptions);
                yield return mergedItem;
            }
        }

        static IEnumerable<TResult> MergeCollectionWithConvertion<TSource, TResult>(IEnumerable<TSource> values,
            FieldMask fieldMask,
            Func<TSource, TResult> convert,
            MergeOptions? options = null)
            where TResult : IMessage, new()
        {
            foreach (var item in values)
            {
                var mergedItem = new TResult();
                fieldMask.Merge(convert(item), mergedItem, options ?? _defaultMergeOptions);
                yield return mergedItem;
            }
        }
    }
}
