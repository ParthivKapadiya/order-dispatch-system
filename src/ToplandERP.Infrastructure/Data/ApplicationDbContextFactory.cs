using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ToplandERP.Infrastructure.Identity;

namespace ToplandERP.Infrastructure.Data;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=ToplandERP;TrustServerCertificate=True;Encrypt=True")
            .Options;

        return new ApplicationDbContext(options, SystemCurrentUser.Instance);
    }
}
