using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace SOLIDify {
  class Program {
    private readonly IProjectAnalyzer _projectAnalyzer;

    // Constructor for Program class, initializes the IProjectAnalyzer dependency
    public Program(IProjectAnalyzer projectAnalyzer) {
      _projectAnalyzer = projectAnalyzer;
      Console.WriteLine("Program instance created with IProjectAnalyzer.");
    }

    // Main entry point of the application
    static async Task Main(string[] args) {
      Console.WriteLine("Starting SOLIDify application...");
      if (args.Length == 0) {
        Console.WriteLine("Error: Please provide the path to the C# project directory.");
        return;
      }

      Console.WriteLine("Configuring service collection...");
      var serviceProvider = ConfigureServiceCollection();
      string directoryPath = args[0];
      Console.WriteLine($"Project directory path: {directoryPath}");
      var program = serviceProvider.GetRequiredService<Program>();
      Console.WriteLine("Program instance retrieved from service provider.");

      Console.WriteLine("Starting project analysis...");
      await program.AnalyzeProject(directoryPath);
      Console.WriteLine("Project analysis completed.");
    }

    // Configures the service collection and returns the service provider
    private static IServiceProvider ConfigureServiceCollection() {
      Console.WriteLine("Configuring service collection...");
      var serviceCollection = new ServiceCollection();
      RegisterServices(serviceCollection);
      Console.WriteLine("Service collection configured.");
      return serviceCollection.BuildServiceProvider();
    }

    // Registers all the required services for dependency injection
    private static void RegisterServices(IServiceCollection services) {
      Console.WriteLine("Registering services...");
      services.AddSingleton<IMetricsProvider, SOLIDMetrics>()
              .AddSingleton<IViolationFactory, ViolationFactory>()
              .AddSingleton<IViolationFileDetailFactory, ViolationFileDetailFactory>()
              .AddTransient<IViolation, Violation>()
              .AddTransient<IViolationFileDetail, ViolationFileDetail>()
              .AddSingleton<IProjectAnalyzer, ProjectAnalyzer>()
              .AddSingleton<ISRPChecker, SRPChecker>()
              .AddSingleton<IOCPChecker, OCPChecker>()
              .AddSingleton<ILSPChecker, LSPChecker>()
              .AddSingleton<IISPChecker, ISPChecker>()
              .AddSingleton<IDIPChecker, DIPChecker>()
              .AddSingleton<ViolationManager>()
              .AddSingleton<ViolationFileDetailManager>()
              .AddSingleton<Program>();
      Console.WriteLine("Services registered.");
    }

    // Analyzes the project at the given directory path
    private Task AnalyzeProject(string directoryPath) {
      Console.WriteLine($"Analyzing project at: {directoryPath}");
      return _projectAnalyzer.Analyze(directoryPath);
    }
  }

  // Interface for project analyzer
  public interface IProjectAnalyzer {
    Task Analyze(string directoryPath);
  }

  // Implementation of the IProjectAnalyzer interface
  class ProjectAnalyzer : IProjectAnalyzer {
    private readonly IMetricsProvider _metricsProvider;

    // Constructor for ProjectAnalyzer class, initializes the IMetricsProvider dependency
    public ProjectAnalyzer(IMetricsProvider metricsProvider) {
      _metricsProvider = metricsProvider;
      Console.WriteLine("ProjectAnalyzer instance created with IMetricsProvider.");
    }

    // Analyzes the project directory and checks for SOLID principle violations
    public async Task Analyze(string directoryPath) {
      Console.WriteLine($"Starting analysis of directory: {directoryPath}");
      foreach (var file in Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories)) {
        Console.WriteLine($"Analyzing file: {file}");
        var code = await File.ReadAllTextAsync(file);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync() as CompilationUnitSyntax;

        if (root != null) {
          Console.WriteLine("Checking SOLID principles...");
          _metricsProvider.CheckSRP(root, file);
          _metricsProvider.CheckOCP(root, file);
          _metricsProvider.CheckLSP(root, file);
          _metricsProvider.CheckISP(root, file);
          _metricsProvider.CheckDIP(root, file);
          Console.WriteLine("SOLID principles checked.");
        } else {
          Console.WriteLine($"Warning: Unable to parse file {file}");
        }
      }

      Console.WriteLine("Displaying metrics...");
      _metricsProvider.DisplayMetrics();
      Console.WriteLine("Sending results to ChatGPT...");
      await _metricsProvider.SendToChatGPT("your-key");
      Console.WriteLine("Generating HTML report...");
      await _metricsProvider.GenerateHtmlReport();
      Console.WriteLine("Analysis completed.");
    }
  }

  // Interface for checking various SOLID principles
  public interface IMetricsChecker {
    void CheckSRP(CompilationUnitSyntax root, string fileName);
    void CheckOCP(CompilationUnitSyntax root, string fileName);
    void CheckLSP(CompilationUnitSyntax root, string fileName);
    void CheckISP(CompilationUnitSyntax root, string fileName);
    void CheckDIP(CompilationUnitSyntax root, string fileName);
  }

  // Interface for reporting metrics
  public interface IMetricsReporter {
    void DisplayMetrics();
    Task SendToChatGPT(string apiKey);
    Task GenerateHtmlReport();
  }

  // Interface that combines both checking and reporting metrics
  public interface IMetricsProvider : IMetricsChecker, IMetricsReporter {}

  // Interface for representing a violation
  public interface IViolation {
    string Principle { get; }
    IReadOnlyList<IViolationFileDetail> FileDetails { get; }
    string ChatGPTResponse { get; set; }
  }

  // Interface for representing details of a violation in a file
  public interface IViolationFileDetail {
    string FileName { get; }
    int LineNumber { get; }
    string Code { get; }
  }

  // Interface for creating violation instances
  public interface IViolationFactory {
    IViolation CreateViolation(string principle, IEnumerable<IViolationFileDetail> fileDetails);
  }

  // Interface for creating violation file detail instances
  public interface IViolationFileDetailFactory {
    IViolationFileDetail CreateViolationFileDetail(string fileName, int lineNumber, string code);
  }

  // Implementation of the IViolationFactory interface
  public class ViolationFactory : IViolationFactory {
    private readonly IServiceProvider _serviceProvider;

    // Constructor for ViolationFactory class, initializes the IServiceProvider dependency
    public ViolationFactory(IServiceProvider serviceProvider) {
      _serviceProvider = serviceProvider;
      Console.WriteLine("ViolationFactory instance created.");
    }

    // Creates a new violation instance
    public IViolation CreateViolation(string principle, IEnumerable<IViolationFileDetail> fileDetails) {
      Console.WriteLine($"Creating violation for principle: {principle}");
      return ActivatorUtilities.CreateInstance<Violation>(_serviceProvider, principle, fileDetails.ToList());
    }
  }

  // Implementation of the IViolationFileDetailFactory interface
  public class ViolationFileDetailFactory : IViolationFileDetailFactory {
    private readonly IServiceProvider _serviceProvider;

    // Constructor for ViolationFileDetailFactory class, initializes the IServiceProvider dependency
    public ViolationFileDetailFactory(IServiceProvider serviceProvider) {
      _serviceProvider = serviceProvider;
      Console.WriteLine("ViolationFileDetailFactory instance created.");
    }

    // Creates a new violation file detail instance
    public IViolationFileDetail CreateViolationFileDetail(string fileName, int lineNumber, string code) {
      Console.WriteLine($"Creating violation file detail for file: {fileName}, line: {lineNumber}");
      return ActivatorUtilities.CreateInstance<ViolationFileDetail>(_serviceProvider, fileName, lineNumber, code);
    }
  }

  // New class for managing violations
  public class ViolationManager {
    // Manages a given violation
    public void ManageViolation(IViolation violation) {
      // Logic for managing violations
    }
  }

  // New class for managing violation file details
  public class ViolationFileDetailManager {
    // Adds a violation file detail to a given violation
    public void AddViolationFileDetail(IViolation violation, IViolationFileDetail fileDetail) {
      // Logic for adding violation file details
    }
  }

  // Violation class focused on representing a violation
  public class Violation : IViolation {
    public string Principle { get; }
    private readonly List<IViolationFileDetail> _fileDetails;
    public IReadOnlyList<IViolationFileDetail> FileDetails => _fileDetails.AsReadOnly();
    public string ChatGPTResponse { get; set; }

    // Constructor for Violation class, initializes the principle and file details
    public Violation(string principle, List<IViolationFileDetail> fileDetails) {
      Principle = principle;
      _fileDetails = fileDetails;
      Console.WriteLine($"Violation instance created for principle: {principle}");
    }

    // Returns a string representation of the violation
    public override string ToString() {
      return $"Principle: {Principle}, Files: {string.Join("; ", FileDetails)}";
    }
  }

  // ViolationFileDetail class focused on representing details of a violation in a file
  public class ViolationFileDetail : IViolationFileDetail {
    public string FileName { get; }
    public int LineNumber { get; }
    public string Code { get; }

    // Constructor for ViolationFileDetail class, initializes the file name, line number, and code
    public ViolationFileDetail(string fileName, int lineNumber, string code) {
      FileName = fileName;
      LineNumber = lineNumber;
      Code = code;
      Console.WriteLine($"ViolationFileDetail instance created for file: {fileName}, line: {lineNumber}");
    }

    // Returns a string representation of the violation file detail
    public override string ToString() {
      return $"File: {FileName}, Line: {LineNumber}, Code: {Code}";
    }
  }

  // SOLIDMetrics class that implements the IMetricsProvider interface
  public class SOLIDMetrics : IMetricsProvider {
    private readonly IViolationFactory _violationFactory;
    private readonly IViolationFileDetailFactory _violationFileDetailFactory;
    private readonly ISRPChecker _srpChecker;
    private readonly IEnumerable<ISOLIDChecker> _checkers;
    private readonly ViolationManager _violationManager;
    private readonly ViolationFileDetailManager _violationFileDetailManager;

    public List<IViolation> Violations { get; } = new List<IViolation>();

    // Constructor for SOLIDMetrics class, initializes various dependencies
    public SOLIDMetrics(IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory, ISRPChecker srpChecker, IEnumerable<ISOLIDChecker> checkers, ViolationManager violationManager, ViolationFileDetailManager violationFileDetailManager) {
      _violationFactory = violationFactory;
      _violationFileDetailFactory = violationFileDetailFactory;
      _srpChecker = srpChecker;
      _checkers = checkers;
      _violationManager = violationManager;
      _violationFileDetailManager = violationFileDetailManager;
      Console.WriteLine("SOLIDMetrics instance created.");
    }

    // Checks for SRP violations in the given file
    public void CheckSRP(CompilationUnitSyntax root, string fileName) {
      Console.WriteLine($"Checking SRP for file: {fileName}");
      _srpChecker.Check(root, fileName, Violations, _violationFactory, _violationFileDetailFactory);
    }

    // Checks for OCP violations in the given file
    public void CheckOCP(CompilationUnitSyntax root, string fileName) {
      Console.WriteLine($"Checking OCP for file: {fileName}");
      var checker = _checkers.OfType<IOCPChecker>().FirstOrDefault();
      checker?.Check(root, fileName, Violations, _violationFactory, _violationFileDetailFactory);
    }

    // Checks for LSP violations in the given file
    public void CheckLSP(CompilationUnitSyntax root, string fileName) {
      Console.WriteLine($"Checking LSP for file: {fileName}");
      var checker = _checkers.OfType<ILSPChecker>().FirstOrDefault();
      checker?.Check(root, fileName, Violations, _violationFactory, _violationFileDetailFactory);
    }

    // Checks for ISP violations in the given file
    public void CheckISP(CompilationUnitSyntax root, string fileName) {
      Console.WriteLine($"Checking ISP for file: {fileName}");
      var checker = _checkers.OfType<IISPChecker>().FirstOrDefault();
      checker?.Check(root, fileName, Violations, _violationFactory, _violationFileDetailFactory);
    }

    // Checks for DIP violations in the given file
    public void CheckDIP(CompilationUnitSyntax root, string fileName) {
      Console.WriteLine($"Checking DIP for file: {fileName}");
      var checker = _checkers.OfType<IDIPChecker>().FirstOrDefault();
      checker?.Check(root, fileName, Violations, _violationFactory, _violationFileDetailFactory);
    }

    // Displays the collected metrics
    public void DisplayMetrics() {
      Console.WriteLine("Displaying metrics:");
      foreach (var violation in Violations) {
        Console.WriteLine(violation.ToString());
      }
    }

    // Sends the collected violations to ChatGPT for further analysis
    public async Task SendToChatGPT(string apiKey) {
      Console.WriteLine("Sending violations to ChatGPT...");
      using var client = new HttpClient();
      client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

      foreach (var violation in Violations) {
        var fileDetailsString = string.Join("\n", violation.FileDetails.Select(fd =>
            $"File: {fd.FileName}, Line: {fd.LineNumber}, Code: {fd.Code}"));

        var prompt = new {
          model = "gpt-3.5-turbo",
          messages = new[] {
            new { 
              role = "system", 
              content = "You are a seasoned software development expert with extensive experience in SOLID principles. Your goal is to identify violations of these principles in code and provide thorough suggestions for improvement."
            },
            new { 
              role = "user", 
              content = $"I need your expertise to analyze a specific violation of a SOLID principle within a given piece of code. The details of the violation and the full code of the class or function are provided below. Please thoroughly examine the code, identify the issues related to the specified SOLID principle, and rewrite the code to ensure it fully complies with this principle. Your detailed explanation of the changes and the rationale behind them will be highly appreciated.\n\n" +
                        $"Principle violated: {violation.Principle}\n\n" +
                        $"Details of the code file:\n{fileDetailsString}\n\n" +
                        $"Complete code of the class or function:\n\n{violation.FileDetails.First().Code}\n\n" +
                        "Please provide a detailed analysis and a rewritten version of the code that adheres to the specified SOLID principle."
            }
          },
          max_tokens = 1500
        };

        var json = JsonConvert.SerializeObject(prompt);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        Console.WriteLine($"Sending request to ChatGPT for violation in files:\n{fileDetailsString}");
        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode) {
          Console.WriteLine($"Received successful response for violation in files:\n{fileDetailsString}");
          Console.WriteLine(result);
          violation.ChatGPTResponse = result;
        } else {
          Console.WriteLine($"Error response for violation in files:\n{fileDetailsString}");
          Console.WriteLine(result);
          violation.ChatGPTResponse = result;
        }
      }
      Console.WriteLine("Finished sending violations to ChatGPT.");
    }

    // Generates an HTML report of the collected metrics
    public async Task GenerateHtmlReport() {
      Console.WriteLine("Generating HTML report...");
      var html = @"
      <html>
        <head>
          <style>
            body { font-family: Arial, sans-serif; margin: 20px; background-color: #f0f0f0; }
            h1 { color: #333; }
            h2 { color: #555; }
            ul { list-style-type: none; padding: 0; }
            li { background: #fff; margin: 10px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
            .file-name { font-weight: bold; }
            .line-number { color: #888; }
            .code { font-family: 'Courier New', Courier, monospace; background: #f4f4f4; padding: 5px; display: block; }
            .gpt-response { margin-top: 10px; padding: 10px; background: #e8e8e8; border-left: 5px solid #ccc; }
          </style>
        </head>
        <body>
          <h1>SOLID Metrics Report</h1>";
      
      if (Violations.Count == 0) {
        html += "<h2>Congratulations, No SOLID Violations Suspected.</h2>";
        Console.WriteLine("No violations found.");
      } else {
        Console.WriteLine($"Found {Violations.Count} violations.");
        foreach (var violation in Violations) {
          html += @"
            <h2>" + violation.Principle + @"</h2>
            <ul>";
          foreach (var fileDetail in violation.FileDetails) {
            html += @"
              <li>
                <span class='file-name'>" + fileDetail.FileName + @"</span>
                <span class='line-number'>(Line " + fileDetail.LineNumber + @")</span>: 
                <span class='code'>" + System.Web.HttpUtility.HtmlEncode(fileDetail.Code) + @"</span></li>";
          }
          html += @"
            </ul>
            <div class='gpt-response'>
              <strong>ChatGPT Response:</strong>
              <pre style='white-space: pre-wrap;'>" + System.Web.HttpUtility.HtmlEncode(violation.ChatGPTResponse) + @"</pre></div>";
        }
      }
      html += @"
        </body>
      </html>";

      await File.WriteAllTextAsync("Report.html", html);
    }
  }

  // Interface for SOLID principle checkers
  public interface ISOLIDChecker {
    void Check(CompilationUnitSyntax root, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory);
  }

  // Interface for SRP checker
  public interface ISRPChecker : ISOLIDChecker {}
  // Interface for OCP checker
  public interface IOCPChecker : ISOLIDChecker {}
  // Interface for LSP checker
  public interface ILSPChecker : ISOLIDChecker {}
  // Interface for ISP checker
  public interface IISPChecker : ISOLIDChecker {}
  // Interface for DIP checker
  public interface IDIPChecker : ISOLIDChecker {}

  // Implementation of the SRP checker
  public class SRPChecker : ISRPChecker {
    private readonly ViolationFileDetailManager _violationFileDetailManager;

    // Constructor for SRPChecker class, initializes the ViolationFileDetailManager dependency
    public SRPChecker(ViolationFileDetailManager violationFileDetailManager) {
      _violationFileDetailManager = violationFileDetailManager;
    }

    // Checks for SRP violations in the given file
    public void Check(CompilationUnitSyntax root, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      Console.WriteLine($"SRPChecker: Checking file {fileName}");
      var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
      foreach (var classDeclaration in classes) {
        var methods = classDeclaration.Members.OfType<MethodDeclarationSyntax>().ToList();
        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().ToList();
        var responsibilities = AnalyzeResponsibilities(methods, properties);
        
        if (responsibilities.Count > 1 || methods.Count > 10 || properties.Count > 10) {
          Console.WriteLine($"SRPChecker: Violation found in class {classDeclaration.Identifier.Text} in file {fileName}");
          AddViolation("SRP", classDeclaration, fileName, violations, violationFactory, violationFileDetailFactory);
        }
      }
    }

    // Analyzes the responsibilities of the given methods and properties
    private List<string> AnalyzeResponsibilities(List<MethodDeclarationSyntax> methods, List<PropertyDeclarationSyntax> properties) {
      var responsibilities = new List<string>();
      var methodGroups = methods.GroupBy(m => {
        var methodName = m.Identifier.Text.ToLower();
        var body = m.Body?.ToString() ?? string.Empty;
        return GetResponsibilityCategory(methodName, body);
      });

      foreach (var group in methodGroups) {
        responsibilities.Add(group.Key);
      }

      if (properties.Any(p => p.Modifiers.Any(SyntaxKind.PublicKeyword))) {
        responsibilities.Add("DataManagement");
      }

      return responsibilities;
    }

    // Determines the responsibility category of a method based on its name and body
    private string GetResponsibilityCategory(string methodName, string body) {
      if (methodName.Contains("calculate") || methodName.Contains("compute")) return "Calculation";
      if (methodName.Contains("save") || methodName.Contains("load") || methodName.Contains("fetch")) return "DataAccess";
      if (methodName.Contains("validate") || methodName.Contains("check")) return "Validation";
      if (methodName.Contains("format") || methodName.Contains("parse")) return "Formatting";
      if (body.Contains("Console.") || body.Contains("Debug.")) return "Logging";
      return "Other";
    }

    // Adds a violation to the list of violations
    private void AddViolation(string principle, SyntaxNode node, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      var lineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
      var fileDetail = violationFileDetailFactory.CreateViolationFileDetail(fileName, lineNumber, node.ToString());
      var violation = violations.FirstOrDefault(v => v.Principle == principle);
      if (violation == null) {
        violation = violationFactory.CreateViolation(principle, new List<IViolationFileDetail> { fileDetail });
        violations.Add(violation);
      } else {
        _violationFileDetailManager.AddViolationFileDetail(violation, fileDetail);
      }
    }
  }

  // Implementation of the OCP checker
  public class OCPChecker : IOCPChecker {
    private readonly ViolationFileDetailManager _violationFileDetailManager;

    // Constructor for OCPChecker class, initializes the ViolationFileDetailManager dependency
    public OCPChecker(ViolationFileDetailManager violationFileDetailManager) {
      _violationFileDetailManager = violationFileDetailManager;
    }

    // Checks for OCP violations in the given file
    public void Check(CompilationUnitSyntax root, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      Console.WriteLine($"OCPChecker: Checking file {fileName}");
      var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
      foreach (var classDeclaration in classes) {
        bool isOpenForExtension = IsOpenForExtension(classDeclaration);
        bool isClosedForModification = IsClosedForModification(classDeclaration);

        if (!isOpenForExtension || !isClosedForModification) {
          Console.WriteLine($"OCPChecker: Violation found in class {classDeclaration.Identifier.Text} in file {fileName}");
          AddViolation("OCP", classDeclaration, fileName, violations, violationFactory, violationFileDetailFactory);
        }
      }
    }

    // Determines if a class is open for extension
    private bool IsOpenForExtension(ClassDeclarationSyntax classDeclaration) {
      var hasInheritance = classDeclaration.BaseList?.Types.Any() ?? false;
      var hasInterfaces = classDeclaration.BaseList?.Types.OfType<SimpleBaseTypeSyntax>()
          .Any(t => t.Type is IdentifierNameSyntax) ?? false;
      var hasVirtualMethods = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
          .Any(m => m.Modifiers.Any(SyntaxKind.VirtualKeyword));
      var hasAbstractMethods = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
          .Any(m => m.Modifiers.Any(SyntaxKind.AbstractKeyword));
      var hasExtensionMethods = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
          .Any(m => m.ParameterList.Parameters.Any(p => p.Modifiers.Any(SyntaxKind.ThisKeyword)));
      var usesStrategyPattern = classDeclaration.Members.OfType<FieldDeclarationSyntax>()
          .Any(f => f.Declaration.Type is IdentifierNameSyntax);

      return hasInheritance || hasInterfaces || hasVirtualMethods || hasAbstractMethods || hasExtensionMethods || usesStrategyPattern;
    }

    // Determines if a class is closed for modification
    private bool IsClosedForModification(ClassDeclarationSyntax classDeclaration) {
      var hasSealedModifier = classDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword);
      var hasPrivateSetters = classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
          .All(p => p.AccessorList?.Accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration && a.Modifiers.Any(SyntaxKind.PrivateKeyword)) ?? true);
      var hasReadonlyFields = classDeclaration.Members.OfType<FieldDeclarationSyntax>()
          .All(f => f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword));
      var hasNoPublicMutableState = !classDeclaration.Members.OfType<FieldDeclarationSyntax>()
          .Any(f => f.Modifiers.Any(SyntaxKind.PublicKeyword) && !f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword));

      return (hasSealedModifier || hasPrivateSetters || hasReadonlyFields) && hasNoPublicMutableState;
    }

    // Adds a violation to the list of violations
    private void AddViolation(string principle, SyntaxNode node, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      var lineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
      var fileDetail = violationFileDetailFactory.CreateViolationFileDetail(fileName, lineNumber, node.ToString());
      var violation = violations.FirstOrDefault(v => v.Principle == principle);
      if (violation == null) {
        violation = violationFactory.CreateViolation(principle, new List<IViolationFileDetail> { fileDetail });
        violations.Add(violation);
      } else {
        _violationFileDetailManager.AddViolationFileDetail(violation, fileDetail);
      }
    }
  }

  // Implementation of the LSP checker
  public class LSPChecker : ILSPChecker {
    private readonly Compilation _compilation;
    private readonly ViolationFileDetailManager _violationFileDetailManager;

    // Constructor for LSPChecker class, initializes the Compilation and ViolationFileDetailManager dependencies
    public LSPChecker(Compilation compilation, ViolationFileDetailManager violationFileDetailManager) {
      _compilation = compilation;
      _violationFileDetailManager = violationFileDetailManager;
    }

    // Checks for LSP violations in the given file
    public void Check(CompilationUnitSyntax root, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      Console.WriteLine($"LSPChecker: Checking file {fileName}");
      var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
      foreach (var classDeclaration in classes) {
        if (classDeclaration.BaseList != null) {
          var baseType = classDeclaration.BaseList.Types.First().Type;
          var baseClass = FindBaseClass(root, baseType.ToString());
          if (baseClass != null && !IsSubstitutable(baseClass, classDeclaration)) {
            Console.WriteLine($"LSPChecker: Violation found in class {classDeclaration.Identifier.Text} in file {fileName}");
            AddViolation("LSP", classDeclaration, fileName, violations, violationFactory, violationFileDetailFactory);
          }
        }
      }
    }

    // Finds the base class of a given class
    private ClassDeclarationSyntax FindBaseClass(CompilationUnitSyntax root, string baseTypeName) {
      return root.DescendantNodes()
                 .OfType<ClassDeclarationSyntax>()
                 .FirstOrDefault(c => c.Identifier.Text == baseTypeName);
    }

    // Determines if a derived class is substitutable for its base class
    private bool IsSubstitutable(ClassDeclarationSyntax baseClass, ClassDeclarationSyntax derivedClass) {
      var baseMethods = baseClass.Members.OfType<MethodDeclarationSyntax>().ToList();
      var derivedMethods = derivedClass.Members.OfType<MethodDeclarationSyntax>().ToList();

      foreach (var baseMethod in baseMethods) {
        var derivedMethod = derivedMethods.FirstOrDefault(dm => dm.Identifier.Text == baseMethod.Identifier.Text &&
            dm.ParameterList.Parameters.Count == baseMethod.ParameterList.Parameters.Count &&
            dm.ParameterList.Parameters.Select(p => p.Type.ToString()).SequenceEqual(baseMethod.ParameterList.Parameters.Select(p => p.Type.ToString())));

        if (derivedMethod == null || !derivedMethod.Modifiers.Any(SyntaxKind.OverrideKeyword)) {
          return false;
        }

        if (!IsReturnTypeCovariant(baseMethod.ReturnType, derivedMethod.ReturnType)) {
          return false;
        }

        if (!AreParametersContravariant(baseMethod.ParameterList.Parameters, derivedMethod.ParameterList.Parameters)) {
          return false;
        }
      }

      return true;
    }
    private bool IsReturnTypeCovariant(TypeSyntax baseReturnType, TypeSyntax derivedReturnType) {
      // Check if the derived return type is the same as the base return type
      if (baseReturnType.ToString() == derivedReturnType.ToString()) {
        return true;
      }

      // Check if the derived return type is a subclass of the base return type
      var baseTypeSymbol = GetTypeSymbol(baseReturnType);
      var derivedTypeSymbol = GetTypeSymbol(derivedReturnType);
      if (baseTypeSymbol != null && derivedTypeSymbol != null) {
        return IsSubclassOf(derivedTypeSymbol, baseTypeSymbol);
      }

      return false;
    }

    // Determines if the parameters of the derived method are contravariant with the base method
    private bool AreParametersContravariant(SeparatedSyntaxList<ParameterSyntax> baseParams, SeparatedSyntaxList<ParameterSyntax> derivedParams) {
      if (baseParams.Count != derivedParams.Count) {
        return false;
      }

      for (int i = 0; i < baseParams.Count; i++) {
        var baseParamType = GetTypeSymbol(baseParams[i].Type);
        var derivedParamType = GetTypeSymbol(derivedParams[i].Type);

        if (baseParamType == null || derivedParamType == null || !IsAssignableFrom(baseParamType, derivedParamType)) {
          return false;
        }
      }

      return true;
    }

    // Retrieves the type symbol for a given type syntax
    private ITypeSymbol GetTypeSymbol(TypeSyntax typeSyntax) {
      var semanticModel = _compilation.GetSemanticModel(typeSyntax.SyntaxTree);
      return semanticModel.GetTypeInfo(typeSyntax).Type;
    }

    // Checks if a type is a subclass of another type
    private bool IsSubclassOf(ITypeSymbol derivedType, ITypeSymbol baseType) {
      if (derivedType == null || baseType == null) {
        return false;
      }

      var currentType = derivedType;
      while (currentType != null) {
        if (currentType.Equals(baseType)) {
          return true;
        }
        currentType = currentType.BaseType;
      }

      return false;
    }

    // Checks if a type is assignable from another type
    private bool IsAssignableFrom(ITypeSymbol baseType, ITypeSymbol derivedType) {
      if (baseType == null || derivedType == null) {
        return false;
      }

      if (baseType.Equals(derivedType)) {
        return true;
      }

      return IsSubclassOf(derivedType, baseType);
    }

    // Adds a violation to the list of violations
    private void AddViolation(string principle, SyntaxNode node, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      var lineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
      var fileDetail = violationFileDetailFactory.CreateViolationFileDetail(fileName, lineNumber, node.ToString());
      var violation = violations.FirstOrDefault(v => v.Principle == principle);
      if (violation == null) {
        violation = violationFactory.CreateViolation(principle, new List<IViolationFileDetail> { fileDetail });
        violations.Add(violation);
      } else {
        _violationFileDetailManager.AddViolationFileDetail(violation, fileDetail);
      }
    }
  }

  // Class for checking Interface Segregation Principle (ISP) violations
  public class ISPChecker : IISPChecker {
    private readonly ViolationFileDetailManager _violationFileDetailManager;

    // Constructor for ISPChecker class, initializes the ViolationFileDetailManager dependency
    public ISPChecker(ViolationFileDetailManager violationFileDetailManager) {
      _violationFileDetailManager = violationFileDetailManager;
    }

    // Checks for ISP violations in the given compilation unit
    public void Check(CompilationUnitSyntax root, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      Console.WriteLine($"ISPChecker: Checking file {fileName}");
      var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
      foreach (var @interface in interfaces) {
        if (!IsInterfaceSegregated(@interface)) {
          Console.WriteLine($"ISPChecker: Violation found in interface {@interface.Identifier.Text} in file {fileName}");
          AddViolation("ISP", @interface, fileName, violations, violationFactory, violationFileDetailFactory);
        }
      }
    }

    // Determines if an interface is segregated
    private bool IsInterfaceSegregated(InterfaceDeclarationSyntax @interface) {
      var methods = @interface.Members.OfType<MethodDeclarationSyntax>().ToList();
      var properties = @interface.Members.OfType<PropertyDeclarationSyntax>().ToList();
      var events = @interface.Members.OfType<EventDeclarationSyntax>().ToList();

      if (methods.Count + properties.Count + events.Count > 7) {
        return false;
      }

      var methodGroups = methods.GroupBy(m => GetMethodCategory(m.Identifier.Text));
      if (methodGroups.Count() > 2) {
        return false;
      }

      return true;
    }

    // Categorizes a method based on its name
    private string GetMethodCategory(string methodName) {
      if (methodName.StartsWith("Get") || methodName.StartsWith("Set") || methodName.StartsWith("Is")) return "Accessor";
      if (methodName.StartsWith("Calculate") || methodName.StartsWith("Compute")) return "Calculation";
      if (methodName.StartsWith("Save") || methodName.StartsWith("Load") || methodName.StartsWith("Delete")) return "Persistence";
      if (methodName.StartsWith("Validate") || methodName.StartsWith("Check")) return "Validation";
      return "Other";
    }

    // Adds a violation to the list of violations
    private void AddViolation(string principle, SyntaxNode node, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      var lineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
      var fileDetail = violationFileDetailFactory.CreateViolationFileDetail(fileName, lineNumber, node.ToString());
      var violation = violations.FirstOrDefault(v => v.Principle == principle);
      if (violation == null) {
        violation = violationFactory.CreateViolation(principle, new List<IViolationFileDetail> { fileDetail });
        violations.Add(violation);
      } else {
        _violationFileDetailManager.AddViolationFileDetail(violation, fileDetail);
      }
    }
  }

  // Class for checking Dependency Inversion Principle (DIP) violations
  public class DIPChecker : IDIPChecker {
    private readonly ViolationFileDetailManager _violationFileDetailManager;

    // Constructor for DIPChecker class, initializes the ViolationFileDetailManager dependency
    public DIPChecker(ViolationFileDetailManager violationFileDetailManager) {
      _violationFileDetailManager = violationFileDetailManager;
    }

    // Checks for DIP violations in the given compilation unit
    public void Check(CompilationUnitSyntax root, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      Console.WriteLine($"DIPChecker: Checking file {fileName}");
      var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
      foreach (var classDeclaration in classes) {
        if (!DependsOnAbstractions(classDeclaration)) {
          Console.WriteLine($"DIPChecker: Violation found in class {classDeclaration.Identifier.Text} in file {fileName}");
          AddViolation("DIP", classDeclaration, fileName, violations, violationFactory, violationFileDetailFactory);
        }
      }
    }

    // Determines if a class depends on abstractions
    private bool DependsOnAbstractions(ClassDeclarationSyntax classDeclaration) {
      var fieldTypes = classDeclaration.Members.OfType<FieldDeclarationSyntax>()
          .Select(f => f.Declaration.Type).ToList();
      var propertyTypes = classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
          .Select(p => p.Type).ToList();
      var methodParameterTypes = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
          .SelectMany(m => m.ParameterList.Parameters.Select(p => p.Type)).ToList();

      var allTypes = fieldTypes.Concat(propertyTypes).Concat(methodParameterTypes);

      return allTypes.All(t => t is PredefinedTypeSyntax || t is IdentifierNameSyntax);
    }

    // Adds a violation to the list of violations
    private void AddViolation(string principle, SyntaxNode node, string fileName, List<IViolation> violations, IViolationFactory violationFactory, IViolationFileDetailFactory violationFileDetailFactory) {
      var lineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
      var fileDetail = violationFileDetailFactory.CreateViolationFileDetail(fileName, lineNumber, node.ToString());
      var violation = violations.FirstOrDefault(v => v.Principle == principle);
      if (violation == null) {
        violation = violationFactory.CreateViolation(principle, new List<IViolationFileDetail> { fileDetail });
        violations.Add(violation);
      } else {
        _violationFileDetailManager.AddViolationFileDetail(violation, fileDetail);
      }
    }
  }
}
