// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.CodeGenerators.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Kangaroo.CodeGenerators.Writers;
    using Microsoft.CodeAnalysis;

    internal static class SourceProductionContextExtensions
    {
        public static void WriteNewCSFile(this SourceProductionContext sourceProductionContext, string fileNameWithoutExtension, CSFileWriter fileWriter)
        {
            var fileName = $"{fileNameWithoutExtension}.g.cs";
            var fileContent = fileWriter.GetFileContent();

            sourceProductionContext.AddSource(fileName, fileContent);
            Debug.WriteLine($"The {fileName} was auto generated with this content: " + fileContent);
        }
    }
}
