using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modulyn.Server.Bl
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        internal static string defaultconnectionstring = "Server=HMIIntegration;Database=applicationdb;User Id=IntSvcDbUser;Password=int$svc$db$user$1;MultipleActiveResultSets=true;Encrypt=False";

        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            string connectionString = defaultconnectionstring;
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
