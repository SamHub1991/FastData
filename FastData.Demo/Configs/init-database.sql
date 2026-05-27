-- FastData Demo 数据库初始化脚本
-- 适用于 SQL Server

-- 创建数据库
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FastDataDemo')
BEGIN
    CREATE DATABASE FastDataDemo;
END
GO

USE FastDataDemo;
GO

-- 创建用户表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserName NVARCHAR(50) NOT NULL,
        Email NVARCHAR(100),
        Phone NVARCHAR(20),
        Age INT DEFAULT 0,
        Department NVARCHAR(50),
        Salary DECIMAL(18,2) DEFAULT 0,
        IsActive BIT DEFAULT 1,
        CreateTime DATETIME DEFAULT GETDATE(),
        UpdateTime DATETIME NULL
    );
    
    -- 创建索引
    CREATE INDEX IX_Users_Department ON Users(Department);
    CREATE INDEX IX_Users_IsActive ON Users(IsActive);
    CREATE INDEX IX_Users_CreateTime ON Users(CreateTime);
END
GO

-- 创建订单表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderNo NVARCHAR(50) NOT NULL,
        UserId INT NOT NULL,
        ProductName NVARCHAR(100) NOT NULL,
        Quantity INT DEFAULT 1,
        UnitPrice DECIMAL(18,2) DEFAULT 0,
        TotalAmount DECIMAL(18,2) DEFAULT 0,
        Status INT DEFAULT 0,
        CreateTime DATETIME DEFAULT GETDATE(),
        PayTime DATETIME NULL,
        CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
    );
    
    -- 创建索引
    CREATE INDEX IX_Orders_UserId ON Orders(UserId);
    CREATE INDEX IX_Orders_Status ON Orders(Status);
    CREATE INDEX IX_Orders_CreateTime ON Orders(CreateTime);
    CREATE UNIQUE INDEX IX_Orders_OrderNo ON Orders(OrderNo);
END
GO

-- 创建商品表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE Products (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Category NVARCHAR(50),
        Price DECIMAL(18,2) DEFAULT 0,
        Stock INT DEFAULT 0,
        Description NVARCHAR(500),
        IsOnSale BIT DEFAULT 1,
        CreateTime DATETIME DEFAULT GETDATE()
    );
    
    -- 创建索引
    CREATE INDEX IX_Products_Category ON Products(Category);
    CREATE INDEX IX_Products_IsOnSale ON Products(IsOnSale);
END
GO

-- 创建操作日志表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OperationLogs')
BEGIN
    CREATE TABLE OperationLogs (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NULL,
        Action NVARCHAR(50) NOT NULL,
        Module NVARCHAR(50),
        Detail NVARCHAR(500),
        IpAddress NVARCHAR(50),
        CreateTime DATETIME DEFAULT GETDATE()
    );
    
    -- 创建索引
    CREATE INDEX IX_OperationLogs_UserId ON OperationLogs(UserId);
    CREATE INDEX IX_OperationLogs_Action ON OperationLogs(Action);
    CREATE INDEX IX_OperationLogs_CreateTime ON OperationLogs(CreateTime);
END
GO

-- 插入测试数据
IF NOT EXISTS (SELECT * FROM Users)
BEGIN
    INSERT INTO Users (UserName, Email, Phone, Age, Department, Salary, IsActive, CreateTime)
    VALUES 
        ('张三', 'zhangsan@example.com', '13800138001', 28, '技术部', 15000.00, 1, GETDATE()),
        ('李四', 'lisi@example.com', '13800138002', 32, '产品部', 18000.00, 1, GETDATE()),
        ('王五', 'wangwu@example.com', '13800138003', 25, '技术部', 12000.00, 1, GETDATE()),
        ('赵六', 'zhaoliu@example.com', '13800138004', 35, '市场部', 20000.00, 1, GETDATE()),
        ('钱七', 'qianqi@example.com', '13800138005', 29, '技术部', 16000.00, 0, GETDATE());
    
    INSERT INTO Orders (OrderNo, UserId, ProductName, Quantity, UnitPrice, TotalAmount, Status, CreateTime)
    VALUES 
        ('ORD20260527001', 1, '笔记本电脑', 1, 5999.00, 5999.00, 1, GETDATE()),
        ('ORD20260527002', 1, '机械键盘', 2, 299.00, 598.00, 3, GETDATE()),
        ('ORD20260527003', 2, '显示器', 1, 1999.00, 1999.00, 0, GETDATE()),
        ('ORD20260527004', 3, '鼠标', 3, 99.00, 297.00, 2, GETDATE()),
        ('ORD20260527005', 4, '耳机', 1, 399.00, 399.00, 1, GETDATE());
    
    INSERT INTO Products (Name, Category, Price, Stock, Description, IsOnSale, CreateTime)
    VALUES 
        ('笔记本电脑', '电子产品', 5999.00, 100, '高性能笔记本电脑', 1, GETDATE()),
        ('机械键盘', '外设', 299.00, 500, 'RGB机械键盘', 1, GETDATE()),
        ('显示器', '电子产品', 1999.00, 200, '27寸4K显示器', 1, GETDATE()),
        ('鼠标', '外设', 99.00, 1000, '无线鼠标', 1, GETDATE()),
        ('耳机', '音频', 399.00, 300, '降噪耳机', 1, GETDATE());
END
GO

PRINT '数据库初始化完成！';
GO
