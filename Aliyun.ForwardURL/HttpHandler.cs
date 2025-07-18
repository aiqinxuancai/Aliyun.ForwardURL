using Aliyun.Serverless.Core;
using Aliyun.Serverless.Core.Http;
using Flurl.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aliyun.ForwardURL
{
    // HTTPTriggerEvent, RequestContext, HTTPTriggerResponse 这些 POCO 类保持不变
    public class HTTPTriggerEvent
    {
        public string Version { get; set; }
        public string RawPath { get; set; }
        public string Body { get; set; }
        public bool IsBase64Encoded { get; set; }
        public RequestContext RequestContext { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> QueryParameters { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class RequestContext
    {
        public string AccountId { get; set; }
        public string DomainName { get; set; }
        public string DomainPrefix { get; set; }
        public string RequestId { get; set; }
        public string Time { get; set; }
        public string TimeEpoch { get; set; }
        public Dictionary<string, string> Http { get; set; }
    }

    public class HTTPTriggerResponse
    {
        public int StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool IsBase64Encoded { get; set; }
        public string Body { get; set; }
    }

    public class HttpHandler
    {
        // 配置在类级别加载一次
        private static readonly string TargetUrl = Environment.GetEnvironmentVariable("TARGET_URL");
        private static readonly string RegexFormat = Environment.GetEnvironmentVariable("REGEX_FORMAT");
        private static readonly string RegexFormatAtBase64Decode = Environment.GetEnvironmentVariable("REGEX_FORMAT_AT_BASE64_DECODE");
        private static readonly bool IsValidationNotHtmlEnabled = "true".Equals(Environment.GetEnvironmentVariable("VALIDATION_NOT_HTML"), StringComparison.OrdinalIgnoreCase);

        // 定义重试策略：重试2次，每次间隔递增
        private static readonly IAsyncPolicy<IFlurlResponse> RetryPolicy = Policy
            .Handle<FlurlHttpException>() // 只针对网络等异常重试
            .OrResult<IFlurlResponse>(r => !r.ResponseMessage.IsSuccessStatusCode) // 或非成功状态码
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(retryAttempt));

        /// <summary>
        /// 新的事件处理入口 (POCO-based)
        /// </summary>
        public async Task<HTTPTriggerResponse> PocoHandler(HTTPTriggerEvent input, IFcContext context)
        {
            context.Logger.LogInformation("PocoHandler invoked. RequestId: {0}", context.RequestId);

            // 1. 检查环境变量配置
            if (string.IsNullOrWhiteSpace(TargetUrl))
            {
                context.Logger.LogWarning("Environment variable 'TARGET_URL' is not configured.");
                return new HTTPTriggerResponse
                {
                    StatusCode = 500, // Internal Server Error
                    Headers = new Dictionary<string, string> { { "Content-Type", "text/plain;charset=UTF-8" } },
                    Body = "服务端错误：未配置目标地址",
                    IsBase64Encoded = false
                };
            }

            string result;
            try
            {
                // 2. 使用 Polly 执行带有重试策略的请求
                var flurlResponse = await RetryPolicy.ExecuteAsync(() =>
                    TargetUrl.WithTimeout(20).GetAsync());

                string newContent = await flurlResponse.GetStringAsync();

                // 3. 执行所有验证
                ValidateContent(newContent, context.Logger);

                // 4. 所有验证通过，保存并使用新内容
                CacheManager.SaveConfig(TargetUrl, newContent);
                result = newContent;
                context.Logger.LogInformation($"Successfully fetched and validated new content from {TargetUrl}.");
            }
            catch (Exception ex)
            {
                // 5. 异常处理：包括请求失败、重试耗尽、验证失败等所有异常
                context.Logger.LogError(ex, $"Failed to fetch or validate content from {TargetUrl}. Attempting to use cached data.");

                var cachedData = CacheManager.LoadConfig(TargetUrl);
                if (!string.IsNullOrWhiteSpace(cachedData))
                {
                    result = cachedData;
                    context.Logger.LogInformation("Successfully loaded content from cache.");
                }
                else
                {
                    // 6. 如果连缓存都没有，则必须返回错误
                    context.Logger.LogError("No cached data available. Returning error to client.");
                    return new HTTPTriggerResponse
                    {
                        StatusCode = 502, // Bad Gateway，表示代理无法从上游获取有效响应
                        Headers = new Dictionary<string, string> { { "Content-Type", "text/plain;charset=UTF-8" } },
                        Body = "服务暂时不可用，请稍后重试",
                        IsBase64Encoded = false
                    };
                }
            }

            context.Logger.LogInformation($"Responding with content of size: {result.Length}");

            // 7. 成功返回结果 (无论是新获取的还是来自缓存的)
            return new HTTPTriggerResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain;charset=UTF-8" } },
                Body = result,
                IsBase64Encoded = false
            };
        }


        private void ValidateContent(string content, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("Validation failed: Content is empty.");
            }

            // 1. HTML 内容验证
            if (IsValidationNotHtmlEnabled && content.Contains("<html>"))
            {
                throw new Exception("Validation failed: Content appears to be an HTML document.");
            }

            // 2. 原文正则验证
            if (!string.IsNullOrEmpty(RegexFormat))
            {
                var regex = new Regex(RegexFormat, RegexOptions.Singleline | RegexOptions.Multiline);
                if (regex.Matches(content).Count <= 1)
                {
                    throw new Exception("Validation failed: Raw content does not match REGEX_FORMAT.");
                }
            }

            // 3. Base64 解码后验证
            if (!string.IsNullOrEmpty(RegexFormatAtBase64Decode))
            {
                string decodedContent;
                try
                {
                    var base64Bytes = Convert.FromBase64String(content);
                    decodedContent = Encoding.UTF8.GetString(base64Bytes);
                }
                catch (FormatException ex)
                {
                    logger.LogWarning(ex, "Content is not a valid Base64 string, skipping REGEX_FORMAT_AT_BASE64_DECODE validation.");
                    // 此处直接抛出，因为配置要求了对Base64解码后内容的验证
                    throw new Exception("Validation failed: Content is not a valid Base64 string.", ex);
                }

                var regex = new Regex(RegexFormatAtBase64Decode, RegexOptions.Singleline | RegexOptions.Multiline);
                if (regex.Matches(decodedContent).Count <= 1)
                {
                    throw new Exception("Validation failed: Base64-decoded content does not match REGEX_FORMAT_AT_BASE64_DECODE.");
                }
            }
            logger.LogInformation("Content validation successful.");
        }

        // 旧的 HandleRequest 和 Init 方法可以被安全地移除了，因为函数计算会直接调用 PocoHandler
    }

}