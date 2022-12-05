# Aliyun.ForwardURL

使用阿里云函数计算转发自己的机场订阅，带有缓存功能，避免突发情况订阅无法访问等问题

### 部署项目需要什么

你只需要注册阿里云账号，然后开通OSS对象存储和函数计算即可，按量付费费用基本可以忽略不计。

**如果你仅需要转发，不需要缓存，则可跳过OSS的开通和挂载**

### 地域的选择

在创建函数计算时，可选择国内及国外各种地域。
* 香港，经过BGP线路，稳定性相对高，缺点是还有可能失联，但概率非常低（相对直接连接订阅）
* 国内，拥有更好的直连性能，缺点是如果国内无法访问到你的订阅，那么转发节点也无法访问。

### 创建对象存储OSS存储桶

登录到阿里云后，访问[OSS的控制台](https://oss.console.aliyun.com/bucket)

点击【创建Bucket】

输入任意Bucket名、**地域**请和后面创建的函数计算一致。
![](https://pic1.zhimg.com/80/v2-6a1ee5e18a1a1d18c5f97a1754491324_720w.png)

### 创建函数计算服务并配置

访问[函数计算控制台]([https://oss.console.aliyun.com/bucket](https://fcnext.console.aliyun.com/cn-shanghai/services))

**地域**请和前面创建的OSS一致。

服务名称任意，**日志开启后会有多余的费用**，建议禁用。

![](https://pica.zhimg.com/80/v2-305618b73863c82e760f06eacd233a29_720w.png)

展开下方的**存储配置**挂载OSS，路径为/home/app
![image](https://user-images.githubusercontent.com/4475018/205268634-c0b15df2-4ad0-4c27-af6f-541d211a50b2.png)

进入刚才创建的服务点击【创建函数】，函数名字任意

![QQ截图20221202175708](https://user-images.githubusercontent.com/4475018/205267449-3df690da-1b41-4c06-8eec-b1a1ab66fa39.png)

代码包从github下载并上传

[https://github.com/aiqinxuancai/Aliyun.ForwardURL/releases](https://github.com/aiqinxuancai/Aliyun.ForwardURL/releases)

![QQ截图20221202175749](https://user-images.githubusercontent.com/4475018/205267463-93d19bfd-7fc5-4ff5-ac14-377ae2100158.png)

函数入口填写为

```
Aliyun.ForwardURL::Aliyun.ForwardURL.HttpHandler::HandleRequest
```

订阅分别为
    
* **TARGET_URL** [必填]你的订阅地址
* REGEX_FORMAT [可选]填写正则表达式验证结果
* REGEX_FORMAT_AT_BASE64_DECODE [可选]填写正则表达式验证结果（先对结果进行base64解码）
* VALIDATION_NOT_HTML [可选]验证内容必须是非HTML代码，默认为false，可设置为true打开

点击左侧【测试函数】，成功啦

![](https://pic2.zhimg.com/80/v2-a03960f98b3db988ac6cb6d100664bd1_720w.png)

上图中**请求地址**就是你的的转发订阅地址，在路由器插件或其他客户端中替换为此订阅地址即可。
