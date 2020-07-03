using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Linq.Dynamic.Core;
using Alyio.AspNetCore.ApiMessages;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using Microsoft.AspNetCore.WebUtilities;

namespace Dataflow.SaicEnergyTracker.Controllers
{
    [Route("api/v1/soc-prediction-model")]
    [ApiController]
    public class SocPredictionModelController : Controller
    {
        private readonly CarModelContext _context;
        public SocPredictionModelController(CarModelContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
        }

        /// <summary>
        /// 验证当前车机模型是否最新
        /// </summary>
        /// <param name="car_version"></param>
        /// <param name="car_id"></param>
        /// <param name="user_id"></param>
        /// <param name="so_version"></param>
        /// <param name="config_version"></param>
        /// <returns></returns>
        [ServiceFilter(typeof(LoggingActionFilter))]
        [HttpGet]
        public async Task<SocPredictionModel> ValidCurrentCarModelLatestAsync(
            [FromQuery] string car_version,
            [FromQuery] string car_id,
            [FromQuery] string user_id,
            [FromQuery] string so_version,
            [FromQuery] string config_version
            )
        {
            // 构建动态LINQ 字符串
            var query = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.ToString());
            var items = query.ToDictionary(x => x.Key.ToLower(), x => x.Value);

            if (string.IsNullOrEmpty(car_version))
                car_version = "_default";
            if (_context.CarEnergyModels.Where(x => x.Car_Version == car_version).FirstOrDefault() == null)
                car_version = "_default";
            items["car_version"] = car_version;

            if (string.IsNullOrEmpty(car_id))
                car_id = "_default";
            if (_context.CarEnergyModels.Where(x => x.Car_Id == car_id).FirstOrDefault() == null)
                car_id = "_default";
            items["car_id"] = car_id;

            if (string.IsNullOrEmpty(user_id))
                user_id = "_default";
            if (_context.CarEnergyModels.Where(x => x.User_Id == user_id).FirstOrDefault() == null)
                user_id = "_default";
            items["user_id"] = user_id;

            var validQueryArray = items.Where(x => (new string[] { "car_version", "car_id", "user_id", "so_version" }).Contains(x.Key, StringComparer.OrdinalIgnoreCase))
                .Where(x => x.Value.Count>=1);
            var predicate = validQueryArray.Select((x, i) => $"{x.Key}==@{i}").ToArray();
            var paramses = validQueryArray.Select(x => x.Value.ToString()).ToArray();
            string strPredicate = string.Join(" and ", predicate);
            strPredicate = string.IsNullOrEmpty(strPredicate) ? " 1=1" : strPredicate;

            var data = await _context.CarEnergyModels.Where(strPredicate, paramses).OrderBy("UploadTime descending")
                .Take(1)
                .Select(x => new
                {
                    x.Config_Version,
                    x.Config_Content
                }).FirstOrDefaultAsync();

            if (data == null || data.Config_Version == null)
                return new SocPredictionModel
                {
                    Latest = true,
                    Content = null
                };
            if (data.Config_Version == config_version)
                return new SocPredictionModel
                {
                    Latest = true,
                    Content = null
                };
            else
                return new SocPredictionModel
                {
                    Latest = false,
                    Content = data.Config_Content
                };
        }

        /// <summary>
        /// 查询该车历史全量能耗模型
        /// </summary>
        [Route("all")]
        [ServiceFilter(typeof(LoggingActionFilter))]
        [HttpGet]
        public async Task<List<CarEnergyModelEntity>> GetModeParametersAsync(
           [FromQuery] string car_version,
           [FromQuery] string car_id,
           [FromQuery] string user_id,
           [FromQuery] string so_version,
           [FromQuery] string config_version,
           [FromQuery] string startTime,
           [FromQuery] string endTime
            )
        {
            // 构建动态查询
            var query = HttpContext.Request.Query;
            var validQueryArray1 = query.Where(x => (new string[] { "car_version", "car_id", "user_id", "so_version", "config_version" }).Contains(x.Key, StringComparer.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrEmpty(x.Value));
            var validQueryArray2 = query.Where(x => (new string[] { "startTime", "endTime" }).Contains(x.Key, StringComparer.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrEmpty(x.Value) && DateTime.TryParse(x.Value, out _))
                .Select(x => DateTime.Parse(x.Value));

            var predicate = validQueryArray1.Select((x, i) => $"{x.Key}==@{i}").ToArray();
            var paramses = validQueryArray1.Select(x => x.Value.ToString()).ToArray();
            string strPredicate = string.Join(" and ", predicate);
            strPredicate = string.IsNullOrEmpty(strPredicate) ? " 1=1" : strPredicate;
            var sqlQuery = _context.CarEnergyModels.Where(strPredicate, paramses);

            if (validQueryArray2.Count() == 2)
                sqlQuery = sqlQuery.Where(x => x.UploadTime >= validQueryArray2.First() && x.UploadTime <= validQueryArray2.Last());

            var models =  await sqlQuery.ToListAsync();
            return models;
        }

        /// <summary>
        /// 新增能耗模型,请在payload放置configContent
        /// </summary>
        [HttpPost]
        [ServiceFilter(typeof(LoggingActionFilter))]
        public async Task<IActionResult> CreateModeParametersAsync(
            [FromQuery]string car_version,
            [FromQuery]string car_id,
            [FromQuery]string user_id,
            [FromQuery]string so_version,
            [FromQuery]string config_version
            )
        {
            string configContent = null;
            var ctx = HttpContext;
            using (var reader = new StreamReader(ctx.Request.Body))
                configContent = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(configContent))
                   throw new BadRequestMessage($"nameof(configContent) is null ");

            var model = new CarEnergyModelEntity {
                Car_Version = car_version,
                Car_Id = car_id,
                User_Id = user_id,
                So_Version = so_version,
                Config_Version = config_version,
                Config_Content = configContent,
                UploadTime = DateTime.UtcNow
            };
            _context.CarEnergyModels.Add(model);
            return Content((await _context.SaveChangesAsync()).ToString());

        }

    }

    public sealed class SocPredictionModel
    {
        public bool Latest { get; set; }

        public string Content { get; set; }
    }
}