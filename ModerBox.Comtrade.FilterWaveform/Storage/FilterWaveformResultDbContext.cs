using Microsoft.EntityFrameworkCore;

namespace ModerBox.Comtrade.FilterWaveform.Storage {
    public class FilterWaveformResultDbContext : DbContext {
        public DbSet<FilterWaveformResultEntity> Results => Set<FilterWaveformResultEntity>();
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

        public static FilterWaveformResultDbContext Create(string dbPath) {
            var options = new DbContextOptionsBuilder<FilterWaveformResultDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            return new FilterWaveformResultDbContext(options);
        }
    }
}
