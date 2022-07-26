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

    internal static class APICodeWriter
    {
        public static void Generate(CodeGeneratorSettings codeGeneratorSettings, List<CodeGenerator> codeGenerators, SourceProductionContext sourceProductionContext)
        {
            foreach (var codeGenerator in codeGenerators)
            {
                foreach (var entity in codeGenerator.Entity)
                {
                    if (codeGeneratorSettings.APISettings != null)
                    {
                        GenerateControllers(codeGeneratorSettings, sourceProductionContext, entity);
                    }
                }

                foreach (var summary in codeGenerator.Summary)
                {
                    if (codeGeneratorSettings.APISettings != null)
                    {
                        GenerateControllers(codeGeneratorSettings, sourceProductionContext, summary);
                    }
                }
            }
        }

        public static void GenerateControllers(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Entity entity)
        {
            if (entity.GenerateEntityHandlerRequest?.GenerateController != null)
            {
                WriteEntityHandlerController(codeGeneratorSettings, sourceProductionContext, entity);
            }

            if (entity.GenerateEntityGetterRequest?.GenerateController != null)
            {
                WriteEntityGetterController(codeGeneratorSettings, sourceProductionContext, entity);
            }

            if (entity.GenerateEntitiesGetterRequest?.GenerateController != null)
            {
                WriteEntitiesGetterController(codeGeneratorSettings, sourceProductionContext, entity);
            }
        }

        public static void GenerateControllers(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Summary summary)
        {
            if (summary.GenerateSummaryGetterRequest?.GenerateController != null)
            {
                WriteSummaryGetterController(codeGeneratorSettings, sourceProductionContext, summary);
            }

            if (summary.GenerateSummariesGetterRequest?.GenerateController != null)
            {
                WriteSummariesGetterController(codeGeneratorSettings, sourceProductionContext, summary);
            }
        }

        private static void WriteEntityHandlerController(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Entity entity)
        {
            var entityName = entity.Name;
            var className = $"{entityName}HandlerController";
            var controllerFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.APISettings?.ControllersNamespace,
                className,
                isPartial: true,
                inheritance: "ControllerBase");

            controllerFileWriter.WriteClassAttribute("ApiController");
            controllerFileWriter.WriteClassAttribute("Route(\"/api/[controller]/[action]\")");

            controllerFileWriter.WriteUsing("System");
            controllerFileWriter.WriteUsing("System.Threading.Tasks");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Authorization");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Mvc");
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.ServicesNamespace);
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.EntitiesNamespace);

            if (entity.GenerateEntityHandlerRequest.GenerateController.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in entity.GenerateEntityHandlerRequest.GenerateController.AdditionalUsings.Using)
                {
                    controllerFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (entity.GenerateEntityHandlerRequest.GenerateController.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in entity.GenerateEntityHandlerRequest.GenerateController.CustomAttributes.CustomAttribute)
                {
                    controllerFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            var serviceInterfaceType = string.IsNullOrEmpty(entity.GenerateEntityHandlerRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"I{entityName}HandlerService"
                : entity.GenerateEntityHandlerRequest.GenerateController.UseExistingInterfaceServiceName;

            var serviceInterfaceName = string.IsNullOrEmpty(entity.GenerateEntityHandlerRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"{entityName.FirstCharToLowerCase()}HandlerService"
                : $"{entity.GenerateEntityHandlerRequest.GenerateController.UseExistingInterfaceServiceName.Remove(0, 1).FirstCharToLowerCase()}HandlerService";

            controllerFileWriter.WriteDependencyInjection(serviceInterfaceType, serviceInterfaceName);

            var serviceMethodName = string.IsNullOrEmpty(entity.GenerateEntityHandlerRequest.GenerateController.UseExistingServiceMethodName)
                ? $"SaveAsync"
                : entity.GenerateEntityHandlerRequest.GenerateController.UseExistingServiceMethodName;

            var bodyLines = new List<string>();
            bodyLines.Add($"return Ok(await this.{serviceInterfaceName}.{serviceMethodName}(request, cancellationToken));");

            var attributes = new List<string>();
            attributes.Add("HttpPost");

            if (entity.GenerateEntityHandlerRequest.GenerateController.IsAuthenticationRequired)
            {
                if (entity.GenerateEntityHandlerRequest.GenerateController.Permissions != null)
                {
                    foreach (var permission in entity.GenerateEntityHandlerRequest.GenerateController.Permissions.Permission)
                    {
                        attributes.Add($"Authorize(Roles = \"{permission.Name}\")");
                    }
                }
                else
                {
                    attributes.Add($"Authorize()");
                }
            }

            controllerFileWriter.WriteMethod(
                "Post",
                returnType: "async Task<IActionResult>",
                parameters: $"[FromBody] {entityName}HandlerRequest request, CancellationToken cancellationToken",
                attributes: attributes,
                bodyLines: bodyLines);

            sourceProductionContext.WriteNewCSFile(className, controllerFileWriter);
        }

        private static void WriteEntityGetterController(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Entity entity)
        {
            var entityName = entity.Name;
            var className = $"{entityName}GetterController";
            var controllerFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.APISettings?.ControllersNamespace,
                className,
                isPartial: true,
                inheritance: "ControllerBase");

            controllerFileWriter.WriteClassAttribute("ApiController");
            controllerFileWriter.WriteClassAttribute("Route(\"/api/[controller]/[action]\")");

            controllerFileWriter.WriteUsing("System");
            controllerFileWriter.WriteUsing("System.Threading.Tasks");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Authorization");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Mvc");
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.ServicesNamespace);
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.EntitiesNamespace);

            if (entity.GenerateEntityGetterRequest.GenerateController.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in entity.GenerateEntityGetterRequest.GenerateController.AdditionalUsings.Using)
                {
                    controllerFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (entity.GenerateEntityGetterRequest.GenerateController.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in entity.GenerateEntityGetterRequest.GenerateController.CustomAttributes.CustomAttribute)
                {
                    controllerFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            var serviceInterfaceType = string.IsNullOrEmpty(entity.GenerateEntityGetterRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"I{entityName}GetterService"
                : entity.GenerateEntityGetterRequest.GenerateController.UseExistingInterfaceServiceName;

            var serviceInterfaceName = string.IsNullOrEmpty(entity.GenerateEntityGetterRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"{entityName.FirstCharToLowerCase()}GetterService"
                : $"{entity.GenerateEntityGetterRequest.GenerateController.UseExistingInterfaceServiceName.Remove(0, 1).FirstCharToLowerCase()}HandlerService";

            controllerFileWriter.WriteDependencyInjection(serviceInterfaceType, serviceInterfaceName);

            var serviceMethodName = string.IsNullOrEmpty(entity.GenerateEntityGetterRequest.GenerateController.UseExistingServiceMethodName)
                ? $"GetAsync"
                : entity.GenerateEntityGetterRequest.GenerateController.UseExistingServiceMethodName;

            var bodyLines = new List<string>();
            bodyLines.Add($"return Ok(await this.{serviceInterfaceName}.{serviceMethodName}(request, cancellationToken));");

            var attributes = new List<string>();
            attributes.Add("HttpGet");

            if (entity.GenerateEntityGetterRequest.GenerateController.IsAuthenticationRequired)
            {
                if (entity.GenerateEntityGetterRequest.GenerateController.Permissions != null)
                {
                    foreach (var permission in entity.GenerateEntityGetterRequest.GenerateController.Permissions.Permission)
                    {
                        attributes.Add($"Authorize(Roles = \"{permission.Name}\")");
                    }
                }
                else
                {
                    attributes.Add($"Authorize()");
                }
            }

            controllerFileWriter.WriteMethod(
                "Get",
                returnType: "async Task<IActionResult>",
                parameters: $"[FromQuery] {entityName}GetterRequest request, CancellationToken cancellationToken",
                attributes: attributes,
                bodyLines: bodyLines);

            sourceProductionContext.WriteNewCSFile(className, controllerFileWriter);
        }

        private static void WriteEntitiesGetterController(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Entity entity)
        {
            var entityName = entity.PluralName;
            var className = $"{entityName}GetterController";
            var controllerFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.APISettings?.ControllersNamespace,
                className,
                isPartial: true,
                inheritance: "ControllerBase");

            controllerFileWriter.WriteClassAttribute("ApiController");
            controllerFileWriter.WriteClassAttribute("Route(\"/api/[controller]/[action]\")");

            controllerFileWriter.WriteUsing("System");
            controllerFileWriter.WriteUsing("System.Threading.Tasks");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Authorization");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Mvc");
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.ServicesNamespace);
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.EntitiesNamespace);

            if (entity.GenerateEntitiesGetterRequest.GenerateController.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in entity.GenerateEntitiesGetterRequest.GenerateController.AdditionalUsings.Using)
                {
                    controllerFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (entity.GenerateEntitiesGetterRequest.GenerateController.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in entity.GenerateEntitiesGetterRequest.GenerateController.CustomAttributes.CustomAttribute)
                {
                    controllerFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            var serviceInterfaceType = string.IsNullOrEmpty(entity.GenerateEntitiesGetterRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"I{entityName}GetterService"
                : entity.GenerateEntitiesGetterRequest.GenerateController.UseExistingInterfaceServiceName;

            var serviceInterfaceName = string.IsNullOrEmpty(entity.GenerateEntitiesGetterRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"{entityName.FirstCharToLowerCase()}GetterService"
                : $"{entity.GenerateEntitiesGetterRequest.GenerateController.UseExistingInterfaceServiceName.Remove(0, 1).FirstCharToLowerCase()}HandlerService";

            controllerFileWriter.WriteDependencyInjection(serviceInterfaceType, serviceInterfaceName);

            var serviceMethodName = string.IsNullOrEmpty(entity.GenerateEntitiesGetterRequest.GenerateController.UseExistingServiceMethodName)
                ? $"GetAsync"
                : entity.GenerateEntitiesGetterRequest.GenerateController.UseExistingServiceMethodName;

            var bodyLines = new List<string>();
            bodyLines.Add($"return Ok(await this.{serviceInterfaceName}.{serviceMethodName}(request, cancellationToken));");

            var attributes = new List<string>();
            attributes.Add("HttpGet");

            if (entity.GenerateEntitiesGetterRequest.GenerateController.IsAuthenticationRequired)
            {
                if (entity.GenerateEntitiesGetterRequest.GenerateController.Permissions != null)
                {
                    foreach (var permission in entity.GenerateEntitiesGetterRequest.GenerateController.Permissions.Permission)
                    {
                        attributes.Add($"Authorize(Roles = \"{permission.Name}\")");
                    }
                }
                else
                {
                    attributes.Add($"Authorize()");
                }
            }

            controllerFileWriter.WriteMethod(
                "Get",
                returnType: "async Task<IActionResult>",
                parameters: $"[FromQuery] {entityName}GetterRequest request, CancellationToken cancellationToken",
                attributes: attributes,
                bodyLines: bodyLines);

            sourceProductionContext.WriteNewCSFile(className, controllerFileWriter);
        }

        private static void WriteSummaryGetterController(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Summary summary)
        {
            var summaryName = summary.Name;
            var className = $"{summaryName}GetterController";
            var controllerFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.APISettings?.ControllersNamespace,
                className,
                isPartial: true,
                inheritance: "ControllerBase");

            controllerFileWriter.WriteClassAttribute("ApiController");
            controllerFileWriter.WriteClassAttribute("Route(\"/api/[controller]/[action]\")");

            controllerFileWriter.WriteUsing("System");
            controllerFileWriter.WriteUsing("System.Threading.Tasks");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Authorization");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Mvc");
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.ServicesNamespace);
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.EntitiesNamespace);

            if (summary.GenerateSummaryGetterRequest.GenerateController.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in summary.GenerateSummaryGetterRequest.GenerateController.AdditionalUsings.Using)
                {
                    controllerFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (summary.GenerateSummaryGetterRequest.GenerateController.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in summary.GenerateSummaryGetterRequest.GenerateController.CustomAttributes.CustomAttribute)
                {
                    controllerFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            var serviceInterfaceType = string.IsNullOrEmpty(summary.GenerateSummaryGetterRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"I{summaryName}GetterService"
                : summary.GenerateSummaryGetterRequest.GenerateController.UseExistingInterfaceServiceName;

            var serviceInterfaceName = string.IsNullOrEmpty(summary.GenerateSummaryGetterRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"{summaryName.FirstCharToLowerCase()}GetterService"
                : $"{summary.GenerateSummaryGetterRequest.GenerateController.UseExistingInterfaceServiceName.Remove(0, 1).FirstCharToLowerCase()}HandlerService";

            controllerFileWriter.WriteDependencyInjection(serviceInterfaceType, serviceInterfaceName);

            var serviceMethodName = string.IsNullOrEmpty(summary.GenerateSummaryGetterRequest.GenerateController.UseExistingServiceMethodName)
                ? $"GetAsync"
                : summary.GenerateSummaryGetterRequest.GenerateController.UseExistingServiceMethodName;

            var bodyLines = new List<string>();
            bodyLines.Add($"return Ok(await this.{serviceInterfaceName}.{serviceMethodName}(request, cancellationToken));");

            var attributes = new List<string>();
            attributes.Add("HttpGet");

            if (summary.GenerateSummaryGetterRequest.GenerateController.IsAuthenticationRequired)
            {
                if (summary.GenerateSummaryGetterRequest.GenerateController.Permissions != null)
                {
                    foreach (var permission in summary.GenerateSummaryGetterRequest.GenerateController.Permissions.Permission)
                    {
                        attributes.Add($"Authorize(Roles = \"{permission.Name}\")");
                    }
                }
                else
                {
                    attributes.Add($"Authorize()");
                }
            }

            controllerFileWriter.WriteMethod(
                "Get",
                returnType: "async Task<IActionResult>",
                parameters: $"[FromQuery] {summaryName}GetterRequest request, CancellationToken cancellationToken",
                attributes: attributes,
                bodyLines: bodyLines);

            sourceProductionContext.WriteNewCSFile(className, controllerFileWriter);
        }

        private static void WriteSummariesGetterController(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Summary summary)
        {
            var summaryName = summary.PluralName;
            var className = $"{summaryName}GetterController";
            var controllerFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.APISettings?.ControllersNamespace,
                className,
                isPartial: true,
                inheritance: "ControllerBase");

            controllerFileWriter.WriteClassAttribute("ApiController");
            controllerFileWriter.WriteClassAttribute("Route(\"/api/[controller]/[action]\")");

            controllerFileWriter.WriteUsing("System");
            controllerFileWriter.WriteUsing("System.Threading.Tasks");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Authorization");
            controllerFileWriter.WriteUsing("Microsoft.AspNetCore.Mvc");
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.ServicesNamespace);
            controllerFileWriter.WriteUsing(codeGeneratorSettings.APISettings?.EntitiesNamespace);

            if (summary.GenerateSummariesGetterRequest.GenerateController.AdditionalUsings?.Using != null)
            {
                foreach (var customUsing in summary.GenerateSummariesGetterRequest.GenerateController.AdditionalUsings.Using)
                {
                    controllerFileWriter.WriteUsing(customUsing.Content);
                }
            }

            if (summary.GenerateSummariesGetterRequest.GenerateController.CustomAttributes?.CustomAttribute != null)
            {
                foreach (var classAttribute in summary.GenerateSummariesGetterRequest.GenerateController.CustomAttributes.CustomAttribute)
                {
                    controllerFileWriter.WriteClassAttribute(classAttribute.Attribute);
                }
            }

            var serviceInterfaceType = string.IsNullOrEmpty(summary.GenerateSummariesGetterRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"I{summaryName}GetterService"
                : summary.GenerateSummariesGetterRequest.GenerateController.UseExistingInterfaceServiceName;

            var serviceInterfaceName = string.IsNullOrEmpty(summary.GenerateSummariesGetterRequest.GenerateController.UseExistingInterfaceServiceName)
                ? $"{summaryName.FirstCharToLowerCase()}GetterService"
                : $"{summary.GenerateSummariesGetterRequest.GenerateController.UseExistingInterfaceServiceName.Remove(0, 1).FirstCharToLowerCase()}HandlerService";

            controllerFileWriter.WriteDependencyInjection(serviceInterfaceType, serviceInterfaceName);

            var serviceMethodName = string.IsNullOrEmpty(summary.GenerateSummariesGetterRequest.GenerateController.UseExistingServiceMethodName)
                ? $"GetAsync"
                : summary.GenerateSummariesGetterRequest.GenerateController.UseExistingServiceMethodName;

            var bodyLines = new List<string>();
            bodyLines.Add($"return Ok(await this.{serviceInterfaceName}.{serviceMethodName}(request, cancellationToken));");

            var attributes = new List<string>();
            attributes.Add("HttpGet");

            if (summary.GenerateSummariesGetterRequest.GenerateController.IsAuthenticationRequired)
            {
                if (summary.GenerateSummariesGetterRequest.GenerateController.Permissions != null)
                {
                    foreach (var permission in summary.GenerateSummariesGetterRequest.GenerateController.Permissions.Permission)
                    {
                        attributes.Add($"Authorize(Roles = \"{permission.Name}\")");
                    }
                }
                else
                {
                    attributes.Add($"Authorize()");
                }
            }

            controllerFileWriter.WriteMethod(
                "Get",
                returnType: "async Task<IActionResult>",
                parameters: $"[FromQuery] {summaryName}GetterRequest request, CancellationToken cancellationToken",
                attributes: attributes,
                bodyLines: bodyLines);

            sourceProductionContext.WriteNewCSFile(className, controllerFileWriter);
        }
    }
}
