# CI/CD & Build Configuration

## Overview

This document describes the Continuous Integration and Continuous Deployment (CI/CD) setup for the ProjectX LEAN Brokerage integration.

## Build System

### Technology Stack
- **Framework**: .NET 10 (Preview)
- **Build Tool**: MSBuild via `dotnet` CLI
- **CI Platform**: GitHub Actions
- **Test Framework**: NUnit 4.x

### Framework Version History
- **Current**: .NET 10 Preview (March 2026) - Active development
- **Previous**: .NET 8.0 (LTS)
- **Rationale**: .NET 10 required for LEAN Engine compatibility

## GitHub Actions Workflows

### Main Build Workflow (`.github/workflows/build.yml`)

**Triggers:**
- Push to `master`, `develop` branches
- Pull requests to `master`, `develop`
- Manual workflow dispatch

**Branching Strategy:**
- **`master`**: Release branch (production-ready code only)
- **`develop`**: Active development branch (target for feature branches)
- **Feature branches**: Branch off from `develop`, merge back via PR
- **Release flow**: `master` ← `develop` ← `feature-branch`

**Jobs:**

#### 1. Build Job
**Purpose**: Compile and test on multiple operating systems

**Matrix Strategy**:
- Ubuntu Latest (Linux)
- Windows Latest
- macOS Latest

**Steps**:
1. **Checkout Code**
   - Checks out ProjectX Brokerage repository
   - Checks out LEAN repository (dependency)
   
2. **Setup .NET**
   - Installs .NET 10 Preview SDK (`dotnet-quality: 'preview'`)
   
3. **Restore Dependencies**
   - Runs `dotnet restore` to download NuGet packages
   
4. **Build Solution**
   - Compiles in Release configuration
   - Validates all projects build successfully
   
5. **Run Tests**
   - Executes NUnit tests
   - **Filters**: Excludes tests requiring live API credentials
   - Test filter: `Category!=RequiresApiCredentials&Category!=Integration`
   - Generates TRX test results
   
6. **Publish Test Results**
   - Uploads test results to GitHub Actions UI
   - Only on Linux runner (avoids duplication)
   
7. **Upload Build Artifacts**
   - Uploads compiled binaries (Linux only)
   - Retention: 7 days

#### 2. Code Quality Job
**Purpose**: Ensure code quality and security

**Checks**:
- **Code Formatting**: Verifies consistent code style using `dotnet format`
- **Security Scan**: Checks for vulnerable NuGet package dependencies
- **Continues on error**: Won't block builds (advisory only)

#### 3. Documentation Job
**Purpose**: Validate documentation completeness

**Checks**:
- Verifies existence of key documentation files:
  - ✅ **Required**: `README.md`
  - ⚠️ **Optional**: `PRD.md`, `CONFIGURATION.md`, `config-schema.json`
- Validates JSON file syntax

## Local Build Instructions

### Prerequisites
```bash
# Install .NET 10 Preview SDK
# Windows (via installer):
# Download from: https://dotnet.microsoft.com/download/dotnet/10.0

# macOS/Linux (via script):
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 10.0.103 --quality preview
```

### Clone Repositories
```bash
# Clone ProjectX Brokerage
git clone https://github.com/adammarquette/Lean.Brokerages.ProjectX.git
cd Lean.Brokerages.ProjectX

# Switch to develop branch for active development
git checkout develop

# Clone LEAN (required dependency)
cd ..
git clone https://github.com/adammarquette/Lean.git
cd Lean.Brokerages.ProjectX

# Create a feature branch from develop
git checkout -b feature/my-feature develop
```

**Directory Structure**:
```
parent-directory/
├── Lean/                           # LEAN Engine repository
└── Lean.Brokerages.ProjectX/       # ProjectX Brokerage (this repo)
```

### Build Commands

#### Full Build
```bash
# Restore NuGet packages
dotnet restore QuantConnect.ProjectXBrokerage.sln

# Build solution (Release configuration)
dotnet build QuantConnect.ProjectXBrokerage.sln --configuration Release

# Build solution (Debug configuration)
dotnet build QuantConnect.ProjectXBrokerage.sln --configuration Debug
```

#### Individual Project Build
```bash
# Build main brokerage project only
dotnet build QuantConnect.ProjectXBrokerage/QuantConnect.ProjectXBrokerage.csproj

# Build tests only
dotnet build QuantConnect.ProjectXBrokerage.Tests/QuantConnect.ProjectXBrokerage.Tests.csproj

# Build ToolBox only
dotnet build QuantConnect.ProjectXBrokerage.ToolBox/QuantConnect.ProjectXBrokerage.ToolBox.csproj
```

#### Clean Build
```bash
# Clean all build outputs
dotnet clean QuantConnect.ProjectXBrokerage.sln

# Rebuild from scratch
dotnet build QuantConnect.ProjectXBrokerage.sln --no-incremental
```

## Running Tests

### All Tests (Excluding Live API Tests)
```bash
dotnet test QuantConnect.ProjectXBrokerage.sln \
  --configuration Release \
  --filter "Category!=RequiresApiCredentials&Category!=Integration"
```

### Unit Tests Only
```bash
dotnet test QuantConnect.ProjectXBrokerage.Tests/QuantConnect.ProjectXBrokerage.Tests.csproj \
  --filter "FullyQualifiedName~UnitTests"
```

### Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~ProjectXBrokerageSymbolMapperTests"
```

### Run With Code Coverage
```bash
dotnet test QuantConnect.ProjectXBrokerage.sln \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults
```

### Test Categories

Tests are categorized using NUnit's `[Category]` attribute:

- **No Category**: Standard unit tests (always run)
- **`[Category("RequiresApiCredentials")]`**: Tests requiring live ProjectX API keys (skipped in CI)
- **`[Category("Integration")]`**: Integration tests with external dependencies (skipped in CI)
- **`[Category("Performance")]`**: Performance benchmarks (run manually)

**Example**:
```csharp
[Test]
[Category("RequiresApiCredentials")]
public void PlaceOrder_WithValidCredentials_Succeeds()
{
    // This test will be skipped in GitHub Actions
}
```

## Code Quality Tools

### Code Formatting (dotnet-format)
```bash
# Check code formatting (dry-run)
dotnet format QuantConnect.ProjectXBrokerage.sln --verify-no-changes

# Auto-fix formatting issues
dotnet format QuantConnect.ProjectXBrokerage.sln
```

### Security Scanning
```bash
# Check for vulnerable packages
dotnet list QuantConnect.ProjectXBrokerage.sln package --vulnerable --include-transitive
```

### Static Code Analysis (Optional)
```bash
# Install Roslyn analyzers (if not already included)
dotnet add package Microsoft.CodeAnalysis.NetAnalyzers

# Build with analysis warnings as errors
dotnet build /p:TreatWarningsAsErrors=true
```

## Troubleshooting

### Common Build Issues

#### Issue 1: "LEAN projects not found"
**Cause**: LEAN repository not cloned or in wrong location

**Solution**:
```bash
# Ensure LEAN is at ../Lean relative to Lean.Brokerages.ProjectX
cd ..
git clone https://github.com/adammarquette/Lean.git
cd Lean.Brokerages.ProjectX
```

#### Issue 2: "TargetFramework 'net10.0' is not supported"
**Cause**: .NET 10 SDK not installed (should not happen after migration to .NET 8)

**Solution**: Verify `global.json` and all `.csproj` files target `net8.0`

#### Issue 3: NuGet restore failures
**Cause**: Missing NuGet package sources or authentication

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore with verbose logging
dotnet restore --verbosity detailed
```

#### Issue 4: Tests fail in CI but pass locally
**Cause**: Environment-specific issues or missing test isolation

**Solution**:
- Ensure tests don't depend on local file paths
- Mock external dependencies
- Check for test parallelization conflicts

### GitHub Actions Debugging

#### View Workflow Logs
1. Go to repository → **Actions** tab
2. Select failing workflow run
3. Click on failed job → Expand step logs

#### Enable Debug Logging
```bash
# Add repository secret (Settings → Secrets → Actions):
ACTIONS_STEP_DEBUG = true
ACTIONS_RUNNER_DEBUG = true
```

#### Re-run Failed Jobs
- Click **Re-run jobs** → **Re-run failed jobs** in Actions UI

## Build Status Badge

Add this to `README.md`:

```markdown
[![Build Status](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions/workflows/build.yml/badge.svg)](https://github.com/adammarquette/Lean.Brokerages.ProjectX/actions)
```

## Performance Benchmarks

### Build Times (Approximate)
- **Ubuntu**: 2-3 minutes
- **Windows**: 3-4 minutes
- **macOS**: 3-4 minutes

### Optimization Tips
- Enable NuGet caching in GitHub Actions (already enabled)
- Use `--no-restore` and `--no-build` flags when appropriate
- Run tests in parallel (default NUnit behavior)

## Future Enhancements

### Planned CI/CD Improvements
- [ ] Add code coverage reporting (Coveralls/Codecov)
- [ ] Implement release automation with semantic versioning
- [ ] Add Docker build for containerized testing
- [ ] Create nightly builds against LEAN's latest commit
- [ ] Add performance regression testing
- [ ] Integrate with QuantConnect Cloud CI (if applicable)

### .NET Version Strategy
- **Current**: .NET 10 Preview (matches LEAN)
- **Future**: Track LEAN's .NET version; migrate when LEAN migrates
- **Note**: When .NET 10 reaches stable release, remove `dotnet-quality: 'preview'` from GitHub Actions workflow

## Contributing

### Pre-commit Checklist
- [ ] Code builds locally without errors
- [ ] All tests pass (excluding live API tests)
- [ ] Code formatted: `dotnet format`
- [ ] No new security vulnerabilities
- [ ] XML documentation for public APIs
- [ ] Updated `CHANGELOG.md` (if applicable)

### Pull Request Process
1. Create feature branch: `feature/your-feature-name`
2. Commit changes with descriptive messages
3. Push to GitHub
4. Open Pull Request against `develop` branch
5. Ensure all CI checks pass (green ✓)
6. Request review from maintainers
7. Address review feedback
8. Merge after approval

## References

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET CLI Reference](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [NUnit Documentation](https://docs.nunit.org/)
- [LEAN Contribution Guide](https://github.com/QuantConnect/Lean/blob/master/CONTRIBUTING.md)

---

**Document Version**: 1.0  
**Last Updated**: March 2026  
**Maintained By**: Project Lead
