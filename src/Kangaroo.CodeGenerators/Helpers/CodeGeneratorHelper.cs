﻿// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.CodeGenerators.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Kangaroo.CodeGenerators.CodeWriters;
    using Kangaroo.CodeGenerators.Structure;
    using Microsoft.CodeAnalysis;

    internal static class CodeGeneratorHelper
    {
        public static void Generate(CodeGeneratorSettings codeGeneratorSettings, List<CodeGenerator> codeGenerators, SourceProductionContext sourceProductionContext)
        {
            if (codeGeneratorSettings.BackendEnumsSettings != null || codeGeneratorSettings.FrontendEnumsSettings != null)
            {
                EnumsCodeWriter.Generate(codeGeneratorSettings, codeGenerators, sourceProductionContext);
            }

            if (codeGeneratorSettings.DatabaseRepositoriesSettings != null)
            {
                DatabaseRepositoriesCodeWriter.Generate(codeGeneratorSettings, codeGenerators, sourceProductionContext);
            }

            if (codeGeneratorSettings.BackendEntititesSettings != null || codeGeneratorSettings.FrontendEntititesSettings != null)
            {
                EntitiesCodeWriter.Generate(codeGeneratorSettings, codeGenerators, sourceProductionContext);
            }

            if (codeGeneratorSettings.BackendCustomRequestsSettings != null || codeGeneratorSettings.FrontendCustomRequestsSettings != null)
            {
                CustomRequestsCodeWriter.Generate(codeGeneratorSettings, codeGenerators, sourceProductionContext);
            }

            if (codeGeneratorSettings.BackendCustomResponsesSettings != null || codeGeneratorSettings.FrontendCustomResponsesSettings != null)
            {
                CustomResponsesCodeWriter.Generate(codeGeneratorSettings, codeGenerators, sourceProductionContext);
            }

            if (codeGeneratorSettings.ServicesSettings != null)
            {
                ServicesCodeWriter.Generate(codeGeneratorSettings, codeGenerators, sourceProductionContext);
            }

            if (codeGeneratorSettings.APISettings != null)
            {
                APICodeWriter.Generate(codeGeneratorSettings, codeGenerators, sourceProductionContext);
            }

            if (codeGeneratorSettings.APIClientSettings != null)
            {
                APIClientCodeWriter.Generate(codeGeneratorSettings, codeGenerators, sourceProductionContext);
            }
        }
    }
}
