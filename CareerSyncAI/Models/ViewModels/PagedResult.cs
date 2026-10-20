namespace CareerSyncAI.Models.ViewModels;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    public static PagedResult<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        var all = source.ToList();
        var totalCount = all.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        pageNumber = Math.Max(1, Math.Min(pageNumber, Math.Max(1, totalPages)));

        return new PagedResult<T>
        {
            Items = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
