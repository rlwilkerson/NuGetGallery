﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;

namespace NuGetGallery.Jobs
{
    public class JobRequestQueue
    {
        public static readonly string DefaultQueueName = "nugetjobrequests";

        private CloudQueue _queue;

        public JobRequestQueue(CloudQueue queue)
        {
            _queue = queue;
        }

        public static JobRequestQueue WithDefaultName(CloudStorageAccount account)
        {
            return WithDefaultName(account.CreateCloudQueueClient());
        }

        public static JobRequestQueue WithDefaultName(CloudQueueClient client)
        {
            return new JobRequestQueue(client.GetQueueReference(DefaultQueueName));
        }

        public async Task<JobDequeueResult> Dequeue(TimeSpan invisibleFor, CancellationToken token)
        {
            var message = await _queue.SafeExecute(q => q.GetMessageAsync(
                invisibleFor,
                new QueueRequestOptions(),
                new OperationContext(),
                token));

            if (message == null)
            {
                return null;
            }
            else
            {
                JobDequeueResult result = null;
                try
                {
                    result = new JobDequeueResult(JobRequest.Parse(message.AsString, message));
                }
                catch(Exception ex)
                {
                    result = new JobDequeueResult(ex, message.AsString);
                }
                
                if (result.ParseException != null)
                {
                    // Have to delete the message to prevent it from reappearing
                    await _queue.DeleteMessageAsync(message);
                }

                return result;
            }
        }

        public async Task Acknowledge(JobRequest request)
        {
            if (request.Message != null)
            {
                await _queue.SafeExecute(q => q.DeleteMessageAsync(request.Message));
            }
        }

        public Task Enqueue(JobRequest req)
        {
            return EnqueueCore(req, visibilityTimeout: null);
        }

        public Task Enqueue(JobRequest req, TimeSpan visibilityTimeout)
        {
            return EnqueueCore(req, visibilityTimeout);
        }

        private Task EnqueueCore(JobRequest req, TimeSpan? visibilityTimeout)
        {
            var messageBody = req.Render();
            var message = new CloudQueueMessage(messageBody);
            return _queue.SafeExecute(q => q.AddMessageAsync(message, null, visibilityTimeout, new QueueRequestOptions(), new OperationContext()));
        }
    }
}