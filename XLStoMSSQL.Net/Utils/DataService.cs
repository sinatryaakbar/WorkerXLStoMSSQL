using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;

namespace XLStoMSSQL.Net.Utils
{
    public class DataService
    {
        private static string _connectionString = string.Empty;

        public static string ConnectionString
        {
            get
            {
                return _connectionString;
            }
            set
            {
                _connectionString = value;
            }
        }

        private static void Init()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                ConnectionString = ConnectionString ?? string.Empty;
        }

        public static bool CheckConnection()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return true;
            }
        }

        public static async Task<bool> CheckConnectionAsync()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                return true;
            }
        }

        public static bool CheckDatabase()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var CurDatabase = conn.Database;
                conn.ConnectionString = ConnectionString.Replace(CurDatabase, "master");
                conn.Open();

                var qry = $"SELECT db_id('{CurDatabase}')";

                int? exists = conn.ExecuteScalar<int>(qry);
                if (exists == null || exists <= 0)
                {
                    qry = $"Create database {CurDatabase}";
                    conn.Execute(qry);
                }

                conn.ChangeDatabase(CurDatabase);

                return true;
            }
        }

        public static async Task<bool> CheckDatabaseAsync()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                var CurDatabase = conn.Database;
                conn.ConnectionString = ConnectionString.Replace(CurDatabase, "master");
                await conn.OpenAsync();
                var qry = $"SELECT db_id('{CurDatabase}')";

                int? exists = await conn.ExecuteScalarAsync<int>(qry);
                if (exists == null || exists <= 0)
                {
                    qry = $"Create database {CurDatabase}";
                    conn.Execute(qry);
                }

                conn.ChangeDatabase(CurDatabase);
                return true;
            }
        }

        public static int Execute(string cmd, object? parameter = null, bool IsTransaction = false)
        {
            var res = 0;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                SqlTransaction? trans = null;
                try
                {
                    if (IsTransaction)
                        trans = conn.BeginTransaction();

                    res = conn.Execute(cmd, parameter, trans);

                    if (IsTransaction && trans != null)
                        trans.Commit();
                }
                catch
                {
                    if (IsTransaction && trans != null)
                        trans.Rollback();
                    trans = null;
                    res = 0;
                    throw;
                }
            }
            return res;
        }
        public static async Task<int> ExecuteAsync(string cmd, object? parameter = null, bool IsTransaction = false)
        {
            var res = 0;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                SqlTransaction? trans = null;
                try
                {
                    if (IsTransaction)
                        trans = conn.BeginTransaction();

                    res = await conn.ExecuteAsync(cmd, parameter, trans);

                    if (IsTransaction && trans != null)
                        trans.Commit();
                }
                catch
                {
                    if (IsTransaction && trans != null)
                        trans.Rollback();
                    trans = null;
                    res = 0;
                    throw;
                }
            }
            return res;
        }


        public static object ExecuteScalar(string cmd, object? parameter = null)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.ExecuteScalar(cmd, parameter);
            }
        }


        public static async Task<object> ExecuteScalarAsync(string cmd, object? parameter = null)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                return await conn.ExecuteScalarAsync(cmd, parameter);
            }
        }

        public static List<T> FindList<T>(string cmd, object? parameter = null)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.Query<T>(cmd, parameter).ToList();
            }

        }
        public static async Task<List<T>> FindListAsync<T>(string cmd, object? parameter = null)
        {

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                var res = await conn.QueryAsync<T>(cmd, parameter);
                return res.ToList();
            }

        }

        public static T Find<T>(string cmd, object? param = null)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                return conn.QuerySingleOrDefault<T>(cmd, param);
            }
        }

        public static async Task<T> FindAsync<T>(string cmd, object? param = null)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                return await conn.QuerySingleOrDefaultAsync<T>(cmd, param);
            }
        }

        public static int ExecuteMultiple(List<DapperTransaction> data)
        {
            var queryIndex = 0;
            var res = 0;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var x in data)
                        {
                            res += conn.Execute(x.Query, x.QueryParameter, trans);
                            queryIndex++;
                        }
                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        res = 0;
                        throw new Exception($"{ex.Message} | QueryIx {queryIndex}: {JsonConvert.SerializeObject(data[queryIndex])}");
                    }
                }
            }
            return res;
        }


        public static async Task<int> ExecuteMultipleAsync(List<DapperTransaction> data)
        {
            var queryIndex = 0;
            var res = 0;
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var x in data)
                        {
                            res += await conn.ExecuteAsync(x.Query, x.QueryParameter, trans);
                            queryIndex++;
                        }
                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        res = 0;
                        throw new Exception($"{ex.Message} | QueryIx {queryIndex}: {JsonConvert.SerializeObject(data[queryIndex])}");
                    }
                }
            }
            return res;
        }

    }


    public class DapperTransaction
    {
        public string? Query { get; set; }
        public Object? QueryParameter { get; set; }
    }
}
