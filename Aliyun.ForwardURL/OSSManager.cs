using Aliyun.OSS;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Maying
{
    class OSSManager
    {
        const string endpoint = ""; //oss的endpoint
        const string accessKeyId = "";
        const string accessKeySecret = "";
        const string bucketName = ""; //oss桶的名字

        public static string LoadConfig(string fileName)
        {
            var objectName = fileName.Replace(@"/", "_");
            string ret = string.Empty;
            var client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            try
            {
                //文件是否存在
                if (client.DoesObjectExist(bucketName, objectName))
                {
                    HttpHandler.FcContext.Logger.LogInformation("dataExist");
                    var oldData = client.GetObject(bucketName, objectName).Content;
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

        public static void SaveConfig(string fileName, string fileContent)
        {
            HttpHandler.FcContext.Logger.LogInformation($"开始存储配置{fileName}...");
            var objectName = fileName.Replace(@"/", "_");

            var client = new OssClient(endpoint, accessKeyId, accessKeySecret);
            try
            {
                byte[] array = Encoding.UTF8.GetBytes(fileContent);
                MemoryStream stream = new MemoryStream(array);
                client.PutObject(bucketName, objectName, stream);
                stream.Dispose();
            }
            catch (Exception ex)
            {
                HttpHandler.FcContext.Logger.LogInformation("存储配置失败, {0}", ex.Message);
            }
        }
    }
}
