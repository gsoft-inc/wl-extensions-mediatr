using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace GSoft.Extensions.MediatR.Analyzers.Tests;

// This class can be copy-pasted and reused as-is in other Roslyn Analyzers test projects
internal sealed class CompilationBuilder
{
    private const string GlobalUsingsFilename = "CSharp10GlobalUsings.cs";
    private const string GlobalUsingsSourceCode = @"
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
";

    private readonly HashSet<MetadataReference> _references;
    private readonly Dictionary<string, string> _sourceFiles;
    private readonly HashSet<DiagnosticAnalyzer> _analyzers;
    private readonly string _projectName;
    private LanguageVersion _langVersion;
    private OutputKind _outputKind;

    public CompilationBuilder()
    {
        this._references = new HashSet<MetadataReference>(GetAlreadyLoadedAssemblyReferences());
        this._sourceFiles = new Dictionary<string, string>();
        this._analyzers = new HashSet<DiagnosticAnalyzer>();
        this._projectName = "TestProject";
        this._langVersion = LanguageVersion.CSharp10;
        this._outputKind = OutputKind.DynamicallyLinkedLibrary;
    }

    private static IEnumerable<MetadataReference> GetAlreadyLoadedAssemblyReferences()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location));
    }

    public CompilationBuilder WithCSharpLanguageVersion(LanguageVersion langVersion)
    {
        this._langVersion = langVersion;
        return this;
    }

    public CompilationBuilder WithOutputKind(OutputKind outputKind)
    {
        this._outputKind = outputKind;
        return this;
    }

    public CompilationBuilder WithAssemblyReference<T>()
    {
        return this.WithAssemblyReference(typeof(T).Assembly);
    }

    public CompilationBuilder WithAssemblyReference(Assembly assembly)
    {
        this._references.Add(MetadataReference.CreateFromFile(assembly.Location));
        return this;
    }

    public CompilationBuilder WithSourceFile(string contents)
    {
        return this.WithSourceFile(Guid.NewGuid().ToString("N") + ".cs", contents);
    }

    public CompilationBuilder WithSourceFile(string filename, string contents)
    {
        this._sourceFiles.Add(filename, contents);
        return this;
    }

    public CompilationBuilder WithAnalyzer<T>()
        where T : DiagnosticAnalyzer, new()
    {
        this._analyzers.Add(new T());
        return this;
    }

    public async Task<Diagnostic[]> Compile()
    {
        using var workspace = new AdhocWorkspace();

        var project = this.CreateProject(workspace);
        var compilation = await this.CompileProject(project).ConfigureAwait(false);
        var compilationWithAnalyzers = compilation.WithAnalyzers(this._analyzers.ToImmutableArray());
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(compilationWithAnalyzers.CancellationToken).ConfigureAwait(false);

        return diagnostics.OrderBy(x => x.Location.SourceSpan.Start).ToArray();
    }

    private Project CreateProject(Workspace workspace)
    {
        var projectId = ProjectId.CreateNewId(debugName: this._projectName);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, this._projectName, this._projectName, LanguageNames.CSharp)
            .WithProjectParseOptions(projectId, CSharpParseOptions.Default.WithLanguageVersion(this._langVersion))
            .WithProjectMetadataReferences(projectId, this._references);

        if (this._langVersion >= LanguageVersion.CSharp10)
        {
            var documentId = DocumentId.CreateNewId(projectId, GlobalUsingsFilename);
            solution = solution.AddDocument(documentId, GlobalUsingsFilename, SourceText.From(GlobalUsingsSourceCode), filePath: GlobalUsingsFilename);
        }

        foreach (var kvp in this._sourceFiles)
        {
            var documentId = DocumentId.CreateNewId(projectId, kvp.Key);
            solution = solution.AddDocument(documentId, kvp.Key, SourceText.From(kvp.Value), filePath: kvp.Key);
        }

        var project = solution.GetProject(projectId);
        Assert.NotNull(project);

        return project;
    }

    private async Task<Compilation> CompileProject(Project project)
    {
        var options = this._analyzers.Aggregate(new CSharpCompilationOptions(this._outputKind), (options, analyzer) =>
        {
            var reportDiagnostics = analyzer.SupportedDiagnostics.ToDictionary(x => x.Id, GetReportDiagnostic);
            return options.WithSpecificDiagnosticOptions(reportDiagnostics);
        });

        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
        Assert.NotNull(compilation);

        compilation = compilation.WithOptions(options);

        var result = compilation.Emit(Stream.Null);
        Assert.True(result.Success, "The code doesn't compile. " + string.Join(Environment.NewLine, result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)));

        return compilation;
    }

    private static ReportDiagnostic GetReportDiagnostic(DiagnosticDescriptor descriptor) => descriptor.DefaultSeverity switch
    {
        DiagnosticSeverity.Hidden => ReportDiagnostic.Hidden,
        DiagnosticSeverity.Info => ReportDiagnostic.Info,
        DiagnosticSeverity.Warning => ReportDiagnostic.Warn,
        DiagnosticSeverity.Error => ReportDiagnostic.Error,
        _ => ReportDiagnostic.Info,
    };
}