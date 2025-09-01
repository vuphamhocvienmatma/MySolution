

namespace Application.Common.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Áp dụng phân trang cho một IQueryable.
    /// </summary>
    /// <param name="source">Nguồn dữ liệu IQueryable.</param>
    /// <param name="pageNumber">Số trang hiện tại (bắt đầu từ 1).</param>
    /// <param name="pageSize">Số lượng mục trên mỗi trang.</param>
    /// <returns>Một PagedResult chứa dữ liệu của trang hiện tại và thông tin phân trang.</returns>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber > 0 ? pageNumber : 1;
        pageSize = pageSize > 0 ? pageSize : 10;

        var count = await source.CountAsync(cancellationToken);

        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, count, pageNumber, pageSize);
    }
}