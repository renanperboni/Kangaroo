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

    internal static class APIClientCodeWriter
    {
        public static void Generate(CodeGeneratorSettings codeGeneratorSettings, List<CodeGenerator> codeGenerators, SourceProductionContext sourceProductionContext)
        {
            foreach (var codeGenerator in codeGenerators)
            {
                foreach (var entity in codeGenerator.Entity)
                {
                    if (codeGeneratorSettings.APIClientSettings != null)
                    {
                        GenerateAPIClients(codeGeneratorSettings, sourceProductionContext, entity);
                    }
                }

                foreach (var summary in codeGenerator.Summary)
                {
                    if (codeGeneratorSettings.APIClientSettings != null)
                    {
                        GenerateAPIClients(codeGeneratorSettings, sourceProductionContext, summary);
                    }
                }
            }
        }

        public static void GenerateAPIClients(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Entity entity)
        {
            if (entity.GenerateEntityHandlerRequest?.GenerateController?.GenerateAPIClient == true)
            {
                WriteEntityHandlerAPIClient(codeGeneratorSettings, sourceProductionContext, entity);
            }

            if (entity.GenerateEntityGetterRequest?.GenerateController?.GenerateAPIClient == true)
            {
                WriteGettersAPIClient(codeGeneratorSettings, sourceProductionContext, entity.Name, entity.GenerateEntityGetterRequest?.GenerateController?.IsAuthenticationRequired == true);
            }

            if (entity.GenerateEntitiesGetterRequest?.GenerateController?.GenerateAPIClient == true)
            {
                WriteGettersAPIClient(codeGeneratorSettings, sourceProductionContext, entity.PluralName, entity.GenerateEntitiesGetterRequest?.GenerateController?.IsAuthenticationRequired == true);
            }
        }

        public static void GenerateAPIClients(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Summary summary)
        {
            if (summary.GenerateSummaryGetterRequest?.GenerateController?.GenerateAPIClient == true)
            {
                WriteGettersAPIClient(codeGeneratorSettings, sourceProductionContext, summary.Name, summary.GenerateSummaryGetterRequest?.GenerateController?.IsAuthenticationRequired == true);
            }

            if (summary.GenerateSummariesGetterRequest?.GenerateController?.GenerateAPIClient == true)
            {
                WriteGettersAPIClient(codeGeneratorSettings, sourceProductionContext, summary.PluralName, summary.GenerateSummariesGetterRequest?.GenerateController?.IsAuthenticationRequired == true);
            }
        }

        private static void WriteEntityHandlerAPIClient(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            Entity entity)
        {
            var entityName = entity.Name;
            var interfaceName = $"I{entityName}HandlerClient";
            var inheritance = entity.GenerateEntityHandlerRequest?.GenerateController?.IsAuthenticationRequired == true
                ? "IKangarooAuthenticatedAPIClient"
                : "IKangarooAnonymousAPIClient";
            var apiClientFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.APIClientSettings?.APIClientNamespace,
                interfaceName,
                isPartial: true,
                inheritance: inheritance);

            apiClientFileWriter.WriteUsing("System");
            apiClientFileWriter.WriteUsing("System.Threading.Tasks");
            apiClientFileWriter.WriteUsing("Kangaroo.APIClient");
            apiClientFileWriter.WriteUsing("Refit");
            apiClientFileWriter.WriteUsing(codeGeneratorSettings.APIClientSettings.EntitiesNamespace);

            var attributes = new List<string>();
            attributes.Add($"Post(\"/api/{entityName}Handler/Post\")");

            if (entity.GenerateEntityHandlerRequest.GenerateController.IsAuthenticationRequired)
            {
                attributes.Add("Headers(\"Authorization: Bearer\")");
            }

            apiClientFileWriter.WriteMethod(
                "PostAsync",
                returnType: $"Task<{entityName}HandlerResponse>",
                parameters: $"[Body] {entityName}HandlerRequest request",
                attributes: attributes);

            sourceProductionContext.WriteNewCSFile(interfaceName, apiClientFileWriter);
        }

        private static void WriteGettersAPIClient(
            CodeGeneratorSettings codeGeneratorSettings,
            SourceProductionContext sourceProductionContext,
            string entityName,
            bool isAuthenticationRequired)
        {
            var interfaceName = $"I{entityName}GetterClient";
            var inheritance = isAuthenticationRequired
                ? "IKangarooAuthenticatedAPIClient"
                : "IKangarooAnonymousAPIClient";
            var apiClientFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.APIClientSettings?.APIClientNamespace,
                interfaceName,
                isPartial: true,
                inheritance: inheritance);

            apiClientFileWriter.WriteUsing("System");
            apiClientFileWriter.WriteUsing("System.Threading.Tasks");
            apiClientFileWriter.WriteUsing("Kangaroo.APIClient");
            apiClientFileWriter.WriteUsing("Refit");
            apiClientFileWriter.WriteUsing(codeGeneratorSettings.APIClientSettings.EntitiesNamespace);

            var attributes = new List<string>();
            attributes.Add($"Get(\"/api/{entityName}Getter/Get\")");

            if (isAuthenticationRequired)
            {
                attributes.Add("Headers(\"Authorization: Bearer\")");
            }

            apiClientFileWriter.WriteMethod(
                "GetAsync",
                returnType: $"Task<{entityName}GetterResponse>",
                parameters: $"[Query] {entityName}GetterRequest request",
                attributes: attributes);

            sourceProductionContext.WriteNewCSFile(interfaceName, apiClientFileWriter);
        }
    }
}
