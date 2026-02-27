using Microsoft.EntityFrameworkCore;
using GamP_SCPeriop.Shared;

namespace GamP_SCPeriop.Server.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
