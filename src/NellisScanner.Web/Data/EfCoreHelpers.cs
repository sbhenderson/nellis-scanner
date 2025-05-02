using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace NellisScanner.Web.Data;

/// <summary>
/// Helper class to provide alternate implementation for bulk operations when using InMemory database
/// </summary>
public static class EfCoreHelpers
{
    /// <summary>
    /// Determines if the database context is using an in-memory provider
    /// </summary>
    public static bool IsInMemoryDatabase(DbContext context)
    {
        return context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
    }

    /// <summary>
    /// Bulk insert or update entities based on the database provider
    /// </summary>
    public static async Task BulkInsertOrUpdateEntitiesAsync<T>(
        DbContext context, 
        List<T> entities, 
        BulkConfig bulkConfig,
        List<string> keyProperties,
        List<string>? excludedUpdateProperties = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (IsInMemoryDatabase(context))
        {
            await InsertOrUpdateEntitiesForInMemoryAsync(
                context, 
                entities, 
                keyProperties, 
                excludedUpdateProperties, 
                cancellationToken);
        }
        else
        {
            await context.BulkInsertOrUpdateAsync(entities, bulkConfig, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Naive implementation of bulk insert or update for in-memory provider
    /// </summary>
    private static async Task InsertOrUpdateEntitiesForInMemoryAsync<T>(
        DbContext context,
        List<T> entities,
        List<string> keyProperties,
        List<string>? excludedUpdateProperties = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var entityType = context.Model.FindEntityType(typeof(T));
        if (entityType == null)
            throw new ArgumentException($"Entity type {typeof(T).Name} not found in context");

        var dbSet = context.Set<T>();
        
        foreach (var entity in entities)
        {
            // Build the query to find if the entity exists based on key properties
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
            System.Linq.Expressions.Expression? predicate = null;
            
            foreach (var keyProperty in keyProperties)
            {
                var propertyInfo = typeof(T).GetProperty(keyProperty);
                if (propertyInfo == null)
                    continue;
                    
                var leftSide = System.Linq.Expressions.Expression.Property(parameter, propertyInfo);
                var rightSide = System.Linq.Expressions.Expression.Constant(propertyInfo.GetValue(entity));
                var equalExpression = System.Linq.Expressions.Expression.Equal(leftSide, rightSide);
                
                predicate = predicate == null 
                    ? equalExpression 
                    : System.Linq.Expressions.Expression.AndAlso(predicate, equalExpression);
            }
            
            if (predicate == null)
                continue;
                
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(predicate, parameter);
            
            // Check if entity exists
            var existingEntity = await dbSet.FirstOrDefaultAsync(lambda, cancellationToken);
            
            if (existingEntity == null)
            {
                // Insert
                await dbSet.AddAsync(entity, cancellationToken);
            }
            else
            {
                // Update - copy all properties except excluded ones
                foreach (var property in typeof(T).GetProperties())
                {
                    // Skip key properties and excluded properties
                    if (keyProperties.Contains(property.Name) || 
                        (excludedUpdateProperties != null && excludedUpdateProperties.Contains(property.Name)))
                        continue;
                        
                    var value = property.GetValue(entity);
                    property.SetValue(existingEntity, value);
                }
            }
        }
        
        await context.SaveChangesAsync(cancellationToken);
    }
}