-- FastData Demo SQLite 数据库初始化脚本

-- 创建用户表
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName TEXT NOT NULL,
    Email TEXT,
    Phone TEXT,
    Age INTEGER DEFAULT 0,
    Department TEXT,
    Salary DECIMAL DEFAULT 0,
    IsActive INTEGER DEFAULT 1,
    CreateTime TEXT DEFAULT CURRENT_TIMESTAMP,
    UpdateTime TEXT
);

-- 创建索引
CREATE INDEX IF NOT EXISTS IX_Users_Department ON Users(Department);
CREATE INDEX IF NOT EXISTS IX_Users_IsActive ON Users(IsActive);
CREATE INDEX IF NOT EXISTS IX_Users_CreateTime ON Users(CreateTime);

-- 创建订单表
CREATE TABLE IF NOT EXISTS Orders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderNo TEXT NOT NULL,
    UserId INTEGER NOT NULL,
    ProductName TEXT NOT NULL,
    Quantity INTEGER DEFAULT 1,
    UnitPrice DECIMAL DEFAULT 0,
    TotalAmount DECIMAL DEFAULT 0,
    Status INTEGER DEFAULT 0,
    CreateTime TEXT DEFAULT CURRENT_TIMESTAMP,
    PayTime TEXT,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS IX_Orders_UserId ON Orders(UserId);
CREATE INDEX IF NOT EXISTS IX_Orders_Status ON Orders(Status);
CREATE INDEX IF NOT EXISTS IX_Orders_CreateTime ON Orders(CreateTime);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Orders_OrderNo ON Orders(OrderNo);

-- 创建商品表
CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Category TEXT,
    Price DECIMAL DEFAULT 0,
    Stock INTEGER DEFAULT 0,
    Description TEXT,
    IsOnSale INTEGER DEFAULT 1,
    CreateTime TEXT DEFAULT CURRENT_TIMESTAMP
);

-- 创建索引
CREATE INDEX IF NOT EXISTS IX_Products_Category ON Products(Category);
CREATE INDEX IF NOT EXISTS IX_Products_IsOnSale ON Products(IsOnSale);

-- 创建操作日志表
CREATE TABLE IF NOT EXISTS OperationLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER,
    Action TEXT NOT NULL,
    Module TEXT,
    Detail TEXT,
    IpAddress TEXT,
    CreateTime TEXT DEFAULT CURRENT_TIMESTAMP
);

-- 创建索引
CREATE INDEX IF NOT EXISTS IX_OperationLogs_UserId ON OperationLogs(UserId);
CREATE INDEX IF NOT EXISTS IX_OperationLogs_Action ON OperationLogs(Action);
CREATE INDEX IF NOT EXISTS IX_OperationLogs_CreateTime ON OperationLogs(CreateTime);

-- 插入测试数据
INSERT OR IGNORE INTO Users (Id, UserName, Email, Phone, Age, Department, Salary, IsActive, CreateTime) VALUES
    (1, '张三', 'zhangsan@example.com', '13800138001', 28, '技术部', 15000.00, 1, CURRENT_TIMESTAMP),
    (2, '李四', 'lisi@example.com', '13800138002', 32, '产品部', 18000.00, 1, CURRENT_TIMESTAMP),
    (3, '王五', 'wangwu@example.com', '13800138003', 25, '技术部', 12000.00, 1, CURRENT_TIMESTAMP),
    (4, '赵六', 'zhaoliu@example.com', '13800138004', 35, '市场部', 20000.00, 1, CURRENT_TIMESTAMP),
    (5, '钱七', 'qianqi@example.com', '13800138005', 29, '技术部', 16000.00, 0, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO Orders (Id, OrderNo, UserId, ProductName, Quantity, UnitPrice, TotalAmount, Status, CreateTime) VALUES
    (1, 'ORD20260527001', 1, '笔记本电脑', 1, 5999.00, 5999.00, 1, CURRENT_TIMESTAMP),
    (2, 'ORD20260527002', 1, '机械键盘', 2, 299.00, 598.00, 3, CURRENT_TIMESTAMP),
    (3, 'ORD20260527003', 2, '显示器', 1, 1999.00, 1999.00, 0, CURRENT_TIMESTAMP),
    (4, 'ORD20260527004', 3, '鼠标', 3, 99.00, 297.00, 2, CURRENT_TIMESTAMP),
    (5, 'ORD20260527005', 4, '耳机', 1, 399.00, 399.00, 1, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO Products (Id, Name, Category, Price, Stock, Description, IsOnSale, CreateTime) VALUES
    (1, '笔记本电脑', '电子产品', 5999.00, 100, '高性能笔记本电脑', 1, CURRENT_TIMESTAMP),
    (2, '机械键盘', '外设', 299.00, 500, 'RGB机械键盘', 1, CURRENT_TIMESTAMP),
    (3, '显示器', '电子产品', 1999.00, 200, '27寸4K显示器', 1, CURRENT_TIMESTAMP),
    (4, '鼠标', '外设', 99.00, 1000, '无线鼠标', 1, CURRENT_TIMESTAMP),
    (5, '耳机', '音频', 399.00, 300, '降噪耳机', 1, CURRENT_TIMESTAMP);

SELECT '数据库初始化完成！' AS Message;