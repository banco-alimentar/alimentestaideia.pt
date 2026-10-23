# Contributing to [alimentestaideia.pt](http://alimentestaideia.pt/)

[Alimentestaideia.pt](http://alimentestaideia.pt/) is website that enables the food donations for [Federação Portuguesa de Bancos Alimentares Contra a Fome](https://www.bancoalimentar.pt/).

Everybody is invited and welcome to contribute to this project. There is work for developers, for designers, for data reporting lovers, for testers and for anyone who wants to send any suggestions or submit bugs.

The process is straight-forward.

 - Read [the open issues](https://github.com/banco-alimentar/alimentestaideia.pt/issues) or submit a new one. Most important issues are labeled [very.high.priority](https://github.com/banco-alimentar/alimentestaideia.pt/issues?q=is%3Aissue+is%3Aopen+label%3Avery.high.priority) and [high.priority](https://github.com/banco-alimentar/alimentestaideia.pt/issues?q=is%3Aissue+is%3Aopen+label%3AHigh.Priority).
 - Once you pick an issue, please assign it to yourself.
 - Create a branch for your input. use your username "/" issue that you are solving. eg: `tiagonmas/112`.
 - Write the code for your issue.
 - Ensure tests work.
 - Keep package versions in `Directory.Packages.props`; project files should declare package references without versions.
 - Run `dotnet restore BancoAlimentar.AlimentaEstaIdeia.Web.sln` and `dotnet list BancoAlimentar.AlimentaEstaIdeia.Web.sln package --vulnerable --include-transitive --no-restore` when changing dependencies. Repeat both commands for `BancoAlimentar.AlimentaEstaIdeia.Tools.Tests/BancoAlimentar.AlimentaEstaIdeia.Tools.Tests.csproj`, which is not in the solution.
 - [Mark any commit with the issue that you're working on](https://gitdailies.com/articles/link-github-commit-to-issue/)
 - Create a Pull Request against the [developer](https://github.com/banco-alimentar/alimentestaideia.pt/tree/developer) branch. Pick an approver (eg: tiagonmas)

We make an effor to follow Microsoft's [Code With Engineering Playbook](https://github.com/Microsoft/code-with-engineering-playbook)

NuGet audits include direct and transitive dependencies at low severity or higher, and audit warnings fail the build. If a finding cannot be fixed immediately, add a `NuGetAuditSuppress` item for its advisory URL with a comment documenting the rationale and planned removal; do not suppress an NU190x warning category.

Still interested? Then you should take a peek at [Code of Conduct](CODE_OF_CONDUCT.md) and that you agree with the [Hippocratic License](LICENSE.md) start setting up your development environment:

- [Windows setup](Contributing-Windows-setup.md)
- [MacOS setup](Contributing-MacOS-setup.md)
