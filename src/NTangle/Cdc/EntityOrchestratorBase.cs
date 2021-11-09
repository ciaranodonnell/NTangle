﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/NTangle

using Microsoft.Extensions.Logging;
using NTangle.Data;
using NTangle.Events;
using NTangle.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NTangle.Cdc
{
    /// <summary>
    /// Provides the base (internal) Change Data Capture (CDC) entity (aggregate root) orchestration capability.
    /// </summary>
    /// <typeparam name="TEntity">The root entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TEntityEnvelopeColl">The <typeparamref name="TEntityEnvelope"/> collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TEntityEnvelope">The <typeparamref name="TEntity"/> envelope <see cref="Type"/>.</typeparam>
    /// <typeparam name="TVersionTrackerMapper">The <see cref="VersionTracker"/> database and table-valued parameter mapper <see cref="Type"/>.</typeparam>
    public abstract class EntityOrchestratorBase<TEntity, TEntityEnvelopeColl, TEntityEnvelope, TVersionTrackerMapper> : IEntityOrchestrator
        where TEntity : class, IEntity, new()
        where TEntityEnvelopeColl : List<TEntityEnvelope>, new()
        where TEntityEnvelope : class, TEntity, IEntityEnvelope, new()
        where TVersionTrackerMapper : IDatabaseMapper<VersionTracker>, IDatabaseTvp<VersionTracker>, new()
    {
        private const string MaxQuerySizeParamName = "MaxQuerySize";
        private const string ContinueWithDataLossParamName = "ContinueWithDataLoss";
        private const string BatchTrackingIdParamName = "BatchTrackingId";
        private const string VersionTrackingListParamName = "@VersionTrackingList";

        private static readonly BatchTrackerMapper _batchTrackerMapper = new();
        private static readonly TVersionTrackerMapper _versionTrackerMapper = new();
        private string? _name;
        private int _maxQuerySize = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityOrchestratorBase{TEntity, TEntityEnvelopeColl, TEntityEnvelope, TVersionTrackerMapper}"/> class with no identifier mapping support.
        /// </summary>
        /// <param name="db">The <see cref="IDatabase"/>.</param>
        /// <param name="executeStoredProcedureName">The name of the batch execute stored procedure.</param>
        /// <param name="completeStoredProcedureName">The name of the batch complete stored procedure.</param>
        /// <param name="eventPublisher">The <see cref="IEventPublisher"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        internal EntityOrchestratorBase(IDatabase db, string executeStoredProcedureName, string completeStoredProcedureName, IEventPublisher eventPublisher, ILogger logger)
        {
            Db = db ?? throw new ArgumentNullException(nameof(db));
            ExecuteStoredProcedureName = executeStoredProcedureName ?? throw new ArgumentNullException(nameof(executeStoredProcedureName));
            CompleteStoredProcedureName = completeStoredProcedureName ?? throw new ArgumentNullException(nameof(completeStoredProcedureName));
            EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        public IDatabase Db { get; }

        /// <summary>
        /// Gets the name of the batch <b>execute</b> stored procedure.
        /// </summary>
        public string ExecuteStoredProcedureName { get; }

        /// <summary>
        /// Gets the name of the batch <b>complete</b> stored procedure.
        /// </summary>
        public string CompleteStoredProcedureName { get; }

        /// <summary>
        /// Gets the <see cref="IEventPublisher"/>.
        /// </summary>
        public IEventPublisher EventPublisher { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Gets the Execution Identifier for the current execution.
        /// </summary>
        /// <remarks>This value is managed/allocated internally.</remarks>
        protected Guid ExecutionId { get; private set; }

        /// <summary>
        /// Gets the service name (used for logging).
        /// </summary>
        protected virtual string ServiceName => _name ??= GetType().Name;

        /// <summary>
        /// Gets the <see cref="EventData.Subject"/> (to be further formatted as per <see cref="EventSubjectFormat"/>).
        /// </summary>
        protected abstract string EventSubject { get; }

        /// <summary>
        /// Gets the <see cref="EventData.Subject"/> <see cref="Events.EventSubjectFormat"/>.
        /// </summary>
        protected virtual EventSubjectFormat EventSubjectFormat { get; } = EventSubjectFormat.NameAndKey;

        /// <summary>
        /// Gets the <see cref="EventData.Action"/> <see cref="Events.EventActionFormat"/>.
        /// </summary>
        protected virtual EventActionFormat EventActionFormat { get; } = EventActionFormat.None;

        /// <summary>
        /// Gets the <see cref="EventData.Source"/>.
        /// </summary>
        protected virtual Uri? EventSource { get; }

        /// <summary>
        /// Gets the <see cref="EventData.Source"/> <see cref="Events.EventSourceFormat"/>.
        /// </summary>
        protected virtual EventSourceFormat EventSourceFormat { get; } = EventSourceFormat.NameAndKey;

        /// <summary>
        /// Gets the list of property names that should be excluded from the serialized JSON <see cref="IETag"/> generation.
        /// </summary>
        protected virtual string[]? ExcludePropertiesFromETag { get; }

        /// <summary>
        /// Gets or sets the maximum query size to limit the number of CDC (Change Data Capture) rows that are batched in a <see cref="BatchTracker"/>.
        /// </summary>
        /// <remarks>Defaults to 100.</remarks>
        public int MaxQuerySize { get => _maxQuerySize; set => _maxQuerySize = value < 1 ? 100 : value; }

        /// <summary>
        /// Indicates whether to ignore any data loss and continue using the CDC (Change Data Capture) data that is available.
        /// </summary>
        /// <remarks>For more information as to why data loss may occur see: https://docs.microsoft.com/en-us/sql/relational-databases/track-changes/administer-and-monitor-change-data-capture-sql-server </remarks>
        public bool ContinueWithDataLoss { get; set; }

        /// <summary>
        /// Gets or sets the delegate to perform additional processing on the consolidated set of entities.
        /// </summary>
        /// <remarks>This is invoked directly after consolidation, and prior to versioning and completion.</remarks>
        protected Func<TEntityEnvelopeColl, Task>? AdditionalEnvelopeProcessing { get; set; }

        /// <summary>
        /// Completes an existing batch updating the corresponding <paramref name="versionTracking"/> where appropriate.
        /// </summary>
        /// <param name="batchTrackerId">The <see cref="BatchTracker.Id"/>.</param>
        /// <param name="versionTracking">The <see cref="VersionTracker"/> list.</param>
        /// <returns>The <see cref="EntityOrchestratorResult"/>.</returns>
        Task<EntityOrchestratorResult> IEntityOrchestrator.CompleteAsync(long batchTrackerId, List<VersionTracker> versionTracking) => CompleteAsync(batchTrackerId, versionTracking);

        /// <summary>
        /// Completes an existing batch updating the corresponding <paramref name="versionTracking"/> where appropriate.
        /// </summary>
        /// <param name="batchTrackerId">The <see cref="BatchTracker.Id"/>.</param>
        /// <param name="versionTracking">The <see cref="VersionTracker"/> list.</param>
        /// <returns>The <see cref="EntityOrchestratorResult"/>.</returns>
        public async Task<EntityOrchestratorResult> CompleteAsync(long batchTrackerId, List<VersionTracker> versionTracking)
        {
            if (ExecutionId == Guid.Empty)
                ExecutionId = Guid.NewGuid();

            var result = new EntityOrchestratorResult<TEntityEnvelopeColl, TEntityEnvelope> { ExecutionId = ExecutionId };
            var msa = new MultiSetSingleArgs<BatchTracker>(_batchTrackerMapper, r => result.Batch = r, isMandatory: false, stopOnNull: true);

            try
            {
                await Db.StoredProcedure(CompleteStoredProcedureName, p =>
                {
                    p.AddParameter(BatchTrackingIdParamName, batchTrackerId);
                    p.AddTableValuedParameter(VersionTrackingListParamName, _versionTrackerMapper.CreateTableValuedParameter(versionTracking));
                }).SelectMultiSetAsync(msa).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || (ex is AggregateException aex && aex.InnerException is TaskCanceledException))
                {
                    Logger.LogWarning($"{ServiceName}: Task canceled.");
                    throw;
                }

                result.Exception = ex;

                if (ex is DatabaseErrorException)
                    Logger.LogError($"{ServiceName}: {result.Exception.Message}");
                else
                    Logger.LogCritical($"{ServiceName}: Unexpected Exception encountered: {result.Exception.Message}");
            }

            await Task.CompletedTask;
            return result;
        }

        /// <inheritdoc/>
        public async Task<EntityOrchestratorResult> ExecuteAsync(CancellationToken? cancellationToken = null)
        {
            try
            {
                ExecutionId = Guid.NewGuid();
                return await ExecuteInternalAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ExecutionId = Guid.Empty;
            }
        }

        /// <summary>
        /// Performs the actual execution.
        /// </summary>
        private async Task<EntityOrchestratorResult> ExecuteInternalAsync(CancellationToken? cancellationToken)
        { 
            // Get the requested batch data.
            EntityOrchestratorResult<TEntityEnvelopeColl, TEntityEnvelope> result;
            Logger.LogTrace("{ServiceName}: Query for next (new) Change Data Capture batch. [MaxQuerySize={MaxQuerySize}, ContinueWithDataLoss={ContinueWithDataLoss}, ExecutionId={ExecutionId}]", ServiceName, MaxQuerySize, ContinueWithDataLoss, ExecutionId);

            var sw = Stopwatch.StartNew();

            try
            {
                result = await GetBatchEntityDataAsync().ConfigureAwait(false);
                result.ExecutionId = ExecutionId;
                sw.Stop();
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || (ex is AggregateException aex && aex.InnerException is TaskCanceledException))
                {
                    Logger.LogWarning("{ServiceName}: Task canceled. [ExecutionId={ExecutionId}]", ServiceName, ExecutionId);
                    throw;
                }

                result = new EntityOrchestratorResult<TEntityEnvelopeColl, TEntityEnvelope> { Exception = ex, ExecutionId = ExecutionId };

                if (ex is DatabaseErrorException)
                    Logger.LogError("{ServiceName}: {DatabaseErrorMessage} [ExecutionId={ExecutionId}]", ServiceName, result.Exception.Message, ExecutionId);
                else
                    Logger.LogCritical(ex, "{ServiceName}: Unexpected Exception encountered: {ExceptionMessage} [ExecutionId={ExecutionId}]", ServiceName, result.Exception.Message, ExecutionId);

                return result;
            }

            if (result.Batch == null)
            {
                Logger.LogTrace("{ServiceName}: Batch 'none': No new Change Data Capture data was found. [ExecutionId={ExecutionId}]", ServiceName, ExecutionId);
                return result;
            }

            Logger.LogInformation("{ServiceName}: Batch '{BatchId}': {EntityCount} entity(s) were found. [MaxQuerySize={MaxQuerySize}, ContinueWithDataLoss={ContinueWithDataLoss}, CorrelationId={CorrelationId}, ExecutionId={ExecutionId}, Elapsed={Elapsed}ms]",
                ServiceName, result.Batch.Id, result.Result.Count, MaxQuerySize, ContinueWithDataLoss, result.Batch.CorrelationId, ExecutionId, sw.ElapsedMilliseconds);

            if ((cancellationToken ??= CancellationToken.None).IsCancellationRequested)
            {
                Logger.LogWarning("{ServiceName}: Batch '{BatchId}': Incomplete as a result of Cancellation. [CorrelationId={CorrelationId}, ExecutionId={ExecutionId}]", ServiceName, result.Batch.Id, result.Batch.CorrelationId, ExecutionId);
                return await Task.FromCanceled<EntityOrchestratorResult<TEntityEnvelopeColl, TEntityEnvelope>>(cancellationToken.Value).ConfigureAwait(false);
            }

            // Consolidate the results to the 'set' that is to be sent (i.e. ignore unneccessary).
            var coll = new TEntityEnvelopeColl();
            foreach (var grp in result.Result.GroupBy(x => new { x.PrimaryKey }))
            {
                // Find delete and use.
                var item = grp.Where(x => x.DatabaseOperationType == OperationType.Delete).FirstOrDefault();
                if (item != null && grp.Any(x => x.DatabaseOperationType == OperationType.Create))
                    continue;  // Created and deleted in quick succession; no need to publish.

                // Where there is no delete then just use the first.
                if (item == null)
                {
                    item = grp.First();

                    // Where the table key is initial (being the actual table primary key columns versus from CDC) then it has been _subsequently_ deleted (physically) so skip.
                    if (item is IGlobalIdentifier gi && gi.TableKey.IsInitial)
                        continue;
                }

                // Where supports logical delete and IsDeleted, then override DatabaseOperationType.
                if (item is ILogicallyDeleted ld && ld.IsDeleted.HasValue && ld.IsDeleted.Value)
                {
                    item.DatabaseOperationType = OperationType.Delete;
                    ld.ClearWhereDeleted();
                }

                coll.Add(item);
            }

            // Check cancellation.
            if ((cancellationToken ??= CancellationToken.None).IsCancellationRequested)
            {
                Logger.LogWarning("{ServiceName}: Batch '{BatchId}': Incomplete as a result of Cancellation. [CorrelationId={CorrelationId}, ExecutionId={ExecutionId}]", ServiceName, result.Batch.Id, result.Batch.CorrelationId, ExecutionId);
                return await Task.FromCanceled<EntityOrchestratorResult<TEntityEnvelopeColl, TEntityEnvelope>>(cancellationToken.Value).ConfigureAwait(false);
            }

            // Where additional envelope processing is required then execute.
            if (AdditionalEnvelopeProcessing != null)
                await AdditionalEnvelopeProcessing(coll).ConfigureAwait(false);

            // Determine whether anything may have been sent before and exclude (i.e. do not send again).
            var coll2 = new TEntityEnvelopeColl();
            var tracking = new List<VersionTracker>();
            foreach (var item in coll)
            {
                var entity = item as TEntity;

                // Calculate the serialized hash for the ETag.
                if (ExcludePropertiesFromETag == null || ExcludePropertiesFromETag.Length == 0)
                    entity.ETag = ETagGenerator.Generate(entity);
                else
                {
                    var json = JsonPropertyFilter.Apply(entity, null, ExcludePropertiesFromETag);
                    entity.ETag = ETagGenerator.Generate(json!.ToString(Newtonsoft.Json.Formatting.None));
                }

                // Where the ETag and TrackingHash match then skip (has already been published).
                if (item.DatabaseTrackingHash == null || item.DatabaseTrackingHash != entity.ETag)
                {
                    coll2.Add(item);
                    tracking.Add(new VersionTracker { Key = entity.PrimaryKey.ToString(), Hash = entity.ETag });
                }
            }

            if ((cancellationToken ??= CancellationToken.None).IsCancellationRequested)
            {
                Logger.LogWarning("{ServiceName}: Batch '{BatchId}': Incomplete as a result of Cancellation. [CorrelationId={CorrelationId}, ExecutionId={ExecutionId}]", ServiceName, result.Batch.Id, result.Batch.CorrelationId, ExecutionId);
                return await Task.FromCanceled<EntityOrchestratorResult<TEntityEnvelopeColl, TEntityEnvelope>>(cancellationToken.Value).ConfigureAwait(false);
            }

            // Publish & send the events.
            if (coll2.Count == 0)
                Logger.LogInformation("{ServiceName}: Batch '{BatchId}': No event(s) were published; no unique version tracking hash found. [CorrelationId={CorrelationId}, ExecutionId={ExecutionId}, Elapsed={Elapsed}ms]",
                    ServiceName, result.Batch.Id, result.Batch.CorrelationId, ExecutionId, sw.ElapsedMilliseconds);
            else
            {
                var events = (await CreateEventsAsync(coll2, result.Batch.CorrelationId, cancellationToken.Value).ConfigureAwait(false)).ToArray();
                sw = Stopwatch.StartNew();
                await EventPublisher.SendAsync(events).ConfigureAwait(false);
                sw.Stop();

                Logger.LogInformation("{ServiceName}: Batch '{BatchId}': {EventCount} event(s) were published/sent successfully. [CorrelationId={CorrelationId}, ExecutionId={ExecutionId}, Elapsed={Elapsed}ms]",
                    ServiceName, result.Batch.Id, events.Length, result.Batch.CorrelationId, ExecutionId, sw.ElapsedMilliseconds);
            }

            // Complete the batch (ignore any further 'cancel' as event(s) have been published and we *must* complete to minimise chance of sending more than once).
            sw = Stopwatch.StartNew();
            var cresult = await CompleteAsync(result.Batch.Id, tracking).ConfigureAwait(false);
            sw.Stop();

            if (cresult.IsSuccessful)
                Logger.LogInformation("{ServiceName}: Batch '{BatchId}': Marked as Completed. [CorrelationId={CorrelationId}, ExecutionId={ExecutionId}, Elapsed={Elapsed}ms]",
                    ServiceName, result.Batch.Id, result.Batch.CorrelationId, ExecutionId, sw.ElapsedMilliseconds);

            return cresult;
        }

        /// <summary>
        /// Executes a multi-dataset query command with one or more <paramref name="multiSetArgs"/>; whilst also outputing the resulting return value.
        /// </summary>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <returns>The resultant <see cref="EntityOrchestratorResult{TEntityEnvelopeColl, TEntityEnvelope}"/>.</returns>
        protected async Task<EntityOrchestratorResult<TEntityEnvelopeColl, TEntityEnvelope>> SelectQueryMultiSetAsync(params IMultiSetArgs[] multiSetArgs)
        {
            var result = new EntityOrchestratorResult<TEntityEnvelopeColl, TEntityEnvelope> { ExecutionId = ExecutionId };
            var msa = new List<IMultiSetArgs> { new MultiSetSingleArgs<BatchTracker>(_batchTrackerMapper, r => result.Batch = r, isMandatory: false, stopOnNull: true) };
            msa.AddRange(multiSetArgs);

            await Db.StoredProcedure(ExecuteStoredProcedureName, p =>
            {
                p.AddParameter(MaxQuerySizeParamName, MaxQuerySize);
                p.AddParameter(ContinueWithDataLossParamName, ContinueWithDataLoss);
            }).SelectMultiSetAsync(msa.ToArray()).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Gets the batch entity data from the database.
        /// </summary>
        /// <returns>A <typeparamref name="TEntityEnvelopeColl"/>.</returns>
        protected abstract Task<EntityOrchestratorResult<TEntityEnvelopeColl, TEntityEnvelope>> GetBatchEntityDataAsync();

        /// <summary>
        /// Provides the capability to create none or more <see cref="EventData">events</see> from the entity <paramref name="coll">collection.</paramref> using the <paramref name="getEntityFunc"/>
        /// to get the entity value to publish. A <c>null</c> function response indicates entity is not found and/or do not publish (i.e. skip).
        /// </summary>
        /// <param name="coll">The entity envelope collection.</param>
        /// <param name="getEntityFunc">The function to get the entity value to publish.</param>
        /// <param name="correlationId">The correlarion identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>None or more <see cref="EventData">events</see> to be published.</returns>
        protected async Task<IEnumerable<EventData>> CreateEventsWithGetAsync(TEntityEnvelopeColl coll, Func<TEntityEnvelope, Task<TEntity>> getEntityFunc, string? correlationId, CancellationToken cancellationToken)
        {
            var events = new List<EventData>();

            foreach (var item in coll ?? throw new ArgumentNullException(nameof(coll)))
            {
                if (cancellationToken.IsCancellationRequested)
                    await Task.FromCanceled(cancellationToken).ConfigureAwait(false);

                switch (item.DatabaseOperationType)
                {
                    case OperationType.Delete:
                        events.Add(CreateEvent(item, OperationType.Delete, correlationId));
                        break;

                    default:
                        var entity = await getEntityFunc(item).ConfigureAwait(false);
                        if (entity != null)
                            events.Add(CreateEvent(entity, item.DatabaseOperationType, correlationId));

                        break;
                }
            }

            return events;
        }

        /// <summary>
        /// Creates none or more <see cref="EventData">events</see> from the entity <paramref name="coll">collection.</paramref>.
        /// </summary>
        /// <param name="coll">The entity envelope collection.</param>
        /// <param name="correlationId">The correlarion identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>None or more <see cref="EventData">events</see> to be published.</returns>
        protected virtual Task<IEnumerable<EventData>> CreateEventsAsync(TEntityEnvelopeColl coll, string? correlationId, CancellationToken cancellationToken)
        {
            var events = new List<EventData>();

            foreach (var item in coll ?? throw new ArgumentNullException(nameof(coll)))
            {
                events.Add(CreateEvent(item, item.DatabaseOperationType, correlationId));
            }

            return Task.FromResult(events.AsEnumerable());
        }

        /// <summary>
        /// Creates an <see cref="EventData"/> for the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The <see cref="OperationType"/> to infer the <see cref="EventData.Action"/>.</param>
        /// <param name="correlationId">The correlarion identifier.</param>
        /// <returns>The <see cref="EventData"/>.</returns>
        protected virtual EventData CreateEvent<T>(T value, OperationType operationType, string? correlationId) where T : IPrimaryKey
        {
            var ed = new EventData
            {
                Subject = EventSubjectFormatter.Format(EventSubject, value, EventSubjectFormat),
                Action = EventActionFormatter.Format(operationType, EventActionFormat),
                PrimaryKey = value.PrimaryKey,
                CorrelationId = correlationId,
                Data = value,
                Source = EventSourceFormatter.Format(EventSource, value, EventSourceFormat)
            };

            ed.Type = $"{EventSubject}.{ed.Action}";
            return ed;
        }
    }
}