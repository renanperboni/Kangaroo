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

    internal static class CustomResponsesCodeWriter
    {
        public static void Generate(CodeGeneratorSettings codeGeneratorSettings, List<CodeGenerator> codeGenerators, SourceProductionContext sourceProductionContext)
        {
            foreach (var codeGenerator in codeGenerators)
            {
                foreach (var customResponse in codeGenerator.CustomResponse)
                {
                    if (codeGeneratorSettings.BackendCustomResponsesSettings != null
                        && (customResponse.Location == Structure.Location.Both || customResponse.Location == Structure.Location.Backend))
                    {
                        WriteResponse(codeGeneratorSettings, sourceProductionContext, customResponse, true);
                    }

                    if (codeGeneratorSettings.FrontendCustomResponsesSettings != null
                        && (customResponse.Location == Structure.Location.Both || customResponse.Location == Structure.Location.Frontend))
                    {
                        WriteResponse(codeGeneratorSettings, sourceProductionContext, customResponse, false);
                    }
                }
            }
        }

        private static void WriteResponse(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, CustomResponse customResponse, bool isBackend)
        {
            var className = $"{customResponse.Name}Response";
            var currentLocation = isBackend ? Structure.Location.Backend : Structure.Location.Frontend;
            var keyField = customResponse.ResponseFields?.KeyField;
            var keyType = keyField?.KeyType;
            var inheritance = "IResponse";
            var classNamespace = isBackend ? codeGeneratorSettings.BackendCustomResponsesSettings?.CustomResponsesNamespace : codeGeneratorSettings.FrontendCustomResponsesSettings?.CustomResponsesNamespace;
            var validatorNamespace = isBackend ? codeGeneratorSettings.BackendCustomResponsesSettings?.ValidatorsNamespace : codeGeneratorSettings.FrontendCustomResponsesSettings?.ValidatorsNamespace;
            var shouldGenerateNotifyPropertyChanges = isBackend ? false : codeGeneratorSettings.FrontendCustomResponsesSettings?.GenerateNotifyPropertyChanges ?? false;

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

            if (customResponse.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in customResponse.AdditionalUsings.Using)
                {
                    fileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (customResponse.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in customResponse.CustomAttributes.CustomAttribute)
                {
                    fileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            customResponse.ResponseFields?.HandleFields(WriteField(fileWriter, validatorFileWriter, shouldGenerateNotifyPropertyChanges, currentLocation));

            WriteKeyField(customResponse.ResponseFields?.KeyField, fileWriter, currentLocation);

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
