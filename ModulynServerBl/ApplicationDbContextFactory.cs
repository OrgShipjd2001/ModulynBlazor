using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modulyn.Server.Bl
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        internal static string defaultconnectionstring = "Data Source=DBServer;Initial Catalog=ModulynAuth;Integrated Security=True;Pooling=False;MultipleActiveResultSets=True;Encrypt=False";

        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            string connectionString = defaultconnectionstring;
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
