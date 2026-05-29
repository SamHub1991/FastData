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
        Task<List<AppUser>> GetAllAsync();
        Task<AppUser> GetByIdAsync(int id);
        Task<List<AppUser>> GetByDepartmentAsync(string department);
        Task<List<AppUser>> GetActiveUsersAsync();
        Task<int> AddAsync(AppUser user);
        Task<int> UpdateAsync(AppUser user);
        Task<int> DeleteAsync(int id);
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
        Task<List<AppOrder>> GetAllAsync();
        Task<AppOrder> GetByIdAsync(int id);
        Task<List<AppOrder>> GetByUserIdAsync(int userId);
        Task<List<AppOrder>> GetByStatusAsync(int status);
        Task<int> AddAsync(AppOrder order);
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
