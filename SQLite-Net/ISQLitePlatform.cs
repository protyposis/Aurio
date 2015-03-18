using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;
using Result = SQLite.SQLite3.Result;
using ColType = SQLite.SQLite3.ColType;
using ExtendedResult = SQLite.SQLite3.ExtendedResult;

namespace SQLite {

    /// <summary>
    /// This is a platform abstraction interface currently serving to switch between
    /// Windows x86 and x64 interops with the sqlite3.dll, but implementations
    /// could also abstract other implementations such as csharp-sqlite or
    /// SQLitePCL.raw. The approach here is similar to the PCL model like in
    /// SQLitePCL.raw or SQLite.Net-PCL.
    /// </summary>
    public interface ISQLitePlatform {

        Result Open(string filename, out Sqlite3DatabaseHandle db);

        Result Open(string filename, out Sqlite3DatabaseHandle db, int flags, IntPtr zVfs);

        Result Open(byte[] filename, out Sqlite3DatabaseHandle db, int flags, IntPtr zvfs);

        Result Close(Sqlite3DatabaseHandle db);

        Result BusyTimeout(Sqlite3DatabaseHandle db, int milliseconds);

        int Changes(Sqlite3DatabaseHandle db);

        Sqlite3Statement Prepare2(Sqlite3DatabaseHandle db, string query);

        Result Step(Sqlite3Statement stmt);

        Result Reset(Sqlite3Statement stmt);

        Result Finalize(Sqlite3Statement stmt);

        long LastInsertRowid(Sqlite3DatabaseHandle db);

        string GetErrmsg(Sqlite3DatabaseHandle db);

        int BindParameterIndex(Sqlite3Statement stmt, string name);

        int BindNull(Sqlite3Statement stmt, int index);

        int BindInt(Sqlite3Statement stmt, int index, int val);

        int BindInt64(Sqlite3Statement stmt, int index, long val);

        int BindDouble(Sqlite3Statement stmt, int index, double val);

        int BindText(Sqlite3Statement stmt, int index, string val, int n, IntPtr free);

        int BindBlob(Sqlite3Statement stmt, int index, byte[] val, int n, IntPtr free);

        int ColumnCount(Sqlite3Statement stmt);

        string ColumnName(Sqlite3Statement stmt, int index);

        string ColumnName16(Sqlite3Statement stmt, int index);

        ColType ColumnType(Sqlite3Statement stmt, int index);

        int ColumnInt(Sqlite3Statement stmt, int index);

        long ColumnInt64(Sqlite3Statement stmt, int index);

        double ColumnDouble(Sqlite3Statement stmt, int index);

        string ColumnText(Sqlite3Statement stmt, int index);

        string ColumnText16(Sqlite3Statement stmt, int index);

        byte[] ColumnBlob(Sqlite3Statement stmt, int index);

        int ColumnBytes(Sqlite3Statement stmt, int index);

        string ColumnString(Sqlite3Statement stmt, int index);

        byte[] ColumnByteArray(Sqlite3Statement stmt, int index);

        Result EnableLoadExtension(Sqlite3DatabaseHandle db, int onoff);

        ExtendedResult ExtendedErrCode(Sqlite3DatabaseHandle db);
    }
}
