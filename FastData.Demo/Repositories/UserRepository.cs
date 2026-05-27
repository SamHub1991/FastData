using FastData.Demo.Models;
using FastData.Repository;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace FastData.Demo.Repositories
{
    /// <summary>
    /// 用户仓储接口
    /// </summary>
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<User> GetByIdAsync(int id);
        Task<List<User>> GetByDepartmentAsync(string department);
        Task<List<User>> GetActiveUsersAsync();
        Task<int> AddAsync(User user);
        Task<int> UpdateAsync(User user);
        Task<int> DeleteAsync(int id);
        Task<List<User>> GetPagedAsync(int pageIndex, int pageSize);
    }

    /// <summary>
    /// 用户仓储实现
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IReadRepository _readRepo;
        private readonly IWriteRepository _writeRepo;

        public UserRepository(IReadRepository readRepo, IWriteRepository writeRepo)
        {
            _readRepo = readRepo;
            _writeRepo = writeRepo;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _readRepo.QueryAsy<User>("SELECT * FROM Users ORDER BY Id", null);
        }

        public async Task<User> GetByIdAsync(int id)
        {
            var users = await _readRepo.QueryAsy<User>(
                "SELECT * FROM Users WHERE Id = @Id",
                new DbParameter[] { });
            return users.Count > 0 ? users[0] : null;
        }

        public async Task<List<User>> GetByDepartmentAsync(string department)
        {
            return await _readRepo.QueryAsy<User>(
                "SELECT * FROM Users WHERE Department = @Department",
                new DbParameter[] { });
        }

        public async Task<List<User>> GetActiveUsersAsync()
        {
            return await _readRepo.QueryAsy<User>(
                "SELECT * FROM Users WHERE IsActive = 1",
                null);
        }

        public async Task<int> AddAsync(User user)
        {
            user.CreateTime = DateTime.Now;
            user.IsActive = true;
            var result = await _writeRepo.AddAsy(user);
            return result.IsSuccess ? 1 : 0;
        }

        public async Task<int> UpdateAsync(User user)
        {
            user.UpdateTime = DateTime.Now;
            var result = await _writeRepo.UpdateAsy(user, a => new { a.UserName, a.Email, a.Phone, a.Age, a.Department, a.Salary, a.UpdateTime });
            return result.IsSuccess ? 1 : 0;
        }

        public async Task<int> DeleteAsync(int id)
        {
            var result = await _writeRepo.DeleteAsy<User>(a => a.Id == id);
            return result.IsSuccess ? 1 : 0;
        }

        public async Task<List<User>> GetPagedAsync(int pageIndex, int pageSize)
        {
            var sql = $"SELECT * FROM Users ORDER BY Id OFFSET {(pageIndex - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            return await _readRepo.QueryAsy<User>(sql, null);
        }
    }

    /// <summary>
    /// 订单仓储接口
    /// </summary>
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllAsync();
        Task<Order> GetByIdAsync(int id);
        Task<List<Order>> GetByUserIdAsync(int userId);
        Task<List<Order>> GetByStatusAsync(int status);
        Task<int> AddAsync(Order order);
        Task<int> UpdateStatusAsync(int id, int status);
    }

    /// <summary>
    /// 订单仓储实现
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly IReadRepository _readRepo;
        private readonly IWriteRepository _writeRepo;

        public OrderRepository(IReadRepository readRepo, IWriteRepository writeRepo)
        {
            _readRepo = readRepo;
            _writeRepo = writeRepo;
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _readRepo.QueryAsy<Order>("SELECT * FROM Orders ORDER BY Id DESC", null);
        }

        public async Task<Order> GetByIdAsync(int id)
        {
            var orders = await _readRepo.QueryAsy<Order>(
                "SELECT * FROM Orders WHERE Id = @Id",
                new DbParameter[] { });
            return orders.Count > 0 ? orders[0] : null;
        }

        public async Task<List<Order>> GetByUserIdAsync(int userId)
        {
            return await _readRepo.QueryAsy<Order>(
                "SELECT * FROM Orders WHERE UserId = @UserId ORDER BY CreateTime DESC",
                new DbParameter[] { });
        }

        public async Task<List<Order>> GetByStatusAsync(int status)
        {
            return await _readRepo.QueryAsy<Order>(
                "SELECT * FROM Orders WHERE Status = @Status",
                new DbParameter[] { });
        }

        public async Task<int> AddAsync(Order order)
        {
            order.CreateTime = DateTime.Now;
            order.Status = 0;
            order.OrderNo = $"ORD{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
            var result = await _writeRepo.AddAsy(order);
            return result.IsSuccess ? 1 : 0;
        }

        public async Task<int> UpdateStatusAsync(int id, int status)
        {
            var order = await GetByIdAsync(id);
            if (order == null) return 0;

            order.Status = status;
            if (status == 1) order.PayTime = DateTime.Now;
            var result = await _writeRepo.UpdateAsy(order, a => new { a.Status, a.PayTime });
            return result.IsSuccess ? 1 : 0;
        }
    }
}
