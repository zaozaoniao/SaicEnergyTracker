using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Dataflow.SaicEnergyTracker
{
    public interface IMsgBatchSender
    {
        Task<bool> PostMsgsync(string txt);
        Task CompleteAsync();
    }


    public class MsgBatchSender : IMsgBatchSender
    {
        private readonly EventHubProducerClient Client;
        private readonly TransformBlock<string, EventData> _transformBlock;
        private readonly BatchBlock<EventData> _packer;
        private readonly ActionBlock<EventData[]> _batchSender;

        private readonly DataflowOption _dataflowOption;
        private readonly Timer _trigger;
        private readonly ILogger _logger;

        public MsgBatchSender(EventHubClientWrapper ClientWrapper,IOptions<DataflowOption> option,ILoggerFactory loggerFactory)
        {
            Client = ClientWrapper.Client;
            _dataflowOption = option.Value;
            var dfLinkoption = new DataflowLinkOptions { PropagateCompletion = true };

            _transformBlock = new TransformBlock<string, EventData>(
                text => new EventData(Encoding.UTF8.GetBytes(text)),
                   new ExecutionDataflowBlockOptions
                   {
                       MaxDegreeOfParallelism = _dataflowOption.MaxDegreeOfParallelism
                   });
            _packer = new BatchBlock<EventData>(_dataflowOption.BatchSize);
            _batchSender = new ActionBlock<EventData[]>(msgs=> BatchSendAsync(msgs));
            _packer.LinkTo(_batchSender, dfLinkoption);

            _transformBlock.LinkTo(_packer, dfLinkoption, x => x != null);

            _trigger = new Timer(_ => _packer.TriggerBatch(), null, TimeSpan.Zero, TimeSpan.FromSeconds(_dataflowOption.TriggerInterval));

            _logger = loggerFactory.CreateLogger<DataTrackerMiddleware>();
        }

        private async Task BatchSendAsync(EventData[] msgs)
        {
            try
            {
                if (msgs != null)
                {
                    var i = 0;
                    while (i < msgs.Length)
                    {
                        var batch = await Client.CreateBatchAsync();
                        while (i < msgs.Length)
                        {
                            if (batch.TryAdd(msgs[i++]) == false)
                            {
                                break;
                            }
                        }
                        if(batch!= null && batch.Count>0)
                        {
                            await Client.SendAsync(batch);
                            batch.Dispose();
                        }
                    }
                }
            }
             catch (Exception ex)
            {
                // ignore and log any exception
                _logger.LogError(ex, "SendEventsAsync: {error}", ex.Message);
            }

        }

        public  async Task<bool> PostMsgsync(string txt)
        {
            return await _transformBlock.SendAsync(txt);
        }

        public async Task CompleteAsync()
        {
            _transformBlock.Complete();
            await _transformBlock.Completion;
            await _batchSender.Completion;
            await _batchSender.Completion;
        }
    }
}


/*
 https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-specify-the-degree-of-parallelism-in-a-dataflow-block
 描述了指定并发度（dataflow块同一个时间同时处理多个消息），
 这个操作在你的dataflow块在执行一个长时间计算的行为时能从并行处理消息中受益。
 默认的每个内置的块以他们输入的顺序输出消息（尽管你指定MaxParallelism让多个消息同时被处理）， 
 他们依然以他们输入的顺序输出。

 因为maxdegreeofParallelism 属性代表最大的执行并发度，dataflow也许会议小于指定的数字执行任务（为满足功能要求或者基于可用的系统资源考虑），
 dataflow绝不会使用比你指定的axdegreeofParallelism 大的执行并发度。
     */
