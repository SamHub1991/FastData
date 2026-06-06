-- MySQL 初始化脚本
-- 创建 fastdata 数据库（已在环境变量中创建，此处为额外配置）

-- 设置字符集
SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- 确保数据库存在
CREATE DATABASE IF NOT EXISTS fastdata DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 用户可以直接使用 root 账号连接
-- 账号：root
-- 密码：FastData@Test123
-- 数据库：fastdata
