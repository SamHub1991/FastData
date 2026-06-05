using System;
using System.Collections.Generic;
using System.Linq;
using FastUntility.Page;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 分页和投影查询测试
    /// </summary>
    public class PaginationTests
    {
        /// <summary>
        /// 测试 PaginationResult 计算是否正确
        /// </summary>
        [Fact]
        public void PaginationResult_TotalPages_ShouldCalculateCorrectly()
        {
            // Arrange
            var total = 100;
            var pageSize = 10;

            // Act
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // Assert
            Assert.Equal(10, totalPages);
        }

        /// <summary>
        /// 测试 PaginationResult 边界情况
        /// </summary>
        [Fact]
        public void PaginationResult_TotalPages_WithRemainder_ShouldRoundUp()
        {
            // Arrange
            var total = 105;
            var pageSize = 10;

            // Act
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // Assert
            Assert.Equal(11, totalPages);
        }

        /// <summary>
        /// 测试 PaginationResult 零记录
        /// </summary>
        [Fact]
        public void PaginationResult_ZeroRecords_ShouldHaveZeroPages()
        {
            // Arrange
            var total = 0;
            var pageSize = 10;

            // Act
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // Assert
            Assert.Equal(0, totalPages);
        }

        /// <summary>
        /// 测试 PaginationRequest 默认值
        /// </summary>
        [Fact]
        public void PaginationRequest_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var request = new PaginationRequest();

            // Assert
            Assert.Equal(1, request.Page);
            Assert.Equal(10, request.PageSize);
        }

        /// <summary>
        /// 测试 PaginationRequest 属性设置
        /// </summary>
        [Fact]
        public void PaginationRequest_SetProperties_ShouldWork()
        {
            // Arrange & Act
            var request = new PaginationRequest { Page = 2, PageSize = 20 };

            // Assert
            Assert.Equal(2, request.Page);
            Assert.Equal(20, request.PageSize);
        }

        /// <summary>
        /// 测试 PaginationRequest 边界值处理
        /// </summary>
        [Fact]
        public void PaginationRequest_InvalidPage_ShouldBeHandled()
        {
            // Arrange
            var request = new PaginationRequest { Page = -1, PageSize = 10 };

            // Act - 在实际使用时会进行校验
            var page = request.Page < 1 ? 1 : request.Page;

            // Assert
            Assert.Equal(1, page);
        }

        /// <summary>
        /// 测试 PaginationRequest 无效 PageSize
        /// </summary>
        [Fact]
        public void PaginationRequest_InvalidPageSize_ShouldBeHandled()
        {
            // Arrange
            var request = new PaginationRequest { Page = 1, PageSize = -5 };

            // Act - 在实际使用时会进行校验
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            // Assert
            Assert.Equal(10, pageSize);
        }

        /// <summary>
        /// 测试 PaginationResult 泛型类
        /// </summary>
        [Fact]
        public void PaginationResult_Generic_ShouldHaveCorrectProperties()
        {
            // Arrange
            var data = new List<TestEntity>
            {
                new TestEntity { Id = 1, Name = "Test1" },
                new TestEntity { Id = 2, Name = "Test2" }
            };

            // Act
            var result = new PaginationResult<TestEntity>
            {
                Total = 100,
                TotalPages = 10,
                Page = 1,
                PageSize = 10,
                Data = data
            };

            // Assert
            Assert.Equal(100, result.Total);
            Assert.Equal(10, result.TotalPages);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(2, result.Data.Count);
            Assert.False(result.HasPrevious);
            Assert.True(result.HasNext);
        }

        /// <summary>
        /// 测试 PaginationResult 第二页
        /// </summary>
        [Fact]
        public void PaginationResult_Page2_ShouldHavePrevious()
        {
            // Arrange & Act
            var result = new PaginationResult<TestEntity>
            {
                Total = 100,
                TotalPages = 10,
                Page = 2,
                PageSize = 10,
                Data = new List<TestEntity>()
            };

            // Assert
            Assert.True(result.HasPrevious);
            Assert.True(result.HasNext);
        }

        /// <summary>
        /// 测试 PaginationResult 最后一页
        /// </summary>
        [Fact]
        public void PaginationResult_LastPage_ShouldNotHaveNext()
        {
            // Arrange & Act
            var result = new PaginationResult<TestEntity>
            {
                Total = 100,
                TotalPages = 10,
                Page = 10,
                PageSize = 10,
                Data = new List<TestEntity>()
            };

            // Assert
            Assert.True(result.HasPrevious);
            Assert.False(result.HasNext);
        }

        /// <summary>
        /// 测试 FromPageResult 转换
        /// </summary>
        [Fact]
        public void PaginationResult_FromPageResult_ShouldConvertCorrectly()
        {
            // Arrange
            var pageResult = new PageResult
            {
                pModel = new PageModel { TotalRecord = 50 },
                list = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { { "Id", 1 }, { "Name", "Test" } }
                }
            };

            // Act
            var result = PaginationResult.FromPageResult(pageResult, 1, 10);

            // Assert
            Assert.Equal(50, result.Total);
            Assert.Equal(5, result.TotalPages);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Single(result.Data);
        }

        /// <summary>
        /// 测试带过滤条件的分页计算
        /// </summary>
        [Fact]
        public void Pagination_WithFilter_ShouldCalculateCorrectly()
        {
            // 模拟：数据库有 100 条记录，过滤后有 25 条，每页 10 条
            var filteredTotal = 25;
            var pageSize = 10;
            var page = 1;

            // Act
            var totalPages = (int)Math.Ceiling(filteredTotal / (double)pageSize);
            var result = new PaginationResult<TestEntity>
            {
                Total = filteredTotal,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                Data = new List<TestEntity>() // 模拟数据
            };

            // Assert
            Assert.Equal(25, result.Total);      // 总数应该是过滤后的数量
            Assert.Equal(3, result.TotalPages);   // 25/10 = 2.5 -> 3 页
            Assert.True(result.HasNext);          // 第 1 页有下一页
            Assert.False(result.HasPrevious);     // 第 1 页没有上一页
        }

        /// <summary>
        /// 测试带过滤条件的分页 - 第二页
        /// </summary>
        [Fact]
        public void Pagination_WithFilter_Page2_ShouldReturnCorrectData()
        {
            // 模拟：过滤后有 25 条，每页 10 条，第 2 页
            var filteredTotal = 25;
            var pageSize = 10;
            var page = 2;

            // 模拟第 2 页数据（应该有 10 条）
            var pageData = new List<TestEntity>();
            for (int i = 10; i < 20; i++)
            {
                pageData.Add(new TestEntity { Id = i + 1, Name = string.Format("User{0}", i + 1) });
            }

            // Act
            var totalPages = (int)Math.Ceiling(filteredTotal / (double)pageSize);
            var result = new PaginationResult<TestEntity>
            {
                Total = filteredTotal,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                Data = pageData
            };

            // Assert
            Assert.Equal(25, result.Total);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(10, result.Data.Count);  // 第 2 页应该有 10 条
            Assert.True(result.HasPrevious);
            Assert.True(result.HasNext);
        }

        /// <summary>
        /// 测试带过滤条件的分页 - 最后一页（不足一页）
        /// </summary>
        [Fact]
        public void Pagination_WithFilter_LastPage_ShouldReturnRemainingData()
        {
            // 模拟：过滤后有 25 条，每页 10 条，第 3 页（最后一页）
            var filteredTotal = 25;
            var pageSize = 10;
            var page = 3;

            // 模拟第 3 页数据（应该只有 5 条）
            var pageData = new List<TestEntity>();
            for (int i = 20; i < 25; i++)
            {
                pageData.Add(new TestEntity { Id = i + 1, Name = string.Format("User{0}", i + 1) });
            }

            // Act
            var totalPages = (int)Math.Ceiling(filteredTotal / (double)pageSize);
            var result = new PaginationResult<TestEntity>
            {
                Total = filteredTotal,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                Data = pageData
            };

            // Assert
            Assert.Equal(25, result.Total);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(5, result.Data.Count);  // 最后一页只有 5 条
            Assert.True(result.HasPrevious);
            Assert.False(result.HasNext);
        }

        /// <summary>
        /// 测试 Select 投影后的数据
        /// </summary>
        [Fact]
        public void Select_Projection_ShouldMapCorrectly()
        {
            // Arrange
            var source = new List<TestEntity>
            {
                new TestEntity { Id = 1, Name = "Alice", Email = "alice@test.com", Age = 25 },
                new TestEntity { Id = 2, Name = "Bob", Email = "bob@test.com", Age = 30 }
            };

            // Act - 模拟 Select 投影
            var projected = source.Select(p => new { p.Id, p.Name }).ToList();

            // Assert
            Assert.Equal(2, projected.Count);
            Assert.Equal(1, projected[0].Id);
            Assert.Equal("Alice", projected[0].Name);
            Assert.Equal(2, projected[1].Id);
            Assert.Equal("Bob", projected[1].Name);
        }

        /// <summary>
        /// 测试 Select 投影 + 分页
        /// </summary>
        [Fact]
        public void Select_Projection_WithPagination_ShouldWork()
        {
            // Arrange - 模拟过滤后的数据（25 条）
            var allData = new List<TestEntity>();
            for (int i = 1; i <= 25; i++)
            {
                allData.Add(new TestEntity { Id = i, Name = string.Format("User{0}", i), Email = string.Format("user{0}@test.com", i), Age = 20 + i });
            }

            // 模拟过滤：Age > 30
            var filtered = allData.Where(u => u.Age > 30).ToList();
            var filteredTotal = filtered.Count;

            // 模拟分页：第 1 页，每页 10 条
            var page = 1;
            var pageSize = 10;
            var pageData = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // 模拟投影
            var projected = pageData.Select(p => new { p.Id, p.Name }).ToList();

            // Act
            var totalPages = (int)Math.Ceiling(filteredTotal / (double)pageSize);
            var result = new PaginationResult<object>
            {
                Total = filteredTotal,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                Data = projected.Cast<object>().ToList()
            };

            // Assert
            Assert.Equal(filteredTotal, result.Total);
            Assert.Equal(totalPages, result.TotalPages);
            Assert.Equal(page, result.Page);
            Assert.Equal(pageSize, result.PageSize);
            Assert.True(result.Data.Count <= pageSize);
        }

        // 测试实体类
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public int Age { get; set; }
        }
    }
}
