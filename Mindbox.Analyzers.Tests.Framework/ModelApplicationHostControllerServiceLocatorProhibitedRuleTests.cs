using System.Reflection;
using Itc.Commons.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MindboxAnalyzers.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MindboxAnalyzers.Tests;

[TestClass]
public class ModelApplicationHostControllerServiceLocatorProhibitedRuleTests
{
	private readonly ModelApplicationHostControllerServiceLocatorProhibitedRule _mahcRule = new();

	[TestMethod]
	public void MAHC_NoCalls_ProducesNoProblems()
	{
		const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""No MAHC calls!"");
    }
}";
		var semanticModel = GetSemanticModelFromSourceCode("Program.cs", givenSource);
		_mahcRule.AnalyzeModel(semanticModel, out var problems);

		Assert.AreEqual(0, problems.Count);
	}

	[TestMethod]
	public void MAHC_OneSingleLineCall_ProducesOneProblem()
	{
		const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.IFeatureService>();
    }
}";

		const string filename = "Program.cs";

		var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
		_mahcRule.AnalyzeModel(semanticModel, out var problems);

		Assert.AreEqual(1, problems.Count);
		var problem = problems.Single();
		Assert.AreEqual(_mahcRule.DiagnosticDescriptor.Id, problem.Id);
		Assert.AreEqual(6, problem.Location.GetLineSpan().StartLinePosition.Line); // LinePosition.Line is 0-based
		Assert.AreEqual(6, problem.Location.GetLineSpan().EndLinePosition.Line);
	}

	[TestMethod]
	public void MAHC_TwoSingleLineCalls_ProducesTwoProblems()
	{
		const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service1 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.IFeatureService>();
        var separator = 0;
        var service2 = Itc.Commons.Model.ModelApplicationHostController.Instance.Get<Itc.Commons.Model.IFeatureService>();
    }
}";

		const string filename = "Program.cs";

		var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
		_mahcRule.AnalyzeModel(semanticModel, out var problemCollection);
		var problems = problemCollection.ToArray();

		Assert.AreEqual(2, problems.Length);

		Assert.AreEqual(_mahcRule.DiagnosticDescriptor.Id, problems[0].Id);
		Assert.AreEqual(6, problems[0].Location.GetLineSpan().StartLinePosition.Line); // LinePosition.Line is 0-based
		Assert.AreEqual(6, problems[0].Location.GetLineSpan().EndLinePosition.Line);
		Assert.AreEqual(_mahcRule.DiagnosticDescriptor.Id, problems[1].Id);
		Assert.AreEqual(8, problems[1].Location.GetLineSpan().StartLinePosition.Line); // LinePosition.Line is 0-based
		Assert.AreEqual(8, problems[1].Location.GetLineSpan().EndLinePosition.Line);
	}

	[TestMethod]
	public void MAHC_OneMultiLineCall_ProducesOneProblem()
	{
		const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.IFeatureService>();
    }
}";

		const string filename = "Program.cs";

		var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
		_mahcRule.AnalyzeModel(semanticModel, out var problemCollection);
		var problem = problemCollection.Single();

		Assert.AreEqual(1, problemCollection.Count);

		Assert.AreEqual(_mahcRule.DiagnosticDescriptor.Id, problem.Id);
		Assert.AreEqual(6, problem.Location.GetLineSpan().StartLinePosition.Line); // LinePosition.Line is 0-based
		Assert.AreEqual(8, problem.Location.GetLineSpan().EndLinePosition.Line);
	}

	[TestMethod]
	public void MAHC_TwoMultiLineCalls_ProducesTwoProblems()
	{
		const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var service1 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.IFeatureService>();
        var separator = 0;
        var service2 = Itc.Commons.Model.ModelApplicationHostController
            .Instance
            .Get<Itc.Commons.Model.IFeatureService>();
    }
}";

		const string filename = "Program.cs";

		var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
		_mahcRule.AnalyzeModel(semanticModel, out var problemCollection);
		var problems = problemCollection.ToArray();

		Assert.AreEqual(2, problems.Length);

		Assert.AreEqual(_mahcRule.DiagnosticDescriptor.Id, problems[0].Id);
		Assert.AreEqual(6, problems[0].Location.GetLineSpan().StartLinePosition.Line); // LinePosition.Line is 0-based
		Assert.AreEqual(8, problems[0].Location.GetLineSpan().EndLinePosition.Line);
		Assert.AreEqual(_mahcRule.DiagnosticDescriptor.Id, problems[1].Id);
		Assert.AreEqual(10, problems[1].Location.GetLineSpan().StartLinePosition.Line); // LinePosition.Line is 0-based
		Assert.AreEqual(12, problems[1].Location.GetLineSpan().EndLinePosition.Line);
	}

	[TestMethod]
	public void MAHC_InstanceInAVariable_ProducesOneProblem()
	{
		const string givenSource = @"using System;

public class Program
{
    public static void Main(string[] args)
    {
        var mahc = Itc.Commons.Model.ModelApplicationHostController.Instance;
        var service = mahc.Get<Itc.Commons.Model.IFeatureService>(); // <------ Problematic line: MAHC.Get
    }
}";

		const string filename = "Program.cs";

		var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
		_mahcRule.AnalyzeModel(semanticModel, out var problemCollection);
		var problems = problemCollection.ToArray();

		Assert.AreEqual(1, problems.Length);
		var problem = problems.Single();

		Assert.AreEqual(_mahcRule.DiagnosticDescriptor.Id, problem.Id);
		Assert.AreEqual(7, problem.Location.GetLineSpan().StartLinePosition.Line); // LinePosition.Line is 0-based
		Assert.AreEqual(7, problem.Location.GetLineSpan().EndLinePosition.Line);
	}

	[TestMethod]
	public void MAHC_PassedToMethod_ProducesOneProblem()
	{
		const string givenSource = @"using System;
using Itc.Commons.Model;

public class Program
{
    public static void Main(string[] args)
    {
        var mahc = Itc.Commons.Model.ModelApplicationHostController.Instance;
        var service = MyGetMethod<IFeatureService>(mahc);
    }

    private static T MyGetMethod<T>(ModelApplicationHostController mahc) where T : class
    {
        return mahc.Get<T>(); // <--------- Problematic line: MAHC.Get
    }
}";

		const string filename = "Program.cs";

		var semanticModel = GetSemanticModelFromSourceCode(filename, givenSource);
		_mahcRule.AnalyzeModel(semanticModel, out var problemCollection);
		var problems = problemCollection.ToArray();

		Assert.AreEqual(1, problems.Length);
		var problem = problems.Single();

		Assert.AreEqual(_mahcRule.DiagnosticDescriptor.Id, problem.Id);
		Assert.AreEqual(13, problem.Location.GetLineSpan().StartLinePosition.Line); // LinePosition.Line is 0-based
		Assert.AreEqual(13, problem.Location.GetLineSpan().EndLinePosition.Line);
	}

	private static SemanticModel GetSemanticModelFromSourceCode(string filename, string sourceCode)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode,
			new CSharpParseOptions().WithLanguageVersion(LanguageVersion.CSharp10), filename);
		var compilation = CSharpCompilation.Create("TestSolution")
			.AddReferences(MetadataReference.CreateFromFile(
					typeof(ModelApplicationHostController).Assembly.Location), // Load Commons into test compilation
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