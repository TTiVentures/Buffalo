using Microsoft.EntityFrameworkCore;

namespace Buffalo
{
   public class FileContext : DbContext
    {
        public FileContext(DbContextOptions options) : base(options) { }
        public DbSet<Models.File> Files { get; set; }
    }
}
