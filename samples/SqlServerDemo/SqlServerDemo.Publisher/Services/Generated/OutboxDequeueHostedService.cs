/*
 * This file is automatically generated; any changes will be lost. 
 */

#nullable enable
#pragma warning disable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTangle.Services;
using System;
using SqlServerDemo.Publisher.Data;
using SqlServerDemo.Publisher.Entities;

namespace SqlServerDemo.Publisher.Services
{
    /// <summary>
    /// Provides the event outbox dequeue and publishing hosted service capabilities (see <see cref="OutboxService{T}"/>).
    /// </summary>
    public partial class OutboxDequeueHostedService : OutboxService<EventOutboxMapper>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutboxDequeueHostedService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <param name="config">The <see cref="IConfiguration"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="synchronizer"> The <see cref="IServiceSynchronizer"/>.</param>
        public OutboxDequeueHostedService(IServiceProvider serviceProvider, ILogger<OutboxDequeueHostedService> logger, IConfiguration config, IServiceSynchronizer synchronizer) : base(serviceProvider, logger, config, synchronizer) { }
    }
}

#pragma warning restore
#nullable restore