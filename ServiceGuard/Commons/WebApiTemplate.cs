﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceGuard.AppLibs;

namespace ServiceGuard.Commons {

    /// <summary>
    /// WebApi 模板 ( 以 Json 進行交互 )
    /// </summary>
    /// <typeparam name="TRequestData">請求内容</typeparam>
    /// <typeparam name="TResponseData">響應内容</typeparam>
    public abstract class WebApiTemplate<TRequestData, TResponseData> : Controller, IResult
        where TRequestData : struct
        where TResponseData : struct {

        #region Properties 屬性
        int IResult.ResultCode { get; set; } = -1;
        string IResult.ResultMsg { get; set; } = "";

        protected abstract ILogger Logger { get; set; }
        protected JObject? JObjRequestData { get; set; } // 用於身份檢查
        protected TRequestData RequestData { get; set; }
#pragma warning disable CS8618
        /*  **** 屏蔽-系統警告 CS8618 ****
        *   由於 RequestRouteValues 不能為 Null，系統建議設定為 RouteValueDictionary? RequestRouteValues;
        *   交由 BuildRequest 時，取得對應的實體參考
        */
        protected RouteValueDictionary RequestRouteValues { get; set; } // 請求參數, 用來記錄 URL 附加參數
#pragma warning restore CS8618
        // 需直接改動内容，因此屬性作爲值類型並不適合, 但爲了可讀性及一致性，變數名稱依舊以屬性命名慣例
        protected TResponseData ResponseData = new(); // { get; set; }
        #endregion

#pragma warning disable CS8618
        /*  **** 屏蔽-系統警告 CS8618 ****
        *   由於 result 不能為 Null，系統建議設定為 IResult? result;
        *   但由於此模板為抽象類，無法實例化，因此 result 在此無法取得實體
        *   交由子類調用 Initialize 方法進行設定
        */
        protected IResult result;
#pragma warning restore CS8618

        /// <summary>
        /// 初始化設定
        /// </summary>
        /// <param name="result">響應資訊實例</param>
        protected virtual void Initialize(IResult result) {
            this.result = result;
        }

        #region PreCheck 前置檢查
        /// <summary>
        /// **前置檢查-資料頭**
        /// </summary>
        /// <param name="parameter"> 
        /// <br> 需要驗證的欄位名稱</br>
        ///     <br> 範例： parameter = @"A,B..."; 檢查請求正文是否擁有欄位 A, B... </br>
        ///     <br> 注意： parameter = @"A,B,C..."; 欄位之間不可以有空格 </br>
        /// </param>
        /// <returns>是否存在必要資料欄位</returns>
        protected virtual bool CheckValidDataHead(string parameter) {
            if (CheckValidData(parameter, "header") == false) {
                BuildResult(WebApiResult.Code.InvalidData, "Header");
                return false;
            }
            return true;
        }
        /// <summary>
        /// **前置檢查-資料體**
        /// </summary>
        /// <param name="parameter"> 
        /// <br> 需要驗證的欄位名稱</br>
        ///     <br> 範例： parameter = @"A,B..."; 檢查請求正文是否擁有欄位 A, B... </br>
        ///     <br> 注意： parameter = @"A,B,C..."; 欄位之間不可以有空格 </br>
        /// </param>
        /// <returns>是否存在必要資料欄位</returns>
        protected virtual bool CheckValidDataBody(string parameter) {
            if(CheckValidData(parameter) == false) {
                BuildResult(WebApiResult.Code.InvalidData, "Body");
                return false;
            }
            return true;
        }
        /// <summary>
        /// **數據有效性驗證** 
        /// </summary>
        /// <remarks> <br> 説明：</br>
        /// <br> 檢驗請求正文是否擁有指定欄位。此方法僅支援内部檢查，并施加于該變數 >> <see cref="JObjRequestData"/> </br>
        /// </remarks>
        /// <param name="parameter">
        ///     <br> 需要驗證的欄位名稱 </br>
        ///     <br> 範例： parameter = @"A,B..."; 檢查請求正文是否擁有欄位 A, B... </br>
        ///     <br> 注意： parameter = @"A,B,C..."; 欄位之間不可以有空格 </br>
        /// </param>
        /// <param name="path">
        ///     <br> 説明：path 為 json 的嵌套路徑 </br>
        ///     <br> 範例：path = "A.B.C..."; </br>
        ///     <br> 預設值：最頂層路徑 A </br>
        /// </param>
        /// <returns>是否存在必要資料欄位</returns>
        protected virtual bool CheckValidData(string parameter, string? path = null) {
            if (JObjRequestData == null) return false;

            if(path == null) {
                foreach (var item in parameter.Split(',')) {   // 以根路徑作爲主目錄
                    Logger.LogInformation($"Parameter: {item}");
                    if (JObjRequestData.SelectToken(item) == null) return false;
                }
            }
            else {
                var token = JObjRequestData.SelectToken(path); // 獲取指定路徑作爲主目錄
                if (token == null) return false;
                foreach (var item in parameter.Split(',')) {
                    Logger.LogInformation($"Parameter Token: {item}");
                    if (token.SelectToken(item) == null) return false;
                }
            }

            return true;
        }
        /// <summary>
        /// 跨域檢查
        /// </summary>
        /// <returns>是否符合及携帶跨域來源</returns>
        protected virtual bool CheckHasCorsPolicy() {
            return true; // Undone: 未準備好跨域許可
            if (AppSettings.Origins.Contains(Request.Headers["Origin"].ToString()) == false) {
                BuildResult(WebApiResult.Code.CheckFailed_CorsPolicy,
                    $"Request is not valid! origin:{Request.Headers["Origin"]} not in List:AppSettings.Origins"
                );
                return false;
            }
            return true;
        }
        /// <summary>
        /// Redis高速資料庫-資料比對
        /// </summary>
        /// <returns>比對是否一致</returns>
        protected virtual bool ComparisonData() {
            /*var machine = RedisHelper.SessionCustomer(InputData.session_key);
            if (machine.customerIdx == -1) {
                OutputData.result_code = 104;
                return false;
            }*/
            return true;
        }
        /// <summary>
        /// 資料解密
        /// </summary>
        /// <returns>解密是否成功</returns>
        protected virtual bool DataDecription() => true;
        #endregion

        #region Data Processing 資料處理
        /// <summary>
        /// 處理請求
        /// </summary>
        protected virtual bool ProcessData() => true;

        string? logStr;
        string formater = "{0,20} : {1,-10}\n";

        protected virtual async Task<bool> HandleBuildRequestBody() {
            // 載入請求正文
            using (StreamReader reader = new(Request.Body)) {
                try {
                    Request.Body.Seek(0, SeekOrigin.Begin); // 重置-讀寫頭位置
                    // 獲取-請求正文内容
                    var content = await reader.ReadToEndAsync();
                    JObjRequestData = JsonConvert.DeserializeObject<JObject>(content); // 反序列化
                    logStr += $"|> Body -------------------------------------------------\n";
                    logStr += string.Format(formater, "Content Length", $"{content.Length}{((content.Length > AppSettings.MaxRequestBodyBufferSize * 0.7) ? " (Big Data)\n" : "")}");
                    logStr += $">>>>>>>>>>>>>>>>>>>>>> JSON\n{content}\n";
                }
                // 捕獲-例外狀況
                catch (Exception ex) {
                    JObjRequestData = null;
                    Logger.LogInformation("\n" +
                        $"!!!!! Exception: {ex.Message}\n"
                    );
                    BuildResult(WebApiResult.Code.Exception, ex.Message);
                    return false;
                }
            }
            return true;
        }

        protected virtual void HandleBuildRequestRouteValues() {
            RequestRouteValues = Request.RouteValues;
            if (RequestRouteValues != null && RequestRouteValues.Count > 0) {
                logStr += $"|> Route ------------------------------------------------\n";
                int i = 0;
                foreach (var param in RequestRouteValues) {
                    if(++i == 3) logStr += $">>>>>>>>>>>>>>>>>>>>>> Values\n";
                        logStr += string.Format(formater, $"{param.Key}", $"{param.Value}");
                }
                logStr += "\n";
            }
        }

        /// <summary>
        /// 建立-請求内容 (準備：正文 或 URL)
        /// </summary>
        /// <returns>是否建立成功</returns>
        protected virtual async Task<bool> BuildRequest() {

            logStr += $">>>>: Request >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n";
            logStr += string.Format(formater, "Type", $"{Request.Method}");

            HandleBuildRequestRouteValues();

            // 請求正文内容有長度(即:有正文)
            if ((Request.ContentLength ?? 0) > 0) {
                await HandleBuildRequestBody(); // 處理-構建請求正文
            } else {
                logStr += $"|> Body -------------------------------------------------\n";
                logStr += string.Format(formater, "Data", "No Data");
            }

            logStr += $">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> End\n";
            Logger.LogInformation("\n" + logStr);

            return true;
        }
        //protected virtual 
        /// <summary>
        /// 建立-響應内容
        /// </summary>
        protected abstract void BuildResponse();
        #endregion

        #region Tools 其他工具方法
        /// <summary>
        /// 建立-響應資訊
        /// </summary>
        /// <param name="code">響應代碼</param>
        /// <param name="msg">響應消息</param>
        protected virtual void BuildResult(WebApiResult.Code code, string? message = null,
            WebApiResult.MsgFormatType type = WebApiResult.MsgFormatType.Preset) {
            WebApiResult.Build(ref result, code, message, type);
        }
        /// <summary>
        /// 一般日志記錄
        /// </summary>
        /// <param name="message">所需記錄的内容</param>
        /// <param name="args">參數</param>
        [Obsolete("This method has BUG", false)]
        protected void LogInformation(string message, params object[] args) {
            var dt = TimeZoneInfo.ConvertTime(DateTime.UtcNow, Utils.TimeZoneInfo).ToString("yy-MM-dd HH:mm:ss.fff");
            var name = GetType().Name;
            var msg = string.Format(message, args);
            Logger.LogInformation($"dt:{dt}, api:{name}, {msg}");
        }
        #endregion

    }

}
