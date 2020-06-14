# Qbcode.Consul
Bumblebee consul 服务发现插件，就是拿官方的插件改了一下，改成了自己更喜欢的样子

## 使用方法

### 1，加载插件
```
g = new Gateway();
.....省略省略
g.LoadPlugin(
                 typeof(Qbcode.Consul.Plugin).Assembly
               );
```

### 2，配置

```
{

    //ConsulAddress 对应Consul的服务地址
    "ConsulAddress": "http://192.168.2.19:8500",
    //检索相应名称的服务列表一个或多个
    "Services": [
        "bumblebee_services"
    ],
    //访问Consul相应的Token信息
    "Token": null,
    //检索相应的数据中心名称
    "DataCenter": "dc1"
}
```

### 注册服务

```
Dictionary<string, string> meta = new Dictionary<string, string>();
meta.Add("path", "^/home/http/.*");
//新增的这个
meta.Add("path-ws", "^/home/ws/.*");
client.Agent.ServiceRegister(new AgentServiceRegistration
{
    Tags = new string[] { "Bumblebee" },
    Address = "127.0.0.1",
    Port = 8080,
    Name = "bumblebee_services",
    Meta = meta,
    ID = "api_test1"
}).Wait();

```
