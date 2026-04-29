using Microsoft.EntityFrameworkCore.Design;

namespace SqExpress.EfCodeGenIntTest;

public sealed class EfCodeGenDbContextFactory : IDesignTimeDbContextFactory<EfCodeGenDbContext>
{
    public EfCodeGenDbContext CreateDbContext(string[] args)
        => new EfCodeGenDbContext();
}
