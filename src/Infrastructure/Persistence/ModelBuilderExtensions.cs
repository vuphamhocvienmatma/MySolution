
namespace Infrastructure.Persistence;

public static class ModelBuilderExtensions
{
    public static void AddQueryFilter<TInterface>(this ModelBuilder modelBuilder, Expression<Func<TInterface, bool>> filter)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(et => et.BaseType == null && et.ClrType.GetInterface(typeof(TInterface).Name) != null);

        foreach (var entityType in entityTypes)
        {
            var parameter = Expression.Parameter(entityType.ClrType);
            var body = ReplacingExpressionVisitor.Replace(filter.Parameters.Single(), parameter, filter.Body);
            var lambda = Expression.Lambda(body, parameter);

            entityType.SetQueryFilter(lambda);
        }
    }
}