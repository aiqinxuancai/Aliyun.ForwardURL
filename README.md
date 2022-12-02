# Aliyun.ForwardURL

在使用机场时，因订阅地址在国外，经常因为网络问题访问失败，这可能导致路由器插件和部分客户端上丢失订阅，而导致必须手动重新订阅来使订阅节点恢复等等很蛋疼的问题。

本项目使用阿里云函数计算（白嫖）进行URL转发，并缓存订阅数据，如源订阅地址访问失败，则使用缓存来进行返回，完全避免偶尔订阅失败的问题。

函数计算调用消耗有非常大的免费额度，是完全用不完的，你只需要付费0.5元/GB的外网流量即可，一个月大概消耗几M的流量，1块钱可以用十几年，无限接近于白嫖。

## 部署项目需要什么

你只需要有个阿里云账号（淘宝账号），然后开通对象存储桶和函数计算即可。

## 创建对象存储OSS存储桶

登录到阿里云后，到达[OSS的控制台](https://oss.console.aliyun.com/bucket)

点击【创建Bucket】

输入Bucket名称（随意）、地域需要和后面创建的函数计算一致，可以考虑香港，这样在短时阻断国内无法访问时，让其可以访问，且BGP也相对其他海外地址来的稳定，当然也可选国内，拥有更好的直连性能。

然后记录下Bucket名称和Endpoint值后续要用到，点击下方的确定进行创建。

![](https://pic1.zhimg.com/80/v2-6a1ee5e18a1a1d18c5f97a1754491324_720w.png)

## 获得AccessKey

后续从函数计算访问OSS需要用到，在[这个页面](https://ram.console.aliyun.com/manage/ak)页面进行创建，创建后请注意保管好ID和KEY，不要暴露给他人。记录下来后续要用到

## 创建函数计算服务并配置

访问[https://fcnext.console.aliyun.com/cn-shanghai/services](https://fcnext.console.aliyun.com/cn-shanghai/services)

注意上方选择地域和OSS为同一个地区。

名称可随你喜欢填写，日志开启后会有多余的费用，建议禁用。

![](https://pica.zhimg.com/80/v2-305618b73863c82e760f06eacd233a29_720w.png)

创建成功后进入刚才创建的服务点击【创建函数】

名字可随你喜欢填写（用随机字符串可避免被穷举到地址）

![QQ截图20221202175708](https://user-images.githubusercontent.com/4475018/205267449-3df690da-1b41-4c06-8eec-b1a1ab66fa39.png)

代码包从github下载并上传

[https://github.com/aiqinxuancai/Aliyun.ForwardURL/releases](https://github.com/aiqinxuancai/Aliyun.ForwardURL/releases)

![QQ截图20221202175749](https://user-images.githubusercontent.com/4475018/205267463-93d19bfd-7fc5-4ff5-ac14-377ae2100158.png)

函数入口填写为

```
Aliyun.ForwardURL::Aliyun.ForwardURL.HttpHandler::HandleRequest
```

订阅分别为
    
* TARGET_URL 你的订阅地址
* REGEX_FORMAT 填写正则表达式验证结果
* REGEX_FORMAT_AT_BASE64_DECODE 填写正则表达式验证结果（先对结果进行base64解码）
* VALIDATION_NOT_HTML 验证内容必须是非HTML代码，默认为false，可设置为true打开

![](https://pic1.zhimg.com/80/v2-bb419f1937cd3f09d58df40b4ea4e438_720w.png)

点击左侧【测试函数】，成功啦

![](https://pic2.zhimg.com/80/v2-a03960f98b3db988ac6cb6d100664bd1_720w.png)

上图中请求地址就是你的的转发订阅地址，在路由器插件或其他客户端中替换为此订阅地址即可。
