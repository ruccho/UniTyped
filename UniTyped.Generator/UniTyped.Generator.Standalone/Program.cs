using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Configuration;
using UniTyped.Generator;

try
{
    MSBuildLocator.RegisterDefaults();

    var switchMappings = new Dictionary<string, string>()
    {
        { "-o", "output" },
        { "-p", "project" },
    };

    var builder = new ConfigurationBuilder().AddCommandLine(args, switchMappings);
    var config = builder.Build();

    var outputPath = config["output"];
    var projectPath = config["project"];


    using var workspace = MSBuildWorkspace.Create();

    var project = await workspace.OpenProjectAsync(projectPath);
    var compilation = await project.GetCompilationAsync();

    if (compilation == null)
    {
        Console.Error.WriteLine("This project has no compilation.");
        return -1;
    }

    var collector = new UniTypedCollector();
    foreach (var tree in compilation.SyntaxTrees)
    {
        Console.WriteLine(tree.FilePath);
        collector.Visit(tree.GetRoot());
    }

    string? result = UniTypedGenerator.Execute(compilation, collector);

    if (result == null)
    {
        Console.WriteLine("No code is generated for this project.");
        return 0;
    }

    if (result != null) File.WriteAllText(outputPath, result);
    else Console.WriteLine("There is no content to emit.");

    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
    return -1;
}

class UniTypedCollector : CSharpSyntaxWalker, IUniTypedCollector
{
    public HashSet<TypeDeclarationSyntax> UniTypedTypes { get; } = new();
    public HashSet<TypeDeclarationSyntax> MaterialViews { get; } = new();

    private void VisitTypeDeclaration(TypeDeclarationSyntax node)
    {
        if (node.AttributeLists.Count > 0)
        {
            var attributes = node.AttributeLists.SelectMany(x => x.Attributes);
            if (attributes.Any(x => x.Name.ToString() is "UniTyped" or "UniTyped.UniTyped" or "UniTypedAttribute"
                    or "UniTyped.UniTypedAttribute"))
            {
                UniTypedTypes.Add(node);
            }

            if (attributes.Any(x => x.Name.ToString() is "UniTypedMaterialView"
                    or "UniTypedMaterialViewAttribute" or "UniTyped.UniTypedMaterialView"
                    or "UniTyped.UniTypedMaterialViewAttribute"))
            {
                MaterialViews.Add(node);
            }
        }
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        VisitTypeDeclaration(node);

        base.VisitClassDeclaration(node);
    }


    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        VisitTypeDeclaration(node);

        base.VisitStructDeclaration(node);
    }
}