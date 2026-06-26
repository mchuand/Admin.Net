using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace Admin.NET.Core;

/// <summary>
/// 高级查询扩展方法
/// 提供通用的 ISugarQueryable 扩展方法，支持自动识别字段所属表
/// </summary>
public static class AdvancedQueryExtension
{
    /// <summary>
    /// 应用高级查询条件
    /// 自动识别查询条件中的字段属于哪个表，并添加对应的查询条件
    /// 如果字段在多个表中存在，取第一个匹配的表
    /// </summary>
    /// <typeparam name="TQuery">查询对象类型（ISugarQueryable 泛型）</typeparam>
    /// <param name="queryable">查询对象</param>
    /// <param name="conditions">查询条件列表</param>
    /// <returns>应用条件后的查询对象</returns>
    public static TQuery ApplyAdvancedQuery<TQuery>(this TQuery queryable, List<QueryConditionItem> conditions)
        where TQuery : class
    {
        if (conditions == null || conditions.Count == 0)
        {
            Console.WriteLine("[高级查询] 条件为空，跳过");
            return queryable;
        }

        Console.WriteLine($"[高级查询] 开始处理 {conditions.Count} 个条件");
        foreach (var condition in conditions)
        {
            Console.WriteLine($"[高级查询] 条件: Field={condition.Field}, Compare={condition.Compare}, Value={condition.Value}");
            queryable = ApplySingleCondition(queryable, condition);
        }

        Console.WriteLine($"[高级查询] 完成处理");
        return queryable;
    }

    /// <summary>
    /// 应用关键字模糊搜索条件
    /// 在指定字段列表中对关键字进行 OR 模糊匹配
    /// </summary>
    /// <typeparam name="TQuery">查询对象类型</typeparam>
    /// <param name="queryable">查询对象</param>
    /// <param name="keywordFields">需要匹配的字段列表</param>
    /// <param name="keyword">关键字值</param>
    /// <returns>应用条件后的查询对象</returns>
    public static TQuery ApplyKeywordSearch<TQuery>(this TQuery queryable, List<string> keywordFields, string keyword)
        where TQuery : class
    {
        keyword = keyword.Trim();
        if (keywordFields == null || keywordFields.Count == 0 || string.IsNullOrWhiteSpace(keyword))
        {
            Console.WriteLine("[关键字搜索] 参数无效，跳过");
            return queryable;
        }

        Console.WriteLine($"[关键字搜索] 开始处理，关键字={keyword}，字段数={keywordFields.Count}");

        var queryType = queryable.GetType();
        var genericArgs = queryType.GetGenericArguments();

        if (genericArgs.Length == 0)
        {
            Console.WriteLine("[关键字搜索] 没有泛型参数，跳过");
            return queryable;
        }

        Console.WriteLine($"[关键字搜索] 泛型参数数量={genericArgs.Length}");

        try
        {
            // 构建参数表达式
            var parameters = new ParameterExpression[genericArgs.Length];
            for (int i = 0; i < genericArgs.Length; i++)
            {
                var paramName = GetParamName(i);
                parameters[i] = Expression.Parameter(genericArgs[i], paramName);
            }

            // 构建所有字段的 Contains 表达式，并用 OrElse 合成 OR 条件
            Expression orExpression = null;

            foreach (var fieldName in keywordFields)
            {
                Console.WriteLine($"[关键字搜索] 处理字段: {fieldName}");

                // 查找字段属于哪个表
                var (matchedType, matchedIndex) = FindFirstMatchingType(fieldName, genericArgs);
                if (matchedType == null)
                {
                    Console.WriteLine($"[关键字搜索] 字段 {fieldName} 未在任何表中找到，跳过");
                    continue;
                }

                Console.WriteLine($"[关键字搜索] 字段 {fieldName} 在表 {matchedType.Name} 中找到");

                var propertyInfo = matchedType.GetProperty(fieldName,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                {
                    Console.WriteLine($"[关键字搜索] 无法获取字段 {fieldName} 的属性信息，跳过");
                    continue;
                }

                // 检查是否为字符串类型
                if (propertyInfo.PropertyType != typeof(string))
                {
                    Console.WriteLine($"[关键字搜索] 字段 {fieldName} 不是字符串类型，跳过");
                    continue;
                }

                // 构建 Contains 表达式: field.Contains("keyword")
                var targetParameter = parameters[matchedIndex];
                var memberAccess = Expression.Property(targetParameter, propertyInfo);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                var containsExpression = Expression.Call(memberAccess, containsMethod, Expression.Constant(keyword));

                // 使用 OrElse 合成 OR 条件
                if (orExpression == null)
                {
                    orExpression = containsExpression;
                }
                else
                {
                    orExpression = Expression.OrElse(orExpression, containsExpression);
                }

                Console.WriteLine($"[关键字搜索] 字段 {fieldName} 条件构建成功");
            }

            if (orExpression == null)
            {
                Console.WriteLine("[关键字搜索] 没有有效的字段条件，跳过");
                return queryable;
            }

            // 构建 Lambda 表达式
            var delegateType = GetFuncType(genericArgs, typeof(bool));
            var lambdaMethod = typeof(Expression).GetMethods()
                .Where(m => m.Name == "Lambda" && m.IsGenericMethod)
                .FirstOrDefault(m =>
                {
                    var ps = m.GetParameters();
                    return ps.Length == 2
                        && ps[0].ParameterType == typeof(Expression)
                        && ps[1].ParameterType == typeof(ParameterExpression[]);
                });

            var genericLambdaMethod = lambdaMethod.MakeGenericMethod(delegateType);
            var lambdaExpression = (Expression)genericLambdaMethod.Invoke(null, new object[] { orExpression, parameters });

            Console.WriteLine($"[关键字搜索] OR表达式构建完成: {lambdaExpression}");

            // 使用 dynamic 调用 Where 方法
            dynamic dynamicQuery = queryable;
            dynamic dynamicExpression = lambdaExpression;
            var result = (TQuery)dynamicQuery.Where(dynamicExpression);

            Console.WriteLine($"[关键字搜索] 处理完成");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[关键字搜索] 异常: {ex.Message}");
            Console.WriteLine($"[关键字搜索] 堆栈: {ex.StackTrace}");
            return queryable;
        }
    }

    /// <summary>
    /// 应用单个查询条件
    /// </summary>
    /// <typeparam name="TQuery">查询对象类型</typeparam>
    /// <param name="queryable">查询对象</param>
    /// <param name="condition">查询条件</param>
    /// <returns>应用条件后的查询对象</returns>
    private static TQuery ApplySingleCondition<TQuery>(TQuery queryable, QueryConditionItem condition)
        where TQuery : class
    {
        var queryType = queryable.GetType();
        var genericArgs = queryType.GetGenericArguments();

        Console.WriteLine($"[ApplySingleCondition] queryType={queryType.Name}, genericArgs.Length={genericArgs.Length}");

        if (genericArgs.Length == 0)
        {
            Console.WriteLine("[ApplySingleCondition] 没有泛型参数，跳过");
            return queryable;
        }

        for (int i = 0; i < genericArgs.Length; i++)
        {
            Console.WriteLine($"[ApplySingleCondition] 泛型参数[{i}]={genericArgs[i].Name}");
        }

        var (matchedType, matchedIndex) = FindFirstMatchingType(condition.Field, genericArgs);
        if (matchedType == null)
        {
            Console.WriteLine($"[ApplySingleCondition] 字段 {condition.Field} 未在任何表中找到");
            return queryable;
        }

        Console.WriteLine($"[ApplySingleCondition] 字段 {condition.Field} 在表 {matchedType.Name} 中找到，索引={matchedIndex}");

        var propertyInfo = matchedType.GetProperty(condition.Field,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo == null)
        {
            Console.WriteLine($"[ApplySingleCondition] 无法获取属性信息");
            return queryable;
        }

        Console.WriteLine($"[ApplySingleCondition] 属性类型: {propertyInfo.PropertyType.Name}, Value类型: {condition.Value?.GetType()?.Name}, Value值: {condition.Value}");

        var conditionExpression = BuildConditionLambda(condition, propertyInfo, matchedIndex, genericArgs);
        if (conditionExpression == null)
        {
            Console.WriteLine($"[ApplySingleCondition] 构建Lambda表达式失败");
            return queryable;
        }

        Console.WriteLine($"[ApplySingleCondition] Lambda表达式构建成功: {conditionExpression}");

        var result = InvokeWhereMethod(queryable, queryType, conditionExpression, genericArgs.Length);
        Console.WriteLine($"[ApplySingleCondition] InvokeWhereMethod返回结果类型: {result?.GetType()?.Name}");

        return result;
    }

    /// <summary>
    /// 查找第一个包含指定字段的类型
    /// </summary>
    /// <param name="fieldName">字段名</param>
    /// <param name="types">类型数组</param>
    /// <returns>匹配的类型和索引</returns>
    private static (Type Type, int Index) FindFirstMatchingType(string fieldName, Type[] types)
    {
        for (int i = 0; i < types.Length; i++)
        {
            var property = types[i].GetProperty(fieldName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                return (types[i], i);
            }

            foreach (var interfaceType in types[i].GetInterfaces())
            {
                property = interfaceType.GetProperty(fieldName,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    return (types[i], i);
                }
            }
        }

        return (null, -1);
    }

    /// <summary>
    /// 构建条件 Lambda 表达式
    /// </summary>
    /// <param name="condition">查询条件</param>
    /// <param name="propertyInfo">属性信息</param>
    /// <param name="paramIndex">参数索引（第几个表）</param>
    /// <param name="allParamTypes">所有参数类型</param>
    /// <returns>Expression&lt;Func&lt;...&gt;&gt; 类型的表达式</returns>
    private static Expression BuildConditionLambda(
        QueryConditionItem condition,
        PropertyInfo propertyInfo,
        int paramIndex,
        Type[] allParamTypes)
    {
        var parameters = new ParameterExpression[allParamTypes.Length];
        for (int i = 0; i < allParamTypes.Length; i++)
        {
            var paramName = GetParamName(i);
            parameters[i] = Expression.Parameter(allParamTypes[i], paramName);
        }

        var targetParameter = parameters[paramIndex];
        var memberAccess = Expression.Property(targetParameter, propertyInfo);
        var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        Expression body;

        switch (condition.Compare)
        {
            case QueryCompareEnum.IsNull:
                body = Expression.Equal(memberAccess, Expression.Constant(null, propertyInfo.PropertyType));
                break;

            case QueryCompareEnum.IsNotNull:
                body = Expression.NotEqual(memberAccess, Expression.Constant(null, propertyInfo.PropertyType));
                break;

            default:
                if (condition.Value == null)
                {
                    return null;
                }
                try
                {
                    body = BuildComparisonExpression(memberAccess, propertyType, propertyInfo.PropertyType, condition);
                    if (body == null)
                    {
                        Console.WriteLine($"[BuildConditionLambda] BuildComparisonExpression 返回 null");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BuildConditionLambda] 异常: {ex.Message}");
                    Console.WriteLine($"[BuildConditionLambda] 堆栈: {ex.StackTrace}");
                    return null;
                }
                break;
        }

        var delegateType = GetFuncType(allParamTypes, typeof(bool));

        // 使用反射调用 Expression.Lambda<TDelegate> 泛型方法，生成正确类型的 Expression<Func<...>>
        var lambdaMethod = typeof(Expression).GetMethods()
            .Where(m => m.Name == "Lambda" && m.IsGenericMethod)
            .FirstOrDefault(m =>
            {
                var ps = m.GetParameters();
                return ps.Length == 2
                    && ps[0].ParameterType == typeof(Expression)
                    && ps[1].ParameterType == typeof(ParameterExpression[]);
            });

        var genericLambdaMethod = lambdaMethod.MakeGenericMethod(delegateType);
        var expression = (Expression)genericLambdaMethod.Invoke(null, new object[] { body, parameters });

        return expression;
    }

    /// <summary>
    /// 构建比较表达式
    /// </summary>
    /// <param name="memberAccess">成员访问表达式</param>
    /// <param name="propertyType">属性类型（去除可空类型后）</param>
    /// <param name="originalPropertyType">原始属性类型</param>
    /// <param name="condition">查询条件</param>
    /// <returns>比较表达式</returns>
    private static Expression BuildComparisonExpression(
        MemberExpression memberAccess,
        Type propertyType,
        Type originalPropertyType,
        QueryConditionItem condition)
    {
        switch (condition.Compare)
        {
            case QueryCompareEnum.Eq:
                var eqValue = ConvertValue(condition.Value, propertyType);
                return Expression.Equal(memberAccess, Expression.Constant(eqValue, originalPropertyType));

            case QueryCompareEnum.Ne:
                var neValue = ConvertValue(condition.Value, propertyType);
                return Expression.NotEqual(memberAccess, Expression.Constant(neValue, originalPropertyType));

            case QueryCompareEnum.Lt:
                var ltValue = ConvertValue(condition.Value, propertyType);
                return Expression.LessThan(memberAccess, Expression.Constant(ltValue, originalPropertyType));

            case QueryCompareEnum.Le:
                var leValue = ConvertValue(condition.Value, propertyType);
                return Expression.LessThanOrEqual(memberAccess, Expression.Constant(leValue, originalPropertyType));

            case QueryCompareEnum.Gt:
                var gtValue = ConvertValue(condition.Value, propertyType);
                return Expression.GreaterThan(memberAccess, Expression.Constant(gtValue, originalPropertyType));

            case QueryCompareEnum.Ge:
                var geValue = ConvertValue(condition.Value, propertyType);
                return Expression.GreaterThanOrEqual(memberAccess, Expression.Constant(geValue, originalPropertyType));

            case QueryCompareEnum.Like:
                if (propertyType != typeof(string))
                {
                    return null;
                }
                var likeValue = GetStringValue(condition.Value);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                return Expression.Call(memberAccess, containsMethod, Expression.Constant(likeValue));

            case QueryCompareEnum.NotLike:
                if (propertyType != typeof(string))
                {
                    return null;
                }
                var notLikeValue = GetStringValue(condition.Value);
                var notContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                return Expression.Not(Expression.Call(memberAccess, notContainsMethod, Expression.Constant(notLikeValue)));

            case QueryCompareEnum.Between:
                return BuildBetweenExpression(memberAccess, originalPropertyType, propertyType, condition);

            case QueryCompareEnum.In:
                return BuildInExpression(memberAccess, originalPropertyType, propertyType, condition);

            case QueryCompareEnum.NotIn:
                var inExpression = BuildInExpression(memberAccess, originalPropertyType, propertyType, condition);
                return inExpression != null ? Expression.Not(inExpression) : null;

            default:
                return null;
        }
    }

    /// <summary>
    /// 构建 Between 范围查询表达式
    /// </summary>
    /// <param name="memberAccess">成员访问表达式</param>
    /// <param name="originalPropertyType">原始属性类型</param>
    /// <param name="propertyType">属性类型（去除可空类型后）</param>
    /// <param name="condition">查询条件</param>
    /// <returns>Between 表达式</returns>
    private static Expression BuildBetweenExpression(
        MemberExpression memberAccess,
        Type originalPropertyType,
        Type propertyType,
        QueryConditionItem condition)
    {
        if (condition.Value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            var array = jsonElement.EnumerateArray().ToArray();
            if (array.Length != 2)
            {
                return null;
            }

            var minValue = ConvertValue(array[0], propertyType);
            var maxValue = ConvertValue(array[1], propertyType);

            var geExpression = Expression.GreaterThanOrEqual(
                memberAccess, Expression.Constant(minValue, originalPropertyType));
            var leExpression = Expression.LessThanOrEqual(
                memberAccess, Expression.Constant(maxValue, originalPropertyType));

            return Expression.AndAlso(geExpression, leExpression);
        }

        if (condition.Value is JArray jArray)
        {
            if (jArray.Count != 2)
            {
                return null;
            }

            var minValue = ConvertValue(jArray[0], propertyType);
            var maxValue = ConvertValue(jArray[1], propertyType);

            var geExpression = Expression.GreaterThanOrEqual(
                memberAccess, Expression.Constant(minValue, originalPropertyType));
            var leExpression = Expression.LessThanOrEqual(
                memberAccess, Expression.Constant(maxValue, originalPropertyType));

            return Expression.AndAlso(geExpression, leExpression);
        }

        if (condition.Value is Array arrayValue)
        {
            if (arrayValue.Length != 2)
            {
                return null;
            }

            var minValue = ConvertValue(arrayValue.GetValue(0), propertyType);
            var maxValue = ConvertValue(arrayValue.GetValue(1), propertyType);

            var geExpression = Expression.GreaterThanOrEqual(
                memberAccess, Expression.Constant(minValue, originalPropertyType));
            var leExpression = Expression.LessThanOrEqual(
                memberAccess, Expression.Constant(maxValue, originalPropertyType));

            return Expression.AndAlso(geExpression, leExpression);
        }

        return null;
    }

    /// <summary>
    /// 构建 In 查询表达式
    /// </summary>
    /// <param name="memberAccess">成员访问表达式</param>
    /// <param name="originalPropertyType">原始属性类型</param>
    /// <param name="propertyType">属性类型（去除可空类型后）</param>
    /// <param name="condition">查询条件</param>
    /// <returns>In 表达式</returns>
    private static Expression BuildInExpression(
        MemberExpression memberAccess,
        Type originalPropertyType,
        Type propertyType,
        QueryConditionItem condition)
    {
        List<object> values = new List<object>();

        if (condition.Value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            values = jsonElement.EnumerateArray()
                .Select(v => ConvertValue(v, propertyType))
                .ToList();
        }
        else if (condition.Value is JArray jArray)
        {
            values = jArray.Select(v => ConvertValue(v, propertyType))
                .ToList();
        }
        else if (condition.Value is Array arrayValue)
        {
            values = arrayValue.Cast<object>()
                .Select(v => ConvertValue(v, propertyType))
                .ToList();
        }

        if (values.Count == 0)
        {
            return null;
        }

        var listType = typeof(List<>).MakeGenericType(originalPropertyType);
        var list = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add");

        foreach (var value in values)
        {
            addMethod.Invoke(list, new[] { value });
        }

        var containsMethod = listType.GetMethod("Contains", new[] { originalPropertyType });
        return Expression.Call(Expression.Constant(list), containsMethod, memberAccess);
    }

    /// <summary>
    /// 获取参数名称（按照 SqlSugar 约定：第一个 u，第二个 a，第三个 b...）
    /// </summary>
    /// <param name="index">参数索引</param>
    /// <returns>参数名称</returns>
    private static string GetParamName(int index)
    {
        if (index == 0) return "u";
        if (index == 1) return "a";
        if (index == 2) return "b";
        if (index == 3) return "c";
        if (index == 4) return "d";
        return $"p{index}";
    }

    /// <summary>
    /// 获取 Func 委托类型
    /// </summary>
    /// <param name="paramTypes">参数类型数组</param>
    /// <param name="returnType">返回类型</param>
    /// <returns>Func 委托类型</returns>
    private static Type GetFuncType(Type[] paramTypes, Type returnType)
    {
        var allTypes = paramTypes.Concat(new[] { returnType }).ToArray();

        switch (allTypes.Length)
        {
            case 2:
                return typeof(Func<,>).MakeGenericType(allTypes);
            case 3:
                return typeof(Func<,,>).MakeGenericType(allTypes);
            case 4:
                return typeof(Func<,,,>).MakeGenericType(allTypes);
            case 5:
                return typeof(Func<,,,,>).MakeGenericType(allTypes);
            case 6:
                return typeof(Func<,,,,,>).MakeGenericType(allTypes);
            default:
                throw new NotSupportedException($"不支持 {paramTypes.Length} 个参数的查询");
        }
    }

    /// <summary>
    /// 通过反射调用 Where 方法
    /// 使用 dynamic 类型绕过类型匹配问题
    /// </summary>
    /// <typeparam name="TQuery">查询对象类型</typeparam>
    /// <param name="queryable">查询对象</param>
    /// <param name="queryType">查询类型</param>
    /// <param name="expression">Expression&lt;Func&lt;...&gt;&gt; 表达式</param>
    /// <param name="paramCount">参数数量</param>
    /// <returns>调用 Where 后的查询对象</returns>
    private static TQuery InvokeWhereMethod<TQuery>(TQuery queryable, Type queryType, Expression expression, int paramCount)
        where TQuery : class
    {
        Console.WriteLine($"[InvokeWhereMethod] 开始查找Where方法，paramCount={paramCount}");

        try
        {
            // 使用 dynamic 类型调用，避免编译时类型检查
            dynamic dynamicQuery = queryable;
            dynamic dynamicExpression = expression;
            var result = dynamicQuery.Where(dynamicExpression);
            Console.WriteLine($"[InvokeWhereMethod] dynamic调用成功，返回类型: {result?.GetType()?.Name}");
            return (TQuery)result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InvokeWhereMethod] dynamic调用失败: {ex.Message}");
        }

        // 如果 dynamic 调用失败，尝试反射查找 Where 方法
        var methods = queryType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Where" && m.ReturnType == queryType)
            .ToList();

        Console.WriteLine($"[InvokeWhereMethod] 找到 {methods.Count} 个Where方法");

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
                continue;

            var paramType = parameters[0].ParameterType;
            Console.WriteLine($"[InvokeWhereMethod] 参数类型: {paramType}, FullName={paramType.FullName}");

            if (paramType.IsGenericType)
            {
                var genericTypeDef = paramType.GetGenericTypeDefinition();
                Console.WriteLine($"[InvokeWhereMethod] 泛型定义: {genericTypeDef}");

                if (genericTypeDef == typeof(Expression<>))
                {
                    var delegateType = paramType.GetGenericArguments()[0];
                    var invokeMethod = delegateType.GetMethod("Invoke");
                    if (invokeMethod != null)
                    {
                        var invokeParamCount = invokeMethod.GetParameters().Length;
                        Console.WriteLine($"[InvokeWhereMethod] Invoke参数数量: {invokeParamCount}, 当前paramCount={paramCount}");

                        if (invokeParamCount == paramCount)
                        {
                            Console.WriteLine($"[InvokeWhereMethod] 找到匹配的Where方法，尝试调用");
                            try
                            {
                                var result = (TQuery)method.Invoke(queryable, new object[] { expression });
                                Console.WriteLine($"[InvokeWhereMethod] 调用成功");
                                return result;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[InvokeWhereMethod] 调用失败: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        Console.WriteLine("[InvokeWhereMethod] 未找到匹配的Where方法");
        return queryable;
    }

    /// <summary>
    /// 转换值类型
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的值</returns>
    private static object ConvertValue(object value, Type targetType)
    {
        if (value is JsonElement jsonElement)
        {
            return ConvertJsonElement(jsonElement, targetType);
        }

        if (value is JValue jValue)
        {
            return ConvertJValue(jValue, targetType);
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlyingType.IsEnum)
        {
            return System.Enum.ToObject(underlyingType, value);
        }

        return Convert.ChangeType(value, targetType);
    }

    /// <summary>
    /// 转换 JValue 类型的值
    /// </summary>
    /// <param name="jValue">JValue 值</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的值</returns>
    private static object ConvertJValue(JValue jValue, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsEnum)
        {
            var intValue = jValue.Value<int>();
            return System.Enum.ToObject(underlyingType, intValue);
        }

        return jValue.ToObject(underlyingType);
    }

    /// <summary>
    /// 转换 JsonElement 类型的值
    /// </summary>
    /// <param name="element">Json 元素</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的值</returns>
    private static object ConvertJsonElement(JsonElement element, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsEnum)
        {
            var intValue = element.GetInt32();
            return System.Enum.ToObject(underlyingType, intValue);
        }

        if (underlyingType == typeof(int))
        {
            return element.GetInt32();
        }
        if (underlyingType == typeof(long))
        {
            return element.GetInt64();
        }
        if (underlyingType == typeof(decimal))
        {
            return element.GetDecimal();
        }
        if (underlyingType == typeof(double))
        {
            return element.GetDouble();
        }
        if (underlyingType == typeof(bool))
        {
            return element.GetBoolean();
        }
        if (underlyingType == typeof(DateTime))
        {
            return element.GetDateTime();
        }
        if (underlyingType == typeof(string))
        {
            return element.GetString();
        }

        return element.ToString();
    }

    /// <summary>
    /// 获取字符串值
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>字符串值</returns>
    private static string GetStringValue(object value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.GetString();
        }
        return value?.ToString();
    }
}
