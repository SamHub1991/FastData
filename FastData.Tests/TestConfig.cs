namespace FastData.Tests;

/// <summary>
/// 测试配置初始化类
/// 使用自动扫描注册机制
/// </summary>
public static class TestConfig
{
    static TestConfig()
    {
        // 使用自动扫描注册机制，无需手动指定提供程序
        FastData.Infrastructure.DbProviderAutoRegistrar.Register();
    }
    
    public static void Init() { }
}
