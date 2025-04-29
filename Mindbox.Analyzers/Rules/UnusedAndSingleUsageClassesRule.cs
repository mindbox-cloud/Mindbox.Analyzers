using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MindboxAnalyzers.Rules;

public class UnusedAndSingleUsageClassesRule : AnalyzerRule, ISemanticModelAnalyzerRule
{
	private const string DiagnosticBaseUnusedId = "MB1050";
	private const string Title = "Class usage analysis";
	private const string UnusedMessageFormat = "Class '{0}' is never used or used only once or used only registered in DI but not used elsewhere";
	private const string Description = "Classes should be used in more than one place to justify their existence.";
	private const string Category = "Usage";

	private static readonly DiagnosticDescriptor _unusedRule;

	private static readonly HashSet<string> _excludedClassNames = new()
	{
		"Program",
		"Startup",
		"ServiceCollectionExtensions",
		"ApplicationBuilder",
		"WebApplication",
		"WebApplicationBuilder",
		"HostBuilder",
		"IHostBuilder",
		"IApplicationBuilder",
		"IWebHostBuilder"
	};

	static UnusedAndSingleUsageClassesRule()
	{
		_unusedRule = new DiagnosticDescriptor(
			DiagnosticBaseUnusedId,
			Title,
			UnusedMessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: Description);
	}

	public UnusedAndSingleUsageClassesRule() : base(
		DiagnosticBaseUnusedId,
		Title,
		UnusedMessageFormat,
		Description,
		Category)
	{
	}

	private enum ClassUsageState
	{
		Unused,
		OnlyDiUsage,
		SingleNonDiUsage,
		MultipleUsages
	}

	private class ClassInfo
	{
		public ClassUsageState UsageState { get; set; } = ClassUsageState.Unused;
		public Location DeclarationLocation { get; }

		public ClassInfo(Location declarationLocation)
		{
			DeclarationLocation = declarationLocation;
		}

		public bool TrackUsage(bool isDiUsage)
		{
			switch (UsageState)
			{
				case ClassUsageState.Unused:
					UsageState = isDiUsage ? ClassUsageState.OnlyDiUsage : ClassUsageState.SingleNonDiUsage;
					return true;

				case ClassUsageState.OnlyDiUsage:
					if (!isDiUsage)
					{
						UsageState = ClassUsageState.MultipleUsages;
						return false;
					}
					return true;

				case ClassUsageState.SingleNonDiUsage:
					UsageState = ClassUsageState.MultipleUsages;
					return false;

				default:
					return false;
			}
		}
	}

	public void AnalyzeModel(SemanticModel model, out ICollection<Diagnostic> foundProblems)
	{
		var problems = new List<Diagnostic>();
		var trackedClasses = new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default);

		var root = model.SyntaxTree.GetRoot();
		foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
		{
			var symbol = model.GetDeclaredSymbol(classDeclaration);
			if (symbol != null
				&& !symbol.GetAttributes().Any()
				&& !_excludedClassNames.Contains(symbol.Name)
				&& !symbol.Name.EndsWith("Extensions")
				&& !IsSystemOrConfigurationClass(symbol))
			{
				trackedClasses[symbol] = new ClassInfo(classDeclaration.GetLocation());
			}
		}

		if (!trackedClasses.Any())
		{
			foundProblems = problems;
			return;
		}

		foreach (var node in root.DescendantNodes())
		{
			if (!trackedClasses.Any())
				break;

			INamedTypeSymbol symbol = null;
			var isDiUsage = false;

			switch (node)
			{
				case InvocationExpressionSyntax invocation:
					if (IsDependencyInjectionRegistration(invocation, model, out var registeredType))
					{
						symbol = registeredType;
						isDiUsage = true;
					}
					break;

				case TypeSyntax typeSyntax:
					symbol = model.GetTypeInfo(typeSyntax).Type as INamedTypeSymbol;
					break;

				case ParameterSyntax parameter:
					symbol = model.GetTypeInfo(parameter.Type).Type as INamedTypeSymbol;
					break;

				case PropertyDeclarationSyntax property:
					symbol = model.GetTypeInfo(property.Type).Type as INamedTypeSymbol;
					break;

				case FieldDeclarationSyntax field:
					symbol = model.GetTypeInfo(field.Declaration.Type).Type as INamedTypeSymbol;
					break;

				case AttributeSyntax attribute:
					symbol = model.GetTypeInfo(attribute).Type as INamedTypeSymbol;
					break;
			}

			if (symbol == null || !trackedClasses.TryGetValue(symbol, out var classInfo))
				continue;

			if (!classInfo.TrackUsage(isDiUsage))
			{
				trackedClasses.Remove(symbol);
			}
		}

		foreach (var pair in trackedClasses)
		{
			var symbol = pair.Key;
			var classInfo = pair.Value;

			switch (classInfo.UsageState)
			{
				case ClassUsageState.SingleNonDiUsage:
				case ClassUsageState.OnlyDiUsage:
				case ClassUsageState.Unused:
					problems.Add(Diagnostic.Create(
						_unusedRule,
						classInfo.DeclarationLocation,
						symbol.Name));
					break;
				default:
					break;
			}
		}

		foundProblems = problems;
	}

	private static bool IsDependencyInjectionRegistration(
		InvocationExpressionSyntax invocation,
		SemanticModel semanticModel,
		out INamedTypeSymbol registeredType)
	{
		registeredType = null;

		if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
			return false;

		if (!IsServiceCollectionExtensionMethod(methodSymbol))
			return false;

		var methodName = methodSymbol.Name;
		if (!IsDiRegistrationMethodName(methodName))
			return false;

		if (invocation.ArgumentList.Arguments.Count == 0)
			return false;

		if (methodSymbol.TypeArguments.Length > 0)
		{
			registeredType = methodSymbol.TypeArguments[0] as INamedTypeSymbol;
			return registeredType != null;
		}

		if (invocation.ArgumentList.Arguments[0].Expression is not TypeOfExpressionSyntax typeOfExpr)
			return false;

		registeredType = semanticModel.GetTypeInfo(typeOfExpr.Type).Type as INamedTypeSymbol;
		return registeredType != null;
	}

	private static bool IsServiceCollectionExtensionMethod(IMethodSymbol methodSymbol)
	{
		if (!methodSymbol.IsExtensionMethod)
			return false;

		if (methodSymbol.ContainingType == null)
			return false;

		var parameters = methodSymbol.Parameters;
		if (parameters.Length == 0)
			return false;

		var firstParam = parameters[0];
		return firstParam.Type.ToString().Contains("IServiceCollection");
	}

	private static bool IsDiRegistrationMethodName(string methodName)
	{
		return methodName.StartsWith("Add") &&
			   (methodName.Contains("Scoped") ||
				methodName.Contains("Singleton") ||
				methodName.Contains("Transient") ||
				methodName == "AddService" ||
				methodName == "TryAddService");
	}

	private static bool IsSystemOrConfigurationClass(INamedTypeSymbol symbol)
	{
		var ns = symbol.ContainingNamespace?.ToString() ?? string.Empty;
		return ns.StartsWith("Microsoft.") ||
			   ns.StartsWith("System.") ||
			   ns.Contains(".Configuration") ||
			   ns.Contains(".Infrastructure") ||
			   ns.Contains(".Startup");
	}
}