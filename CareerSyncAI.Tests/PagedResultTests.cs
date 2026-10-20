using CareerSyncAI.Models.ViewModels;
using Xunit;

namespace CareerSyncAI.Tests;

public class PagedResultTests
{
    [Fact]
    public void Create_ReturnsCorrectPageOfItems()
    {
        var source = Enumerable.Range(1, 25).ToList();
        var page = PagedResult<int>.Create(source, pageNumber: 2, pageSize: 10);

        Assert.Equal(10, page.Items.Count);
        Assert.Equal(11, page.Items.First());
        Assert.Equal(20, page.Items.Last());
    }

    [Fact]
    public void Create_CalculatesTotalPagesCorrectly()
    {
        var source = Enumerable.Range(1, 25).ToList();
        var page = PagedResult<int>.Create(source, pageNumber: 1, pageSize: 10);

        // 25 items at 10/page = 3 pages (last one partial)
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(25, page.TotalCount);
    }

    [Fact]
    public void Create_LastPage_HasRemainderAndNoNext()
    {
        var source = Enumerable.Range(1, 25).ToList();
        var page = PagedResult<int>.Create(source, pageNumber: 3, pageSize: 10);

        Assert.Equal(5, page.Items.Count);
        Assert.False(page.HasNext);
        Assert.True(page.HasPrevious);
    }

    [Fact]
    public void Create_FirstPage_HasNoPrevious()
    {
        var source = Enumerable.Range(1, 25).ToList();
        var page = PagedResult<int>.Create(source, pageNumber: 1, pageSize: 10);

        Assert.False(page.HasPrevious);
        Assert.True(page.HasNext);
    }

    [Fact]
    public void Create_PageNumberBelowOne_ClampsToPageOne()
    {
        var source = Enumerable.Range(1, 25).ToList();
        var page = PagedResult<int>.Create(source, pageNumber: 0, pageSize: 10);

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(1, page.Items.First());
    }

    [Fact]
    public void Create_EmptySource_ReturnsEmptyPageWithoutError()
    {
        var page = PagedResult<int>.Create(new List<int>(), pageNumber: 1, pageSize: 10);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasNext);
        Assert.False(page.HasPrevious);
    }

    [Fact]
    public void Create_PageNumberPastTheEnd_ClampsToLastPage()
    {
        var source = Enumerable.Range(1, 25).ToList();
        var page = PagedResult<int>.Create(source, pageNumber: 9, pageSize: 10);

        Assert.Equal(3, page.PageNumber);
        Assert.Equal(5, page.Items.Count);
    }
}
