using DotNet9.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNet9.Infrastructure.Persistence.Config;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
         .ValueGeneratedNever();

        b.Property(x => x.Email)
         .HasMaxLength(256)
         .IsRequired();

        b.HasIndex(x => x.Email).IsUnique();

        b.Property(x => x.Username)
         .HasMaxLength(64)
         .IsRequired();
    }
}
