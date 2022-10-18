// ******************************************************************************************************************************
//  
// Copyright (c) 2022 InterlockLedger Network
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met
//
// * Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// * Neither the name of the copyright holder nor the names of its
//   contributors may be used to endorse or promote products derived from
//   this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES, LOSS OF USE, DATA, OR PROFITS, OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// ******************************************************************************************************************************

using InterlockLedger.WatchDog.Interfaces;
using InterlockLedger.WatchDog.Models;
using InterlockLedger.WatchDog.Utilities;

using Microsoft.AspNetCore.Mvc;

namespace InterlockLedger.WatchDog.Controllers;
public class WatchPageController : Controller
{
    private readonly IDBHelper _dbHelper;

    public WatchPageController(IDBHelper dbHelper) => _dbHelper = dbHelper;

    public JsonResult Index(string searchString = "", string verbString = "", string statusCode = "", int pageNumber = 1) {
        var logs = _dbHelper.GetAllWatchLogs();
        if (logs != null) {
            if (!string.IsNullOrEmpty(searchString)) {
                searchString = searchString.ToLower();
                logs = logs.Where(l => l.Path.Safe().ToLower().Contains(searchString) || l.Method.Safe().ToLower().Contains(searchString) || l.ResponseStatus.ToString().Contains(searchString) || !string.IsNullOrEmpty(l.QueryString) && l.QueryString.ToLower().Contains(searchString));
            }

            if (!string.IsNullOrEmpty(verbString)) logs = logs.Where(l => l.Method.Safe().ToLower() == verbString.ToLower());

            if (!string.IsNullOrEmpty(statusCode)) logs = logs.Where(l => l.ResponseStatus.ToString() == statusCode);
            logs = logs.OrderByDescending(x => x.StartTime);
        }
        var result = PaginatedList<WatchLog>.CreateAsync(logs.Safe(), pageNumber, Constants.PageSize);
        return Json(new { result.PageIndex, result.TotalPages, HasNext = result.HasNextPage, HasPrevious = result.HasPreviousPage, logs = result });
    }

    public JsonResult Exceptions(string searchString = "", int pageNumber = 1) {
        var logs = _dbHelper.GetAllWatchExceptionLogs();
        if (logs != null) {
            if (!string.IsNullOrEmpty(searchString)) {
                searchString = searchString.ToLower();
                logs = logs.Where(l => l.Message.Safe().ToLower().Contains(searchString) || l.StackTrace.Safe().ToLower().Contains(searchString) || l.Source.Safe().ToString().Contains(searchString));
            }
            logs = logs.OrderByDescending(x => x.EncounteredAt);
        }
        var result = PaginatedList<ExceptionLogModel>.CreateAsync(logs.Safe(), pageNumber, Constants.PageSize);
        return Json(new { result.PageIndex, result.TotalPages, HasNext = result.HasNextPage, HasPrevious = result.HasPreviousPage, logs = result });
    }
    public JsonResult Logs(string searchString = "", int pageNumber = 1) {
        var logs = _dbHelper.GetAllLogs();
        if (logs != null) {
            if (!string.IsNullOrEmpty(searchString)) {
                searchString = searchString.ToLower();
                logs = logs.Where(l => l.Message.Safe().ToLower().Contains(searchString) || l.CallingMethod.Safe().ToLower().Contains(searchString) || l.CallingFrom.Safe().ToString().Contains(searchString));
            }
            logs = logs.OrderByDescending(x => x.Timestamp);
        }
        var result = PaginatedList<WatchLoggerModel>.CreateAsync(logs.Safe(), pageNumber, Constants.PageSize);
        return Json(new { result.PageIndex, result.TotalPages, HasNext = result.HasNextPage, HasPrevious = result.HasPreviousPage, logs = result });
    }

    public JsonResult ClearLogs() {
        bool cleared = _dbHelper.ClearAllLogs();
        return Json(cleared);
    }


    [HttpPost]
    public JsonResult Auth(string username, string password) =>
        username.ToLower() == WatchDogMiddleware.UserName?.ToLower() && password == WatchDogMiddleware.Password
            ? Json(true)
            : Json(false);

    [HttpGet]
    public JsonResult AuthAuto() =>
        Json(
            (User.Identity?.IsAuthenticated ?? false) &&
            (WatchDogMiddleware.RequiredRole.IsBlank() || User.IsInRole(WatchDogMiddleware.RequiredRole)));
}
