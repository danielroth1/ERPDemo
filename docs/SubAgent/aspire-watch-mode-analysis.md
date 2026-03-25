# Aspire Watch Mode Analysis

## Scope

Investigate why .NET Aspire is not starting in development/watch mode in this repository, with emphasis on:

- AppHost orchestration
- launch settings
- solution and project configuration
- service launch profiles
- VS Code tasks and debug configs
- documentation that describes Aspire or hot reload
- any repo-specific issue that would prevent file watching or hot reload

No code changes were made as part of this analysis.

## Files Reviewed

- AppHost/Program.cs
- AppHost/AppHost.csproj
- AppHost/Properties/launchSettings.json
- .vscode/launch.json
- .vscode/tasks.json
- erp.sln
- README.md
- .env
- docker-compose.yml
- infrastructure/docker-compose.dev.yml
- docs/QUICK_START.md
- docs/DEV_QUICKSTART.md
- docs/LOCAL_DEBUGGING.md
- services/gateway/ApiGateway/Properties/launchSettings.json
- services/user-management/UserManagement/Properties/launchSettings.json
- services/inventory/InventoryManagement/Properties/launchSettings.json
- services/sales/SalesManagement/Properties/launchSettings.json
- services/financial/FinancialManagement/Properties/launchSettings.json
- services/dashboard/DashboardAnalytics/Properties/launchSettings.json
- services/orchestration/Orchestration/Properties/launchSettings.json
- representative service project files, including ApiGateway.csproj and UserManagement.csproj

## Executive Summary

The repository currently has two separate local-development models:

1. Aspire orchestration via AppHost
2. Manual hot reload via VS Code tasks that run dotnet watch for each backend service

Those two models are not equivalent in this repo.

The AppHost path is configured and documented as a normal project launch, not a watch-based launch. The VS Code Aspire launch configuration starts AppHost with a standard project launch, and the README also tells developers to start AppHost with dotnet run. In contrast, every repo document that discusses hot reload points developers to the manual watch tasks or direct dotnet watch run commands.

That means the current Aspire startup path does not provide the same backend hot reload workflow as the manual task-based path.

In addition, there is a concrete startup blocker in the current local setup: AppHost launch settings hard-code the Aspire dashboard and resource-service ports, and an existing AppHost instance was already listening on the resource-service port. A second AppHost launch therefore fails before orchestration completes.

## Findings

### 1. AppHost is launched as a normal project, not as dotnet watch

Evidence:

- README.md recommends:
  - dotnet run --project AppHost/AppHost.csproj
- .vscode/launch.json defines Aspire: Launch AppHost as:
  - type: dotnet
  - request: launch
  - projectPath: AppHost/AppHost.csproj
  - launchProfile: http

There is no watch-oriented AppHost task or launch configuration in the repo.

This means the default Aspire entry point in this repository is a normal launch flow, not a watch flow.

### 2. AppHost orchestration code does not opt child projects into dotnet watch

Evidence:

- AppHost/Program.cs uses builder.AddProject for all backend services.
- There is no repo code in AppHost that configures child services to use dotnet watch.
- There are no watch-specific environment variables, launch arguments, or custom executable commands configured for the backend projects in AppHost.

The frontend is the one exception: AppHost uses builder.AddViteApp("frontend", "../frontend", "dev"), which explicitly launches the frontend dev server. The backend services do not have an equivalent watch-specific configuration.

Conclusion:

- In this repo, AppHost is orchestrating backend projects as normal project resources.
- The repo does not contain any AppHost customization that would make backend projects run under dotnet watch.

### 3. The repository already defines a separate hot reload workflow, and it is not Aspire

Evidence:

- .vscode/tasks.json defines backend: watch-gateway, backend: watch-user-management, backend: watch-inventory, backend: watch-sales, backend: watch-financial, and backend: watch-dashboard.
- Each of those tasks runs dotnet watch run --project ...
- backend: watch-all-services depends on those watch tasks.
- docs/DEV_QUICKSTART.md, docs/LOCAL_DEBUGGING.md, and docs/QUICK_START.md consistently say hot reload requires dotnet watch run.
- docs/DEV_QUICKSTART.md explicitly says:
  - Hot reload not working: make sure you are using dotnet watch run, not dotnet run.

Conclusion:

- Repo documentation treats hot reload as the manual watch-task workflow.
- Aspire is recommended for orchestration convenience, but not documented as the backend hot reload path.

### 4. Production is not being forced by AppHost or service launch profiles

Evidence:

- AppHost/Properties/launchSettings.json sets:
  - ASPNETCORE_ENVIRONMENT=Development
  - DOTNET_ENVIRONMENT=Development
- All reviewed service launchSettings.json files set ASPNETCORE_ENVIRONMENT=Development.
- .vscode/launch.json backend debug configurations also set ASPNETCORE_ENVIRONMENT=Development.
- .env contains DOTNET_ENVIRONMENT=Development.

Important nuance:

- docker-compose.yml sets ASPNETCORE_ENVIRONMENT=Production for containerized services.
- That production compose file is separate from the manual watch workflow and separate from AppHost launch settings.
- infrastructure/docker-compose.dev.yml is explicitly infrastructure-only and is intended to be used with locally running dotnet watch services.

Conclusion:

- There is no evidence that AppHost local launch is being forced into Production by repository launch profiles.
- The production environment values are present in the full docker-compose setup, but that is a different local startup model.

### 5. There is no repo-specific watch suppression in project files

Searches found no evidence of:

- DOTNET_WATCH
- DOTNET_USE_POLLING_FILE_WATCHER
- Watch="false"
- custom MSBuild watch exclusions
- hot reload suppression flags

Representative service project files are standard ASP.NET Core projects without watch-disabling configuration.

Conclusion:

- No repo-specific .csproj or MSBuild configuration was found that would disable file watching or hot reload.
- The absence of hot reload under Aspire is a startup-mode issue, not a project-file watch suppression issue.

### 6. There is a concrete local startup blocker: fixed Aspire ports collide with an existing AppHost instance

Runtime evidence:

- Running dotnet run --project AppHost/AppHost.csproj failed with:
  - Failed to bind to address https://127.0.0.1:22227: address already in use.
- AppHost/Properties/launchSettings.json hard-codes the Aspire host ports, including the resource-service and dashboard endpoints.
- lsof showed an existing AppHost process already listening on port 22227.

Implication:

- Starting a second AppHost instance in this repo fails immediately.
- That can easily be mistaken for an Aspire watch-mode problem, even though the first failure is actually a port collision.

### 7. The current local setup can produce workflow collisions

Because the repository supports both:

- AppHost orchestration
- manual backend: watch-all-services tasks

developers can accidentally run both models at once.

That can cause:

- duplicate service launches
- fixed-port conflicts
- confusion about which process owns a service
- false expectations that Aspire should inherit the manual hot reload behavior

## Root Cause Analysis

### Root Cause 1

The repository’s Aspire entry point is not configured as a watch-based development workflow.

More specifically:

- the README starts AppHost with dotnet run
- the VS Code Aspire launch is a normal project launch
- AppHost AddProject resources do not include any repo-specific watch customization

So the current AppHost path should be expected to behave like a normal Aspire launch, not like backend: watch-all-services.

### Root Cause 2

The repository documents two different local workflows, but only one of them guarantees backend hot reload.

- Manual local workflow: yes, via dotnet watch run
- Aspire workflow: no explicit watch configuration is present

This mismatch creates the impression that Aspire is broken, when the actual problem is that the repo equates two different startup models in its developer guidance.

### Root Cause 3

Fixed Aspire local ports in AppHost/Properties/launchSettings.json make AppHost startup fragile when a prior AppHost instance is still running.

In the current environment, a live AppHost process was already bound to port 22227, which prevented another AppHost launch from starting at all.

## What Is Not the Root Cause

- AppHost launch settings forcing Production: not found
- service launch profiles forcing Production: not found
- .env forcing Production: not found
- repo-specific .csproj watch suppression: not found
- service project launch profiles missing Development: not found

## Minimal Code or Config Changes Needed

These are the smallest changes that would make the repo behavior match developer expectations more closely.

### Option A: Minimal and recommended

Clarify the workflows instead of changing orchestration semantics.

1. Add a dedicated VS Code task for AppHost watch mode:
   - dotnet watch run --project AppHost/AppHost.csproj
2. Add a matching launch or task entry named clearly, for example:
   - Aspire: Watch AppHost
3. Update README.md and local-dev docs to state explicitly:
   - Aspire: Launch AppHost is standard orchestration
   - backend hot reload is guaranteed only when using dotnet watch based startup
   - if using AppHost, use the new Aspire watch task when hot reload is desired
4. Add one short troubleshooting note for stale AppHost port ownership:
   - stop the previous AppHost before launching another one

This is the lowest-risk change set because it preserves the current AppHost model and removes the workflow ambiguity.

### Option B: Also useful, but secondary

Reduce port-collision failures for AppHost.

1. Change the AppHost startup guidance to avoid launching multiple instances.
2. Optionally adjust the launch profile strategy if the team wants less rigid local port binding for Aspire dashboard/resource-service endpoints.

This helps startup reliability but does not by itself add backend hot reload.

## Validation Steps After the Fix

Use these steps to confirm the intended hot reload behavior.

### Validate standard Aspire startup

1. Ensure no old AppHost instance is running.
2. Start AppHost once.
3. Confirm the Aspire dashboard opens and all backend services start.
4. Confirm there are no bind errors for ports 15227, 17227, 19227, 20227, 21227, or 22227.

### Validate AppHost watch mode

1. Start the new AppHost watch task or command:
   - dotnet watch run --project AppHost/AppHost.csproj
2. Edit a simple backend file, such as a controller or log message.
3. Save the file.
4. Confirm one of the following occurs without manual restart:
   - hot reload applies the change, or
   - dotnet watch restarts the affected app automatically
5. Confirm the terminal shows dotnet watch output rather than only normal application startup output.

### Validate environment selection

1. Start the AppHost workflow.
2. Check one backend service startup log.
3. Confirm the service is running in Development, not Production.
4. If needed, hit a Development-only behavior such as Swagger or Development config logging to confirm.

### Validate no workflow collision

1. Do not run backend: watch-all-services at the same time as AppHost unless the team intentionally supports that combination.
2. Verify only one local owner exists for each backend service port.
3. Verify only one AppHost instance owns the Aspire dashboard/resource-service ports.

## Final Conclusion

There is no evidence that this repository is forcing Aspire into Production or disabling file watching through project configuration.

The real issues are:

1. The current Aspire path is a normal run/debug launch, not a watch-based backend development workflow.
2. The repo documentation mixes Aspire startup guidance with a separate manual hot reload workflow, which creates incorrect expectations.
3. Fixed AppHost dashboard/resource-service ports make second launches fail when an old AppHost instance is still running.

The smallest effective fix is to add an explicit AppHost watch task or launch profile, document the difference between standard Aspire launch and watch-enabled development, and add a short note about stale AppHost port collisions.