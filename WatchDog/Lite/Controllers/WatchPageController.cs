﻿using Microsoft.AspNetCore.Mvc;

using System.Linq;
using System.Threading.Tasks;

using WatchDog.Lite.Helpers;
using WatchDog.Lite.Managers;
using WatchDog.Lite.Models;
using WatchDog.Lite.Utilities;

namespace WatchDog.Lite.Controllers {
    public class WatchPageController : Controller {
        public async Task<JsonResult> Index(string searchString = "", string verbString = "", string statusCode = "", int pageNumber = 1) {
            var logs = await DynamicDBManager.GetAllWatchLogs();
            if (logs != null) {
                if (!string.IsNullOrEmpty(searchString)) {
                    searchString = searchString.ToLower();
                    logs = logs.Where(l => l.Path.ToLower().Contains(searchString) || l.Method.ToLower().Contains(searchString) || l.ResponseStatus.ToString().Contains(searchString) || !string.IsNullOrEmpty(l.QueryString) && l.QueryString.ToLower().Contains(searchString));
                }

                if (!string.IsNullOrEmpty(verbString)) logs = logs.Where(l => l.Method.ToLower() == verbString.ToLower());

                if (!string.IsNullOrEmpty(statusCode)) logs = logs.Where(l => l.ResponseStatus.ToString() == statusCode);
            }
            logs = logs.OrderByDescending(x => x.StartTime);
            var result = PaginatedList<WatchLog>.CreateAsync(logs, pageNumber, Constants.PageSize);
            return Json(new { result.PageIndex, result.TotalPages, HasNext = result.HasNextPage, HasPrevious = result.HasPreviousPage, logs = result });
        }

        public async Task<JsonResult> Exceptions(string searchString = "", int pageNumber = 1) {
            var logs = await DynamicDBManager.GetAllWatchExceptionLogs();
            if (logs != null) if (!string.IsNullOrEmpty(searchString)) {
                    searchString = searchString.ToLower();
                    logs = logs.Where(l => l.Message.ToLower().Contains(searchString) || l.StackTrace.ToLower().Contains(searchString) || l.Source.ToString().Contains(searchString));
                }
            logs = logs.OrderByDescending(x => x.EncounteredAt);
            var result = PaginatedList<WatchExceptionLog>.CreateAsync(logs, pageNumber, Constants.PageSize);
            return Json(new { result.PageIndex, result.TotalPages, HasNext = result.HasNextPage, HasPrevious = result.HasPreviousPage, logs = result });
        }
        public async Task<JsonResult> Logs(string searchString = "", int pageNumber = 1) {
            var logs = await DynamicDBManager.GetAllLogs();
            if (logs != null) if (!string.IsNullOrEmpty(searchString)) {
                    searchString = searchString.ToLower();
                    logs = logs.Where(l => l.Message.ToLower().Contains(searchString) || l.CallingMethod.ToLower().Contains(searchString) || l.CallingFrom.ToString().Contains(searchString));
                }
            logs = logs.OrderByDescending(x => x.Timestamp);
            var result = PaginatedList<WatchLoggerModel>.CreateAsync(logs, pageNumber, Constants.PageSize);
            return Json(new { result.PageIndex, result.TotalPages, HasNext = result.HasNextPage, HasPrevious = result.HasPreviousPage, logs = result });
        }

        public async Task<JsonResult> ClearLogs() {
            var cleared = await DynamicDBManager.ClearLogs();
            return Json(cleared);
        }


        [HttpPost]
        public JsonResult Auth(string username, string password) {

            return username.ToLower() == WatchDogConfigModel.UserName.ToLower() && password == WatchDogConfigModel.Password
                ? Json(true)
                : Json(false);
        }
    }
}
