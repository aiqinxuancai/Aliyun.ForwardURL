using Aliyun.Serverless.Core;
using Aliyun.Serverless.Core.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using System.Text.RegularExpressions;
using Polly; 

namespace Aliyun.ForwardURL
{
    public class HttpHandler : FcHttpEntrypoint
    {
        // 配置在类级别加载一次
        private static readonly string TargetUrl = Environment.GetEnvironmentVariable("TARGET_URL");
        private static readonly string RegexFormat = Environment.GetEnvironmentVariable("REGEX_FORMAT");
        private static readonly string RegexFormatAtBase64Decode = Environment.GetEnvironmentVariable("REGEX_FORMAT_AT_BASE64_DECODE");
        private static readonly bool IsValidationNotHtmlEnabled = "true".Equals(Environment.GetEnvironmentVariable("VALIDATION_NOT_HTML"), StringComparison.OrdinalIgnoreCase);

        // 定义重试策略：重试2次，每次间隔1秒
        private static readonly IAsyncPolicy<IFlurlResponse> RetryPolicy = Policy
            .Handle<FlurlHttpException>() // 只针对网络等异常重试
            .OrResult<IFlurlResponse>(r => !r.ResponseMessage.IsSuccessStatusCode) // 或非成功状态码
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(retryAttempt));

        protected override void Init(IWebHostBuilder builder)
        {
        }

        public override async Task<HttpResponse> HandleRequest(HttpRequest request, HttpResponse response, IFcContext fcContext)
        {
            if (string.IsNullOrWhiteSpace(TargetUrl))
            {
                fcContext.Logger.LogWarning("Environment variable 'TARGET_URL' is not configured.");
                response.StatusCode = 500; // 使用 500 Internal Server Error 更合适
                response.ContentType = "text/plain;charset=UTF-8";
                await response.WriteAsync("服务端错误：未配置目标地址", encoding: Encoding.UTF8);
                return response;
            }

            string result;
            try
            {
                // 使用 Polly 执行带有重试策略的请求
                var flurlResponse = await RetryPolicy.ExecuteAsync(() =>
                    TargetUrl.WithTimeout(10).GetAsync());

                string newContent = await flurlResponse.GetStringAsync();

                // 执行所有验证
                ValidateContent(newContent, fcContext.Logger);

                // 所有验证通过，保存并使用新内容
                CacheManager.SaveConfig(TargetUrl, newContent);
                result = newContent;
                fcContext.Logger.LogInformation($"Successfully fetched and validated new content from {TargetUrl}.");
            }
            catch (Exception ex)
            {
                // 包括请求失败、重试耗尽、验证失败等所有异常
                fcContext.Logger.LogError(ex, $"Failed to fetch or validate content from {TargetUrl}. Attempting to use cached data.");

                var cachedData = CacheManager.LoadConfig(TargetUrl);
                if (!string.IsNullOrWhiteSpace(cachedData))
                {
                    result = cachedData;
                    fcContext.Logger.LogInformation("Successfully loaded content from cache.");
                }
                else
                {
                    // 如果连缓存都没有，则必须返回错误
                    fcContext.Logger.LogError("No cached data available. Returning error to client.");
                    response.StatusCode = 502; // Bad Gateway，表示代理无法从上游获取有效响应
                    response.ContentType = "text/plain;charset=UTF-8";
                    await response.WriteAsync("服务暂时不可用，请稍后重试", encoding: Encoding.UTF8);
                    return response;
                }
            }

            fcContext.Logger.LogInformation($"Responding with content of size: {result.Length}");

            response.StatusCode = 200;
            // 考虑从目标响应头获取 Content-Type，这里暂时保持原样
            response.ContentType = "text/plain;charset=UTF-8";
            await response.WriteAsync(result, encoding: Encoding.UTF8);
            return response;
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
                    // 根据需求，这里可以选择 return（跳过验证）或 throw（视为验证失败）
                    // 此处选择直接抛出，因为配置要求了对Base64解码后内容的验证
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
    }
}
