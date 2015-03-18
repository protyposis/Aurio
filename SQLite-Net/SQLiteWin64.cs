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

    class SQLiteWin64 : ISQLitePlatform {

        public Result Open(string filename, out Sqlite3DatabaseHandle db) {
            return X64Interop.Open(filename, out db);
        }

        public Result Open(string filename, out Sqlite3DatabaseHandle db, int flags, IntPtr zVfs) {
            return X64Interop.Open(filename, out db, flags, zVfs);
        }

        public Result Open(byte[] filename, out Sqlite3Statement db, int flags, Sqlite3Statement zvfs) {
            return X64Interop.Open(filename, out db, flags, zvfs);
        }

        public Result Close(Sqlite3DatabaseHandle db) {
            return X64Interop.Close(db);
        }

        public Result BusyTimeout(Sqlite3DatabaseHandle db, int milliseconds) {
            return X64Interop.BusyTimeout(db, milliseconds);
        }

        public int Changes(Sqlite3DatabaseHandle db) {
            return X64Interop.Changes(db);
        }

        public Sqlite3Statement Prepare2(Sqlite3DatabaseHandle db, string query) {
            return X64Interop.Prepare2(db, query);
        }

        public Result Step(Sqlite3Statement stmt) {
            return X64Interop.Step(stmt);
        }

        public Result Reset(Sqlite3Statement stmt) {
            return X64Interop.Reset(stmt);
        }

        public Result Finalize(Sqlite3Statement stmt) {
            return X64Interop.Finalize(stmt);
        }

        public long LastInsertRowid(Sqlite3DatabaseHandle db) {
            return X64Interop.LastInsertRowid(db);
        }

        public string GetErrmsg(Sqlite3DatabaseHandle db) {
            return X64Interop.GetErrmsg(db);
        }

        public int BindParameterIndex(Sqlite3Statement stmt, string name) {
            return X64Interop.BindParameterIndex(stmt, name);
        }

        public int BindNull(Sqlite3Statement stmt, int index) {
            return X64Interop.BindNull(stmt, index);
        }

        public int BindInt(Sqlite3Statement stmt, int index, int val) {
            return X64Interop.BindInt(stmt, index, val);
        }

        public int BindInt64(Sqlite3Statement stmt, int index, long val) {
            return X64Interop.BindInt64(stmt, index, val);
        }

        public int BindDouble(Sqlite3Statement stmt, int index, double val) {
            return X64Interop.BindDouble(stmt, index, val);
        }

        public int BindText(Sqlite3Statement stmt, int index, string val, int n, IntPtr free) {
            return X64Interop.BindText(stmt, index, val, n, free);
        }

        public int BindBlob(Sqlite3Statement stmt, int index, byte[] val, int n, IntPtr free) {
            return X64Interop.BindBlob(stmt, index, val, n, free);
        }

        public int ColumnCount(Sqlite3Statement stmt) {
            return X64Interop.ColumnCount(stmt);
        }

        public string ColumnName(Sqlite3Statement stmt, int index) {
            //return X64Interop.ColumnName(stmt, index);
            throw new NotImplementedException();
        }

        public string ColumnName16(Sqlite3Statement stmt, int index) {
            return X64Interop.ColumnName16(stmt, index);
        }

        public ColType ColumnType(Sqlite3Statement stmt, int index) {
            return X64Interop.ColumnType(stmt, index);
        }

        public int ColumnInt(Sqlite3Statement stmt, int index) {
            return X64Interop.ColumnInt(stmt, index);
        }

        public long ColumnInt64(Sqlite3Statement stmt, int index) {
            return X64Interop.ColumnInt64(stmt, index);
        }

        public double ColumnDouble(Sqlite3Statement stmt, int index) {
            return X64Interop.ColumnDouble(stmt, index);
        }

        public string ColumnText(Sqlite3Statement stmt, int index) {
            //return X64Interop.ColumnText(stmt, index);
            throw new NotImplementedException();
        }

        public string ColumnText16(Sqlite3Statement stmt, int index) {
            //return X64Interop.ColumnText16(stmt, index);
            throw new NotImplementedException();
        }

        public byte[] ColumnBlob(Sqlite3Statement stmt, int index) {
            //return X64Interop.ColumnBlob(stmt, index);
            throw new NotImplementedException();
        }

        public int ColumnBytes(Sqlite3Statement stmt, int index) {
            return X64Interop.ColumnBytes(stmt, index);
        }

        public string ColumnString(Sqlite3Statement stmt, int index) {
            return X64Interop.ColumnString(stmt, index);
        }

        public byte[] ColumnByteArray(Sqlite3Statement stmt, int index) {
            return X64Interop.ColumnByteArray(stmt, index);
        }

        public Result EnableLoadExtension(Sqlite3DatabaseHandle db, int onoff) {
            return X64Interop.EnableLoadExtension(db, onoff);
        }

        public ExtendedResult ExtendedErrCode(Sqlite3DatabaseHandle db) {
            return X64Interop.ExtendedErrCode(db);
        }
    }
}
