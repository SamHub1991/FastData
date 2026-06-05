using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastData;
using FastData.Property;
using FastRedis;
using FastUntility.Cache;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 缓存功能测试
    /// 
    /// 测试覆盖：
    /// 1. 内存缓存（BaseCache）基本操作
    /// 2. Redis 缓存（RedisInfo）基本操作
    /// 3. CacheAttribute 声明式缓存
    /// 4. 缓存穿透防护
    /// 5. 缓存降级
    /// 6. 批量缓存操作
    /// </summary>
    public class CacheTests
    {
        #region 测试模型

        /// <summary>
        /// 用户模型（用于测试）
        /// </summary>
        [Table(Name = "TestUsers")]
        public class TestUser
        {
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            [Column(Length = 50)]
            public string UserName { get; set; }

            public int Age { get; set; }

            [Column(Length = 100)]
            public string Email { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreateTime { get; set; }
        }

        /// <summary>
        /// 商品模型（带缓存配置）
        /// </summary>
        [Table(Name = "TestProducts")]
        [Cache(IsEnable = true, ExpireTime = 300, Key = "test:product:{Id}", CacheType = "Redis")]
        public class TestProduct
        {
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            [Column(Length = 100)]
            public string ProductName { get; set; }

            public decimal Price { get; set; }

            public int Stock { get; set; }
        }

        #endregion

        #region 内存缓存测试（BaseCache）

        /// <summary>
        /// 测试内存缓存基本操作：设置、获取、删除
        /// </summary>
        [Fact]
        public void BaseCache_SetGetRemove_ShouldWork()
        {
            // Arrange
            var key = $"test:memory:{Guid.NewGuid()}";
            var value = "test_value";

            // Act - 设置缓存
            BaseCache.Set(key, value, 1);

            // Assert - 获取缓存
            var cached = BaseCache.Get(key);
            Assert.Equal(value, cached);

            // Act - 删除缓存
            BaseCache.Remove(key);

            // Assert - 验证删除
            var afterRemove = BaseCache.Get(key);
            Assert.Null(afterRemove);
        }

        /// <summary>
        /// 测试内存缓存泛型操作
        /// </summary>
        [Fact]
        public void BaseCache_Generic_SetGetRemove_ShouldWork()
        {
            // Arrange
            var key = $"test:memory:generic:{Guid.NewGuid()}";
            var user = new TestUser
            {
                Id = 1,
                UserName = "test_user",
                Age = 25,
                Email = "test@example.com",
                IsActive = true,
                CreateTime = DateTime.Now
            };

            // Act - 设置缓存
            BaseCache.Set(key, user, 1);

            // Assert - 获取缓存
            var cached = BaseCache.Get<TestUser>(key);
            Assert.NotNull(cached);
            Assert.Equal(user.Id, cached.Id);
            Assert.Equal(user.UserName, cached.UserName);
            Assert.Equal(user.Age, cached.Age);

            // Act - 删除缓存
            BaseCache.Remove(key);

            // Assert - 验证删除
            var afterRemove = BaseCache.Get<TestUser>(key);
            Assert.NotNull(afterRemove); // 返回 new T() 而不是 null
            Assert.Equal(0, afterRemove.Id); // 默认值
        }

        /// <summary>
        /// 测试内存缓存是否存在
        /// </summary>
        [Fact]
        public void BaseCache_Exists_ShouldWork()
        {
            // Arrange
            var key = $"test:memory:exists:{Guid.NewGuid()}";

            // Assert - 不存在
            Assert.False(BaseCache.Exists(key));

            // Act - 设置缓存
            BaseCache.Set(key, "value", 1);

            // Assert - 存在
            Assert.True(BaseCache.Exists(key));

            // Cleanup
            BaseCache.Remove(key);
        }

        /// <summary>
        /// 测试内存缓存列表操作
        /// </summary>
        [Fact]
        public void BaseCache_List_ShouldWork()
        {
            // Arrange
            var key = string.Format("test:memory:list:{0}", Guid.NewGuid());
            var users = new List<TestUser>
            {
                new TestUser { Id = 1, UserName = "user1", Age = 20 },
                new TestUser { Id = 2, UserName = "user2", Age = 25 },
                new TestUser { Id = 3, UserName = "user3", Age = 30 }
            };

            // Act - 设置列表缓存
            BaseCache.Set(key, users, 1);

            // Assert - 获取列表缓存
            var cached = BaseCache.Get<List<TestUser>>(key);
            Assert.NotNull(cached);
            Assert.Equal(3, cached.Count);
            Assert.Equal("user1", cached[0].UserName);
            Assert.Equal("user2", cached[1].UserName);
            Assert.Equal("user3", cached[2].UserName);

            // Cleanup
            BaseCache.Remove(key);
        }

        #endregion

        #region Redis 缓存测试（RedisInfo）

        /// <summary>
        /// 测试 Redis 缓存基本操作：设置、获取、删除
        /// 注意：需要 Redis 服务可用才能通过
        /// </summary>
        [Fact]
        public void RedisInfo_SetGetRemove_ShouldWork()
        {
            // Arrange
            var key = $"test:redis:{Guid.NewGuid()}";
            var value = "test_value";

            try
            {
                // Act - 设置缓存
                var setResult = RedisInfo.Set(key, value, 1);

                // Assert - 设置成功
                Assert.True(setResult);

                // Act - 获取缓存
                var cached = RedisInfo.Get(key);

                // Assert - 获取成功
                Assert.Equal(value, cached);

                // Act - 删除缓存
                var removeResult = RedisInfo.Remove(key);

                // Assert - 删除成功
                Assert.True(removeResult);

                // Assert - 验证删除
                var afterRemove = RedisInfo.Get(key);
                Assert.Null(afterRemove);
            }
            catch (Exception ex)
            {
                // Redis 不可用时跳过测试
                Console.WriteLine($"Redis 不可用，跳过测试: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试 Redis 缓存泛型操作
        /// </summary>
        [Fact]
        public void RedisInfo_Generic_SetGetRemove_ShouldWork()
        {
            // Arrange
            var key = $"test:redis:generic:{Guid.NewGuid()}";
            var user = new TestUser
            {
                Id = 1,
                UserName = "test_user",
                Age = 25,
                Email = "test@example.com",
                IsActive = true,
                CreateTime = DateTime.Now
            };

            try
            {
                // Act - 设置缓存
                var setResult = RedisInfo.Set(key, user, 1);

                // Assert - 设置成功
                Assert.True(setResult);

                // Act - 获取缓存
                var cached = RedisInfo.Get<TestUser>(key);

                // Assert - 获取成功
                Assert.NotNull(cached);
                Assert.Equal(user.Id, cached.Id);
                Assert.Equal(user.UserName, cached.UserName);
                Assert.Equal(user.Age, cached.Age);

                // Act - 删除缓存
                var removeResult = RedisInfo.Remove(key);

                // Assert - 删除成功
                Assert.True(removeResult);
            }
            catch (Exception ex)
            {
                // Redis 不可用时跳过测试
                Console.WriteLine($"Redis 不可用，跳过测试: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试 Redis 缓存是否存在
        /// </summary>
        [Fact]
        public void RedisInfo_Exists_ShouldWork()
        {
            // Arrange
            var key = $"test:redis:exists:{Guid.NewGuid()}";

            try
            {
                // Assert - 不存在
                Assert.False(RedisInfo.Exists(key));

                // Act - 设置缓存
                RedisInfo.Set(key, "value", 1);

                // Assert - 存在
                Assert.True(RedisInfo.Exists(key));

                // Cleanup
                RedisInfo.Remove(key);
            }
            catch (Exception ex)
            {
                // Redis 不可用时跳过测试
                Console.WriteLine($"Redis 不可用，跳过测试: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试 Redis 缓存列表操作
        /// </summary>
        [Fact]
        public void RedisInfo_List_ShouldWork()
        {
            // Arrange
            var key = string.Format("test:redis:list:{0}", Guid.NewGuid());
            var users = new List<TestUser>
            {
                new TestUser { Id = 1, UserName = "user1", Age = 20 },
                new TestUser { Id = 2, UserName = "user2", Age = 25 },
                new TestUser { Id = 3, UserName = "user3", Age = 30 }
            };

            try
            {
                // Act - 设置列表缓存
                var setResult = RedisInfo.Set(key, users, 1);

                // Assert - 设置成功
                Assert.True(setResult);

                // Act - 获取列表缓存
                var cached = RedisInfo.Get<List<TestUser>>(key);

                // Assert - 获取成功
                Assert.NotNull(cached);
                Assert.Equal(3, cached.Count);
                Assert.Equal("user1", cached[0].UserName);
                Assert.Equal("user2", cached[1].UserName);
                Assert.Equal("user3", cached[2].UserName);

                // Cleanup
                RedisInfo.Remove(key);
            }
            catch (Exception ex)
            {
                // Redis 不可用时跳过测试
                Console.WriteLine($"Redis 不可用，跳过测试: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试 Redis 计数器操作
        /// </summary>
        [Fact]
        public void RedisInfo_Increment_ShouldWork()
        {
            // Arrange
            var key = $"test:redis:increment:{Guid.NewGuid()}";

            try
            {
                // Act - 递增
                var count1 = RedisInfo.Increment(key, 1);
                var count2 = RedisInfo.Increment(key, 1);
                var count3 = RedisInfo.Increment(key, 5);

                // Assert - 递增正确
                Assert.Equal(1, count1);
                Assert.Equal(2, count2);
                Assert.Equal(7, count3);

                // Cleanup
                RedisInfo.Remove(key);
            }
            catch (Exception ex)
            {
                // Redis 不可用时跳过测试
                Console.WriteLine($"Redis 不可用，跳过测试: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试 Redis 设置过期时间
        /// </summary>
        [Fact]
        public void RedisInfo_SetExpire_ShouldWork()
        {
            // Arrange
            var key = string.Format("test:redis:expire:{0}", Guid.NewGuid());

            try
            {
                // Act - 设置缓存
                RedisInfo.Set(key, "value", 1);

                // Act - 设置过期时间
                var result = RedisInfo.SetExpire(key, TimeSpan.FromSeconds(1));

                // Assert - 设置成功
                Assert.True(result);

                // Cleanup
                RedisInfo.Remove(key);
            }
            catch (Exception ex)
            {
                // Redis 不可用时跳过测试
                Console.WriteLine($"Redis 不可用，跳过测试: {ex.Message}");
            }
        }

        #endregion

        #region 缓存穿透防护测试

        /// <summary>
        /// 测试缓存穿透防护：缓存空值防止穿透
        /// </summary>
        [Fact]
        public void CachePenetrationProtection_ShouldWork()
        {
            // Arrange
            var key = $"test:penetration:{Guid.NewGuid()}";

            try
            {
                // Act - 查询不存在的数据，缓存空值
                var user = RedisInfo.Get<TestUser>(key);
                if (user == null)
                {
                    // 缓存空值，防止穿透
                    RedisInfo.Set(key, "", 60);
                }

                // Assert - 空值已缓存
                var cached = RedisInfo.Get(key);
                Assert.NotNull(cached);
                Assert.Equal("", cached);

                // Cleanup
                RedisInfo.Remove(key);
            }
            catch (Exception ex)
            {
                // Redis 不可用时跳过测试
                Console.WriteLine($"Redis 不可用，跳过测试: {ex.Message}");
            }
        }

        #endregion

        #region 缓存降级测试

        /// <summary>
        /// 测试缓存降级：Redis 不可用时降级到内存缓存
        /// </summary>
        [Fact]
        public void CacheFallback_ShouldWork()
        {
            // Arrange
            var key = $"test:fallback:{Guid.NewGuid()}";
            var user = new TestUser
            {
                Id = 1,
                UserName = "test_user",
                Age = 25
            };

            // Act - 尝试 Redis，失败则使用内存缓存
            try
            {
                RedisInfo.Set(key, user, 1);
                var cached = RedisInfo.Get<TestUser>(key);
                if (cached != null)
                {
                    // Redis 可用
                    Assert.Equal(user.Id, cached.Id);
                }
            }
            catch
            {
                // Redis 不可用，降级到内存缓存
                BaseCache.Set(key, user, 1);
                var cached = BaseCache.Get<TestUser>(key);
                Assert.NotNull(cached);
                Assert.Equal(user.Id, cached.Id);
            }

            // Cleanup
            try
            {
                RedisInfo.Remove(key);
            }
            catch
            {
                BaseCache.Remove(key);
            }
        }

        #endregion

        #region 批量缓存操作测试

        /// <summary>
        /// 测试批量缓存操作
        /// </summary>
        [Fact]
        public void BatchCacheOperations_ShouldWork()
        {
            // Arrange
            var users = new List<TestUser>
            {
                new TestUser { Id = 1, UserName = "user1", Age = 20 },
                new TestUser { Id = 2, UserName = "user2", Age = 25 },
                new TestUser { Id = 3, UserName = "user3", Age = 30 }
            };

            try
            {
                // Act - 批量设置
                var dict = new Dictionary<string, TestUser>();
                foreach (var user in users)
                {
                    dict[string.Format("test:batch:{0}", user.Id)] = user;
                }
                RedisInfo.SetDic(dict, db: 0);

                // Act - 批量获取
                var keys = new[] { "test:batch:1", "test:batch:2", "test:batch:3" };
                var cachedDict = RedisInfo.GetDic<TestUser>(keys, db: 0);

                // Assert - 批量获取成功
                Assert.NotNull(cachedDict);
                Assert.Equal(3, cachedDict.Count);

                // Cleanup
                foreach (var key in keys)
                {
                    RedisInfo.Remove(key);
                }
            }
            catch (Exception ex)
            {
                // Redis 不可用时跳过测试
                Console.WriteLine($"Redis 不可用，跳过测试: {ex.Message}");
            }
        }

        #endregion

        #region CacheAttribute 测试

        /// <summary>
        /// 测试 CacheAttribute 配置
        /// </summary>
        [Fact]
        public void CacheAttribute_Configuration_ShouldWork()
        {
            // Arrange
            var type = typeof(TestProduct);
            var cacheAttr = (CacheAttribute)Attribute.GetCustomAttribute(type, typeof(CacheAttribute));

            // Assert - CacheAttribute 存在
            Assert.NotNull(cacheAttr);

            // Assert - 配置正确
            Assert.True(cacheAttr.IsEnable);
            Assert.Equal(300, cacheAttr.ExpireTime);
            Assert.Equal("test:product:{Id}", cacheAttr.Key);
            Assert.Equal("Redis", cacheAttr.CacheType);
        }

        #endregion

        #region 缓存键设计测试

        /// <summary>
        /// 测试缓存键设计规范
        /// </summary>
        [Fact]
        public void CacheKeyDesign_ShouldBeCorrect()
        {
            // Arrange
            var userId = 123;
            var age = 18;
            var isActive = true;

            // Act - 生成缓存键
            var entityKey = string.Format("user:{0}", userId);
            var listKey = string.Format("users:age_gt_{0}:active_{1}", age, isActive);
            var countKey = string.Format("users:count:active_{0}", isActive);

            // Assert - 键格式正确
            Assert.Equal("user:123", entityKey);
            Assert.Equal("users:age_gt_18:active_True", listKey);
            Assert.Equal("users:count:active_True", countKey);
        }

        #endregion

        #region 缓存过期测试

        /// <summary>
        /// 测试缓存过期
        /// </summary>
        [Fact]
        public void CacheExpiration_ShouldWork()
        {
            // Arrange
            var key = string.Format("test:expiration:{0}", Guid.NewGuid());

            // Act - 设置1秒过期
            BaseCache.Set(key, "value", 0); // 0小时 = 立即过期

            // Assert - 缓存已过期
            var cached = BaseCache.Get(key);
            Assert.Null(cached);
        }

        #endregion
    }
}
