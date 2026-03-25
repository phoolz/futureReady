namespace Apiary.Models.Tables;

/// <summary>
/// Query parameters for sortable, pageable, searchable tables.
/// Designed for use with URL query parameters in Static SSR pages.
/// </summary>
public class TableQuery
{
    public string? Search { get; set; }
    public string Sort { get; set; } = "name";
    public string Dir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 25;

    public bool IsDescending => Dir.Equals("desc", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the opposite direction for toggle behavior.
    /// </summary>
    public string ToggleDir(string column) =>
        Sort.Equals(column, StringComparison.OrdinalIgnoreCase) && !IsDescending ? "desc" : "asc";

    /// <summary>
    /// Builds a query string for a specific column sort.
    /// </summary>
    public string SortUrl(string column)
    {
        var dir = ToggleDir(column);
        var queryParams = new List<string> { $"sort={column}", $"dir={dir}" };

        if (!string.IsNullOrEmpty(Search))
            queryParams.Add($"search={Uri.EscapeDataString(Search)}");

        // Reset to page 1 when sorting changes
        queryParams.Add("page=1");

        if (Size != 25)
            queryParams.Add($"size={Size}");

        return "?" + string.Join("&", queryParams);
    }

    /// <summary>
    /// Builds a query string for pagination.
    /// </summary>
    public string PageUrl(int page)
    {
        var queryParams = new List<string> { $"sort={Sort}", $"dir={Dir}", $"page={page}" };

        if (!string.IsNullOrEmpty(Search))
            queryParams.Add($"search={Uri.EscapeDataString(Search)}");

        if (Size != 25)
            queryParams.Add($"size={Size}");

        return "?" + string.Join("&", queryParams);
    }

    /// <summary>
    /// Checks if a column is the current sort column.
    /// </summary>
    public bool IsSortedBy(string column) =>
        Sort.Equals(column, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Paged result container for table data.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }

    public int TotalPages => Size > 0 ? (int)Math.Ceiling((double)TotalCount / Size) : 0;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public int FirstItemIndex => TotalCount == 0 ? 0 : (Page - 1) * Size + 1;
    public int LastItemIndex => Math.Min(Page * Size, TotalCount);

    public static PagedResult<T> Empty(int page = 1, int size = 25) => new()
    {
        Items = new List<T>(),
        TotalCount = 0,
        Page = page,
        Size = size
    };
}
