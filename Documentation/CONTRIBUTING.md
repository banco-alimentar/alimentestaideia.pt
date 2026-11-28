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
 - Run `dotnet restore BancoAlimentar.AlimentaEstaIdeia.Web.sln` and `dotnet list BancoAlimentar.AlimentaEstaIdeia.Web.sln package --vulnerable --include-transitive --no-restore` when changing dependencies. The solution includes the maintained tool projects and their tests.
 - [Mark any commit with the issue that you're working on](https://gitdailies.com/articles/link-github-commit-to-issue/)
 - Repository admins can push commits directly to the [developer](https://github.com/banco-alimentar/alimentestaideia.pt/tree/developer) branch. Admin direct pushes can bypass the existing validation ruleset, including required status checks and secret-alert resolution, but not the separate deletion-protection ruleset. Contributors without admin access should create a Pull Request against `developer` and pick an approver (eg: tiagonmas).

## Keeping the developer branch

All project pull requests must target `developer`, not `master`. Never use `developer` as the source branch of a pull request into `master`.

The repository has two complementary safeguards:

- **Settings > General > Pull Requests:** **Automatically delete head branches** is disabled. Merged feature branches need manual cleanup.
- **Settings > Rules > Rulesets:** [Preserve developer branch](https://github.com/banco-alimentar/alimentestaideia.pt/rules/28306376) is active, targets exactly `refs/heads/developer`, and restricts deletions with an empty bypass list. Ordinary branch deletion is blocked even for repository admins. The separate [developer validation ruleset](https://github.com/banco-alimentar/alimentestaideia.pt/rules/27791240) retains its existing checks and admin bypass.

Keep the retention ruleset separate from validation rules so allowing an admin to push directly does not also permit deleting the integration branch. Administrators can still edit repository settings; do not disable the retention rule or add bypass actors.

If `developer` is missing, verify its last commit using the relevant merged pull request's head SHA and merge parent before restoring it. Do not assume an old remote-tracking ref is current. On 2026-11-28, PR #951 verified `785ae39cb0379201aff3f8cf6bc79db697b1cbf8` as its final head commit; the branch was restored to that commit. After any restoration, verify the remote SHA, the branch's protected status, and that the no-bypass deletion rule applies. Do not test protection by attempting to delete the live branch.

We make an effor to follow Microsoft's [Code With Engineering Playbook](https://github.com/Microsoft/code-with-engineering-playbook)

NuGet audits include direct and transitive dependencies at low severity or higher, and audit warnings fail the build. If a finding cannot be fixed immediately, add a `NuGetAuditSuppress` item for its advisory URL with a comment documenting the rationale and planned removal; do not suppress an NU190x warning category.

Still interested? Then you should take a peek at [Code of Conduct](CODE_OF_CONDUCT.md) and that you agree with the [Hippocratic License](LICENSE.md) start setting up your development environment:

- [Windows setup](Contributing-Windows-setup.md)
- [MacOS setup](Contributing-MacOS-setup.md)
