using Microsoft.EntityFrameworkCore;

namespace DiaryScraperCore
{
    public class ScrapeContext : DbContext
    {
        public ScrapeContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DownloadResource>()
                    .HasAlternateKey(dr => dr.Url);
        }

        public DbSet<DiaryImage> Images { get; set; }
        public DbSet<DiaryPost> Posts { get; set; }
    }
}