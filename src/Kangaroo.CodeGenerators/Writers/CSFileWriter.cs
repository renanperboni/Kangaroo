// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.CodeGenerators.Writers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Kangaroo.CodeGenerators.Extensions;

    internal class CSFileWriter
    {
        private readonly CSFileWriterType fileWriterType;
        private readonly string fileNamespace;
        private readonly string name;
        private readonly bool isPartial;
        private readonly bool isAbstract;
        private readonly List<string> usingNamespaces = new List<string>();
        private readonly List<string> classAttributes = new List<string>();
        private readonly List<string> constructorAdditionalBodyLines = new List<string>();
        private readonly List<DependencyInjection> dependencyInjections = new List<DependencyInjection>();
        private readonly List<Field> fields = new List<Field>();
        private readonly List<Property> properties = new List<Property>();
        private readonly List<EnumField> enumFields = new List<EnumField>();
        private readonly List<Method> methods = new List<Method>();
        private bool wasNotifyPropertyChangedStructureAdded;
        private string inheritance;

        public CSFileWriter(CSFileWriterType fileWriterType, string fileNamespace, string name, bool isPartial = false, bool isAbstract = false, string inheritance = "")
        {
            this.fileWriterType = fileWriterType;
            this.fileNamespace = fileNamespace;
            this.name = name;
            this.isPartial = isPartial;
            this.isAbstract = isAbstract;
            this.inheritance = inheritance;
        }

        public void WriteUsing(string usingNamespace)
        {
            if (string.IsNullOrEmpty(usingNamespace))
            {
                return;
            }

            this.usingNamespaces.Add($"{this.GetWhiteSpace()}using {usingNamespace};");
        }

        public void WriteClassAttribute(string attribute)
        {
            if (string.IsNullOrEmpty(attribute))
            {
                return;
            }

            this.classAttributes.Add($"{this.GetWhiteSpace()}[{attribute}]");
        }

        public void WriteConstructorAdditionalBodyLine(string bodyLine)
        {
            if (string.IsNullOrEmpty(bodyLine))
            {
                return;
            }

            this.constructorAdditionalBodyLines.Add($"{this.GetWhiteSpace(2)}{bodyLine}");
        }

        public void WriteDependencyInjection(string type, string name, CSFileWriterAccessModifierType accessModifierType = CSFileWriterAccessModifierType.Private, bool shouldSendToConstructorBase = false)
        {
            this.dependencyInjections.Add(new DependencyInjection()
            {
                AccessModifierType = accessModifierType,
                Type = type,
                Name = name,
                ShouldSendToConstructorBase = shouldSendToConstructorBase,
            });
        }

        public void WriteField(string type, string name, CSFileWriterAccessModifierType accessModifierType = CSFileWriterAccessModifierType.Private, string value = "", bool isReadOnly = false)
        {
            this.fields.Add(new Field()
            {
                AccessModifierType = accessModifierType,
                IsReadOnly = isReadOnly,
                Type = type,
                Name = name,
                Value = value,
            });
        }

        public void WriteProperty(string type, string name, string value = "", bool isFullProperty = true, bool isVirtual = false, bool hasNotifyPropertyChanged = false, List<string> attributes = null)
        {
            if (attributes == null)
            {
                attributes = new List<string>();
            }

            this.properties.Add(new Property()
            {
                IsFullProperty = isFullProperty,
                IsVirtual = isVirtual,
                Type = type,
                Name = name,
                Value = value,
                HasNotifyPropertyChanged = hasNotifyPropertyChanged,
                Attributes = attributes,
            });

            if (isFullProperty && !string.IsNullOrEmpty(name))
            {
                this.WriteField(type, name.FirstCharToLowerCase(), value: value, isReadOnly: false);
            }

            if (hasNotifyPropertyChanged)
            {
                this.AddNotifyPropertyChangedStructure();
            }
        }

        public void WriteEnumField(string name, string value = "")
        {
            this.enumFields.Add(new EnumField()
            {
                Name = name,
                Value = value,
            });
        }

        public void WriteMethod(string name, string returnType = "", string parameters = "", List<string> bodyLines = null, CSFileWriterAccessModifierType accessModifierType = CSFileWriterAccessModifierType.Public, bool isPartial = false, bool isVirtual = false, bool isOverride = false)
        {
            if (bodyLines == null)
            {
                bodyLines = new List<string>();
            }

            this.methods.Add(new Method()
            {
                AccessModifierType = accessModifierType,
                IsPartial = isPartial,
                IsVirtual = isVirtual,
                IsOverride = isOverride,
                ReturnType = returnType,
                Name = name,
                Parameters = parameters,
                BodyLines = bodyLines,
            });
        }

        public string GetWhiteSpace(int times = 1)
        {
            var space = string.Empty;

            for (int i = 0; i < times; i++)
            {
                space += "    ";
            }

            return space;
        }

        public string GetFileContent()
        {
            var fileStringBuilder = new StringBuilder();

            this.WriteFileHeader(fileStringBuilder);
            this.OpenFileNamespace(fileStringBuilder);
            this.WriteUsings(fileStringBuilder);
            this.WriteClassAttributes(fileStringBuilder);
            this.OpenFileBody(fileStringBuilder);

            this.WriteDependencyInjectionFields(fileStringBuilder);
            this.WriteFields(fileStringBuilder);
            this.WriteConstructor(fileStringBuilder);
            this.WriteProperties(fileStringBuilder);
            this.WriteEnumFields(fileStringBuilder);
            this.WriteMethods(fileStringBuilder);

            this.CloseFileBody(fileStringBuilder);
            this.CloseFileNamespace(fileStringBuilder);

            return fileStringBuilder.ToString();
        }

        private void AddNotifyPropertyChangedStructure()
        {
            if (!this.wasNotifyPropertyChangedStructureAdded)
            {
                this.wasNotifyPropertyChangedStructureAdded = true;

                if (string.IsNullOrEmpty(this.inheritance))
                {
                    this.inheritance = "INotifyPropertyChanged";
                }
                else
                {
                    this.inheritance += ", INotifyPropertyChanged";
                }

                this.WriteField("event PropertyChangedEventHandler?", "PropertyChanged", CSFileWriterAccessModifierType.Public);

                this.WriteMethod("OnPropertyChanged", parameters: "[CallerMemberName] string propertyName = null", isPartial: true);
                this.WriteMethod("OnPropertyChanging", parameters: "object oldValue, object newValue, [CallerMemberName] string propertyName = null", isPartial: true);

                var notifyPropertyChangedBodyLines = new List<string>();

                notifyPropertyChangedBodyLines.Add("this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));");
                notifyPropertyChangedBodyLines.Add("this.OnPropertyChanged(propertyName);");

                this.WriteMethod("NotifyPropertyChanged", parameters: "[CallerMemberName] string propertyName = null", accessModifierType: CSFileWriterAccessModifierType.Private, bodyLines: notifyPropertyChangedBodyLines);
            }
        }

        private void WriteFileHeader(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(@"// <auto-generated>");
            stringBuilder.AppendLine();
        }

        private void OpenFileNamespace(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine($"namespace {this.fileNamespace}");
            stringBuilder.AppendLine(@"{");
        }

        private void CloseFileNamespace(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(@"}");
        }

        private void WriteUsings(StringBuilder stringBuilder)
        {
            foreach (var usingNamespace in this.usingNamespaces.OrderBy(x => x.StartsWith("System.")))
            {
                stringBuilder.AppendLine(usingNamespace);
            }

            if (this.usingNamespaces.Any())
            {
                stringBuilder.AppendLine();
            }
        }

        private void WriteClassAttributes(StringBuilder stringBuilder)
        {
            foreach (var classAttribute in this.classAttributes)
            {
                stringBuilder.AppendLine(classAttribute);
            }

            if (this.classAttributes.Any())
            {
                stringBuilder.AppendLine();
            }
        }

        private void OpenFileBody(StringBuilder stringBuilder)
        {
            var bodyStatement = string.Empty;

            var partial = this.isPartial ? " partial" : string.Empty;

            var abstractStatement = this.isAbstract ? " abstract" : string.Empty;

            switch (this.fileWriterType)
            {
                case CSFileWriterType.Class:
                    bodyStatement = $"{this.GetWhiteSpace()}public{partial}{abstractStatement} class {this.name}";
                    break;
                case CSFileWriterType.Interface:
                    bodyStatement = $"{this.GetWhiteSpace()}public{partial} interface {this.name}";
                    break;
                case CSFileWriterType.Enum:
                    bodyStatement = $"{this.GetWhiteSpace()}public enum {this.name}";
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(this.inheritance))
            {
                bodyStatement += $" : {this.inheritance}";
            }

            stringBuilder.AppendLine(bodyStatement);
            stringBuilder.AppendLine(this.GetWhiteSpace() + @"{");
        }

        private void CloseFileBody(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(this.GetWhiteSpace() + @"}");
        }

        private void WriteDependencyInjectionFields(StringBuilder stringBuilder)
        {
            foreach (var dependencyInjection in this.dependencyInjections.Where(x => !x.ShouldSendToConstructorBase))
            {
                stringBuilder.AppendLine($"{this.GetWhiteSpace(2)}{this.GetAccessModifierType(dependencyInjection.AccessModifierType)}readonly {dependencyInjection.Type} {dependencyInjection.Name};");
            }

            if (this.dependencyInjections.Any())
            {
                stringBuilder.AppendLine();
            }
        }

        private void WriteFields(StringBuilder stringBuilder)
        {
            foreach (var field in this.fields.OrderByDescending(x => x.IsReadOnly).ThenBy(x => (int)x.AccessModifierType))
            {
                var readOnlyStatement = field.IsReadOnly ? "readonly" : string.Empty;
                var fieldStatement = $"{this.GetWhiteSpace(2)}{this.GetAccessModifierType(field.AccessModifierType)}{readOnlyStatement} {field.Type} {field.Name}";

                if (string.IsNullOrEmpty(field.Value))
                {
                    fieldStatement += ";";
                }
                else
                {
                    fieldStatement += $" = {field.Value};";
                }

                stringBuilder.AppendLine(fieldStatement);
            }

            if (this.fields.Any())
            {
                stringBuilder.AppendLine();
            }
        }

        private void WriteConstructor(StringBuilder stringBuilder)
        {
            if (this.dependencyInjections.Any())
            {
                stringBuilder.AppendLine($"{this.GetWhiteSpace(2)}public {this.name}(");

                var count = 0;

                foreach (var dependencyInjection in this.dependencyInjections)
                {
                    count++;

                    var dependencyInjectionStatement = $"{this.GetWhiteSpace(3)}{dependencyInjection.Type} {dependencyInjection.Name}";

                    if (count == this.dependencyInjections.Count())
                    {
                        dependencyInjectionStatement += ")";
                    }
                    else
                    {
                        dependencyInjectionStatement += ",";
                    }

                    stringBuilder.AppendLine(dependencyInjectionStatement);
                }

                if (this.dependencyInjections.Any(x => x.ShouldSendToConstructorBase))
                {
                    count = 0;
                    var dependencyInjectionBaseStatement = string.Empty;
                    var constructorBaseDependencyInjections = this.dependencyInjections.Where(x => x.ShouldSendToConstructorBase);

                    foreach (var dependencyInjection in constructorBaseDependencyInjections)
                    {
                        count++;

                        dependencyInjectionBaseStatement += dependencyInjection.Name;

                        if (count < constructorBaseDependencyInjections.Count())
                        {
                            dependencyInjectionBaseStatement += ", ";
                        }
                    }

                    stringBuilder.AppendLine($"{this.GetWhiteSpace(3)}: base({dependencyInjectionBaseStatement})");
                }

                stringBuilder.AppendLine(this.GetWhiteSpace(2) + @"{");

                foreach (var dependencyInjection in this.dependencyInjections.Where(x => !x.ShouldSendToConstructorBase))
                {
                    stringBuilder.AppendLine($"{this.GetWhiteSpace(3)}this.{dependencyInjection.Name} = {dependencyInjection.Name};");
                }

                foreach (var constructorAdditionalBodyLine in this.constructorAdditionalBodyLines)
                {
                    stringBuilder.AppendLine(constructorAdditionalBodyLine);
                }

                stringBuilder.AppendLine(this.GetWhiteSpace(2) + @"}");

                stringBuilder.AppendLine();
            }
            else if (this.constructorAdditionalBodyLines.Any())
            {
                stringBuilder.AppendLine($"{this.GetWhiteSpace(2)}public {this.name}()");

                stringBuilder.AppendLine(this.GetWhiteSpace(2) + @"{");

                foreach (var constructorAdditionalBodyLine in this.constructorAdditionalBodyLines)
                {
                    stringBuilder.AppendLine(constructorAdditionalBodyLine);
                }

                stringBuilder.AppendLine(this.GetWhiteSpace(2) + @"}");

                stringBuilder.AppendLine();
            }
        }

        private void WriteProperties(StringBuilder stringBuilder)
        {
            var count = 0;

            foreach (var property in this.properties.OrderBy(x => (int)x.AccessModifierType))
            {
                count++;

                foreach (var attribute in property.Attributes)
                {
                    stringBuilder.AppendLine($"{this.GetWhiteSpace(2)}[{attribute}]");
                }

                var virtualStatement = property.IsVirtual ? "virtual " : string.Empty;

                if (property.IsFullProperty)
                {
                    stringBuilder.AppendLine($"{this.GetWhiteSpace(2)}{this.GetAccessModifierType(property.AccessModifierType)}{virtualStatement}{property.Type} {property.Name}");
                    stringBuilder.AppendLine($"{this.GetWhiteSpace(3)}get => this.{property.Name.FirstCharToLowerCase()};");

                    if (property.HasNotifyPropertyChanged)
                    {
                        stringBuilder.AppendLine($"{this.GetWhiteSpace(3)}set");
                        stringBuilder.AppendLine(this.GetWhiteSpace(3) + @"{");

                        stringBuilder.AppendLine(this.GetWhiteSpace(4) + $"if (this.{property.Name.FirstCharToLowerCase()} != value)");
                        stringBuilder.AppendLine(this.GetWhiteSpace(4) + @"{");

                        stringBuilder.AppendLine(this.GetWhiteSpace(5) + $"this.OnPropertyChanging(this.{property.Name.FirstCharToLowerCase()}, value);");
                        stringBuilder.AppendLine(this.GetWhiteSpace(5) + $"this.{property.Name.FirstCharToLowerCase()} = value;");
                        stringBuilder.AppendLine(this.GetWhiteSpace(5) + $"this.NotifyPropertyChanged();");

                        stringBuilder.AppendLine(this.GetWhiteSpace(4) + @"}");

                        stringBuilder.AppendLine(this.GetWhiteSpace(3) + @"}");
                    }
                    else
                    {
                        stringBuilder.AppendLine($"{this.GetWhiteSpace(3)}set => this.{property.Name.FirstCharToLowerCase()} = value;");
                    }
                }
                else
                {
                    var propertyStatement = $"{this.GetWhiteSpace(2)}public {virtualStatement}{property.Type} {property.Name}" + @" { get; set; }";

                    if (!string.IsNullOrEmpty(property.Value))
                    {
                        propertyStatement += $" = {property.Value};";
                    }

                    stringBuilder.AppendLine(propertyStatement);
                }

                if (count < this.properties.Count() || this.methods.Any())
                {
                    stringBuilder.AppendLine();
                }
            }
        }

        private void WriteEnumFields(StringBuilder stringBuilder)
        {
            var count = 0;

            foreach (var enumField in this.enumFields.OrderBy(x => x.IntValue).ThenBy(x => x.Name))
            {
                count++;

                var enumFieldStatement = $"{this.GetWhiteSpace(2)}{enumField.Name}";

                if (!string.IsNullOrEmpty(enumField.Value))
                {
                    enumFieldStatement += $" = {enumField.Value}";
                }

                if (count < this.enumFields.Count())
                {
                    enumFieldStatement += ",";
                }

                stringBuilder.AppendLine(enumFieldStatement);
            }
        }

        private void WriteMethods(StringBuilder stringBuilder)
        {
            var count = 0;

            foreach (var method in this.methods.OrderBy(x => (int)x.AccessModifierType))
            {
                count++;

                var methodStatement = this.GetWhiteSpace(2);

                if (!method.IsPartial)
                {
                    methodStatement += this.GetAccessModifierType(method.AccessModifierType);
                }

                if (method.IsOverride)
                {
                    methodStatement += "override ";
                }

                methodStatement += method.IsPartial ? "partial " : string.Empty;
                methodStatement += method.IsVirtual ? "virtual " : string.Empty;
                methodStatement += string.IsNullOrEmpty(method.ReturnType) ? "void " : $"{method.ReturnType} ";
                methodStatement += $"{method.Name}(";
                methodStatement += string.IsNullOrEmpty(method.Parameters) ? string.Empty : method.Parameters;
                methodStatement += @")";

                if (method.IsPartial)
                {
                    methodStatement += @";";
                }

                stringBuilder.AppendLine(methodStatement);

                if (!method.IsPartial)
                {
                    stringBuilder.AppendLine(this.GetWhiteSpace(2) + @"{");

                    foreach (var bodyLine in method.BodyLines)
                    {
                        stringBuilder.AppendLine(this.GetWhiteSpace(3) + bodyLine);
                    }

                    stringBuilder.AppendLine(this.GetWhiteSpace(2) + @"}");
                }

                if (count < this.methods.Count())
                {
                    stringBuilder.AppendLine();
                }
            }
        }

        private string GetAccessModifierType(CSFileWriterAccessModifierType accessModifierType)
        {
            switch (accessModifierType)
            {
                case CSFileWriterAccessModifierType.Public:
                    return "public ";
                case CSFileWriterAccessModifierType.Protected:
                    return "protected ";
                case CSFileWriterAccessModifierType.Private:
                    return "private ";
                default:
                    throw new NotImplementedException();
            }
        }

        private class DependencyInjection
        {
            public CSFileWriterAccessModifierType AccessModifierType { get; set; }

            public string Type { get; set; }

            public string Name { get; set; }

            public bool ShouldSendToConstructorBase { get; set; }
        }

        private class Field
        {
            public CSFileWriterAccessModifierType AccessModifierType { get; set; }

            public bool IsReadOnly { get; set; }

            public string Type { get; set; }

            public string Name { get; set; }

            public string Value { get; set; }
        }

        private class Property
        {
            public CSFileWriterAccessModifierType AccessModifierType { get; set; }

            public bool IsFullProperty { get; set; }

            public bool IsVirtual { get; set; }

            public string Type { get; set; }

            public string Name { get; set; }

            public string Value { get; set; }

            public bool HasNotifyPropertyChanged { get; set; }

            public List<string> Attributes { get; set; } = new List<string>();
        }

        private class EnumField
        {
            public string Name { get; set; }

            public string Value { get; set; }

            public int IntValue
            {
                get
                {
                    if (int.TryParse(this.Value, out var intValue))
                    {
                        return intValue;
                    }

                    return default;
                }
            }
        }

        private class Method
        {
            public CSFileWriterAccessModifierType AccessModifierType { get; set; }

            public bool IsPartial { get; set; }

            public bool IsVirtual { get; set; }

            public bool IsOverride { get; set; }

            public string ReturnType { get; set; }

            public string Name { get; set; }

            public string Parameters { get; set; }

            public List<string> BodyLines { get; set; } = new List<string>();
        }
    }
}
