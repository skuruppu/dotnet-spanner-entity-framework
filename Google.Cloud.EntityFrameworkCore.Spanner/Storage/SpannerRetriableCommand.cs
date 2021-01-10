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

using Google.Api.Gax;
using Google.Cloud.EntityFrameworkCore.Spanner.Storage.Internal;
using Google.Cloud.Spanner.Data;
using System.Data;
using System.Data.Common;

namespace Google.Cloud.EntityFrameworkCore.Spanner.Storage
{
    /// <summary>
    /// <see cref="DbCommand"/> implementation for Cloud Spanner that can be retried if the underlying
    /// Spanner transaction is aborted.
    /// </summary>
    public class SpannerRetriableCommand : DbCommand
    {
        private SpannerRetriableConnection _connection;
        private readonly SpannerCommand _spannerCommand;
        private SpannerTransactionBase _transaction;

        internal SpannerRetriableCommand(SpannerRetriableConnection connection, SpannerCommand spannerCommand)
        {
            _connection = connection;
            _spannerCommand = (SpannerCommand)GaxPreconditions.CheckNotNull(spannerCommand, nameof(spannerCommand)).Clone();
        }

        public override string CommandText { get => _spannerCommand.CommandText; set => _spannerCommand.CommandText = value; }
        public override int CommandTimeout { get => _spannerCommand.CommandTimeout; set => _spannerCommand.CommandTimeout = value; }
        public override CommandType CommandType { get => _spannerCommand.CommandType; set => _spannerCommand.CommandType = value; }
        public override bool DesignTimeVisible { get => _spannerCommand.DesignTimeVisible; set => _spannerCommand.DesignTimeVisible = value; }
        public override UpdateRowSource UpdatedRowSource { get => _spannerCommand.UpdatedRowSource; set => _spannerCommand.UpdatedRowSource = value; }
        protected override DbConnection DbConnection
        {
            get => _connection;
            set => _connection = (SpannerRetriableConnection)value;
        }

        protected override DbTransaction DbTransaction
        {
            get => _transaction;
            set => _transaction = (SpannerTransactionBase)value;
        }

        protected override DbParameterCollection DbParameterCollection => _spannerCommand.Parameters;

        protected override DbParameter CreateDbParameter() => new SpannerParameter();

        public override void Cancel() => _spannerCommand.Cancel();

        public override int ExecuteNonQuery() =>
            _transaction == null
            ? _spannerCommand.ExecuteNonQuery()
            : _transaction.ExecuteNonQueryWithRetry(_spannerCommand);

        public override object ExecuteScalar() =>
            _transaction == null
            ? _spannerCommand.ExecuteScalar()
            : _transaction.ExecuteScalarWithRetry(_spannerCommand);

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => _transaction == null
            ? _spannerCommand.ExecuteReader()
            : _transaction.ExecuteDbDataReaderWithRetry(_spannerCommand);

        public override void Prepare() => _spannerCommand.Prepare();
    }
}
