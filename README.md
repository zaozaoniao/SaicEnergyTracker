### TPL Dataflow  sample for  azure eventhubs batch send


## .NetCore 准实时批量发送数据到事件中心
#### .NET库 (Azure.Messaging.EventHubs)
我们使用Asp.NetCore以Azure App Service形式部署，依赖Azure App Service的自动缩放能录应对物联网的潮汐大流量。   
>通常**推荐批量发送**到事件中心，能有效增加web服务的吞吐量和响应能力。  
 目前新版SDk： Azure.Messaging.EventHubs仅支持`分批`发送。

1.  nuget上引入Azure.Messaging.EventHubs库
2. `EventHubProducerClient`客户端负责分批发送数据到事件中心，根据发送时指定的选项，事件数据可能会自动路由到可用分区或发送到特定请求的分区。
> 在以下情况下，建议允许自动路由分区：  
    1） 事件的发送必须高度可用  
    2） 事件数据应在所有可用分区之间平均分配。  
> 自动路由分区的规则:  
    1）使用循环法将事件平均分配到所有可用分区中  
    2）如果某个分区不可用，事件中心将自动检测到该分区并将消息转发到另一个可用分区。  

我们要注意，根据选定的 命令空间定价层， 每批次发给事件中心的最大消息大小也不一样：
![](https://static01.imgkr.com/temp/924e4a07a3dd4763ba2ab7ae825f0c30.png)


#### 分段批量发送策略 
这里我们就需要思考： web程序收集数据是以`个数`为单位； 但是我们分批发送时要根据`分批的字节大小`来切分。  
我的方案是： 因引入TPL Dataflow 管道:

![](https://static01.imgkr.com/temp/d5ccbc04445e47b3b340b7a74f5fb84c.png)

1. web程序收到数据，立刻丢入`TransformBlock<string, EventData>`
2. 转换到EventData之后，使用`BatchBlock<EventData>`按照个数打包
3. 利用`ActionBlock<EventData[]>`在包内 累积指定字节大小批量发送
- 最后我们设置一个定时器(5min)，强制在BatchBlock的前置队列未满时打包，并发送。
