# Aliyun.ForwardURL

使用阿里云函数计算转发自己的机场订阅，带有缓存功能，避免突发情况订阅无法访问等问题。

你只需要注册阿里云账号，然后开通OSS对象存储和函数计算即可，按量付费费用基本可以忽略不计。

## 快速开始



**函数创建**

访问[函数计算控制台]([https://oss.console.aliyun.com/bucket](https://fcnext.console.aliyun.com/cn-shanghai/services))

地域（建议香港）
<img width="788" height="453" alt="image" src="https://github.com/user-attachments/assets/a6ab96bf-7cd4-4bad-83f1-41963f5eb866" />

<img width="730" height="461" alt="image" src="https://github.com/user-attachments/assets/30ade15a-4f7e-43ab-a58f-0c190dc4ae97" />

函数入口填写为：

```
Aliyun.ForwardURL::Aliyun.ForwardURL.HttpHandler::PocoHandler
```


在高级设置中可添加下列环境变量：
    
* **TARGET_URL** [必填]你的订阅地址
* REGEX_FORMAT [可选]填写正则表达式验证结果
* REGEX_FORMAT_AT_BASE64_DECODE [可选]填写正则表达式验证结果（先对结果进行base64解码）
* VALIDATION_NOT_HTML [可选]验证内容必须是非HTML代码，默认为false，可设置为true打开

填写好TARGET_URL后，在触发器界面复制并访问函数URL，如果一切正常，会下载一个json文件，**绑定自定义域名后**，可正常返回完整的转发数据

----

**对象存储创建（可选，不需要缓存可跳过）**

登录到阿里云后，访问[OSS的控制台](https://oss.console.aliyun.com/bucket)

点击【创建Bucket】

输入任意Bucket名、地域（建议香港）请和后面创建的函数计算一致。
![](https://pic1.zhimg.com/80/v2-6a1ee5e18a1a1d18c5f97a1754491324_720w.png)


创建完毕后进到高级设置里，填写对象存储挂载 **（可选，不需要缓存可跳过）**

<img width="813" height="596" alt="image" src="https://github.com/user-attachments/assets/3ad8cdc2-9f91-4b1b-af44-1baf77ba1f34" />
