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


namespace Maying
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

            //使用CDN的话，会导致relativePath无法获取
            if (relativePath != "/" && !string.IsNullOrWhiteSpace(relativePath))
            {
                targetName = $"TARGET_URL{relativePath.Replace("/", "_").ToUpper()}";
            }
            string targetUrl = Environment.GetEnvironmentVariable(targetName);

            if (!string.IsNullOrWhiteSpace(targetUrl))
            {
                fcContext.Logger.LogInformation($"未配置订阅地址（{targetName}），请在环境变量中配置");
            }

            fcContext.Logger.LogInformation("method = {0}; requestPath = {1}; QueryString = {2}", method, relativePath, request.QueryString);
            fcContext.Logger.LogInformation("target = {0}", targetUrl);

            StreamReader sr = new StreamReader(request.Body);
            string requestBody = sr.ReadToEnd();

            string result = string.Empty;
            //获取订阅
            try
            {
                //fcContext.Logger.LogInformation("第一次请求");
                var getResp = await targetUrl.WithTimeout(10).GetAsync(); ;

                result = await getResp.Content.ReadAsStringAsync();
	            if (getResp.IsSuccessStatusCode == false)
	            {
                    //fcContext.Logger.LogInformation("第二次请求");
                    //获取订阅失败，重试一次
                    getResp = await targetUrl.WithTimeout(10).GetAsync(); ;
                    result = await getResp.Content.ReadAsStringAsync();
	            }

                if (!string.IsNullOrWhiteSpace(result))
                {
                    fcContext.Logger.LogInformation("存储");
                    //成功，保存
                    OSSManager.SaveConfig(targetName, result);
                }
            }
            catch (System.Exception ex)
            {
                fcContext.Logger.LogInformation("error = {0}", ex);
                //失败，读取新的ret
                var oldData = OSSManager.LoadConfig(targetName);
                if (!string.IsNullOrWhiteSpace(oldData))
                {
                    result = oldData;
                }
            }

            //将获取到的订阅内容返回
            fcContext.Logger.LogInformation("requestBody1 = {}", requestBody);
            response.StatusCode = 200;
            response.ContentType = "text/plain;charset=UTF-8";
            //Console.WriteLine(ret);

            await response.WriteAsync(result, encoding: Encoding.UTF8);
            return response;
        }
    }
}
