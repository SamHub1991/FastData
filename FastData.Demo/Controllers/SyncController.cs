using System.Threading.Tasks;
using FastData.Demo.Services;
using FastData.Demo.Models;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 数据同步 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/sync")]
    public class SyncController : ControllerBase
    {
        private readonly IDataSyncService _syncService;

        public SyncController(IDataSyncService syncService)
        {
            _syncService = syncService;
        }

        /// <summary>
        /// 同步所有表
        /// </summary>
        [HttpPost("all")]
        public async Task<ActionResult<SyncResult>> SyncAll()
        {
            try
            {
                var result = await _syncService.SyncAllTablesAsync();
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
