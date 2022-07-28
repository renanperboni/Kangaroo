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

            if (!string.IsNullOrEmpty(codeGeneratorSettings.ServicesSettings?.GenerateIdentityServiceBasedOnCustomUserClass))
            {
                GenerateIdentityService(codeGeneratorSettings, sourceProductionContext);
            }
        }

        private static void GenerateServices(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Entity entity)
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

        private static void GenerateServices(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext, Summary summary)
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

        private static void GenerateIdentityService(CodeGeneratorSettings codeGeneratorSettings, SourceProductionContext sourceProductionContext)
        {
            var interfaceName = $"IApplicationUserService";

            var interfaceServiceFileWriter = new CSFileWriter(
                CSFileWriterType.Interface,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                interfaceName,
                isPartial: true,
                inheritance: "ITransientService");

            interfaceServiceFileWriter.WriteUsing("System.Threading.Tasks");
            interfaceServiceFileWriter.WriteUsing("Kangaroo.Services");
            interfaceServiceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);

            interfaceServiceFileWriter.WriteMethod("InsertApplicationUserAsync", returnType: "Task<ApplicationUserInsertResponse>", parameters: "ApplicationUserInsertRequest request, CancellationToken cancellationToken = default");
            interfaceServiceFileWriter.WriteMethod("LoginAsync", returnType: "Task<LoginResponse>", parameters: "LoginRequest request, CancellationToken cancellationToken = default");
            interfaceServiceFileWriter.WriteMethod("RefreshTokenAsync", returnType: "Task<RefreshTokenResponse>", parameters: "RefreshTokenRequest request, CancellationToken cancellationToken = default");
            interfaceServiceFileWriter.WriteMethod("ChangePasswordAsync", returnType: "Task<ChangePasswordResponse>", parameters: "ChangePasswordRequest request, CancellationToken cancellationToken = default");

            sourceProductionContext.WriteNewCSFile(interfaceName, interfaceServiceFileWriter);

            var customUserClassName = codeGeneratorSettings.ServicesSettings?.GenerateIdentityServiceBasedOnCustomUserClass;
            var serviceName = $"ApplicationUserService";
            var serviceFileWriter = new CSFileWriter(
                CSFileWriterType.Class,
                codeGeneratorSettings.ServicesSettings?.ServicesNamespace,
                serviceName,
                isPartial: true,
                inheritance: "ServiceBase, IApplicationUserService");

            serviceFileWriter.WriteUsing("System");
            serviceFileWriter.WriteUsing("System.IdentityModel.Tokens.Jwt");
            serviceFileWriter.WriteUsing("System.Linq");
            serviceFileWriter.WriteUsing("System.Security.Claims");
            serviceFileWriter.WriteUsing("System.Security.Cryptography");
            serviceFileWriter.WriteUsing("System.Text");
            serviceFileWriter.WriteUsing("System.Threading.Tasks");
            serviceFileWriter.WriteUsing("AutoMapper");
            serviceFileWriter.WriteUsing("Kangaroo.Exceptions");
            serviceFileWriter.WriteUsing("Kangaroo.Services");
            serviceFileWriter.WriteUsing("Microsoft.AspNetCore.Identity");
            serviceFileWriter.WriteUsing("Microsoft.Extensions.Configuration");
            serviceFileWriter.WriteUsing("Microsoft.IdentityModel.Tokens");
            serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.DbContextNamespace);
            serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.DatabaseRepositoriesNamespace);
            serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.DatabaseEntitiesNamespace);
            serviceFileWriter.WriteUsing(codeGeneratorSettings.ServicesSettings?.EntitiesNamespace);

            serviceFileWriter.WriteDependencyInjection($"UserManager<{customUserClassName}>", "userManager");
            serviceFileWriter.WriteDependencyInjection($"SignInManager<{customUserClassName}>", "signInManager");
            serviceFileWriter.WriteDependencyInjection("IConfiguration", "configuration");
            serviceFileWriter.WriteDependencyInjection("ICurrentUserService", "currentUserService");

            var insertApplicationUserMethodLines = new List<string>();
            insertApplicationUserMethodLines.Add($"cancellationToken.ThrowIfCancellationRequested();");
            insertApplicationUserMethodLines.Add(string.Empty);
            insertApplicationUserMethodLines.Add($"var newApplicationUser = new {customUserClassName}()");
            insertApplicationUserMethodLines.Add("{");
            insertApplicationUserMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "UserName = request.Name,");
            insertApplicationUserMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "Email = request.Email,");
            insertApplicationUserMethodLines.Add("};");
            insertApplicationUserMethodLines.Add(string.Empty);
            insertApplicationUserMethodLines.Add("var result = await this.userManager.CreateAsync(newApplicationUser, request.Password);");
            insertApplicationUserMethodLines.Add(string.Empty);
            insertApplicationUserMethodLines.Add("if (!result.Succeeded)");
            insertApplicationUserMethodLines.Add("{");
            insertApplicationUserMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "var errors = string.Join(\", \", result.Errors.Select(x => x.Description));");
            insertApplicationUserMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "throw new KangarooException(null, errors);");
            insertApplicationUserMethodLines.Add("}");
            insertApplicationUserMethodLines.Add(string.Empty);
            insertApplicationUserMethodLines.Add("return new ApplicationUserInsertResponse() { WasUserInserted = true };");
            serviceFileWriter.WriteMethod(
                "InsertApplicationUserAsync",
                returnType: "async Task<ApplicationUserInsertResponse>",
                parameters: "ApplicationUserInsertRequest request, CancellationToken cancellationToken = default",
                bodyLines: insertApplicationUserMethodLines);

            var loginMethodLines = new List<string>();
            loginMethodLines.Add("cancellationToken.ThrowIfCancellationRequested();");
            loginMethodLines.Add(string.Empty);
            loginMethodLines.Add("var applicationUser = await this.userManager.FindByEmailAsync(request.Email);");
            loginMethodLines.Add(string.Empty);
            loginMethodLines.Add("if (applicationUser == null)");
            loginMethodLines.Add("{");
            loginMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "throw new KangarooException();");
            loginMethodLines.Add("}");
            loginMethodLines.Add(string.Empty);
            loginMethodLines.Add("var result = await this.signInManager.PasswordSignInAsync(request.Email, request.Password, false, false);");
            loginMethodLines.Add(string.Empty);
            loginMethodLines.Add("if (!result.Succeeded)");
            loginMethodLines.Add("{");
            loginMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "throw new KangarooException();");
            loginMethodLines.Add("}");
            loginMethodLines.Add(string.Empty);
            loginMethodLines.Add($"(string token, string refreshToken) = await this.GenerateTokenAsync(applicationUser);");
            loginMethodLines.Add(string.Empty);
            loginMethodLines.Add("return new LoginResponse()");
            loginMethodLines.Add("{");
            loginMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "Token = token,");
            loginMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "RefreshToken = refreshToken,");
            loginMethodLines.Add("};");
            serviceFileWriter.WriteMethod(
                "LoginAsync",
                returnType: "async Task<LoginResponse>",
                parameters: "LoginRequest request, CancellationToken cancellationToken = default",
                bodyLines: loginMethodLines);

            var refreshTokenMethodLines = new List<string>();
            refreshTokenMethodLines.Add("cancellationToken.ThrowIfCancellationRequested();");
            refreshTokenMethodLines.Add(string.Empty);
            refreshTokenMethodLines.Add("var currentUserId = this.currentUserService.CurrentUserId;");
            refreshTokenMethodLines.Add("var principal = this.GetPrincipalFromToken(request.Token);");
            refreshTokenMethodLines.Add(string.Empty);
            refreshTokenMethodLines.Add("var email = principal.Identity.Name;");
            refreshTokenMethodLines.Add("var applicationUser = await this.userManager.FindByEmailAsync(email);");
            refreshTokenMethodLines.Add(string.Empty);
            refreshTokenMethodLines.Add("if (applicationUser == null");
            refreshTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "|| applicationUser.Id != currentUserId");
            refreshTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "|| applicationUser.RefreshToken != request.RefreshToken");
            refreshTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "|| applicationUser.RefreshTokenExpirationTime <= DateTime.Now)");
            refreshTokenMethodLines.Add("{");
            refreshTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "throw new KangarooException();");
            refreshTokenMethodLines.Add("}");
            refreshTokenMethodLines.Add(string.Empty);
            refreshTokenMethodLines.Add($"(string token, string refreshToken) = await this.GenerateTokenAsync(applicationUser);");
            refreshTokenMethodLines.Add(string.Empty);
            refreshTokenMethodLines.Add("return new RefreshTokenResponse()");
            refreshTokenMethodLines.Add("{");
            refreshTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "Token = token,");
            refreshTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "RefreshToken = refreshToken,");
            refreshTokenMethodLines.Add("};");
            serviceFileWriter.WriteMethod(
                "RefreshTokenAsync",
                returnType: "async Task<RefreshTokenResponse>",
                parameters: "RefreshTokenRequest request, CancellationToken cancellationToken = default",
                bodyLines: refreshTokenMethodLines);

            var changePasswordMethodLines = new List<string>();
            changePasswordMethodLines.Add("cancellationToken.ThrowIfCancellationRequested();");
            changePasswordMethodLines.Add(string.Empty);
            changePasswordMethodLines.Add("var currentUserId = this.currentUserService.CurrentUserId;");
            changePasswordMethodLines.Add("var applicationUser = await this.userManager.FindByIdAsync(currentUserId);");
            changePasswordMethodLines.Add(string.Empty);
            changePasswordMethodLines.Add("var result = await this.userManager.ChangePasswordAsync(");
            changePasswordMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "applicationUser,");
            changePasswordMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "request.CurrentPassword,");
            changePasswordMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "request.NewPassword);");
            changePasswordMethodLines.Add(string.Empty);
            changePasswordMethodLines.Add("if (!result.Succeeded)");
            changePasswordMethodLines.Add("{");
            changePasswordMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "var errors = string.Join(\", \", result.Errors.Select(x => x.Description));");
            changePasswordMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "throw new KangarooException(null, errors);");
            changePasswordMethodLines.Add("}");
            changePasswordMethodLines.Add(string.Empty);
            changePasswordMethodLines.Add("return new ChangePasswordResponse()");
            changePasswordMethodLines.Add("{");
            changePasswordMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "WasPasswordChanged = result.Succeeded,");
            changePasswordMethodLines.Add("};");
            serviceFileWriter.WriteMethod(
                "ChangePasswordAsync",
                returnType: "async Task<ChangePasswordResponse>",
                parameters: "ChangePasswordRequest request, CancellationToken cancellationToken = default",
                bodyLines: changePasswordMethodLines);

            var generateTokenMethodLines = new List<string>();
            generateTokenMethodLines.Add("var validIssuer = this.configuration.GetSection(\"JwtIssuer\").Value;");
            generateTokenMethodLines.Add("var validAudience = this.configuration.GetSection(\"JwtAudience\").Value;");
            generateTokenMethodLines.Add("var expiryInMinutes = Convert.ToInt32(this.configuration.GetSection(\"JwtExpiryInMinutes\").Value);");
            generateTokenMethodLines.Add("var secretKey = this.configuration.GetSection(\"JwtSecurityKey\").Value;");
            generateTokenMethodLines.Add("var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));");
            generateTokenMethodLines.Add("var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);");
            generateTokenMethodLines.Add("var expiry = DateTime.Now.AddMinutes(expiryInMinutes);");
            generateTokenMethodLines.Add(string.Empty);
            generateTokenMethodLines.Add("var claims = new[]");
            generateTokenMethodLines.Add("{");
            generateTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "new Claim(ClaimTypes.Name, applicationUser.Email),");
            generateTokenMethodLines.Add("};");
            generateTokenMethodLines.Add(string.Empty);
            generateTokenMethodLines.Add("var token = new JwtSecurityToken(");
            generateTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "validIssuer,");
            generateTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "validAudience,");
            generateTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "claims,");
            generateTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "expires: expiry,");
            generateTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "signingCredentials: creds);");
            generateTokenMethodLines.Add(string.Empty);
            generateTokenMethodLines.Add("var refreshToken = this.GenerateRefreshToken();");
            generateTokenMethodLines.Add(string.Empty);
            generateTokenMethodLines.Add("applicationUser.RefreshToken = refreshToken;");
            generateTokenMethodLines.Add("applicationUser.RefreshTokenExpirationTime = expiry;");
            generateTokenMethodLines.Add("await this.userManager.UpdateAsync(applicationUser);");
            generateTokenMethodLines.Add(string.Empty);
            generateTokenMethodLines.Add("return (Token: new JwtSecurityTokenHandler().WriteToken(token), RefreshToken: refreshToken);");
            serviceFileWriter.WriteMethod(
                "GenerateTokenAsync",
                returnType: "async Task<(string Token, string RefreshToken)>",
                parameters: $"{customUserClassName} applicationUser",
                accessModifierType: CSFileWriterAccessModifierType.Private,
                bodyLines: generateTokenMethodLines);

            var generateRefreshTokenMethodLines = new List<string>();
            generateRefreshTokenMethodLines.Add("var randomNumber = new byte[32];");
            generateRefreshTokenMethodLines.Add("using var rng = RandomNumberGenerator.Create();");
            generateRefreshTokenMethodLines.Add("rng.GetBytes(randomNumber);");
            generateRefreshTokenMethodLines.Add("return Convert.ToBase64String(randomNumber);");
            serviceFileWriter.WriteMethod("GenerateRefreshToken", returnType: "string", accessModifierType: CSFileWriterAccessModifierType.Private, bodyLines: generateRefreshTokenMethodLines);

            var getPrincipalFromTokenMethodLines = new List<string>();
            getPrincipalFromTokenMethodLines.Add("var validIssuer = this.configuration.GetSection(\"JwtIssuer\").Value;");
            getPrincipalFromTokenMethodLines.Add("var validAudience = this.configuration.GetSection(\"JwtAudience\").Value;");
            getPrincipalFromTokenMethodLines.Add("var secretKey = this.configuration.GetSection(\"JwtSecurityKey\").Value;");
            getPrincipalFromTokenMethodLines.Add(string.Empty);
            getPrincipalFromTokenMethodLines.Add("var tokenValidationParameters = new TokenValidationParameters");
            getPrincipalFromTokenMethodLines.Add("{");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "ValidateAudience = true,");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "ValidateIssuer = true,");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "ValidateIssuerSigningKey = true,");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "ValidateLifetime = false,");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "ValidIssuer = validIssuer,");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "ValidAudience = validAudience,");
            getPrincipalFromTokenMethodLines.Add("};");
            getPrincipalFromTokenMethodLines.Add(string.Empty);
            getPrincipalFromTokenMethodLines.Add("var tokenHandler = new JwtSecurityTokenHandler();");
            getPrincipalFromTokenMethodLines.Add(string.Empty);
            getPrincipalFromTokenMethodLines.Add("var principal = tokenHandler.ValidateToken(");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "token,");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "tokenValidationParameters,");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "out SecurityToken securityToken);");
            getPrincipalFromTokenMethodLines.Add(string.Empty);
            getPrincipalFromTokenMethodLines.Add("if (securityToken is not JwtSecurityToken jwtSecurityToken");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "|| !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))");
            getPrincipalFromTokenMethodLines.Add("{");
            getPrincipalFromTokenMethodLines.Add(serviceFileWriter.GetWhiteSpace(1) + "throw new KangarooException();");
            getPrincipalFromTokenMethodLines.Add("}");
            getPrincipalFromTokenMethodLines.Add(string.Empty);
            getPrincipalFromTokenMethodLines.Add("return principal;");
            serviceFileWriter.WriteMethod(
                "GetPrincipalFromToken",
                returnType: "ClaimsPrincipal",
                parameters: "string token",
                accessModifierType: CSFileWriterAccessModifierType.Private,
                bodyLines: getPrincipalFromTokenMethodLines);

            sourceProductionContext.WriteNewCSFile(serviceName, serviceFileWriter);
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
