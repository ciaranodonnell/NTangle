/*
 * This file is automatically generated; any changes will be lost. 
 */

#nullable enable
#pragma warning disable

using CoreEx;
using CoreEx.Database;
using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.Mapping;
using Microsoft.Extensions.Logging;
using NTangle;
using NTangle.Cdc;
using NTangle.Data;
using NTangle.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SqlServerDemo.Publisher.Entities;

namespace SqlServerDemo.Publisher.Data
{
    /// <summary>
    /// Enables the Change Data Capture (CDC) <see cref="CustomerCdc"/> entity (aggregate root) orchestration (database table '[Legacy].[Customer]').
    /// </summary>
    public partial interface ICustomerCdcOrchestrator : IEntityOrchestrator<CustomerCdc> { }

    /// <summary>
    /// Manages the Change Data Capture (CDC) <see cref="CustomerCdc"/> entity (aggregate root) orchestration (database table '[Legacy].[Customer]').
    /// </summary>
    public partial class CustomerCdcOrchestrator : EntityOrchestrator<CustomerCdc, CustomerCdcOrchestrator.CustomerCdcEnvelopeCollection, CustomerCdcOrchestrator.CustomerCdcEnvelope, VersionTrackingMapper>, ICustomerCdcOrchestrator
    {
        private static readonly CustomerCdcMapper _customerCdcMapper = new CustomerCdcMapper();

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerCdcOrchestrator"/> class.
        /// </summary>
        /// <param name="db">The <see cref="IDatabase"/>.</param>
        /// <param name="eventPublisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public CustomerCdcOrchestrator(IDatabase db, IEventPublisher eventPublisher, IJsonSerializer jsonSerializer, ILogger<CustomerCdcOrchestrator> logger) :
            base(db, "[NTangle].[spCustomerBatchExecute]", "[NTangle].[spCustomerBatchComplete]", eventPublisher, jsonSerializer, logger) => CustomerCdcOrchestratorCtor();

        partial void CustomerCdcOrchestratorCtor(); // Enables additional functionality to be added to the constructor.

        /// <inheritdoc/>
        protected override async Task<EntityOrchestratorResult<CustomerCdcEnvelopeCollection, CustomerCdcEnvelope>> GetBatchEntityDataAsync(CancellationToken cancellationToken = default)
        {
            var cColl = new CustomerCdcEnvelopeCollection();

            var result = await SelectQueryMultiSetAsync(MultiSetArgs.Create(
                // Root table: '[Legacy].[Cust]'
                new MultiSetCollArgs<CustomerCdcEnvelopeCollection, CustomerCdcEnvelope>(_customerCdcMapper, __result => cColl = __result, stopOnNull: true)), cancellationToken).ConfigureAwait(false);

            result.Result.AddRange(cColl);
            return result;
        }

        /// <inheritdoc/>
        protected override string EventSubject => "Legacy.Customer";

        /// <inheritdoc/>
        protected override EventSubjectFormat EventSubjectFormat => EventSubjectFormat.NameOnly;

        /// <inheritdoc/>
        protected override EventActionFormat EventActionFormat => EventActionFormat.PastTense;

        /// <inheritdoc/>
        protected override Uri? EventSource => new Uri("/database/cdc/legacy/cust", UriKind.Relative);

        /// <inheritdoc/>
        protected override EventSourceFormat EventSourceFormat { get; } = EventSourceFormat.NameAndTableKey;

        /// <inheritdoc/>
        protected override string[]? ExcludePropertiesFromETag => new string[] { "RowVersion" };

        /// <summary>
        /// Represents a <see cref="CustomerCdc"/> envelope to append the required (additional) database properties.
        /// </summary>
        public class CustomerCdcEnvelope : CustomerCdc, IEntityEnvelope
        {
            /// <inheritdoc/>
            [JsonIgnore]
            public CdcOperationType DatabaseOperationType { get; set; }

            /// <inheritdoc/>
            [JsonIgnore]
            public string? DatabaseTrackingHash { get; set; }

            /// <inheritdoc/>
            [JsonIgnore]
            public byte[] DatabaseLsn { get; set; }
        }

        /// <summary>
        /// Represents a <see cref="CustomerCdcEnvelope"/> collection.
        /// </summary>
        public class CustomerCdcEnvelopeCollection : List<CustomerCdcEnvelope> { }

        /// <summary>
        /// Represents a <see cref="CustomerCdc"/> database mapper.
        /// </summary>
        public class CustomerCdcMapper : IDatabaseMapper<CustomerCdcEnvelope>
        {
            /// <inheritdoc/>
            public CustomerCdcEnvelope? MapFromDb(DatabaseRecord record, OperationTypes operationType) => new CustomerCdcEnvelope
            {
                Id = record.GetValue<int>("Id"),
                Name = record.GetValue<string?>("Name"),
                Email = record.GetValue<string?>("Email"),
                IsDeleted = record.GetValue<bool?>("IsDeleted"),
                RowVersion = record.GetValue<byte[]?>("RowVersion"),
                DatabaseOperationType = record.GetValue<CdcOperationType>("_OperationType"),
                DatabaseTrackingHash = record.GetValue<string>("_TrackingHash"),
                DatabaseLsn = record.GetValue<byte[]>("_Lsn")
            };

            /// <inheritdoc/>
            void IDatabaseMapper<CustomerCdcEnvelope>.MapToDb(CustomerCdcEnvelope? value, DatabaseParameterCollection parameters, OperationTypes operationType) => throw new NotImplementedException();
        }
    }
}

#pragma warning restore
#nullable restore