using FastData.Aop;
using System;
using System.Collections.Generic;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// AOP 拦截器测试
    /// </summary>
    public class AopTests
    {
        /// <summary>
        /// 测试自定义 AOP 拦截器实现
        /// </summary>
        [Fact]
        public void CustomAop_Implements_IFastAop()
        {
            var aop = new TestAop();
            Assert.NotNull(aop);
            Assert.IsAssignableFrom<IFastAop>(aop);
        }

        /// <summary>
        /// 测试 AOP 拦截器 Before 回调
        /// </summary>
        [Fact]
        public void Aop_Before_Called()
        {
            var aop = new TestAop();
            var called = false;
            aop.OnBefore = ctx => called = true;
            
            aop.Before(new BeforeContext { sql = "SELECT * FROM Users" });
            
            Assert.True(called);
        }

        /// <summary>
        /// 测试 AOP 拦截器 After 回调
        /// </summary>
        [Fact]
        public void Aop_After_Called()
        {
            var aop = new TestAop();
            var called = false;
            aop.OnAfter = ctx => called = true;
            
            aop.After(new AfterContext());
            
            Assert.True(called);
        }

        /// <summary>
        /// 测试 AOP 拦截器记录 SQL
        /// </summary>
        [Fact]
        public void Aop_Records_Sql()
        {
            var aop = new TestAop();
            var sqlList = new List<string>();
            aop.OnBefore = ctx => sqlList.Add(ctx.sql);
            
            aop.Before(new BeforeContext { sql = "SELECT * FROM Users" });
            aop.Before(new BeforeContext { sql = "INSERT INTO Users VALUES (...)" });
            
            Assert.Equal(2, sqlList.Count);
            Assert.Contains("SELECT", sqlList[0]);
            Assert.Contains("INSERT", sqlList[1]);
        }

        /// <summary>
        /// 测试 AOP 拦截器 Exception 回调
        /// </summary>
        [Fact]
        public void Aop_Exception_Called()
        {
            var aop = new TestAop();
            var called = false;
            aop.OnException = ctx => called = true;
            
            aop.Exception(new ExceptionContext());
            
            Assert.True(called);
        }

        /// <summary>
        /// 测试 AOP 拦截器 MapBefore 回调
        /// </summary>
        [Fact]
        public void Aop_MapBefore_Called()
        {
            var aop = new TestAop();
            var called = false;
            aop.OnMapBefore = ctx => called = true;
            
            aop.MapBefore(new MapBeforeContext());
            
            Assert.True(called);
        }

        /// <summary>
        /// 测试 AOP 拦截器 MapAfter 回调
        /// </summary>
        [Fact]
        public void Aop_MapAfter_Called()
        {
            var aop = new TestAop();
            var called = false;
            aop.OnMapAfter = ctx => called = true;
            
            aop.MapAfter(new MapAfterContext());
            
            Assert.True(called);
        }
    }

    /// <summary>
    /// 测试用 AOP 拦截器
    /// </summary>
    public class TestAop : IFastAop
    {
        public Action<BeforeContext> OnBefore { get; set; }
        public Action<AfterContext> OnAfter { get; set; }
        public Action<ExceptionContext> OnException { get; set; }
        public Action<MapBeforeContext> OnMapBefore { get; set; }
        public Action<MapAfterContext> OnMapAfter { get; set; }

        public void Before(BeforeContext context)
        {
            OnBefore?.Invoke(context);
        }

        public void After(AfterContext context)
        {
            OnAfter?.Invoke(context);
        }

        public void Exception(ExceptionContext context)
        {
            OnException?.Invoke(context);
        }

        public void MapBefore(MapBeforeContext context)
        {
            OnMapBefore?.Invoke(context);
        }

        public void MapAfter(MapAfterContext context)
        {
            OnMapAfter?.Invoke(context);
        }
    }
}
