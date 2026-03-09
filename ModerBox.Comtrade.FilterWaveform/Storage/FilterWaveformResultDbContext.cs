using Microsoft.EntityFrameworkCore;

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
    }
}
