// ******************************************************************************************************************************
//  
// Copyright (c) 2018-2022 InterlockLedger Network
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

using LiteDB;

using System.Reflection;

namespace InterlockLedger.WatchDog.Helpers;
internal class LiteDBHelper : IDBHelper
{
    public static string DefaultFolder =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            (Assembly.GetEntryAssembly()?.GetName().Name).Safe());

    private readonly LiteDatabase _db;
    private readonly ILiteCollection<WatchLog> _watchLogs;
    private readonly ILiteCollection<WatchExceptionLog> _watchExLogs;
    private readonly ILiteCollection<WatchLoggerModel> _logs;

    public LiteDBHelper(string? folder) {
        folder ??= DefaultFolder;
        _ = Directory.CreateDirectory(folder);
        _db = new(Path.Combine(folder, "watchlogs.db"));
        _watchLogs = _db.GetCollection<WatchLog>("WatchLogs");
        _watchExLogs = _db.GetCollection<WatchExceptionLog>("WatchExceptionLogs");
        _logs = _db.GetCollection<WatchLoggerModel>("Logs");
    }

    public IEnumerable<WatchLog> GetAllWatchLogs() => _watchLogs.FindAll();

    public bool ClearAllLogs() {
        int watchLogs = ClearWatchLog();
        int exLogs = ClearWatchExceptionLog();
        int logs = ClearLogs();
        return watchLogs > 1 && exLogs > 1 && logs > 1;
    }

    //WATCH lOGS OPERATION
    public WatchLog GetWatchLogById(int id) => _watchLogs.FindById(id);

    public int InsertWatchLog(WatchLog log) => _watchLogs.Insert(log);

    public bool UpdateWatchLog(WatchLog log) => _watchLogs.Update(log);

    public bool DeleteWatchLog(int id) => _watchLogs.Delete(id);

    public int ClearWatchLog() => _watchLogs.DeleteAll();


    //Watch Exception Operations
    public IEnumerable<WatchExceptionLog> GetAllWatchExceptionLogs() => _watchExLogs.FindAll();

    public WatchExceptionLog GetWatchExceptionLogById(int id) => _watchExLogs.FindById(id);

    public int InsertWatchExceptionLog(WatchExceptionLog log) => _watchExLogs.Insert(log);

    public bool UpdateWatchExceptionLog(WatchExceptionLog log) => _watchExLogs.Update(log);

    public bool DeleteWatchExceptionLog(int id) => _watchExLogs.Delete(id);
    public int ClearWatchExceptionLog() => _watchExLogs.DeleteAll();

    //LOGS OPERATION
    public int InsertLog(WatchLoggerModel log) => _logs.Insert(log);
    public int ClearLogs() => _logs.DeleteAll();
    public IEnumerable<WatchLoggerModel> GetAllLogs() => _logs.FindAll();
}
