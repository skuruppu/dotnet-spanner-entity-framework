﻿// Copyright 2021 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Cloud.Spanner.Common.V1;
using Google.Cloud.Spanner.V1.Internal.Logging;
using Microsoft.EntityFrameworkCore;

namespace Google.Cloud.EntityFrameworkCore.Spanner.IntegrationTests
{
    /// <summary>
    /// DbContext for Migration tables.
    /// </summary>
    internal class TestMigrationDbContext : MigrationDbContext
    {
        private readonly DatabaseName _databaseName;

        internal TestMigrationDbContext(DatabaseName databaseName) => _databaseName = databaseName;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseSpanner($"Data Source={_databaseName}");
            }
        }
    }

    public class MigrationTestFixture : SpannerFixtureBase
    {
        public MigrationTestFixture()
        {
            Logger.DefaultLogger.Debug($"Applying pending migration for database {Database.DatabaseName} using migration.");
            ApplyMigration();
            if (!Database.Fresh)
            {
                Logger.DefaultLogger.Debug($"Deleting data in {Database.DatabaseName}");
                ClearTables();
            }
        }

        private void ClearTables()
        {
            using (var con = GetConnection())
            {
                using (var tx = con.BeginTransaction())
                {
                    var cmd = con.CreateBatchDmlCommand();
                    cmd.Transaction = tx;
                    foreach (var table in new string[]
                    {
                        "OrderDetails",
                        "Orders",
                        "Products",
                        "Categories",
                        "AllColTypes"
                    })
                    {
                        cmd.Add($"DELETE FROM {table} WHERE TRUE");
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Applies all Pending migrations.
        /// </summary>
        private void ApplyMigration()
        {
            using var context = new TestMigrationDbContext(Database.DatabaseName);
            context.Database.Migrate();
        }
    }
}