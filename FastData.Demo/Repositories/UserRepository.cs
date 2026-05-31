using FastData;
using FastData.Context;
using FastData.Demo.Models;
using FastUntility.Page;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastData.Demo.Repositories
{
    public interface IUserRepository
    {
        Task<List<AppUser>> GetAllAsync();
        Task<AppUser> GetByIdAsync(int id);
        Task<List<AppUser>> GetByDepartmentAsync(string department);
        Task<List<AppUser>> GetActiveUsersAsync();
        Task<(bool Success, string Message)> AddAsync(AppUser user);
        Task<(bool Success, string Message)> UpdateAsync(AppUser user);
        Task<(bool Success, string Message)> DeleteAsync(int id);
        Task<(List<AppUser> Data, int Total)> GetPagedAsync(int pageIndex, int pageSize);
    }

    public class UserRepository : IUserRepository
    {
        private readonly string _key;

        public UserRepository(string key = null)
        {
            _key = key;
        }

        public async Task<List<AppUser>> GetAllAsync()
        {
            return await Task.Run(() =>
                FastRead.Query<AppUser>(u => u.Id > 0, key: _key)
                    .OrderBy(u => u.Id)
                    .ToList<AppUser>());
        }

        public async Task<AppUser> GetByIdAsync(int id)
        {
            return await Task.Run(() =>
                FastRead.Query<AppUser>(u => u.Id == id, key: _key)
                    .ToItem<AppUser>());
        }

        public async Task<List<AppUser>> GetByDepartmentAsync(string department)
        {
            return await Task.Run(() =>
                FastRead.Query<AppUser>(u => u.Department == department, key: _key)
                    .ToList<AppUser>());
        }

        public async Task<List<AppUser>> GetActiveUsersAsync()
        {
            return await Task.Run(() =>
                FastRead.Query<AppUser>(u => u.IsActive == true, key: _key)
                    .ToList<AppUser>());
        }

        public async Task<(bool Success, string Message)> AddAsync(AppUser user)
        {
            user.CreateTime = DateTime.Now;
            user.IsActive = true;
            var result = await Task.Run(() => FastWrite.Add(user, key: _key));
            return (result.IsSuccess, result.Message);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(AppUser user)
        {
            user.UpdateTime = DateTime.Now;
            var result = await Task.Run(() =>
                FastWrite.Update(user, a => new { a.UserName, a.Email, a.Phone, a.Age, a.Department, a.Salary, a.UpdateTime }, key: _key));
            return (result.IsSuccess, result.Message);
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var result = await Task.Run(() =>
                FastWrite.Delete<AppUser>(a => a.Id == id, key: _key));
            return (result.IsSuccess, result.Message);
        }

        public async Task<(List<AppUser> Data, int Total)> GetPagedAsync(int pageIndex, int pageSize)
        {
            var result = await Task.Run(() =>
                FastRead.Query<AppUser>(u => u.Id > 0, key: _key)
                    .OrderBy(u => u.Id)
                    .ToPage<AppUser>(new PageModel { PageId = pageIndex, PageSize = pageSize }));
            return (result.list, result.pModel.TotalRecord);
        }
    }

    public interface IOrderRepository
    {
        Task<List<AppOrder>> GetAllAsync();
        Task<AppOrder> GetByIdAsync(int id);
        Task<List<AppOrder>> GetByUserIdAsync(int userId);
        Task<List<AppOrder>> GetByStatusAsync(int status);
        Task<(bool Success, string Message, string OrderNo)> AddAsync(AppOrder order);
        Task<(bool Success, string Message)> UpdateStatusAsync(int id, int status);
    }

    public class OrderRepository : IOrderRepository
    {
        private static readonly Random _random = new Random();
        private readonly string _key;

        public OrderRepository(string key = null)
        {
            _key = key;
        }

        public async Task<List<AppOrder>> GetAllAsync()
        {
            return await Task.Run(() =>
                FastRead.Query<AppOrder>(o => o.Id > 0, key: _key)
                    .OrderByDescending(o => o.Id)
                    .ToList<AppOrder>());
        }

        public async Task<AppOrder> GetByIdAsync(int id)
        {
            return await Task.Run(() =>
                FastRead.Query<AppOrder>(o => o.Id == id, key: _key)
                    .ToItem<AppOrder>());
        }

        public async Task<List<AppOrder>> GetByUserIdAsync(int userId)
        {
            return await Task.Run(() =>
                FastRead.Query<AppOrder>(o => o.UserId == userId, key: _key)
                    .OrderByDescending(o => o.CreateTime)
                    .ToList<AppOrder>());
        }

        public async Task<List<AppOrder>> GetByStatusAsync(int status)
        {
            return await Task.Run(() =>
                FastRead.Query<AppOrder>(o => o.Status == status, key: _key)
                    .ToList<AppOrder>());
        }

        public async Task<(bool Success, string Message, string OrderNo)> AddAsync(AppOrder order)
        {
            order.CreateTime = DateTime.Now;
            order.Status = 0;
            order.OrderNo = $"ORD{DateTime.Now:yyyyMMddHHmmss}{_random.Next(1000, 9999)}";
            var result = await Task.Run(() => FastWrite.Add(order, key: _key));
            return (result.IsSuccess, result.Message, order.OrderNo);
        }

        public async Task<(bool Success, string Message)> UpdateStatusAsync(int id, int status)
        {
            var order = await GetByIdAsync(id);
            if (order == null) return (false, "订单不存在");

            order.Status = status;
            if (status == 1) order.PayTime = DateTime.Now;
            var result = await Task.Run(() =>
                FastWrite.Update(order, a => new { a.Status, a.PayTime }, key: _key));
            return (result.IsSuccess, result.Message);
        }
    }
}
