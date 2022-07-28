// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.CodeGenerators.CodeWriters
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Kangaroo.CodeGenerators.Extensions;
    using Kangaroo.CodeGenerators.Structure;
    using Kangaroo.CodeGenerators.Writers;
    using Microsoft.CodeAnalysis;

    internal static class EnumsCodeWriter
    {
        public static void Generate(CodeGeneratorSettings codeGeneratorSettings, List<CodeGenerator> codeGenerators, SourceProductionContext sourceProductionContext)
        {
            foreach (var codeGenerator in codeGenerators)
            {
                foreach (var enumEntity in codeGenerator.Enum)
                {
                    if (codeGeneratorSettings.BackendEnumsSettings != null
                        && (enumEntity.Location == Structure.Location.Both || enumEntity.Location == Structure.Location.Backend))
                    {
                        WriteEnum(codeGeneratorSettings, sourceProductionContext, enumEntity, true);
                    }

                    if (codeGeneratorSettings.FrontendEnumsSettings != null
                        && (enumEntity.Location == Structure.Location.Both || enumEntity.Location == Structure.Location.Frontend))
                    {
                        WriteEnum(codeGeneratorSettings, sourceProductionContext, enumEntity, false);
                    }
                }
            }
        }

        private static void WriteEnum(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, EnumEntity enumEntity, bool isBackend)
        {
            var currentLocation = isBackend ? Structure.Location.Backend : Structure.Location.Frontend;
            var enumEntityNamespace = isBackend ? codeGeneratorSettings.BackendEnumsSettings?.EnumsNamespace : codeGeneratorSettings.FrontendEnumsSettings?.EnumsNamespace;

            var enumEntityFileWriter = new CSFileWriter(
                    CSFileWriterType.Enum,
                    enumEntityNamespace,
                    enumEntity.Name);

            enumEntityFileWriter.WriteUsing("System");

            if (enumEntity.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in enumEntity.AdditionalUsings.Using)
                {
                    enumEntityFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (enumEntity.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in enumEntity.CustomAttributes.CustomAttribute)
                {
                    enumEntityFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            var sequence = 0;

            foreach (var enumValue in enumEntity.EnumValue)
            {
                if (enumValue.Location != Structure.Location.Both && enumValue.Location != currentLocation)
                {
                    return;
                }

                var defaultValue = enumValue.DefaultValue;

                if (enumEntity.AutoGenSequenceNumber)
                {
                    enumEntityFileWriter.WriteEnumField(enumValue.Name, sequence.ToString());
                    sequence++;
                }
                else
                {
                    enumEntityFileWriter.WriteEnumField(enumValue.Name, enumValue.DefaultValue);
                }
            }

            sourceProductionContext.WriteNewCSFile(enumEntity.Name, enumEntityFileWriter);
        }
    }
}
