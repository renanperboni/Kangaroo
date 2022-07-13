// Licensed to Kangaroo under one or more agreements.
// We license this file to you under the MIT license.
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

    internal static class EntitiesCodeWriter
    {
        public static void Generate(CodeGeneratorSettings codeGeneratorSettings, List<CodeGenerator> codeGenerators, SourceProductionContext sourceProductionContext)
        {
            foreach (var codeGenerator in codeGenerators)
            {
                foreach (var entity in codeGenerator.Entity)
                {
                    if (codeGeneratorSettings.BackendEntititesSettings != null)
                    {
                        GenerateEntities(codeGeneratorSettings, sourceProductionContext, entity, true);
                    }

                    if (codeGeneratorSettings.FrontendEntititesSettings != null)
                    {
                        GenerateEntities(codeGeneratorSettings, sourceProductionContext, entity, false);
                    }
                }

                foreach (var summary in codeGenerator.Summary)
                {
                    if (codeGeneratorSettings.BackendEntititesSettings != null)
                    {
                        GenerateSummaries(codeGeneratorSettings, sourceProductionContext, summary, true);
                    }

                    if (codeGeneratorSettings.FrontendEntititesSettings != null)
                    {
                        GenerateSummaries(codeGeneratorSettings, sourceProductionContext, summary, false);
                    }
                }
            }
        }

        public static void GenerateEntities(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Entity entity, bool isBackend)
        {
            WriteEntity(
                codeGeneratorSettings,
                sourceProductionContext,
                entityName: entity.Name,
                defaultInheritance: "IEntity",
                additionalUsings: entity.AdditionalUsings,
                customAttributes: entity.CustomAttributes,
                fields: entity.EntityFields,
                includeDataState: entity.IncludeDataState,
                includeRowVersionControl: entity.IncludeRowVersionControl,
                includeAuditLog: entity.IncludeAuditLog,
                isBackend: isBackend);

            if (entity.GenerateEntityHandlerRequest != null)
            {
                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    entityName: entity.Name,
                    className: $"{entity.Name}HandlerRequest",
                    inheritance: $"IEntityHandlerRequest<{entity.Name}>",
                    entityPropertyType: entity.Name,
                    entityPropertyName: "Entity",
                    entityPropertyValue: string.Empty,
                    additionalUsings: entity.GenerateEntityHandlerRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityHandlerRequest.CustomAttributes,
                    fields: entity.GenerateEntityHandlerRequest.AdditionalFields,
                    isBackend: isBackend);

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    entityName: entity.Name,
                    className: $"{entity.Name}HandlerResponse",
                    inheritance: $"IEntityHandlerResponse<{entity.Name}>",
                    entityPropertyType: entity.Name,
                    entityPropertyName: "Entity",
                    entityPropertyValue: string.Empty,
                    additionalUsings: entity.GenerateEntityHandlerRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityHandlerRequest.CustomAttributes,
                    fields: null,
                    isBackend: isBackend);
            }

            if (entity.GenerateEntityGetterRequest != null)
            {
                var entityGetterRequestInheritance = "IEntityGetterRequest";
                var entityGetterRequestFields = entity.GenerateEntityGetterRequest.AdditionalFields;

                if (entity.EntityFields?.KeyField?.KeyType != null)
                {
                    entityGetterRequestFields = entityGetterRequestFields ?? new Fields();

                    entityGetterRequestFields.KeyField = entity.EntityFields?.KeyField;

                    switch (entity.EntityFields?.KeyField?.KeyType)
                    {
                        case KeyType.Int:
                            entityGetterRequestInheritance += ", IHasIntegerKey";
                            break;
                        case KeyType.Guid:
                            entityGetterRequestInheritance += ", IHasGuidKey";
                            break;
                        default:
                            break;
                    }
                }

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    entityName: entity.Name,
                    className: $"{entity.Name}GetterRequest",
                    inheritance: entityGetterRequestInheritance,
                    entityPropertyType: string.Empty,
                    entityPropertyName: string.Empty,
                    entityPropertyValue: string.Empty,
                    additionalUsings: entity.GenerateEntityGetterRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityGetterRequest.CustomAttributes,
                    fields: entityGetterRequestFields,
                    isBackend: isBackend);

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    entityName: entity.Name,
                    className: $"{entity.Name}GetterResponse",
                    inheritance: $"IEntityGetterResponse<{entity.Name}>",
                    entityPropertyType: entity.Name,
                    entityPropertyName: "Entity",
                    entityPropertyValue: string.Empty,
                    additionalUsings: entity.GenerateEntityGetterRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityGetterRequest.CustomAttributes,
                    fields: null,
                    isBackend: isBackend);
            }

            if (entity.GenerateEntitiesGetterRequest != null)
            {
                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    entityName: entity.Name,
                    className: $"{entity.PluralName}GetterRequest",
                    inheritance: "IEntitiesGetterRequest",
                    entityPropertyType: string.Empty,
                    entityPropertyName: string.Empty,
                    entityPropertyValue: string.Empty,
                    additionalUsings: entity.GenerateEntityGetterRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityGetterRequest.CustomAttributes,
                    fields: entity.GenerateEntityGetterRequest.AdditionalFields,
                    isBackend: isBackend);

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    entityName: entity.Name,
                    className: $"{entity.Name}GetterResponse",
                    inheritance: $"IEntitiesGetterResponse<{entity.Name}>",
                    entityPropertyType: $"IList<{entity.Name}>",
                    entityPropertyName: "Entities",
                    entityPropertyValue: $"new List<{entity.Name}>()",
                    additionalUsings: entity.GenerateEntityGetterRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityGetterRequest.CustomAttributes,
                    fields: null,
                    isBackend: isBackend);
            }
        }

        public static void GenerateSummaries(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Summary summary, bool isBackend)
        {
            WriteEntity(
                codeGeneratorSettings,
                sourceProductionContext,
                entityName: summary.Name,
                defaultInheritance: "ISummary",
                additionalUsings: summary.AdditionalUsings,
                customAttributes: summary.CustomAttributes,
                fields: summary.SummaryFields,
                includeDataState: false,
                includeRowVersionControl: summary.IncludeRowVersionControl,
                includeAuditLog: summary.IncludeAuditLog,
                isBackend: isBackend);

            if (summary.GenerateSummariesGetterRequest != null)
            {
                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    entityName: summary.Name,
                    className: $"{summary.PluralName}GetterRequest",
                    inheritance: "ISummariesGetterRequest",
                    entityPropertyType: string.Empty,
                    entityPropertyName: string.Empty,
                    entityPropertyValue: string.Empty,
                    additionalUsings: summary.GenerateSummariesGetterRequest.AdditionalUsings,
                    customAttributes: summary.GenerateSummariesGetterRequest.CustomAttributes,
                    fields: summary.GenerateSummariesGetterRequest.RequestFields,
                    isBackend: isBackend);

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    entityName: summary.Name,
                    className: $"{summary.PluralName}GetterResponse",
                    inheritance: $"ISummariesGetterResponse<{summary.Name}>",
                    entityPropertyType: $"IList<{summary.Name}>",
                    entityPropertyName: "Summaries",
                    entityPropertyValue: $"new List<{summary.Name}>()",
                    additionalUsings: summary.GenerateSummariesGetterRequest.AdditionalUsings,
                    customAttributes: summary.GenerateSummariesGetterRequest.CustomAttributes,
                    fields: null,
                    isBackend: isBackend);
            }
        }

        private static void WriteEntity(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            string entityName,
            string defaultInheritance,
            AdditionalUsings additionalUsings,
            CustomAttributes customAttributes,
            Fields fields,
            bool includeDataState,
            bool includeRowVersionControl,
            bool includeAuditLog,
            bool isBackend)
        {
            var currentLocation = isBackend ? Structure.Location.Backend : Structure.Location.Frontend;
            var keyField = fields?.KeyField;
            var keyType = keyField?.KeyType;
            var entityNamespace = isBackend ? codeGeneratorSettings.BackendEntititesSettings?.EntitiesNamespace : codeGeneratorSettings.FrontendEntititesSettings?.EntitiesNamespace;
            var shouldGenerateNotifyPropertyChanges = isBackend ? false : codeGeneratorSettings.FrontendEntititesSettings?.GenerateNotifyPropertyChanges ?? false;

            var inheritance = defaultInheritance;

            if (includeDataState)
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

            if (includeRowVersionControl)
            {
                inheritance += ", IHasRowVersionControl";
            }

            if (includeAuditLog)
            {
                inheritance += ", IHasAuditLog";
            }

            var fileWriter = new CSFileWriter(
                    CSFileWriterType.Class,
                    entityNamespace,
                    entityName,
                    isPartial: true,
                    inheritance: inheritance);

            fileWriter.WriteUsing("System");
            fileWriter.WriteUsing("Kangaroo.Models");
            fileWriter.WriteUsing("Kangaroo.Models.Entities");

            foreach (var customUsing in additionalUsings?.Using)
            {
                fileWriter.WriteUsing(customUsing.Content);
            }

            foreach (var classAttribute in customAttributes.CustomAttribute)
            {
                fileWriter.WriteClassAttribute(classAttribute.Attribute);
            }

            fields?.HandleFields(WriteField(fileWriter, shouldGenerateNotifyPropertyChanges, currentLocation));

            if (includeRowVersionControl)
            {
                fileWriter.WriteProperty("byte[]", "RowVersion", isFullProperty: shouldGenerateNotifyPropertyChanges, isVirtual: false, attributes: new List<string>() { "Timestamp" });
            }

            if (includeAuditLog)
            {
                fileWriter.WriteProperty("string", "CreatedByUserName", isFullProperty: shouldGenerateNotifyPropertyChanges, isVirtual: false, attributes: new List<string>() { "Required", "MaxLength(510)" });
                fileWriter.WriteProperty("DateTimeOffset", "CreatedAt", isFullProperty: shouldGenerateNotifyPropertyChanges, isVirtual: false, attributes: new List<string>() { "Required", "MaxLength(510)" });

                fileWriter.WriteProperty("string", "UpdatedByUserName", isFullProperty: shouldGenerateNotifyPropertyChanges, isVirtual: false, attributes: new List<string>() { "MaxLength(510)" });
                fileWriter.WriteProperty("DateTimeOffset?", "UpdatedAt", isFullProperty: shouldGenerateNotifyPropertyChanges, isVirtual: false);
            }

            if (includeDataState)
            {
                fileWriter.WriteProperty("DataState", "DataState", isFullProperty: shouldGenerateNotifyPropertyChanges, isVirtual: false, attributes: new List<string>() { "NotMapped" });
            }

            WriteKeyField(keyField, fileWriter, currentLocation);

            sourceProductionContext.WriteNewCSFile(entityName, fileWriter);
        }

        private static void WriteRequestResponse(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            string entityName,
            string className,
            string inheritance,
            string entityPropertyType,
            string entityPropertyName,
            string entityPropertyValue,
            AdditionalUsings additionalUsings,
            CustomAttributes customAttributes,
            Fields fields,
            bool isBackend)
        {
            var currentLocation = isBackend ? Structure.Location.Backend : Structure.Location.Frontend;
            var classNamespace = isBackend ? codeGeneratorSettings.BackendEntititesSettings?.EntitiesNamespace : codeGeneratorSettings.FrontendEntititesSettings?.EntitiesNamespace;
            var shouldGenerateNotifyPropertyChanges = isBackend ? false : codeGeneratorSettings.FrontendEntititesSettings?.GenerateNotifyPropertyChanges ?? false;

            var fileWriter = new CSFileWriter(
                    CSFileWriterType.Class,
                    classNamespace,
                    entityName,
                    isPartial: true,
                    inheritance: inheritance);

            fileWriter.WriteUsing("System");
            fileWriter.WriteUsing("Kangaroo.Models");
            fileWriter.WriteUsing("Kangaroo.Models.Entities");

            foreach (var customUsing in additionalUsings?.Using)
            {
                fileWriter.WriteUsing(customUsing.Content);
            }

            foreach (var classAttribute in customAttributes.CustomAttribute)
            {
                fileWriter.WriteClassAttribute(classAttribute.Attribute);
            }

            if (!string.IsNullOrEmpty(entityPropertyName))
            {
                fileWriter.WriteProperty(type: entityPropertyType, name: entityPropertyName, value: entityPropertyValue, isFullProperty: shouldGenerateNotifyPropertyChanges);
            }

            fields?.HandleFields(WriteField(fileWriter, shouldGenerateNotifyPropertyChanges, currentLocation));

            WriteKeyField(fields?.KeyField, fileWriter, currentLocation);

            sourceProductionContext.WriteNewCSFile(className, fileWriter);
        }

        private static void WriteKeyField(KeyField keyField, CSFileWriter fileWriter, Structure.Location location)
        {
            if (keyField != null)
            {
                if (keyField.Location != Structure.Location.Both && keyField.Location != location)
                {
                    return;
                }

                List<string> getKeyMethodBodyLines = new List<string>();

                getKeyMethodBodyLines.Add($"return this.{keyField.Name};");

                switch (keyField.KeyType)
                {
                    case KeyType.Int:
                        fileWriter.WriteMethod("GetKey", "int", bodyLines: getKeyMethodBodyLines);
                        break;
                    case KeyType.Guid:
                        fileWriter.WriteMethod("GetKey", "Guid", bodyLines: getKeyMethodBodyLines);
                        break;
                    default:
                        break;
                }
            }
        }

        private static Action<object> WriteField(CSFileWriter fileWriter, bool isFullProperty, Structure.Location location)
        {
            return x =>
            {
                if (x is IField field)
                {
                    if (field.Location != Structure.Location.Both && field.Location != location)
                    {
                        return;
                    }

                    var fieldType = field.GetFieldType();
                    var isList = field is EntityCollectionField;
                    var fieldValue = string.Empty;

                    if (isList)
                    {
                        fieldType = $"IList<{fieldType}>";
                        fieldValue = $"new List<{fieldType}>()";
                    }

                    var attributes = new List<string>();

                    foreach (var attribute in field.CustomAttributes?.CustomAttribute)
                    {
                        attributes.Add(attribute.Attribute);
                    }

                    fileWriter.WriteProperty(fieldType, field.Name, value: fieldValue, isFullProperty: isFullProperty, attributes: attributes);
                }
            };
        }
    }
}
