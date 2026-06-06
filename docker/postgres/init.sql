-- PostgreSQL 初始化脚本
-- 创建 fastdata 数据库（已在环境变量中创建）

-- 设置客户端编码
SET client_encoding TO 'UTF8';

-- 确保数据库存在（已在 POSTGRES_DB 环境变量中创建）
-- CREATE DATABASE fastdata;

-- 用户可以直接使用 postgres 账号连接
-- 账号：postgres
-- 密码：FastData@Test123
-- 数据库：fastdata

-- 注意：PostgreSQL 标识符规则
-- 1. 未加引号的标识符自动转为小写
-- 2. 加引号的标识符保持大小写
-- 示例：
--   SELECT * FROM AppUser WHERE Id > 0  -- 实际查询 appuser 表，列 id
--   SELECT * FROM "AppUser" WHERE "Id" > 0  -- 查询 AppUser 表，列 Id
