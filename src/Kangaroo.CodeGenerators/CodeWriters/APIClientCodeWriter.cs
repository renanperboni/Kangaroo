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
                        GenerateAPIClient(codeGeneratorSettings, sourceProductionContext, entity);
                    }
                }

                foreach (var summary in codeGenerator.Summary)
                {
                    if (codeGeneratorSettings.APIClientSettings != null)
                    {
                        GenerateAPIClient(codeGeneratorSettings, sourceProductionContext, summary);
                    }
                }
            }

            if (codeGeneratorSettings.APIClientSettings?.GenerateAuthAPIClient == true)
            {
                GenerateAuthAPIClient(codeGeneratorSettings, sourceProductionContext);
            }
        }

        private static void GenerateAPIClient(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Entity entity)
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

        private static void GenerateAPIClient(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Summary summary)
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

        private static void GenerateAuthAPIClient(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext)
        {
            var interfaceAnonymousName = $"IAuthAnonymousClient";
            var anonymousAPIClientFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.APIClientSettings?.APIClientNamespace,
                interfaceAnonymousName,
                isPartial: true,
                inheritance: "IKangarooAnonymousAPIClient");

            anonymousAPIClientFileWriter.WriteUsing("System");
            anonymousAPIClientFileWriter.WriteUsing("System.Threading.Tasks");
            anonymousAPIClientFileWriter.WriteUsing("Kangaroo.APIClient");
            anonymousAPIClientFileWriter.WriteUsing("Refit");
            anonymousAPIClientFileWriter.WriteUsing(codeGeneratorSettings.APIClientSettings.EntitiesNamespace);

            anonymousAPIClientFileWriter.WriteMethod(
                "InsertApplicationUserAsync",
                returnType: "Task<ApplicationUserInsertResponse>",
                parameters: "[Body] ApplicationUserInsertRequest request",
                attributes: new List<string>() { "Post(\"/api/Auth/InsertApplicationUser\")" });

            anonymousAPIClientFileWriter.WriteMethod(
                "LoginAsync",
                returnType: "Task<LoginResponse>",
                parameters: "[Body] LoginRequest request",
                attributes: new List<string>() { "Post(\"/api/Auth/Login\")" });

            sourceProductionContext.WriteNewCSFile(interfaceAnonymousName, anonymousAPIClientFileWriter);

            var interfaceAuthenticatedName = $"IAuthAuthenticatedClient";
            var authenticatedAPIClientFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.APIClientSettings?.APIClientNamespace,
                interfaceAuthenticatedName,
                isPartial: true,
                inheritance: "IKangarooAuthenticatedAPIClient");

            authenticatedAPIClientFileWriter.WriteUsing("System");
            authenticatedAPIClientFileWriter.WriteUsing("System.Threading.Tasks");
            authenticatedAPIClientFileWriter.WriteUsing("Kangaroo.APIClient");
            authenticatedAPIClientFileWriter.WriteUsing("Refit");
            authenticatedAPIClientFileWriter.WriteUsing(codeGeneratorSettings.APIClientSettings.EntitiesNamespace);

            authenticatedAPIClientFileWriter.WriteMethod(
                "RefreshTokenAsync",
                returnType: "Task<RefreshTokenResponse>",
                parameters: "[Body] RefreshTokenRequest request",
                attributes: new List<string>() { "Post(\"/api/Auth/RefreshToken\")", "Headers(\"Authorization: Bearer\")" });

            authenticatedAPIClientFileWriter.WriteMethod(
                "LogoutAsync",
                returnType: "Task<LogoutResponse>",
                parameters: "[Body] LogoutRequest request",
                attributes: new List<string>() { "Post(\"/api/Auth/Logout\")", "Headers(\"Authorization: Bearer\")" });

            authenticatedAPIClientFileWriter.WriteMethod(
                "ChangePasswordAsync",
                returnType: "Task<ChangePasswordResponse>",
                parameters: "[Body] ChangePasswordRequest request",
                attributes: new List<string>() { "Post(\"/api/Auth/ChangePassword\")", "Headers(\"Authorization: Bearer\")" });

            sourceProductionContext.WriteNewCSFile(interfaceAuthenticatedName, authenticatedAPIClientFileWriter);
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
