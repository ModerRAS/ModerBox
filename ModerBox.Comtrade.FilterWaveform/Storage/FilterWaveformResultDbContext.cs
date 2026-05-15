using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace ModerBox.Comtrade.FilterWaveform.Storage {
    /// <summary>
    /// 滤波器波形结果数据库上下文，用于访问SQLite中的分合闸操作记录。
    /// </summary>
    public class FilterWaveformResultDbContext : DbContext {
        /// <summary>
        /// 分合闸操作结果表。
        /// </summary>
        public DbSet<FilterWaveformResultEntity> Results => Set<FilterWaveformResultEntity>();
        
        /// <summary>
        /// 已处理的COMTRADE文件记录表。
        /// </summary>
        public DbSet<ProcessedComtradeFileEntity> ProcessedFiles => Set<ProcessedComtradeFileEntity>();

        public FilterWaveformResultDbContext(DbContextOptions<FilterWaveformResultDbContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<FilterWaveformResultEntity>();
            entity.ToTable("filter_waveform_results");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Time);
            entity.HasIndex(x => x.Name);

            var processed = modelBuilder.Entity<ProcessedComtradeFileEntity>();
            processed.ToTable("filter_waveform_processed_files");
            processed.HasKey(x => x.Id);
            processed.HasIndex(x => x.CfgPath).IsUnique();
            processed.HasIndex(x => x.LastUpdatedUtc);
        }

        /// <summary>
        /// 根据数据库文件路径创建数据库上下文。
        /// </summary>
        /// <param name="dbPath">SQLite数据库文件路径。</param>
        /// <returns>数据库上下文实例。</returns>
        public static FilterWaveformResultDbContext Create(string dbPath) {
            var options = new DbContextOptionsBuilder<FilterWaveformResultDbContext>()
                .UseSqlite($"Data Source={dbPath};Pooling=false")
                .Options;
            return new FilterWaveformResultDbContext(options);
        }

        /// <summary>
        /// Adds columns introduced after the first SQLite schema so old result databases stay readable.
        /// </summary>
        public void EnsureCompatibleSchema() {
            var connection = Database.GetDbConnection();
            var shouldClose = connection.State == ConnectionState.Closed;

            if (shouldClose) {
                connection.Open();
            }

            try {
                if (!TableExists(connection, "filter_waveform_results")) {
                    return;
                }

                var columns = GetColumns(connection, "filter_waveform_results");

                AddColumnIfMissing(connection, columns, "PhaseAHasArcReignition", "INTEGER NOT NULL DEFAULT 0");
                AddColumnIfMissing(connection, columns, "PhaseBHasArcReignition", "INTEGER NOT NULL DEFAULT 0");
                AddColumnIfMissing(connection, columns, "PhaseCHasArcReignition", "INTEGER NOT NULL DEFAULT 0");
            } finally {
                if (shouldClose) {
                    connection.Close();
                }
            }
        }

        private static bool TableExists(DbConnection connection, string tableName) {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            return command.ExecuteScalar() is not null;
        }

        private static HashSet<string> GetColumns(DbConnection connection, string tableName) {
            using var command = connection.CreateCommand();
            command.CommandText = $"""PRAGMA table_info("{tableName}")""";

            using var reader = command.ExecuteReader();
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read()) {
                columns.Add(reader.GetString(1));
            }

            return columns;
        }

        private static void AddColumnIfMissing(
            DbConnection connection,
            HashSet<string> columns,
            string columnName,
            string columnDefinition) {
            if (columns.Contains(columnName)) {
                return;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"""ALTER TABLE "filter_waveform_results" ADD COLUMN "{columnName}" {columnDefinition}""";
            command.ExecuteNonQuery();
            columns.Add(columnName);
        }
    }
}
