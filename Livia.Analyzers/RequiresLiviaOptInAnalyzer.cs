using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Livia.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RequiresLiviaOptInAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeMetadataName =
        "Livia.Attributes.RequiresLiviaOptInAttribute";

    private static readonly DiagnosticDescriptor Rule = new(
        id: "LIVIA001",
        title: "Livia API requires explicit opt-in",
        messageFormat:
            "The API '{0}' requires explicit opt-in. Set '{1}' to 'true' in the project file.",
        category: "Livia",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "Some Livia APIs require explicit developer opt-in through an MSBuild property.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterOperationAction(
            AnalyzeInvocation,
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;

        foreach (var attribute in method.GetAttributes())
        {
            if (!string.Equals(
                    attribute.AttributeClass?.ToDisplayString(),
                    AttributeMetadataName,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length == 0)
            {
                return;
            }

            var optIn = attribute.ConstructorArguments[0];

            if (optIn.Value is not int optInValue ||
                optIn.Type is not INamedTypeSymbol enumType)
            {
                return;
            }

            var enumMember = enumType
                .GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(field =>
                    field.HasConstantValue &&
                    field.ConstantValue is int value &&
                    value == optInValue);

            if (enumMember is null)
            {
                return;
            }

            var propertyName = $"LiviaEnable{enumMember.Name}";

            if (propertyName is null)
            {
                return;
            }

            if (IsOptedIn(context, propertyName))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rule,
                    invocation.Syntax.GetLocation(),
                    method.Name,
                    propertyName));

            return;
        }
    }

    private static bool IsOptedIn(
        OperationAnalysisContext context,
        string propertyName)
    {
        var options =
            context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

        return options.TryGetValue(
                   $"build_property.{propertyName}",
                   out var value)
               && string.Equals(
                   value,
                   "true",
                   StringComparison.OrdinalIgnoreCase);
    }
}