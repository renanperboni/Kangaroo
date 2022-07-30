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

    internal static class EntitiesCodeWriter
    {
        public static void Generate(CodeGeneratorSettings codeGeneratorSettings, List<CodeGenerator> codeGenerators, SourceProductionContext sourceProductionContext)
        {
            foreach (var codeGenerator in codeGenerators)
            {
                foreach (var entity in codeGenerator.Entity)
                {
                    if (codeGeneratorSettings.BackendEntititesSettings != null
                        && (entity.Location == Structure.Location.Both || entity.Location == Structure.Location.Backend))
                    {
                        GenerateEntities(codeGeneratorSettings, sourceProductionContext, entity, true);
                    }

                    if (codeGeneratorSettings.FrontendEntititesSettings != null
                        && (entity.Location == Structure.Location.Both || entity.Location == Structure.Location.Frontend))
                    {
                        GenerateEntities(codeGeneratorSettings, sourceProductionContext, entity, false);
                    }
                }

                foreach (var summary in codeGenerator.Summary)
                {
                    if (codeGeneratorSettings.BackendEntititesSettings != null
                        && (summary.Location == Structure.Location.Both || summary.Location == Structure.Location.Backend))
                    {
                        GenerateSummaries(codeGeneratorSettings, sourceProductionContext, summary, true);
                    }

                    if (codeGeneratorSettings.FrontendEntititesSettings != null
                        && (summary.Location == Structure.Location.Both || summary.Location == Structure.Location.Frontend))
                    {
                        GenerateSummaries(codeGeneratorSettings, sourceProductionContext, summary, false);
                    }
                }
            }

            if (codeGeneratorSettings.BackendEntititesSettings?.GenerateAuthEntities == true)
            {
                GenerateAuthEntities(codeGeneratorSettings, sourceProductionContext, true);
            }

            if (codeGeneratorSettings.FrontendEntititesSettings?.GenerateAuthEntities == true)
            {
                GenerateAuthEntities(codeGeneratorSettings, sourceProductionContext, false);
            }
        }

        private static void GenerateEntities(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Entity entity, bool isBackend)
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
                    className: $"{entity.Name}HandlerRequest",
                    inheritance: $"IEntityHandlerRequest<{entity.Name}>",
                    entityPropertyType: entity.Name,
                    entityPropertyName: "Entity",
                    entityPropertyValue: string.Empty,
                    entityPropertyHasValidator: true,
                    additionalUsings: entity.GenerateEntityHandlerRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityHandlerRequest.CustomAttributes,
                    fields: entity.GenerateEntityHandlerRequest.AdditionalFields,
                    isBackend: isBackend);

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    className: $"{entity.Name}HandlerResponse",
                    inheritance: $"IEntityHandlerResponse<{entity.Name}>",
                    entityPropertyType: entity.Name,
                    entityPropertyName: "Entity",
                    entityPropertyValue: string.Empty,
                    entityPropertyHasValidator: false,
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
                    className: $"{entity.Name}GetterRequest",
                    inheritance: entityGetterRequestInheritance,
                    entityPropertyType: string.Empty,
                    entityPropertyName: string.Empty,
                    entityPropertyValue: string.Empty,
                    entityPropertyHasValidator: true,
                    additionalUsings: entity.GenerateEntityGetterRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityGetterRequest.CustomAttributes,
                    fields: entityGetterRequestFields,
                    isBackend: isBackend);

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    className: $"{entity.Name}GetterResponse",
                    inheritance: $"IEntityGetterResponse<{entity.Name}>",
                    entityPropertyType: entity.Name,
                    entityPropertyName: "Entity",
                    entityPropertyValue: string.Empty,
                    entityPropertyHasValidator: false,
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
                    className: $"{entity.PluralName}GetterRequest",
                    inheritance: "IEntitiesGetterRequest",
                    entityPropertyType: string.Empty,
                    entityPropertyName: string.Empty,
                    entityPropertyValue: string.Empty,
                    entityPropertyHasValidator: false,
                    additionalUsings: entity.GenerateEntityGetterRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityGetterRequest.CustomAttributes,
                    fields: entity.GenerateEntityGetterRequest.AdditionalFields,
                    isBackend: isBackend);

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    className: $"{entity.PluralName}GetterResponse",
                    inheritance: $"IEntitiesGetterResponse<{entity.Name}>",
                    entityPropertyType: $"IList<{entity.Name}>",
                    entityPropertyName: "Entities",
                    entityPropertyValue: $"new List<{entity.Name}>()",
                    entityPropertyHasValidator: false,
                    additionalUsings: entity.GenerateEntityGetterRequest.AdditionalUsings,
                    customAttributes: entity.GenerateEntityGetterRequest.CustomAttributes,
                    fields: null,
                    isBackend: isBackend);
            }
        }

        private static void GenerateSummaries(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Summary summary, bool isBackend)
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

            if (summary.GenerateSummaryGetterRequest != null)
            {
                var summaryGetterRequestInheritance = "ISummaryGetterRequest";
                var summaryGetterRequestFields = summary.GenerateSummaryGetterRequest.AdditionalFields;

                if (summary.SummaryFields?.KeyField?.KeyType != null)
                {
                    summaryGetterRequestFields = summaryGetterRequestFields ?? new Fields();

                    summaryGetterRequestFields.KeyField = summary.SummaryFields?.KeyField;

                    switch (summary.SummaryFields?.KeyField?.KeyType)
                    {
                        case KeyType.Int:
                            summaryGetterRequestInheritance += ", IHasIntegerKey";
                            break;
                        case KeyType.Guid:
                            summaryGetterRequestInheritance += ", IHasGuidKey";
                            break;
                        default:
                            break;
                    }
                }

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    className: $"{summary.Name}GetterRequest",
                    inheritance: summaryGetterRequestInheritance,
                    entityPropertyType: string.Empty,
                    entityPropertyName: string.Empty,
                    entityPropertyValue: string.Empty,
                    entityPropertyHasValidator: true,
                    additionalUsings: summary.GenerateSummaryGetterRequest.AdditionalUsings,
                    customAttributes: summary.GenerateSummaryGetterRequest.CustomAttributes,
                    fields: summaryGetterRequestFields,
                    isBackend: isBackend);

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    className: $"{summary.Name}GetterResponse",
                    inheritance: $"ISummaryGetterResponse<{summary.Name}>",
                    entityPropertyType: summary.Name,
                    entityPropertyName: "Summary",
                    entityPropertyValue: string.Empty,
                    entityPropertyHasValidator: false,
                    additionalUsings: summary.GenerateSummaryGetterRequest.AdditionalUsings,
                    customAttributes: summary.GenerateSummaryGetterRequest.CustomAttributes,
                    fields: null,
                    isBackend: isBackend);
            }

            if (summary.GenerateSummariesGetterRequest != null)
            {
                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    className: $"{summary.PluralName}GetterRequest",
                    inheritance: "ISummariesGetterRequest",
                    entityPropertyType: string.Empty,
                    entityPropertyName: string.Empty,
                    entityPropertyValue: string.Empty,
                    entityPropertyHasValidator: false,
                    additionalUsings: summary.GenerateSummariesGetterRequest.AdditionalUsings,
                    customAttributes: summary.GenerateSummariesGetterRequest.CustomAttributes,
                    fields: summary.GenerateSummariesGetterRequest.RequestFields,
                    isBackend: isBackend);

                WriteRequestResponse(
                    codeGeneratorSettings,
                    sourceProductionContext,
                    className: $"{summary.PluralName}GetterResponse",
                    inheritance: $"ISummariesGetterResponse<{summary.Name}>",
                    entityPropertyType: $"IList<{summary.Name}>",
                    entityPropertyName: "Summaries",
                    entityPropertyValue: $"new List<{summary.Name}>()",
                    entityPropertyHasValidator: false,
                    additionalUsings: summary.GenerateSummariesGetterRequest.AdditionalUsings,
                    customAttributes: summary.GenerateSummariesGetterRequest.CustomAttributes,
                    fields: null,
                    isBackend: isBackend);
            }
        }

        private static void GenerateAuthEntities(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, bool isBackend)
        {
            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "ApplicationUserInsertRequest",
                inheritance: $"IRequest",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: new Fields()
                {
                    StringField = new List<StringField>()
                    {
                        new StringField()
                        {
                            Name = "Name",
                            IsRequired = true,
                            MaxLength = 255,
                        },
                        new StringField()
                        {
                            Name = "Email",
                            IsRequired = true,
                            MaxLength = 255,
                        },
                        new StringField()
                        {
                            Name = "Password",
                            IsRequired = true,
                            MaxLength = 255,
                        },
                    },
                },
                isBackend: isBackend);

            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "ApplicationUserInsertResponse",
                inheritance: $"IResponse",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: null,
                isBackend: isBackend);

            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "LoginRequest",
                inheritance: $"IRequest",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: new Fields()
                {
                    StringField = new List<StringField>()
                    {
                        new StringField()
                        {
                            Name = "Email",
                            IsRequired = true,
                            MaxLength = 255,
                        },
                        new StringField()
                        {
                            Name = "Password",
                            IsRequired = true,
                            MaxLength = 255,
                        },
                    },
                },
                isBackend: isBackend);

            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "LoginResponse",
                inheritance: $"IResponse",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: new Fields()
                {
                    StringField = new List<StringField>()
                    {
                        new StringField()
                        {
                            Name = "Token",
                            IsRequired = true,
                        },
                        new StringField()
                        {
                            Name = "RefreshToken",
                            IsRequired = true,
                        },
                    },
                },
                isBackend: isBackend);

            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "RefreshTokenRequest",
                inheritance: $"IRequest",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: new Fields()
                {
                    StringField = new List<StringField>()
                    {
                        new StringField()
                        {
                            Name = "Token",
                            IsRequired = true,
                        },
                        new StringField()
                        {
                            Name = "RefreshToken",
                            IsRequired = true,
                        },
                    },
                },
                isBackend: isBackend);

            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "RefreshTokenResponse",
                inheritance: $"IResponse",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: new Fields()
                {
                    StringField = new List<StringField>()
                    {
                        new StringField()
                        {
                            Name = "Token",
                            IsRequired = true,
                        },
                        new StringField()
                        {
                            Name = "RefreshToken",
                            IsRequired = true,
                        },
                    },
                },
                isBackend: isBackend);

            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "LogoutRequest",
                inheritance: $"IRequest",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: null,
                isBackend: isBackend);

            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "LogoutResponse",
                inheritance: $"IResponse",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: null,
                isBackend: isBackend);

            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "ChangePasswordRequest",
                inheritance: $"IRequest",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: new Fields()
                {
                    StringField = new List<StringField>()
                    {
                        new StringField()
                        {
                            Name = "CurrentPassword",
                            IsRequired = true,
                            MaxLength = 255,
                        },
                        new StringField()
                        {
                            Name = "NewPassword",
                            IsRequired = true,
                            MaxLength = 255,
                        },
                    },
                },
                isBackend: isBackend);

            WriteRequestResponse(
                codeGeneratorSettings,
                sourceProductionContext,
                className: "ChangePasswordResponse",
                inheritance: $"IResponse",
                entityPropertyType: string.Empty,
                entityPropertyName: string.Empty,
                entityPropertyValue: string.Empty,
                entityPropertyHasValidator: false,
                additionalUsings: null,
                customAttributes: null,
                fields: null,
                isBackend: isBackend);
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
            var entityValidatorNamespace = isBackend ? codeGeneratorSettings.BackendEntititesSettings?.ValidatorsNamespace : codeGeneratorSettings.FrontendEntititesSettings?.ValidatorsNamespace;
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

            var entityFileWriter = new CSFileWriter(
                    CSFileWriterType.Class,
                    entityNamespace,
                    entityName,
                    isPartial: true,
                    inheritance: inheritance);

            entityFileWriter.WriteUsing("System");
            entityFileWriter.WriteUsing("Kangaroo.Models");
            entityFileWriter.WriteUsing("Kangaroo.Models.Entities");

            var validatorClassName = $"{entityName}Validator";
            var entityValidatorFileWriter = new CSFileWriter(
                    CSFileWriterType.Class,
                    entityValidatorNamespace,
                    validatorClassName,
                    isPartial: true,
                    inheritance: $"AbstractValidator<{entityName}>");

            entityValidatorFileWriter.WriteUsing("System");
            entityValidatorFileWriter.WriteUsing("FluentValidation");
            entityValidatorFileWriter.WriteUsing("Kangaroo.Models.Entities");
            entityValidatorFileWriter.WriteUsing(entityNamespace);

            entityValidatorFileWriter.WriteMethod("SetCustomRules", isPartial: true);

            if (additionalUsings?.Using != null)
            {
                foreach (var customUsing in additionalUsings.Using)
                {
                    entityFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (customAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in customAttributes.CustomAttribute)
                {
                    entityFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            fields?.HandleFields(WriteField(entityFileWriter, entityValidatorFileWriter, shouldGenerateNotifyPropertyChanges, currentLocation));

            if (includeRowVersionControl)
            {
                entityFileWriter.WriteProperty("byte[]", "RowVersion", hasNotifyPropertyChanged: shouldGenerateNotifyPropertyChanges, isVirtual: false);
            }

            if (includeAuditLog)
            {
                entityFileWriter.WriteProperty("string", "CreatedByUserName", hasNotifyPropertyChanged: shouldGenerateNotifyPropertyChanges, isVirtual: false);
                entityFileWriter.WriteProperty("DateTimeOffset", "CreatedAt", hasNotifyPropertyChanged: shouldGenerateNotifyPropertyChanges, isVirtual: false);

                entityFileWriter.WriteProperty("string", "UpdatedByUserName", hasNotifyPropertyChanged: shouldGenerateNotifyPropertyChanges, isVirtual: false);
                entityFileWriter.WriteProperty("DateTimeOffset?", "UpdatedAt", hasNotifyPropertyChanged: shouldGenerateNotifyPropertyChanges, isVirtual: false);
            }

            if (includeDataState)
            {
                entityFileWriter.WriteProperty("DataState", "DataState", hasNotifyPropertyChanged: shouldGenerateNotifyPropertyChanges, isVirtual: false);
            }

            WriteKeyField(keyField, entityFileWriter, currentLocation);

            entityValidatorFileWriter.WriteConstructorAdditionalBodyLine($"this.SetCustomRules();");

            sourceProductionContext.WriteNewCSFile(entityName, entityFileWriter);
            sourceProductionContext.WriteNewCSFile(validatorClassName, entityValidatorFileWriter);
        }

        private static void WriteRequestResponse(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            string className,
            string inheritance,
            string entityPropertyType,
            string entityPropertyName,
            string entityPropertyValue,
            bool entityPropertyHasValidator,
            AdditionalUsings additionalUsings,
            CustomAttributes customAttributes,
            Fields fields,
            bool isBackend)
        {
            var currentLocation = isBackend ? Structure.Location.Backend : Structure.Location.Frontend;
            var classNamespace = isBackend ? codeGeneratorSettings.BackendEntititesSettings?.EntitiesNamespace : codeGeneratorSettings.FrontendEntititesSettings?.EntitiesNamespace;
            var validatorNamespace = isBackend ? codeGeneratorSettings.BackendEntititesSettings?.ValidatorsNamespace : codeGeneratorSettings.FrontendEntititesSettings?.ValidatorsNamespace;
            var shouldGenerateNotifyPropertyChanges = isBackend ? false : codeGeneratorSettings.FrontendEntititesSettings?.GenerateNotifyPropertyChanges ?? false;

            var fileWriter = new CSFileWriter(
                    CSFileWriterType.Class,
                    classNamespace,
                    className,
                    isPartial: true,
                    inheritance: inheritance);

            fileWriter.WriteUsing("System");
            fileWriter.WriteUsing("Kangaroo.Models");
            fileWriter.WriteUsing("Kangaroo.Models.Entities");

            var validatorClassName = $"{className}Validator";
            var validatorFileWriter = new CSFileWriter(
                    CSFileWriterType.Class,
                    validatorNamespace,
                    validatorClassName,
                    isPartial: true,
                    inheritance: $"AbstractValidator<{className}>");

            validatorFileWriter.WriteUsing("System");
            validatorFileWriter.WriteUsing("FluentValidation");
            validatorFileWriter.WriteUsing("Kangaroo.Models.Entities");
            validatorFileWriter.WriteUsing(classNamespace);

            validatorFileWriter.WriteMethod("SetCustomRules", isPartial: true);

            if (additionalUsings?.Using != null)
            {
                foreach (var customUsing in additionalUsings.Using)
                {
                    fileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (customAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in customAttributes.CustomAttribute)
                {
                    fileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            if (!string.IsNullOrEmpty(entityPropertyName))
            {
                fileWriter.WriteProperty(type: entityPropertyType, name: entityPropertyName, value: entityPropertyValue, hasNotifyPropertyChanged: shouldGenerateNotifyPropertyChanges);

                if (entityPropertyHasValidator)
                {
                    validatorFileWriter.WriteConstructorAdditionalBodyLine($"this.RuleFor(x => x.{entityPropertyName}).NotNull().NotEmpty();");
                    validatorFileWriter.WriteConstructorAdditionalBodyLine($"this.RuleFor(x => x.{entityPropertyName}).SetValidator(x => new {entityPropertyType}Validator());");
                }
            }

            fields?.HandleFields(WriteField(fileWriter, validatorFileWriter, shouldGenerateNotifyPropertyChanges, currentLocation));

            WriteKeyField(fields?.KeyField, fileWriter, currentLocation);

            validatorFileWriter.WriteConstructorAdditionalBodyLine($"this.SetCustomRules();");

            sourceProductionContext.WriteNewCSFile(className, fileWriter);
            sourceProductionContext.WriteNewCSFile(validatorClassName, validatorFileWriter);
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
                List<string> setKeyMethodBodyLines = new List<string>();

                getKeyMethodBodyLines.Add($"return this.{keyField.Name};");
                setKeyMethodBodyLines.Add($"this.{keyField.Name} = key;");

                switch (keyField.KeyType)
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
        }

        private static Action<object> WriteField(CSFileWriter fileWriter, CSFileWriter validatorFileWriter, bool hasNotifyPropertyChanged, Structure.Location location)
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

                    if (isRequired)
                    {
                        validatorFileWriter.WriteConstructorAdditionalBodyLine($"this.RuleFor(x => x.{field.Name}).NotNull().NotEmpty();");
                    }

                    if (maxLength > 0)
                    {
                        validatorFileWriter.WriteConstructorAdditionalBodyLine($"this.RuleFor(x => x.{field.Name}).MaximumLength({maxLength});");
                    }

                    if (field is EntityField entityField)
                    {
                        validatorFileWriter.WriteConstructorAdditionalBodyLine($"this.RuleFor(x => x.{field.Name}).SetValidator(x => new {entityField.Type}Validator());");
                    }

                    var attributes = new List<string>();

                    if (field.CustomAttributes?.CustomAttribute != null)
                    {
                        foreach (var attribute in field.CustomAttributes.CustomAttribute)
                        {
                            attributes.Add(attribute.Attribute);
                        }
                    }

                    fileWriter.WriteProperty(fieldType, field.Name, value: fieldValue, hasNotifyPropertyChanged: hasNotifyPropertyChanged, attributes: attributes);
                }
            };
        }
    }
}
