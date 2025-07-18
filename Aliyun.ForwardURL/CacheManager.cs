using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aliyun.ForwardURL
{
    class CacheManager
    {

        static CacheManager()
        {
        }

        public static string LoadConfig(string fileName)
        {
            if (!File.Exists($"/home/app/{MD5Helper.GetMD5(fileName)}"))
            {
                return "";
            }
            else
            {
                return File.ReadAllText($"/home/app/{MD5Helper.GetMD5(fileName)}");
            }


        }

        //
        public static void SaveConfig(string fileName, string fileContent)
        {
            File.WriteAllText($"/home/app/{MD5Helper.GetMD5(fileName)}", fileContent);
        }


    }
}
