using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Domain.Entities;
using TimeTracker.Domain.Enums;

namespace TimeTracker.Infrastructure.Persistence.Configurations;

public class FieldDataTypeLookupConfiguration : IEntityTypeConfiguration<FieldDataTypeLookup>
{
    public void Configure(EntityTypeBuilder<FieldDataTypeLookup> builder)
    {
        builder.ToTable("t_type_field_data_type");

        // Ids are fixed to the FieldDataType enum values, so don't auto-generate.
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasData(
            new FieldDataTypeLookup { Id = FieldDataType.Text, Code = "text", Name = "Text" },
            new FieldDataTypeLookup { Id = FieldDataType.Number, Code = "number", Name = "Number" },
            new FieldDataTypeLookup { Id = FieldDataType.Date, Code = "date", Name = "Date" },
            new FieldDataTypeLookup { Id = FieldDataType.Boolean, Code = "boolean", Name = "Boolean" },
            new FieldDataTypeLookup { Id = FieldDataType.Select, Code = "select", Name = "Select" });
    }
}
