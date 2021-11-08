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
            HttpHandler.FcContext.Logger.LogInformation($"读缓存{fileName}...");
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
                HttpHandler.FcContext.Logger.LogInformation("读取缓存失败, {0}", ex.Message);
            }

            return ret;
        }

        public static void SaveConfig(string fileName, string fileContent)
        {
            HttpHandler.FcContext.Logger.LogInformation($"写缓存{fileName}...");
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
                HttpHandler.FcContext.Logger.LogInformation("存储缓存失败, {0}", ex.Message);
            }
        }
    }
}
