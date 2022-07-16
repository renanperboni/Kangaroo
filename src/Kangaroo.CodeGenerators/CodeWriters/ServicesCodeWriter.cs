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

    internal static class ServicesCodeWriter
    {
        public static void Generate(CodeGeneratorSettings codeGeneratorSettings, List<CodeGenerator> codeGenerators, SourceProductionContext sourceProductionContext)
        {
            foreach (var codeGenerator in codeGenerators)
            {
                foreach (var entity in codeGenerator.Entity)
                {
                    if (codeGeneratorSettings.ServicesSettings != null)
                    {
                        GenerateServices(codeGeneratorSettings, sourceProductionContext, entity);
                    }
                }

                foreach (var summary in codeGenerator.Summary)
                {
                    if (codeGeneratorSettings.ServicesSettings != null)
                    {
                        GenerateServices(codeGeneratorSettings, sourceProductionContext, summary);
                    }
                }
            }
        }

        public static void GenerateServices(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Entity entity)
        {
            if (entity.GenerateEntityHandlerRequest?.GenerateEntityHandlerService != null)
            {
                WriteEntityHandlerService(codeGeneratorSettings, sourceProductionContext, entity);
            }

            if (entity.GenerateEntityGetterRequest?.GenerateEntityGetterService != null)
            {
                WriteEntityGetterService(codeGeneratorSettings, sourceProductionContext, entity);
            }

            if (entity.GenerateEntitiesGetterRequest?.GenerateEntitiesGetterService != null)
            {
                WriteEntitiesGetterService(codeGeneratorSettings, sourceProductionContext, entity);
            }
        }

        public static void GenerateServices(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Summary summary)
        {
            if (summary.GenerateSummaryGetterRequest?.GenerateSummaryGetterService != null)
            {
                WriteSummaryGetterService(codeGeneratorSettings, sourceProductionContext, summary);
            }

            if (summary.GenerateSummariesGetterRequest?.GenerateSummariesGetterService != null)
            {
                WriteSummariesGetterService(codeGeneratorSettings, sourceProductionContext, summary);
            }
        }

        private static void WriteEntityHandlerService(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Entity entity)
        {
            var databaseEntityName = GetDatabaseEntityNameWithPrefix(codeGeneratorSettings, entity.Name);
            var entityName = entity.Name;
            var handlerRequestName = $"{entity.Name}HandlerRequest";
            var handlerResponseName = $"{entity.Name}HandlerResponse";

            var interfaceInheritance = entity.GenerateEntityHandlerRequest.GenerateEntityHandlerService.IsDatabaseEntityHandler
                ? $"IDatabaseEntityHandlerService<{databaseEntityName}, {entityName}, {handlerRequestName}, {handlerResponseName}>"
                : $"IEntityHandlerService<{entityName}, {handlerRequestName}, {handlerResponseName}>";
            var interfaceName = $"I{entityName}HandlerService";

            var interfaceServiceFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                interfaceName,
                isPartial: true,
                inheritance: interfaceInheritance);

            interfaceServiceFileWriter.WriteUsing("Kangaroo.Services");

            if (entity.GenerateEntityHandlerRequest.GenerateEntityHandlerService.IsDatabaseEntityHandler)
            {
                interfaceServiceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.DatabaseRepositoriesNamespace);
                interfaceServiceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.DatabaseEntitiesNamespace);
            }

            interfaceServiceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);
            sourceProductionContext.WriteNewCSFile(interfaceName, interfaceServiceFileWriter);

            var serviceInheritance = entity.GenerateEntityHandlerRequest.GenerateEntityHandlerService.IsDatabaseEntityHandler
                ? $"DatabaseEntityHandlerService<ApplicationDbContext, {databaseEntityName}, {entityName}, {handlerRequestName}, {handlerResponseName}>"
                : $"EntityHandlerService<{entityName}, {handlerRequestName}, {handlerResponseName}>";
            serviceInheritance += $", {interfaceName}";
            var serviceName = $"{entityName}HandlerService";
            var serviceFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                serviceName,
                isPartial: true,
                inheritance: serviceInheritance);

            serviceFileWriter.WriteUsing("System");
            serviceFileWriter.WriteUsing("Kangaroo.Services");

            if (entity.GenerateEntityHandlerRequest.GenerateEntityHandlerService.IsDatabaseEntityHandler)
            {
                serviceFileWriter.WriteUsing("AutoMapper");
                serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.DbContextNamespace);
                serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.DatabaseRepositoriesNamespace);
                serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.DatabaseEntitiesNamespace);
            }

            serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);

            if (entity.GenerateEntityHandlerRequest.GenerateEntityHandlerService.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in entity.GenerateEntityHandlerRequest.GenerateEntityHandlerService.AdditionalUsings.Using)
                {
                    serviceFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (entity.GenerateEntityHandlerRequest.GenerateEntityHandlerService.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in entity.GenerateEntityHandlerRequest.GenerateEntityHandlerService.CustomAttributes.CustomAttribute)
                {
                    serviceFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            if (entity.GenerateEntityHandlerRequest.GenerateEntityHandlerService.IsDatabaseEntityHandler)
            {
                serviceFileWriter.WriteDependencyInjection("IApplicationDatabaseRepository", "applicationDatabaseRepository", shouldSendToConstructorBase: true);
            }

            serviceFileWriter.WriteDependencyInjection("IMapper", "mapper", shouldSendToConstructorBase: true);
            serviceFileWriter.WriteDependencyInjection("ICurrentUserService", "currentUserService", shouldSendToConstructorBase: true);

            sourceProductionContext.WriteNewCSFile(serviceName, serviceFileWriter);
        }

        private static void WriteEntityGetterService(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Entity entity)
        {
            var entityName = entity.Name;
            var getterRequestName = $"{entity.Name}GetterRequest";
            var getterResponseName = $"{entity.Name}GetterResponse";

            var interfaceInheritance = $"IEntityGetterService<{entityName}, {getterRequestName}, {getterResponseName}>";
            var interfaceName = $"I{entityName}GetterService";

            var interfaceServiceFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                interfaceName,
                isPartial: true,
                inheritance: interfaceInheritance);

            interfaceServiceFileWriter.WriteUsing("AutoMapper");
            interfaceServiceFileWriter.WriteUsing("Kangaroo.Services");
            interfaceServiceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);
            sourceProductionContext.WriteNewCSFile(interfaceName, interfaceServiceFileWriter);

            var serviceInheritance = $"EntityGetterService<{entityName}, {getterRequestName}, {getterResponseName}>";
            serviceInheritance += $", {interfaceName}";
            var serviceName = $"{entityName}GetterService";
            var serviceFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                serviceName,
                isPartial: true,
                inheritance: serviceInheritance);

            serviceFileWriter.WriteUsing("System");
            serviceFileWriter.WriteUsing("Kangaroo.Services");
            serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);

            if (entity.GenerateEntityGetterRequest.GenerateEntityGetterService.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in entity.GenerateEntityGetterRequest.GenerateEntityGetterService.AdditionalUsings.Using)
                {
                    serviceFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (entity.GenerateEntityGetterRequest.GenerateEntityGetterService.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in entity.GenerateEntityGetterRequest.GenerateEntityGetterService.CustomAttributes.CustomAttribute)
                {
                    serviceFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            sourceProductionContext.WriteNewCSFile(serviceName, serviceFileWriter);
        }

        private static void WriteEntitiesGetterService(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Entity entity)
        {
            var entityName = entity.Name;
            var entityPluralName = entity.PluralName;
            var getterRequestName = $"{entity.PluralName}GetterRequest";
            var getterResponseName = $"{entity.PluralName}GetterResponse";

            var interfaceInheritance = $"IEntitiesGetterService<{entityName}, {getterRequestName}, {getterResponseName}>";
            var interfaceName = $"I{entityPluralName}GetterService";

            var interfaceServiceFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                interfaceName,
                isPartial: true,
                inheritance: interfaceInheritance);

            interfaceServiceFileWriter.WriteUsing("AutoMapper");
            interfaceServiceFileWriter.WriteUsing("Kangaroo.Services");
            interfaceServiceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);
            sourceProductionContext.WriteNewCSFile(interfaceName, interfaceServiceFileWriter);

            var serviceInheritance = $"EntitiesGetterService<{entityName}, {getterRequestName}, {getterResponseName}>";
            serviceInheritance += $", {interfaceName}";
            var serviceName = $"{entityPluralName}GetterService";
            var serviceFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                serviceName,
                isPartial: true,
                inheritance: serviceInheritance);

            serviceFileWriter.WriteUsing("System");
            serviceFileWriter.WriteUsing("Kangaroo.Services");
            serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);

            if (entity.GenerateEntitiesGetterRequest.GenerateEntitiesGetterService.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in entity.GenerateEntitiesGetterRequest.GenerateEntitiesGetterService.AdditionalUsings.Using)
                {
                    serviceFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (entity.GenerateEntitiesGetterRequest.GenerateEntitiesGetterService.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in entity.GenerateEntitiesGetterRequest.GenerateEntitiesGetterService.CustomAttributes.CustomAttribute)
                {
                    serviceFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            sourceProductionContext.WriteNewCSFile(serviceName, serviceFileWriter);
        }

        private static void WriteSummaryGetterService(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Summary summary)
        {
            var summaryName = summary.Name;
            var getterRequestName = $"{summary.Name}GetterRequest";
            var getterResponseName = $"{summary.Name}GetterResponse";

            var interfaceInheritance = $"ISummaryGetterService<{summaryName}, {getterRequestName}, {getterResponseName}>";
            var interfaceName = $"I{summaryName}GetterService";

            var interfaceServiceFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                interfaceName,
                isPartial: true,
                inheritance: interfaceInheritance);

            interfaceServiceFileWriter.WriteUsing("AutoMapper");
            interfaceServiceFileWriter.WriteUsing("Kangaroo.Services");
            interfaceServiceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);
            sourceProductionContext.WriteNewCSFile(interfaceName, interfaceServiceFileWriter);

            var serviceInheritance = $"SummaryGetterService<{summaryName}, {getterRequestName}, {getterResponseName}>";
            serviceInheritance += $", {interfaceName}";
            var serviceName = $"{summaryName}GetterService";
            var serviceFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                serviceName,
                isPartial: true,
                inheritance: serviceInheritance);

            serviceFileWriter.WriteUsing("System");
            serviceFileWriter.WriteUsing("Kangaroo.Services");
            serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);

            if (summary.GenerateSummaryGetterRequest.GenerateSummaryGetterService.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in summary.GenerateSummaryGetterRequest.GenerateSummaryGetterService.AdditionalUsings.Using)
                {
                    serviceFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (summary.GenerateSummaryGetterRequest.GenerateSummaryGetterService.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in summary.GenerateSummaryGetterRequest.GenerateSummaryGetterService.CustomAttributes.CustomAttribute)
                {
                    serviceFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            sourceProductionContext.WriteNewCSFile(serviceName, serviceFileWriter);
        }

        private static void WriteSummariesGetterService(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Summary summary)
        {
            var summaryName = summary.Name;
            var summaryPluralName = summary.PluralName;
            var getterRequestName = $"{summary.PluralName}GetterRequest";
            var getterResponseName = $"{summary.PluralName}GetterResponse";

            var interfaceInheritance = $"ISummariesGetterService<{summaryName}, {getterRequestName}, {getterResponseName}>";
            var interfaceName = $"I{summaryPluralName}GetterService";

            var interfaceServiceFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                interfaceName,
                isPartial: true,
                inheritance: interfaceInheritance);

            interfaceServiceFileWriter.WriteUsing("AutoMapper");
            interfaceServiceFileWriter.WriteUsing("Kangaroo.Services");

            interfaceServiceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);
            sourceProductionContext.WriteNewCSFile(interfaceName, interfaceServiceFileWriter);

            var serviceInheritance = $"SummariesGetterService<{summaryName}, {getterRequestName}, {getterResponseName}>";
            serviceInheritance += $", {interfaceName}";
            var serviceName = $"{summaryPluralName}GetterService";
            var serviceFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                serviceName,
                isPartial: true,
                inheritance: serviceInheritance);

            serviceFileWriter.WriteUsing("System");
            serviceFileWriter.WriteUsing("Kangaroo.Services");
            serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);

            if (summary.GenerateSummariesGetterRequest.GenerateSummariesGetterService.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in summary.GenerateSummariesGetterRequest.GenerateSummariesGetterService.AdditionalUsings.Using)
                {
                    serviceFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (summary.GenerateSummariesGetterRequest.GenerateSummariesGetterService.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in summary.GenerateSummariesGetterRequest.GenerateSummariesGetterService.CustomAttributes.CustomAttribute)
                {
                    serviceFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            sourceProductionContext.WriteNewCSFile(serviceName, serviceFileWriter);
        }

        private static string GetDatabaseEntityNameWithPrefix(CodeGeneratorSettings codeGeneratorSettings, string databaseEntityName) => codeGeneratorSettings?.ServicesSettings?.DatabaseEntityPrefix + databaseEntityName;
    }
}
