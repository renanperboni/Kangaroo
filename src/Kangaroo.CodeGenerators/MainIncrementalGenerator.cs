// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.CodeGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Kangaroo.CodeGenerators.Helpers;
    using Kangaroo.CodeGenerators.Structure;
    using Microsoft.CodeAnalysis;

    [Generator]
    internal class MainIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            /*#if DEBUG
                        if (!Debugger.IsAttached)
                        {
                            Debugger.Launch();
                        }
            #endif*/

            IncrementalValuesProvider<AdditionalText> providerSettingsAdditionalTextList = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".kcgs.xml"));
            IncrementalValuesProvider<(string SettingsFileName, string SettingsContent)> providerSettingsFiles = providerSettingsAdditionalTextList.Select((text, cancellationToken) =>
                (FileName: Path.GetFileName(text.Path), Content: text.GetText(cancellationToken)?.ToString()));

            IncrementalValuesProvider<AdditionalText> providerKCGAdditionalTextList = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".kcg.xml"));
            IncrementalValuesProvider<(string KCGFileName, string KCGContent)> providerKCGFiles = providerKCGAdditionalTextList.Select((text, cancellationToken) =>
                (FileName: Path.GetFileName(text.Path), Content: text.GetText(cancellationToken)?.ToString()));

            var combinedList = providerKCGFiles.Collect().Combine(providerSettingsFiles.Collect());

            context.RegisterSourceOutput(combinedList, (sourceProductionContext, combinedFiles) =>
            {
                var settingsFiles = combinedFiles.Right;
                var kcgFiles = combinedFiles.Left;

                if (!settingsFiles.Any())
                {
                    throw new Exception("You must have a settings file.");
                }

                if (settingsFiles.Count() > 1)
                {
                    throw new Exception("You cannot have more than one settings file.");
                }

                var codeGeneratorSettings = (CodeGeneratorSettings)new XmlSerializer(typeof(CodeGeneratorSettings)).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(settingsFiles.FirstOrDefault().SettingsContent)));

                var codeGenerators = new List<CodeGenerator>();

                foreach (var kcgFile in kcgFiles)
                {
                    codeGenerators.Add((CodeGenerator)new XmlSerializer(typeof(CodeGenerator)).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(kcgFile.KCGContent))));
                }

                CodeGeneratorHelper.Generate(codeGeneratorSettings, codeGenerators, sourceProductionContext);
            });
        }
    }
}
