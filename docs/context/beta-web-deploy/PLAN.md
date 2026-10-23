# Beta web deployment pipeline plan

## Status

Implementation in progress.

## Tasks

1. Create `azure-pipelines/beta-debug.yml`. **Complete**
   - Copy the Developer pipeline's Build stage and Web publish artifact contract.
   - Change the trigger to `734-2026-atcud` and the deployment slot/environment to
     `beta`/`Beta`.
   - Omit Function App, migration, swap, and timer tasks.
   - Verify: inspect YAML trigger, task list, package path, and slot values.

2. Update `Documentation/CI-Azure-DevOps.md`. **Complete**
   - Document the Beta pipeline, branch, Web-only limitation, Beta URL, and required
     Azure DevOps environment.
   - Verify: documentation agrees with the YAML values.

3. Validate and commit the repository change. **In progress**
   - Validate YAML structure and compare the Build task sequence with
     `developer-debug.yml`.
   - Commit the pipeline and documentation on `734-2026-atcud`.

4. Publish and register the pipeline.
   - Push `734-2026-atcud` to the configured Git remote.
   - Create the Azure DevOps YAML pipeline using `azure-pipelines/beta-debug.yml` on
     that branch.
   - Verify the Azure DevOps definition has the exact branch trigger and no Function
     deployment stage.

5. Run a controlled first deployment.
   - Queue or allow one run after registration.
   - Verify the `Beta` deployment environment and
     `https://alimentaestaideia-beta.azurewebsites.net` show the new build.

## Dependency graph

`YAML + documentation` -> `validation + commit` -> `push + Azure DevOps registration`
-> `first deployment verification`
