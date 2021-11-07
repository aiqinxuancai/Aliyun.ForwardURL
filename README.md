# Aliyun.ForwardURL

很多人在使用机场时，因为订阅地址都是在海外，经常因为网络问题访问不到，这可能会导致路由器插件和部分客户端上丢失订阅，而导致必须手动重新订阅来使订阅节点恢复等等很蛋疼的问题。

本项目使用自架阿里云函数计算（白嫖）来进行一次转发，并缓存订阅数据，如订阅失败，则使用缓存来进行返回，完全避免上述问题。

函数计算调用消耗有非常大的免费额度，是完全用不完的，你只需要付费0.5元/GB的外网流量即可，一个月大概消耗几M的流量，1块钱可以用十几年，无限接近于白嫖。

## 需要什么

当然这个过程并不需要什么技术含量，你只需要有个阿里云账号（淘宝账号），然后开通一个对象存储桶，然后开通一个函数计算即可。

## 创建对象存储OSS存储桶

登录到阿里云后，到达[OSS的控制台](https://oss.console.aliyun.com/bucket)

点击【创建Bucket】

输入Bucket名称（随意）、地域需要和后面创建的函数计算一致，可以考虑香港，这样对大陆以外的地址有较好的访问质量，当然也可选国内，拥有更好的直连性能。

然后记录下Bucket名称和Endpoint值后续要用到，点击下方的确定进行创建。

![](https://pic1.zhimg.com/80/v2-6a1ee5e18a1a1d18c5f97a1754491324_720w.png)

## 获得AccessKey

后续从函数计算访问OSS需要用到，在[这个页面](https://ram.console.aliyun.com/manage/ak)页面进行创建，创建后请注意保管好ID和KEY，不要暴露给他人。记录下来后续要用到

## 创建函数计算服务并配置

访问[https://fcnext.console.aliyun.com/cn-shanghai/services](https://fcnext.console.aliyun.com/cn-shanghai/services)

注意上方选择地域和OSS为同一个地区。

名称可随你喜欢填写，日志也可随意启用或禁用。

![](https://pica.zhimg.com/80/v2-305618b73863c82e760f06eacd233a29_720w.png)

创建成功后进入刚才创建的服务点击【创建函数】

![](https://pic2.zhimg.com/80/v2-622ea4ea2d51ab08fde10d64b5623ed2_720w.png)

创建成功后编辑环境信息

![](https://pic2.zhimg.com/80/v2-d938df443b0277729d666340c7a0c1ed_720w.png)

![](https://pica.zhimg.com/80/v2-ece3b70d823aecf526cc7ed2ad9922b9_720w.png)

![](https://pic2.zhimg.com/80/v2-746b6f880826ffddd913ce7fcc3f4d0f_720w.png)

函数入口填写为

```
Aliyun.ForwardURL::Aliyun.ForwardURL.HttpHandler::HandleRequest
```

接下来添加配置环境变量

![](https://pic2.zhimg.com/80/v2-29b032b7d183240e3192feccdffc7756_720w.png)

  

![](https://pic4.zhimg.com/v2-71e18ab1ab28afb27f9e628ae94e3697_b.png)

分别为

1.  COS_ACCESSID （AccessId，上面的步骤有获取方法）
    
2.  COS_ACCESSSECRET（AccessSecret，上面的步骤有获取方法）
    
3.  COS_BUCKETNAME （对象存储Bucket名称）
    
4.  COS_ENDPOINT （对象存储Bucket的Endpoint，前面拼上https:// ，例：[https://oss-cn-hangzhou.aliyuncs.com](https://oss-cn-hangzhou.aliyuncs.com/)）
    
5.  TARGET_URL （你的订阅地址）
    

回到函数代码

![](https://pic3.zhimg.com/80/v2-75880cbcfe637618a4eab3ceb6e6f9aa_720w.png)

代码包从github下载并上传

[https://github.com/aiqinxuancai/Aliyun.ForwardURL/releases](https://github.com/aiqinxuancai/Aliyun.ForwardURL/releases)

![](https://pic1.zhimg.com/80/v2-bb419f1937cd3f09d58df40b4ea4e438_720w.png)

点击左侧【测试函数】，成功啦

![](https://pic2.zhimg.com/80/v2-a03960f98b3db988ac6cb6d100664bd1_720w.png)

上图中请求地址就是你的的转发订阅地址，在路由器插件或其他客户端中替换为此订阅地址即可。
