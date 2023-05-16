﻿using Npgsql;
using Microsoft.EntityFrameworkCore;
using ServiceGuard.Commons;
using ServiceGuard.Databases;
using ServiceGuard.Sample.Models;

// 注意: 此命名空間為：參考範本，禁止使用範本空間 ( 即：ServiceGuard.Sample 開頭的命名空間 )
namespace ServiceGuard.Sample.Databases {

    /*  範本背景假設 (參考範本實作時，請刪除此段背景假設)
    *   - 用途：用戶資訊管理 ( UserManager )
    *   - 命名：calss Npgsql_UserManagerDbCtx
    */

    /// <summary>
    /// Npgsql 資料庫上下文 (範本)
    /// </summary>
    /// <remarks>
    /// 
    /// <br>説明：</br>
    ///     <br> - 資料庫上下文是作爲資料庫連綫的輕量級操作單元，用以資料庫相關操作如：查詢、刪除、添加及修改等</br>
    /// <br></br>
    /// 
    /// <br>注意事項：</br>
    ///     <br> - 請將相關業務邏輯統整到同一個上下文</br>
    /// <br></br>
    /// 
    /// <br>繼承關係：</br>
    ///     <br> - <see cref="Npgsql_SampleDbCtx" /> ( 此類別：僅支援 Npgsql 資料庫的上下文 ) </br>
    ///     <br> - <see cref="NpgsqlDbCtxTemplate" /> ( Npgsql 資料庫上下文-模板 ) </br>
    ///     <br> - <see cref="DbCtxTemplate" /> ( 資料庫上下文通用型-模板 ) </br>
    ///     <br> - <see cref="DbContext" /> ( Supporter: Microsoft's Entity Framework Core 微軟支援的資料庫實體框架核心 ) </br>
    /// <br></br>
    /// 
    /// </remarks>
    public class Npgsql_UserManagerDbCtx : NpgsqlDbCtxTemplate {

        #region Constructor 構建式
        public Npgsql_UserManagerDbCtx(ILogger<DbCtxTemplate> logger)
            : base(logger) {

        }
        public Npgsql_UserManagerDbCtx(DbContextOptions<DbCtxTemplate> options, ILogger<DbCtxTemplate> logger)
            : base(options, logger) {
        
        }
        #endregion

        /* ============================================== */

        #region ResultDataModel 結果資料模型(承載體)
        /* **** 資料庫查詢所需承載的結果資料模型 ****
        *   - 命名規範：
        *       - 以資料模型後綴 + Model 為名稱 ( ...Xxx.Result> 即 XxxResultModel )
        *       - 範例：DbSet<UserDataModel.Xxx.Result> XxxResultModel { get; set; }
        *       
        *   - 注解事項:
        *       - 不要為{結果資料模型}屬性進行任何的注解描述,例如加注： <summary> ... </summary>
        *       - {結果資料模型}的説明注解請於資料模型中進行描述,例如到 UserDataModel 中進行注解而非如下屬性
        */

        public DbSet<UserDataModel.Login.Result> LoginResultModel { get; set; }
        // More...

        #endregion

        #region Service 服務 (業務邏輯)
        /* **** 此服務(UserManager用戶管理) 相關查詢邏輯 ****
        * 
        */

        /// <summary>
        /// 用戶-登入
        /// </summary>
        /// <param name="parameter">查詢參數</param>
        /// <param name="result">結果資料載體</param>
        /// <returns></returns>
        public bool Login(UserDataModel.Login.Linq parameter, out UserDataModel.Login.Result? result) {
            
            /* 建立-查詢指令
            *   - SQL 範例：SELECT 結果欄位 FROM 資料庫名稱 WHERE 條件
            */
            string cmd = "SELECT * FROM User WHERE id=@id, password=@password";

            // 建立-查詢參數
            NpgsqlParameter[] param = new NpgsqlParameter[] {
                new NpgsqlParameter("@id", parameter.Id),
                new NpgsqlParameter("@password", parameter.Password),
            };

            // 呼叫-資料庫
            var resultList = LoginResultModel.FromSqlRaw(cmd, param).ToList();

            // 獲取一筆資料
            result = resultList.FirstOrDefault(); // 多筆資料需注解此行

            /* 獲取多筆資料
            *   - 1. 請注解掉該行内容: result = resultList.FirstOrDefault();
            *   - 2. 將 result 參數改爲: out List<UserDataModel.Login.Result>? result
            */

            // 返回執行判斷結果
            return resultList.Count > 0; // 大於 0 筆資料即為查詢到結果，返回 true; 反之亦然。
        }

        // More...

        #endregion

    }
}
