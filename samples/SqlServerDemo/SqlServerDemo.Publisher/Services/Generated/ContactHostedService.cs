/*
 * This file is automatically generated; any changes will be lost. 
 */

#nullable enable
#pragma warning disable

namespace SqlServerDemo.Publisher.Services
{
    /// <summary>
    /// Provides the Change Data Capture (CDC) <see cref="ContactCdc"/> entity (aggregate root) <see cref="HostedService{T}"/> capabilities (database table '[Legacy].[Contact]').
    /// </summary>
    public partial class ContactHostedService : CdcHostedService<IContactOrchestrator, ContactCdc>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContactHostedService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="synchronizer"> The <see cref="IServiceSynchronizer"/>.</param>
        public ContactHostedService(IServiceProvider serviceProvider, ILogger<ContactHostedService> logger, SettingsBase settings, IServiceSynchronizer synchronizer) : base(serviceProvider, logger, settings, synchronizer) { }
    }
}

#pragma warning restore
#nullable restore