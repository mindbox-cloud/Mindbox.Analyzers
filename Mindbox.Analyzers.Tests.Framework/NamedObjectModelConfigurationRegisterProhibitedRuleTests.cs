using System.Reflection;
using Itc.Commons.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MindboxAnalyzers.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LocalizableString = Mindbox.I18n.Abstractions.LocalizableString;

namespace MindboxAnalyzers.Tests;

[TestClass]
public class NamedObjectModelConfigurationRegisterProhibitedRuleTests
{
	private readonly NamedObjectModelConfigurationRegisterProhibitedRule _nomcRule = new();

	private const string NomcCodeUsings = @"using Itc.Commons; using Itc.Commons.Model;";

	private const string NamedObjectsDefinition = @"public class TestNamedObject : NamedObject
{
    public TestNamedObject(string systemName) 
        : base(systemName, null, null)
    {
    }
}

public class TestNamedObjectComponent : NamedObjectComponent<TestNamedObject>
{
    public TestNamedObjectComponent()
    {
        TestNC1 = Add(new TestNamedObject(""TestNC1""));
        TestNC2 = Add(new TestNamedObject(""TestNC2""));
        TestNC3 = Add(new TestNamedObject(""TestNC3""));
    }
    
    public TestNamedObject TestNC1 { get; }
    public TestNamedObject TestNC2 { get; }
    public TestNamedObject TestNC3 { get; }
}
";

	[TestMethod]
	public void NOMC_NoCalls_ProducesNoProblems()
	{
		const string givenSource = $@"{NomcCodeUsings}

bool entryPoint = false; // Program does not contain a static 'Main' method suitable for an entry point

public class TstModelPart : Module
{{
    protected override void RegisterDependencies(ModuleDependenciesConfiguration configuration)
    {{
        // No NOMC.Register calls!
    }}
}}

{NamedObjectsDefinition}";

		var semanticModel = GetSemanticModelFromSourceCode("Program.cs", givenSource);
		_nomcRule.AnalyzeModel(semanticModel, out var problems);

		Assert.AreEqual(0, problems.Count);
	}

	[TestMethod]
	public void NOMC_OneSingleLineCall_ProducesOneProblem()
	{
		const string givenSource = $@"{NomcCodeUsings}

bool entryPoint = false; // Program does not contain a static 'Main' method suitable for an entry point

public class TstModelPart : Module
{{
    protected override void RegisterDependencies(ModuleDependenciesConfiguration configuration)
    {{
        Get<INamedObjectModelConfiguration>().Register<TestNamedObject, TestNamedObjectComponent>();
    }}
}}

{NamedObjectsDefinition}";

		const string filename = "Program.cs";

		var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
		_nomcRule.AnalyzeModel(semanticModel, out var problems);

		Assert.AreEqual(1, problems.Count);
		var problem = problems.Single();
		Assert.AreEqual(_nomcRule.DiagnosticDescriptor.Id, problem.Id);
		Assert.AreEqual(8, problem.Location.GetLineSpan().StartLinePosition.Line); // LinePosition.Line is 0-based
		Assert.AreEqual(8, problem.Location.GetLineSpan().EndLinePosition.Line);
	}

	private static SemanticModel GetSemanticModelFromSourceCode(string filename, string sourceCode)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode,
			new CSharpParseOptions().WithLanguageVersion(LanguageVersion.CSharp10), filename);
		var compilation = CSharpCompilation.Create("TestSolution")
			.AddReferences(MetadataReference.CreateFromFile(
					typeof(ModelApplicationHostController).Assembly.Location), // Load Commons into test compilation
				MetadataReference.CreateFromFile(typeof(LocalizableString).Assembly.Location), // Load I18n.Abstractions into test compilation
				MetadataReference.CreateFromFile(
					Assembly.Load("Mindbox.I18n, Version=1.3.2.0, Culture=neutral, PublicKeyToken=null")
						.Location), // Load Mindbox.I18n into test compilation
				MetadataReference.CreateFromFile(
					Assembly.Load(
							"System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
						.Location), // Load System.Private.CoreLib into test compilation
				MetadataReference.CreateFromFile(
					typeof(Console).Assembly.Location), // Load System.Console into test compilation
				MetadataReference.CreateFromFile(
					Assembly.Load("System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
						.Location)) // Load System.Runtime into test compilation
			.AddSyntaxTrees(syntaxTree);

		var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
		if (diagnostics.Length > 0)
		{
			throw new Exception(
				$"There were {diagnostics.Length} compile errors:\n{string.Join("\n", diagnostics.Select(d => d.ToString()))}");
		}

		return compilation.GetSemanticModel(syntaxTree);
	}
}