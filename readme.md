# SOLIDify

SOLIDify is a C# project designed to analyze other C# projects for adherence to SOLID principles. The project leverages various design patterns to ensure a modular, maintainable, and scalable architecture.

## Table of Contents
- [Introduction](#introduction)
- [Installation](#installation)
- [Usage](#usage)
- [Design Patterns](#design-patterns)
  - [Dependency Injection](#dependency-injection)
  - [Singleton](#singleton)
  - [Factory](#factory)
  - [Strategy](#strategy)
- [Architecture](#architecture)
- [License](#license)

## Introduction

SOLIDify is a tool that helps developers ensure their C# projects follow the SOLID principles:
- **S**ingle Responsibility Principle
- **O**pen/Closed Principle
- **L**iskov Substitution Principle
- **I**nterface Segregation Principle
- **D**ependency Inversion Principle

## Installation

To install and run SOLIDify, follow these steps:

1. Clone the repository:
   ```sh
   git clone https://github.com/yourusername/SOLIDify.git
   ```
2. Navigate to the project directory:
   ```sh
   cd SOLIDify
   ```
3. Build the project:
   ```sh
   dotnet build
   ```

## Usage

To analyze a C# project, run the following command:
   ```sh
   dotnet run --path /path/to/your/project
   ```

## Design Patterns

### Dependency Injection

Dependency Injection is used extensively throughout the project to manage dependencies and promote loose coupling. The `ConfigureServiceCollection` method sets up the dependency injection container, and various services are registered within it. This conforms to the **D**ependency Inversion Principle by ensuring high-level modules are not dependent on low-level modules but rather on abstractions.

### Singleton

The Singleton pattern is used to ensure that certain services, such as `SOLIDMetrics`, `ViolationFactory`, `ViolationFileDetailFactory`, `ViolationManager`, `ViolationFileDetailManager`, and `ProjectAnalyzer`, have only one instance throughout the application's lifecycle. This is achieved by registering these services as singletons in the `RegisterServices` method. This pattern helps in maintaining a single source of truth and managing shared resources efficiently.

### Factory

The Factory pattern is used to create instances of various violation-related classes. The `ViolationFactory` and `ViolationFileDetailFactory` are responsible for creating instances of `IViolation` and `IViolationFileDetail`, respectively. This pattern supports the **O**pen/Closed Principle by allowing the system to be open for extension but closed for modification.

### Strategy

The Strategy pattern is employed to encapsulate the algorithms for checking each of the SOLID principles. Each principle has its own checker class (e.g., `SRPChecker`, `OCPChecker`, `LSPChecker`, `ISPChecker`, `DIPChecker`), and these classes implement the respective interfaces (`ISRPChecker`, `IOCPChecker`, `ILSPChecker`, `IISPChecker`, `IDIPChecker`). This pattern adheres to the **S**ingle Responsibility Principle by ensuring each class has one reason to change.

## Architecture

The architecture of SOLIDify is designed to be modular and extensible. The main components are:

- **Program**: The entry point of the application. It sets up the dependency injection container and initiates the project analysis.
- **IProjectAnalyzer**: An interface that defines the contract for analyzing a project.
- **ProjectAnalyzer**: The concrete implementation of `IProjectAnalyzer`. It uses `IMetricsProvider` to analyze the project's code files.
- **IMetricsProvider**: An interface that defines the contract for providing metrics related to SOLID principles.
- **SOLIDMetrics**: The concrete implementation of `IMetricsProvider`. It provides the actual metrics and checks for SOLID principles.
- **ViolationManager**: A class responsible for managing violations.
- **ViolationFileDetailManager**: A class responsible for managing violation file details.

This architecture ensures adherence to the SOLID principles by promoting loose coupling, high cohesion, and separation of concerns. Each component has a well-defined responsibility, making the system easier to understand, maintain, and extend.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

