using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configs;

public sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(u => u.LastName)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(u => u.PasswordHash)
               .IsRequired();

        // Value Object Email: se almacena como string en la columna "Email"
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                 .HasColumnName("Email")
                 .HasMaxLength(256)
                 .IsRequired();

            email.HasIndex(e => e.Value).IsUnique();
        });

        // Value Object Address (opcional)
        builder.OwnsOne(u => u.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100);
            address.Property(a => a.ZipCode).HasColumnName("ZipCode").HasMaxLength(20);
        });

        builder.Property(u => u.Role)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();
    }
}
