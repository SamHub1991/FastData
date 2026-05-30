using FastData;
using FastData.Demo.Models;
using FastUntility.Page;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastData.Demo.Repositories
{
    /// <summary>
    /// 用户仓储接口
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// 获取所有用户
        /// </summary>
        /// <returns>用户列表</returns>
        Task<List<AppUser>> GetAllAsync();

        /// <summary>
        /// 根据 ID 获取用户
        /// </summary>
        /// <param name="id">用户 ID</param>
        /// <returns>用户信息</returns>
        Task<AppUser> GetByIdAsync(int id);

        /// <summary>
        /// 根据部门获取用户
        /// </summary>
        /// <param name="department">部门名称</param>
        /// <returns>用户列表</returns>
        Task<List<AppUser>> GetByDepartmentAsync(string department);

        /// <summary>
        /// 获取活跃用户
        /// </summary>
        /// <returns>用户列表</returns>
        Task<List<AppUser>> GetActiveUsersAsync();

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>影响行数</returns>
        Task<int> AddAsync(AppUser user);

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>影响行数</returns>
        Task<int> UpdateAsync(AppUser user);

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="id">用户 ID</param>
        /// <returns>影响行数</returns>
        Task<int> DeleteAsync(int id);

        /// <summary>
        /// 分页获取用户
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页记录数</param>
        /// <returns>用户列表</returns>
        Task<List<AppUser>> GetPagedAsync(int pageIndex, int pageSize);
    }

    /// <summary>
    /// 用户仓储实现
    /// </summary>
    public class UserRepository : IUserRepository
    {
        public async Task<List<AppUser>> GetAllAsync()
        {
            return await Task.Run(() => 
                FastRead.Query<AppUser>(u => u.Id > 0)
                    .OrderBy(u => u.Id)
                    .ToList());
        }

        public async Task<AppUser> GetByIdAsync(int id)
        {
            return await Task.Run(() => 
                FastRead.Query<AppUser>(u => u.Id == id)
                    .ToItem());
        }

        public async Task<List<AppUser>> GetByDepartmentAsync(string department)
        {
            return await Task.Run(() => 
                FastRead.Query<AppUser>(u => u.Department == department)
                    .ToList());
        }

        public async Task<List<AppUser>> GetActiveUsersAsync()
        {
            return await Task.Run(() => 
                FastRead.Query<AppUser>(u => u.IsActive == true)
                    .ToList());
        }

        public async Task<int> AddAsync(AppUser user)
        {
            try
            {
                user.CreateTime = DateTime.Now;
                user.IsActive = true;
                var result = await Task.Run(() => FastWrite.Add(user));
                return result.IsSuccess ? 1 : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> UpdateAsync(AppUser user)
        {
            user.UpdateTime = DateTime.Now;
            var result = await Task.Run(() => 
                FastWrite.Update(user, a => new { a.UserName, a.Email, a.Phone, a.Age, a.Department, a.Salary, a.UpdateTime }));
            return result.IsSuccess ? 1 : 0;
        }

        public async Task<int> DeleteAsync(int id)
        {
            var result = await Task.Run(() => 
                FastWrite.Delete<AppUser>(a => a.Id == id));
            return result.IsSuccess ? 1 : 0;
        }

        public async Task<List<AppUser>> GetPagedAsync(int pageIndex, int pageSize)
        {
            var result = await Task.Run(() => 
                FastRead.Query<AppUser>(u => u.Id > 0)
                    .OrderBy(u => u.Id)
                    .ToPage<AppUser>(new PageModel { PageId = pageIndex, PageSize = pageSize }));
            return result.list;
        }
    }

    /// <summary>
    /// 订单仓储接口
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// 获取所有订单
        /// </summary>
        /// <returns>订单列表</returns>
        Task<List<AppOrder>> GetAllAsync();

        /// <summary>
        /// 根据 ID 获取订单
        /// </summary>
        /// <param name="id">订单 ID</param>
        /// <returns>订单信息</returns>
        Task<AppOrder> GetByIdAsync(int id);

        /// <summary>
        /// 根据用户 ID 获取订单
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <returns>订单列表</returns>
        Task<List<AppOrder>> GetByUserIdAsync(int userId);

        /// <summary>
        /// 根据状态获取订单
        /// </summary>
        /// <param name="status">订单状态</param>
        /// <returns>订单列表</returns>
        Task<List<AppOrder>> GetByStatusAsync(int status);

        /// <summary>
        /// 添加订单
        /// </summary>
        /// <param name="order">订单信息</param>
        /// <returns>影响行数</returns>
        Task<int> AddAsync(AppOrder order);

        /// <summary>
        /// 更新订单状态
        /// </summary>
        /// <param name="id">订单 ID</param>
        /// <param name="status">订单状态</param>
        /// <returns>影响行数</returns>
        Task<int> UpdateStatusAsync(int id, int status);
    }

    /// <summary>
    /// 订单仓储实现
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        public async Task<List<AppOrder>> GetAllAsync()
        {
            return await Task.Run(() => 
                FastRead.Query<AppOrder>(o => o.Id > 0)
                    .OrderByDescending(o => o.Id)
                    .ToList());
        }

        public async Task<AppOrder> GetByIdAsync(int id)
        {
            return await Task.Run(() => 
                FastRead.Query<AppOrder>(o => o.Id == id)
                    .ToItem());
        }

        public async Task<List<AppOrder>> GetByUserIdAsync(int userId)
        {
            return await Task.Run(() => 
                FastRead.Query<AppOrder>(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreateTime)
                    .ToList());
        }

        public async Task<List<AppOrder>> GetByStatusAsync(int status)
        {
            return await Task.Run(() => 
                FastRead.Query<AppOrder>(o => o.Status == status)
                    .ToList());
        }

        public async Task<int> AddAsync(AppOrder order)
        {
            order.CreateTime = DateTime.Now;
            order.Status = 0;
            order.OrderNo = $"ORD{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
            var result = await Task.Run(() => FastWrite.Add(order));
            return result.IsSuccess ? 1 : 0;
        }

        public async Task<int> UpdateStatusAsync(int id, int status)
        {
            var order = await GetByIdAsync(id);
            if (order == null) return 0;

            order.Status = status;
            if (status == 1) order.PayTime = DateTime.Now;
            var result = await Task.Run(() => 
                FastWrite.Update(order, a => new { a.Status, a.PayTime }));
            return result.IsSuccess ? 1 : 0;
        }
    }
}
