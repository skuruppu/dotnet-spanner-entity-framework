﻿// Copyright 2020 Google LLC
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

using Google.Api.Gax;
using Google.Cloud.Spanner.Common.V1;
using Google.Cloud.Spanner.Data;
using Google.Cloud.Spanner.V1.Internal.Logging;
using System;

namespace Google.Cloud.EntityFrameworkCore.Spanner.IntegrationTests
{
    /// <summary>
    /// A database created on-demand for testing.
    /// </summary>
    public class SpannerTestDatabase
    {
        private static readonly object s_lock = new object();
        private static SpannerTestDatabase s_instance = null;

        /// <summary>
        /// Fetches the database, creating it if necessary. Uses the following environment
        /// variables to determine the name of the instance and database to create/use:
        /// TEST_SPANNER_INSTANCE: The instance id to use. Defaults to 'spannerintegration'.
        /// TEST_SPANNER_DATABASE: The database id to use. If not set, a unique id will be
        ///                        generated and the database will be created.
        /// </summary>
        /// <param name="projectId">The project ID to use, typically from a fixture.</param>
        public static SpannerTestDatabase GetInstance(string projectId)
        {
            lock (s_lock)
            {
                if (s_instance == null)
                {
                    s_instance = new SpannerTestDatabase(projectId);
                }
                else if (s_instance.ProjectId != projectId)
                {
                    throw new ArgumentException($"A database for project ID {s_instance.ProjectId} has already been created; this test requested {projectId}");
                }
                return s_instance;
            }
        }

        private static readonly string s_generatedDatabaseName = IdGenerator.FromDateTime(prefix: "testdb_", pattern: "yyyyMMdd't'HHmmss");

        public string SpannerHost { get; } = GetEnvironmentVariableOrDefault("TEST_SPANNER_HOST", null);
        public string SpannerPort { get; } = GetEnvironmentVariableOrDefault("TEST_SPANNER_PORT", null);
        public string SpannerInstance { get; } = GetEnvironmentVariableOrDefault("TEST_SPANNER_INSTANCE", "spannerintegration");
        public string SpannerDatabase { get; } = GetEnvironmentVariableOrDefault("TEST_SPANNER_DATABASE", s_generatedDatabaseName);

        // This is the simplest way of checking whether the environment variable was specified or not.
        // It's a little ugly, but simpler than the alternatives.

        /// <summary>
        /// Returns true if the database was created just for this test, or false if the database was an existing one
        /// specified through an environment variable.
        /// </summary>
        public bool Fresh => SpannerDatabase == s_generatedDatabaseName;

        // Connection string including database, generated from the above properties
        public string ConnectionString { get; }
        // Connection string without the database, generated from the above properties
        public string NoDbConnectionString { get; }
        public string ProjectId { get; }
        public DatabaseName DatabaseName { get; }

        private SpannerTestDatabase(string projectId)
        {
            ProjectId = projectId;
            var builder = new SpannerConnectionStringBuilder
            {
                Host = SpannerHost,
                DataSource = $"projects/{ProjectId}/instances/{SpannerInstance}",
                EmulatorDetection = EmulatorDetection.EmulatorOrProduction
            };
            if (SpannerPort != null)
            {
                builder.Port = int.Parse(SpannerPort);
            }
            NoDbConnectionString = builder.ConnectionString;
            var databaseBuilder = builder.WithDatabase(SpannerDatabase);
            ConnectionString = databaseBuilder.ConnectionString;
            DatabaseName = databaseBuilder.DatabaseName;

            if (Fresh)
            {
                using (var connection = new SpannerConnection(NoDbConnectionString))
                {
                    var createCmd = connection.CreateDdlCommand($"CREATE DATABASE {SpannerDatabase}");
                    createCmd.ExecuteNonQuery();
                    Logger.DefaultLogger.Debug($"Created database {SpannerDatabase}");
                }
            }
            else
            {
                Logger.DefaultLogger.Debug($"Using existing database {SpannerDatabase}");
            }
        }

        private static string GetEnvironmentVariableOrDefault(string name, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
        
        public SpannerConnection GetConnection() => new SpannerConnection(ConnectionString);

        // Creates a SpannerConnection with a specific logger.
        public SpannerConnection GetConnection(Logger logger) =>
            new SpannerConnection(new SpannerConnectionStringBuilder(ConnectionString) { SessionPoolManager = SessionPoolManager.Create(new Cloud.Spanner.V1.SessionPoolOptions(), logger) });
    }
}
