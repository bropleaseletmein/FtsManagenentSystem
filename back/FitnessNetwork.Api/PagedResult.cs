namespace FitnessNetwork.Api;

public record PagedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize)
{
    public int TotalPages => (Total + PageSize - 1) / PageSize;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
