using FluentAssertions;
using RBMS.Application.Common.Models;
using Xunit;

namespace RBMS.UnitTests.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0, false, false)]
    [InlineData(45, 20, 3, true, false)]   // page 1 of 3
    public void Computes_pagination_metadata(int total, int size, int expectedPages, bool hasNext, bool hasPrev)
    {
        var result = new PagedResult<int>(new[] { 1, 2, 3 }, total, page: 1, pageSize: size);

        result.TotalPages.Should().Be(expectedPages);
        result.HasNext.Should().Be(hasNext);
        result.HasPrevious.Should().Be(hasPrev);
    }

    [Fact]
    public void Last_page_has_no_next()
    {
        var result = new PagedResult<int>(new[] { 1 }, totalCount: 45, page: 3, pageSize: 20);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeTrue();
    }
}
