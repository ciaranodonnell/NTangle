﻿using NTangle.Test;
using NUnit.Framework;
using SqlServerDemo.Publisher.Data;
using System.Threading.Tasks;

namespace SqlServerDemo.Test
{
    [TestFixture]
    [NonParallelizable]
    public class AAACustomerTest
    {
        [Test]
        public async Task InitialNoChanges()
        {
            // NOTE: This is intended for execution directly after the database has been created and no CDC capture has occured on the underlying table.

            using var db = SqlServerUnitTest.GetDatabase();
            var logger = UnitTest.GetLogger<CustomerCdcOrchestrator>();

            // Execute should pick up the update and delete.
            var tep = new TestEventPublisher();
            var cdc = new CustomerCdcOrchestrator(db, tep, logger);
            var cdcr = await cdc.ExecuteAsync().ConfigureAwait(false);
            UnitTest.WriteResult(cdcr, tep);

            // Assert/verify the results.
            Assert.NotNull(cdcr);
            Assert.IsTrue(cdcr.IsSuccessful);
            Assert.IsNull(cdcr.Batch);
            Assert.IsNull(cdcr.Exception);
            Assert.AreEqual(0, cdcr.ExecuteStatus.InitialCount);
            Assert.IsNull(cdcr.ExecuteStatus.ConsolidatedCount);
            Assert.IsNull(cdcr.ExecuteStatus.PublishCount);
            Assert.AreEqual(0, tep.Events.Count);
        }
    }
}