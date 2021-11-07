using Aliyun.OSS;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aliyun.ForwardURL
{
    class OSSManager
    {
        private static string _endpoint = "";
        private static string _accessKeyId = "";
        private static string _accessKeySecret = "";
        private static string _bucketName = "";

        static OSSManager()
        {
            _endpoint = Environment.GetEnvironmentVariable("COS_ENDPOINT");
            _accessKeyId = Environment.GetEnvironmentVariable("COS_ACCESSID");
            _accessKeySecret = Environment.GetEnvironmentVariable("COS_ACCESSSECRET");
            _bucketName = Environment.GetEnvironmentVariable("COS_BUCKETNAME");
        }


        public static string LoadConfig(string fileName)
        {
            var objectName = fileName.Replace(@"/", "_");
            string ret = string.Empty;
            var client = new OssClient(_endpoint, _accessKeyId, _accessKeySecret);
            try
            {
                //文件是否存在
                if (client.DoesObjectExist(_bucketName, objectName))
                {
                    HttpHandler.FcContext.Logger.LogInformation("dataExist");
                    var oldData = client.GetObject(_bucketName, objectName).Content;
                    StreamReader reader = new StreamReader(oldData);
                    string oldText = reader.ReadToEnd();
                    ret = oldText;
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }

            return ret;
        }

        //
        public static void SaveConfig(string fileName, string fileContent)
        {
            //涨跌幅每10分钟
            //价格固定推送
            HttpHandler.FcContext.Logger.LogInformation($"开始存储配置{fileName}...");
            var objectName = fileName.Replace(@"/", "_");

            var client = new OssClient(_endpoint, _accessKeyId, _accessKeySecret);
            try
            {
                byte[] array = Encoding.UTF8.GetBytes(fileContent);
                MemoryStream stream = new MemoryStream(array);
                client.PutObject(_bucketName, objectName, stream);
                stream.Dispose();
            }
            catch (Exception ex)
            {
                HttpHandler.FcContext.Logger.LogInformation("存储配置失败, {0}", ex.Message);
            }
        }
    }
}
