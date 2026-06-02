using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
