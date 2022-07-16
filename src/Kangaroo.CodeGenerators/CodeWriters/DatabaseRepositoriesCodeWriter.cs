// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.CodeGenerators.CodeWriters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Kangaroo.CodeGenerators.Extensions;
    using Kangaroo.CodeGenerators.Structure;
    using Kangaroo.CodeGenerators.Writers;
    using Microsoft.CodeAnalysis;

    internal static class DatabaseRepositoriesCodeWriter
    {
        public static void Generate(CodeGeneratorSettings codeGeneratorSettings, List<CodeGenerator> codeGenerators, SourceProductionContext sourceProductionContext)
        {
            foreach (var codeGenerator in codeGenerators)
            {
                if (codeGeneratorSettings.DatabaseRepositoriesSettings?.GenerateDatabaseEntity == true
                    || codeGeneratorSettings.DatabaseRepositoriesSettings?.GenerateDatabaseEntityTypeConfiguration == true)
                {
                    foreach (var entity in codeGenerator.Entity.Where(x => x.GenerateDatabaseEntity != null))
                    {
                        WriteDatabaseEntityTypeConfiguration(codeGeneratorSettings, sourceProductionContext, entity);
                        WriteDatabaseEntity(codeGeneratorSettings, sourceProductionContext, entity);
                    }
                }
            }

            if (codeGeneratorSettings.DatabaseRepositoriesSettings?.GenerateDbContext == true)
            {
                WriteDbContext(codeGeneratorSettings, sourceProductionContext, codeGenerators.SelectMany(x => x.Entity));
                WriteDatabaseRepository(codeGeneratorSettings, sourceProductionContext);
            }

            WriteAutoMapperProfiler(codeGeneratorSettings, sourceProductionContext, codeGenerators);
        }

        private static void WriteDatabaseEntity(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Entity entity)
        {
            if (codeGeneratorSettings.DatabaseRepositoriesSettings?.GenerateDatabaseEntity != true)
            {
                return;
            }

            var keyType = entity.EntityFields?.KeyField?.KeyType;
            var entityName = GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, entity.Name);

            var inheritance = "IDatabaseEntity";

            if (entity.IncludeDataState)
            {
                inheritance += ", IHasDataState";
            }

            if (keyType.HasValue)
            {
                switch (keyType)
                {
                    case KeyType.Int:
                        inheritance += ", IHasIntegerKey";
                        break;
                    case KeyType.Guid:
                        inheritance += ", IHasGuidKey";
                        break;
                    default:
                        break;
                }
            }

            if (entity.IncludeRowVersionControl)
            {
                inheritance += ", IHasRowVersionControl";
            }

            if (entity.IncludeAuditLog)
            {
                inheritance += ", IHasAuditLog";
            }

            var fileWriter = new CSFileWriter(
                    CSFileWriterType.Class,
                    codeGeneratorSettings.DatabaseRepositoriesSettings?.DatabaseEntitiesNamespace,
                    entityName,
                    isPartial: true,
                    inheritance: inheritance);

            fileWriter.WriteUsing("System");
            fileWriter.WriteUsing("System.ComponentModel.DataAnnotations");
            fileWriter.WriteUsing("System.ComponentModel.DataAnnotations.Schema");
            fileWriter.WriteUsing("Kangaroo.Models");
            fileWriter.WriteUsing("Kangaroo.Models.DatabaseEntities");

            if (entity.GenerateDatabaseEntity?.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in entity.GenerateDatabaseEntity.AdditionalUsings.Using)
                {
                    fileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (entity.GenerateDatabaseEntity?.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in entity.GenerateDatabaseEntity?.CustomAttributes.CustomAttribute)
                {
                    fileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            entity.EntityFields?.HandleFields(WriteField(codeGeneratorSettings, fileWriter, entity));
            entity.GenerateDatabaseEntity.AdditionalFields?.HandleFields(WriteField(codeGeneratorSettings, fileWriter, entity));

            if (entity.IncludeRowVersionControl)
            {
                fileWriter.WriteProperty("byte[]", "RowVersion", isFullProperty: false, isVirtual: false, attributes: new List<string>() { "Timestamp" });
            }

            if (entity.IncludeAuditLog)
            {
                fileWriter.WriteProperty("string", "CreatedByUserName", isFullProperty: false, isVirtual: false, attributes: new List<string>() { "Required", "MaxLength(510)" });
                fileWriter.WriteProperty("DateTimeOffset", "CreatedAt", isFullProperty: false, isVirtual: false, attributes: new List<string>() { "Required", "MaxLength(510)" });

                fileWriter.WriteProperty("string", "UpdatedByUserName", isFullProperty: false, isVirtual: false, attributes: new List<string>() { "MaxLength(510)" });
                fileWriter.WriteProperty("DateTimeOffset?", "UpdatedAt", isFullProperty: false, isVirtual: false);
            }

            if (entity.IncludeDataState)
            {
                fileWriter.WriteProperty("DataState", "DataState", isFullProperty: false, isVirtual: false, attributes: new List<string>() { "NotMapped" });
            }

            if (entity.EntityFields?.KeyField != null)
            {
                List<string> getKeyMethodBodyLines = new List<string>();
                List<string> setKeyMethodBodyLines = new List<string>();

                getKeyMethodBodyLines.Add($"return this.{entity.EntityFields?.KeyField.Name};");
                setKeyMethodBodyLines.Add($"this.{entity.EntityFields?.KeyField.Name} = key;");

                switch (entity.EntityFields?.KeyField.KeyType)
                {
                    case KeyType.Int:
                        fileWriter.WriteMethod("GetKey", "int", bodyLines: getKeyMethodBodyLines);
                        fileWriter.WriteMethod("SetKey", parameters: "int key", bodyLines: setKeyMethodBodyLines);
                        break;
                    case KeyType.Guid:
                        fileWriter.WriteMethod("GetKey", "Guid", bodyLines: getKeyMethodBodyLines);
                        fileWriter.WriteMethod("SetKey", parameters: "Guid key", bodyLines: setKeyMethodBodyLines);
                        break;
                    default:
                        break;
                }
            }

            sourceProductionContext.WriteNewCSFile(entityName, fileWriter);

            static Action<object> WriteField(CodeGeneratorSettings codeGeneratorSettings, CSFileWriter fileWriter, Entity entity)
            {
                return x =>
                {
                    if (x is IField field)
                    {
                        if (field is not IDatabaseEntityField databaseEntityField || entity.GenerateDatabaseEntity?.IgnoreFields?.IgnoreField.Any(y => y.Name == field.Name) == true)
                        {
                            return;
                        }

                        var fieldType = field.GetFieldType();
                        var isList = field is EntityCollectionField;

                        if (isList)
                        {
                            fieldType = $"IList<{fieldType}>";
                        }

                        var isRequired = false;

                        if (field is ICanBeRequired requiredField)
                        {
                            isRequired = requiredField.IsRequired;
                        }

                        var maxLength = 0;

                        if (field is StringField stringField)
                        {
                            maxLength = stringField.MaxLength;
                        }

                        var foreignKeyFieldName = string.Empty;
                        var isVirtual = false;

                        if (field is EntityField entityField)
                        {
                            foreignKeyFieldName = entity.GenerateDatabaseEntity?.ForeignKeyFields?.ForeignKeyField.FirstOrDefault(y => y.EntityFieldName == field.Name)?.IsForeignKeyFor ?? string.Empty;
                            isVirtual = true;
                            fieldType = GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, fieldType);
                        }

                        var attributes = new List<string>();

                        if (field.CustomAttributes?.CustomAttribute != null)
                        {
                            foreach (var attribute in field.CustomAttributes?.CustomAttribute)
                            {
                                attributes.Add(attribute.Attribute);
                            }
                        }

                        if (field is KeyField)
                        {
                            attributes.Add("Key");
                        }

                        if (isRequired)
                        {
                            attributes.Add("Required");
                        }

                        if (maxLength > 0)
                        {
                            attributes.Add($"MaxLength({maxLength})");
                        }

                        if (!string.IsNullOrEmpty(foreignKeyFieldName))
                        {
                            attributes.Add($"ForeignKey(nameof({foreignKeyFieldName}))");
                        }

                        fileWriter.WriteProperty(fieldType, field.Name, isFullProperty: false, isVirtual: isVirtual, attributes: attributes);
                    }
                };
            }
        }

        private static void WriteDatabaseEntityTypeConfiguration(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Entity entity)
        {
            if (codeGeneratorSettings.DatabaseRepositoriesSettings?.GenerateDatabaseEntityTypeConfiguration != true)
            {
                return;
            }

            var configurationClassName = $"{GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, entity.Name)}Configuration";
            var configurationfileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.DatabaseRepositoriesSettings?.DatabaseEntityTypeConfigurationNamespace,
                configurationClassName,
                isPartial: true,
                inheritance: $"IEntityTypeConfiguration<{GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, entity.Name)}>");

            configurationfileWriter.WriteUsing("System");
            configurationfileWriter.WriteUsing("System.Collections.Generic");
            configurationfileWriter.WriteUsing("System.Text");
            configurationfileWriter.WriteUsing("Microsoft.EntityFrameworkCore");
            configurationfileWriter.WriteUsing("Microsoft.EntityFrameworkCore.Metadata.Builders");
            configurationfileWriter.WriteUsing(codeGeneratorSettings.DatabaseRepositoriesSettings?.DatabaseEntitiesNamespace);

            if (entity.GenerateDatabaseEntity?.GenerateDatabaseEntityConfiguration?.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in entity.GenerateDatabaseEntity.GenerateDatabaseEntityConfiguration.AdditionalUsings.Using)
                {
                    configurationfileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (entity.GenerateDatabaseEntity?.GenerateDatabaseEntityConfiguration?.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var customAttribute in entity.GenerateDatabaseEntity?.GenerateDatabaseEntityConfiguration?.CustomAttributes?.CustomAttribute)
                {
                    configurationfileWriter.WriteClassAttribute(customAttribute.Attribute);
                }
            }

            List<string> configureMethodBodyLines = new List<string>();

            if (entity.GenerateDatabaseEntity?.GenerateDatabaseEntityConfiguration?.Indexes?.IndexField != null)
            {
                foreach (var indexedField in entity.GenerateDatabaseEntity.GenerateDatabaseEntityConfiguration.Indexes.IndexField)
                {
                    configureMethodBodyLines.Add($"builder.HasIndex(x => x.{indexedField.FieldName});");
                }
            }

            if (entity.EntityFields?.DecimalField != null)
            {
                foreach (var precisionField in entity.EntityFields.DecimalField)
                {
                    if (!entity.GenerateDatabaseEntity?.IgnoreFields?.IgnoreField.Any(x => x.Name == precisionField.Name) == true)
                    {
                        configureMethodBodyLines.Add($"builder.Property(x => x.{precisionField.Name}).HasPrecision({precisionField.Precision}, {precisionField.Scale});");
                    }
                }
            }

            configureMethodBodyLines.Add($"this.OnConfiguring(builder);");

            configurationfileWriter.WriteMethod("Configure", parameters: $"EntityTypeBuilder<{GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, entity.Name)}> builder", bodyLines: configureMethodBodyLines);

            configurationfileWriter.WriteMethod("OnConfiguring", parameters: $"EntityTypeBuilder<{GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, entity.Name)}> builder", isPartial: true);

            sourceProductionContext.WriteNewCSFile(configurationClassName, configurationfileWriter);
        }

        private static void WriteDbContext(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, IEnumerable<Entity> entities)
        {
            var dbContextClassName = "ApplicationDbContext";
            var dbContextFileWriter = new CSFileWriter(
                        CSFileWriterType.Class,
                        codeGeneratorSettings.DatabaseRepositoriesSettings?.DbContextNamespace,
                        dbContextClassName,
                        isPartial: true,
                        inheritance: codeGeneratorSettings.DatabaseRepositoriesSettings?.UseIdentityDbContext == true ? $"IdentityDbContext<{codeGeneratorSettings.DatabaseRepositoriesSettings?.IdentityDbContextCustomUserClass}>" : string.Empty);

            if (codeGeneratorSettings.DatabaseRepositoriesSettings?.UseIdentityDbContext == true)
            {
                dbContextFileWriter.WriteUsing("Microsoft.AspNetCore.Identity.EntityFrameworkCore");
            }

            dbContextFileWriter.WriteUsing("Microsoft.EntityFrameworkCore");
            dbContextFileWriter.WriteUsing("Microsoft.Extensions.Logging");
            dbContextFileWriter.WriteUsing(codeGeneratorSettings.DatabaseRepositoriesSettings?.DatabaseEntitiesNamespace);
            dbContextFileWriter.WriteUsing(codeGeneratorSettings.DatabaseRepositoriesSettings?.DatabaseEntityTypeConfigurationNamespace);

            dbContextFileWriter.WriteDependencyInjection($"DbContextOptions<{dbContextClassName}>", "options", shouldSendToConstructorBase: true);

            List<string> modelCreatingMethodBodyLines = new List<string>()
            {
                "base.OnModelCreating(modelBuilder);",
            };

            foreach (var entity in entities.Where(x => x.GenerateDatabaseEntity?.GenerateDatabaseEntityConfiguration != null))
            {
                dbContextFileWriter.WriteProperty($"DbSet<{GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, entity.Name)}>", entity.PluralName, isFullProperty: false);

                if (codeGeneratorSettings.DatabaseRepositoriesSettings?.GenerateDatabaseEntityTypeConfiguration == true)
                {
                    var configurationClassName = $"{GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, entity.Name)}Configuration";
                    modelCreatingMethodBodyLines.Add($"modelBuilder.ApplyConfiguration(new {configurationClassName}());");
                }
            }

            modelCreatingMethodBodyLines.Add("this.OnCustomModelCreating(modelBuilder);");

            dbContextFileWriter.WriteMethod("OnModelCreating", parameters: "ModelBuilder modelBuilder", bodyLines: modelCreatingMethodBodyLines, accessModifierType: CSFileWriterAccessModifierType.Protected, isOverride: true);
            dbContextFileWriter.WriteMethod("OnCustomModelCreating", parameters: "ModelBuilder modelBuilder", isPartial: true);

            sourceProductionContext.WriteNewCSFile(dbContextClassName, dbContextFileWriter);
        }

        private static void WriteDatabaseRepository(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext)
        {
            var interfaceName = "IApplicationDatabaseRepository";
            var interfaceRepositoryFileWriter = new CSFileWriter(
                        CSFileWriterType.Interface,
                        codeGeneratorSettings.DatabaseRepositoriesSettings?.DatabaseRepositoriesNamespace,
                        interfaceName,
                        isPartial: true,
                        inheritance: "IDatabaseRepository<ApplicationDbContext>");
            interfaceRepositoryFileWriter.WriteUsing("AutoMapper");
            interfaceRepositoryFileWriter.WriteUsing("Kangaroo.Infrastructure.DatabaseRepositories");
            interfaceRepositoryFileWriter.WriteUsing(codeGeneratorSettings.DatabaseRepositoriesSettings?.DbContextNamespace);
            sourceProductionContext.WriteNewCSFile(interfaceName, interfaceRepositoryFileWriter);

            var databaseRepositoryClassName = "ApplicationDatabaseRepository";
            var databaseRepositoryFileWriter = new CSFileWriter(
                        CSFileWriterType.Class,
                        codeGeneratorSettings.DatabaseRepositoriesSettings?.DatabaseRepositoriesNamespace,
                        databaseRepositoryClassName,
                        isPartial: true,
                        inheritance: "DatabaseRepository<ApplicationDbContext>, IApplicationDatabaseRepository");

            databaseRepositoryFileWriter.WriteUsing("AutoMapper");
            databaseRepositoryFileWriter.WriteUsing("Kangaroo.Infrastructure.DatabaseRepositories");
            databaseRepositoryFileWriter.WriteUsing(codeGeneratorSettings.DatabaseRepositoriesSettings?.DbContextNamespace);

            databaseRepositoryFileWriter.WriteDependencyInjection($"ApplicationDbContext", "applicationDbContext", shouldSendToConstructorBase: true);
            databaseRepositoryFileWriter.WriteDependencyInjection($"IMapper", "mapper", shouldSendToConstructorBase: true);

            sourceProductionContext.WriteNewCSFile(databaseRepositoryClassName, databaseRepositoryFileWriter);
        }

        private static void WriteAutoMapperProfiler(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, IEnumerable<CodeGenerator> codeGenerators)
        {
            var applicationAutoMapperProfileClassName = "ApplicationAutoMapperProfile";
            var applicationAutoMapperProfileFileWriter = new CSFileWriter(
                        CSFileWriterType.Class,
                        codeGeneratorSettings.DatabaseRepositoriesSettings?.DatabaseEntityMapperNamespace,
                        applicationAutoMapperProfileClassName,
                        isPartial: true,
                        inheritance: "Profile");

            applicationAutoMapperProfileFileWriter.WriteUsing("AutoMapper");
            applicationAutoMapperProfileFileWriter.WriteUsing(codeGeneratorSettings.DatabaseRepositoriesSettings?.DatabaseEntitiesNamespace);
            applicationAutoMapperProfileFileWriter.WriteUsing(codeGeneratorSettings.DatabaseRepositoriesSettings?.EntitiesNamespace);

            foreach (var entity in codeGenerators.SelectMany(x => x.Entity))
            {
                WriteAutoMapper(codeGeneratorSettings, applicationAutoMapperProfileFileWriter, entity.Name, entity.GenerateAutoMapper, GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, entity.Name));
            }

            foreach (var summary in codeGenerators.SelectMany(x => x.Summary))
            {
                WriteAutoMapper(codeGeneratorSettings, applicationAutoMapperProfileFileWriter, summary.Name, summary.GenerateAutoMapper, summary.DatabaseEntityName);
            }

            applicationAutoMapperProfileFileWriter.WriteMethod("CustomMapperConfiguration", isPartial: true);

            sourceProductionContext.WriteNewCSFile(applicationAutoMapperProfileClassName, applicationAutoMapperProfileFileWriter);

            static void WriteAutoMapper(CodeGeneratorSettings codeGeneratorSettings, CSFileWriter applicationAutoMapperProfileFileWriter, string entityName, List<GenerateAutoMapper> autoMapperList, string defaultDatabaseName)
            {
                foreach (var autoMapper in autoMapperList)
                {
                    var databaseEntityName = string.IsNullOrEmpty(autoMapper.DatabaseEntityName) ? defaultDatabaseName : autoMapper.DatabaseEntityName;
                    var source = autoMapper.AutoMapperSourceType == AutoMapperSourceType.Entity ? entityName : databaseEntityName;
                    var destination = autoMapper.AutoMapperSourceType != AutoMapperSourceType.Entity ? entityName : databaseEntityName;

                    var ignoreFieldCount = autoMapper.IgnoreFields?.IgnoreField.Count() ?? 0;

                    applicationAutoMapperProfileFileWriter.WriteConstructorAdditionalBodyLine($"this.OnMappingExpression(CreateMap<{source}, {destination}>()" +
                        (ignoreFieldCount == 0 ? ");" : string.Empty));

                    if (autoMapper.IgnoreFields?.IgnoreField != null)
                    {
                        foreach (var ignoreField in autoMapper.IgnoreFields.IgnoreField)
                        {
                            ignoreFieldCount--;

                            applicationAutoMapperProfileFileWriter.WriteConstructorAdditionalBodyLine(
                                applicationAutoMapperProfileFileWriter.GetWhiteSpace() + $".ForMember(x => x.{ignoreField.Name}, x => x.Ignore())" +
                                (ignoreFieldCount == 0 ? ");" : string.Empty));
                        }
                    }

                    applicationAutoMapperProfileFileWriter.WriteMethod("OnMappingExpression", parameters: $"IMappingExpression<{source}, {destination}> mappingExpression", isPartial: true);
                }
            }
        }

        private static string GetDatabaseEntityNameWithPrefix(CodeGeneratorSettings codeGeneratorSettings, string databaseEntityName) => codeGeneratorSettings?.DatabaseRepositoriesSettings?.DatabaseEntityPrefix + databaseEntityName;
    }
}
