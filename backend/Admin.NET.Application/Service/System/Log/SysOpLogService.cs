using Admin.NET.Core;
using Furion.DatabaseAccessor;
using Furion.DependencyInjection;
using Furion.DynamicApiController;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace Admin.NET.Application
{
    /// <summary>
    /// 操作日志服务
    /// </summary>
    [ApiDescriptionSettings(Name = "OpLog", Order = 100)]
    [Route("api")]
    public class SysOpLogService : ISysOpLogService, IDynamicApiController, ITransient
    {
        private readonly IRepository<SysLogOp> _sysOpLogRep; // 操作日志表仓储

        public SysOpLogService(IRepository<SysLogOp> sysOpLogRep)
        {
            _sysOpLogRep = sysOpLogRep;
        }

        /// <summary>
        /// 分页查询操作日志
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpGet("sysOpLog/page")]
        public async Task<PageResult<OpLogOutput>> QueryOpLogPageList([FromQuery] OpLogPageInput input)
        {
            var name = !string.IsNullOrEmpty(input.Name?.Trim());
            var success = !string.IsNullOrEmpty(input.Success.ToString());
            var searchBeginTime = !string.IsNullOrEmpty(input.SearchBeginTime?.Trim());
            var ReqMethod = !string.IsNullOrEmpty(input.ReqMethod?.Trim());
            var opLogs = await _sysOpLogRep.DetachedEntities
                                           .Where((name, u => EF.Functions.Like(u.Name, $"%{input.Name.Trim()}%")))
                                           .Where(success, u => u.Success == input.Success)
                                           .Where(ReqMethod, u => u.ReqMethod == input.ReqMethod)
                                           .Where(searchBeginTime, u => u.OpTime >= DateTime.Parse(input.SearchBeginTime.Trim()) &&
                                                                   u.OpTime <= DateTime.Parse(input.SearchEndTime.Trim()))
                                           .OrderBy(PageInputOrder.OrderBuilder(input)) // 封装了任意字段排序示例
                                           .ProjectToType<OpLogOutput>()
                                           .ToADPagedListAsync(input.PageNo, input.PageSize);
            return opLogs;
        }

        /// <summary>
        /// 清空操作日志
        /// </summary>
        /// <returns></returns>
        [HttpPost("sysOpLog/delete")]
        public async Task ClearOpLog()
        {
            await _sysOpLogRep.Context.DeleteRangeAsync<SysLogOp>();
        }
    }
}