using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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

            if (!File.Exists($"/home/app/{fileName}"))
            {
                return "";
            }
            else
            {
                return File.ReadAllText($"/home/app/{fileName}");
            }


        }

        //
        public static void SaveConfig(string fileName, string fileContent)
        {
            File.WriteAllText($"/home/app/{fileName}", fileContent);
        }


    }
}
