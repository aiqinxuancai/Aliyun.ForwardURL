using Aliyun.Serverless.Core;
using Aliyun.Serverless.Core.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl;
using System.Text.RegularExpressions;

namespace Aliyun.ForwardURL
{
    public class HttpHandler : FcHttpEntrypoint
    {

        public static IFcContext FcContext { set; get; }

        protected override void Init(IWebHostBuilder builder)
        {

        }

        
        //用于中转代理的订阅
        public override async Task<HttpResponse> HandleRequest(HttpRequest request, HttpResponse response, IFcContext fcContext)
        {
            string method = request.Method;
            string relativePath = request.Path.Value;
            FcContext = fcContext;

            string targetName = "TARGET_URL";
            string VALIDATION_FORMAT = Environment.GetEnvironmentVariable("VALIDATION_FORMAT");
            string VALIDATION_NOT_HTML = Environment.GetEnvironmentVariable("VALIDATION_NOT_HTML"); 

            //这里支持使用在URL后增加/***/***的方法来配置多个URL，环境变量里只需配置对应的TARGET_URL_***_*** (使用CDN的话，似乎会导致relativePath无法获取)
            if (relativePath != "/" && !string.IsNullOrWhiteSpace(relativePath))
            {
                targetName = $"TARGET_URL{relativePath.Replace("/", "_").ToUpper()}";
            }

            string targetUrl = Environment.GetEnvironmentVariable(targetName);

            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                fcContext.Logger.LogInformation($"未配置订阅地址（{targetName}），请在环境变量中配置");
                response.StatusCode = 200;
                response.ContentType = "text/plain;charset=UTF-8";
                await response.WriteAsync("未配置地址", encoding: Encoding.UTF8);
                return response;
            }

            //这里暂时不打印了
            //fcContext.Logger.LogInformation("method = {0}; requestPath = {1}; QueryString = {2}", method, relativePath, request.QueryString);
            //fcContext.Logger.LogInformation("target = {0}", targetUrl);

            StreamReader sr = new StreamReader(request.Body);
            string requestBody = sr.ReadToEnd();

            string result = string.Empty;
            //获取订阅
            try
            {
                //fcContext.Logger.LogInformation("第一次请求");
                var getResp = await targetUrl.WithTimeout(10).GetAsync();

                result = await getResp.Content.ReadAsStringAsync();
	            if (getResp.IsSuccessStatusCode == false)
	            {
                    //fcContext.Logger.LogInformation("第二次请求");
                    //获取订阅失败，重试一次
                    getResp = await targetUrl.WithTimeout(10).GetAsync();
                    result = await getResp.Content.ReadAsStringAsync();
	            }

                if (getResp.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(result))
                {
                    //如果非base64，可能失败
                    var base64bytes = Convert.FromBase64String(result);
                    var baseData = Encoding.UTF8.GetString(base64bytes);

                    //验证数据非网页数据
                    if (VALIDATION_NOT_HTML == "true")
                    {
                        
                        if (!result.Contains("<"))
                        {
                            //成功，保存
                            CacheManager.SaveConfig(targetName, result);
                        }
                        else
                        {
                            throw new Exception("格式验证失败(是HTML内容，可能为报错页面)");
                        }
                    }

                    //验证数据
                    if (VALIDATION_FORMAT == "true")
                    {
                        var regex = new Regex("^ss(|r):", RegexOptions.Singleline | RegexOptions.Multiline);
                        if (regex.Matches(baseData).Count > 1)
                        {
                            //成功，保存
                            CacheManager.SaveConfig(targetName, result);
                        }
                        else
                        {
                            throw new Exception("格式验证失败(解析后没有订阅内容)");
                        }
                    }


                    CacheManager.SaveConfig(targetName, result);
                }
                else
                {
                    getResp.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                fcContext.Logger.LogInformation("请求源地址失败 = {0}", ex);
                //失败，读取新的ret
                var oldData = CacheManager.LoadConfig(targetName);
                if (!string.IsNullOrWhiteSpace(oldData))
                {
                    result = oldData;
                }
            }

            //这里暂时不打印了
            //fcContext.Logger.LogInformation("requestBody1 = {}", requestBody);

            fcContext.Logger.LogInformation($"返回URL内容尺寸{requestBody.Length}");

            response.StatusCode = 200;
            response.ContentType = "text/plain;charset=UTF-8";

            await response.WriteAsync(result, encoding: Encoding.UTF8);
            return response;
        }
    }
}
