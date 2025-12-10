# Repository Structure and Development Workflow

## 📁 Repository Structure

```
erp/
├── .github/
│   ├── workflows/
│   │   └── ci-cd.yml              # CI/CD pipeline
│   └── README.md                   # This file
│
├── services/                       # Microservices
│   ├── user-management/           # User authentication & management
│   │   ├── UserManagement/        # Main project
│   │   │   ├── Controllers/
│   │   │   ├── Models/
│   │   │   ├── Services/
│   │   │   ├── Infrastructure/
│   │   │   └── Program.cs
│   │   ├── Dockerfile
│   │   ├── .dockerignore
│   │   └── README.md
│   │
│   ├── inventory/                 # Inventory & stock management
│   ├── sales/                     # Orders & invoices
│   ├── financial/                 # Accounting & ledger
│   ├── dashboard/                 # Analytics & reporting
│   └── gateway/               # API Gateway with YARP
│
├── frontend/                       # React SPA
│   ├── src/
│   │   ├── features/              # Feature-based organization
│   │   │   ├── auth/
│   │   │   ├── inventory/
│   │   │   ├── sales/
│   │   │   ├── financial/
│   │   │   └── dashboard/
│   │   ├── store/                 # Redux store
│   │   ├── components/            # Shared components
│   │   ├── services/              # API clients
│   │   └── App.tsx
│   ├── Dockerfile
│   ├── nginx.conf
│   └── package.json
│
├── infrastructure/                 # Infrastructure as Code
│   ├── k8s/
│   │   ├── base/                  # Base Kubernetes manifests
│   │   │   ├── mongodb.yaml
│   │   │   ├── kafka.yaml
│   │   │   └── services/
│   │   ├── local/                 # Local dev overlays
│   │   │   └── kustomization.yaml
│   │   └── production/            # Production configs
│   │       ├── kustomization.yaml
│   │       ├── ingress.yaml
│   │       └── secrets.yaml.example
│   │
│   ├── monitoring/                # Prometheus & Grafana
│   │   ├── prometheus/
│   │   │   └── prometheus.yml
│   │   └── grafana/
│   │       ├── datasources.yml
│   │       ├── dashboards.yml
│   │       └── dashboards/
│   │
│   ├── logging/                   # Loki configuration
│   │   └── loki-config.yml
│   │
│   ├── cert-manager/              # TLS certificate management
│   │   ├── cluster-issuer.yaml
│   │   └── install.sh
│   │
│   └── docker/                    # Docker-specific configs
│       └── mongodb-init.js        # MongoDB initialization
│
├── tests/                          # Test suites
│   ├── unit/                      # Unit tests per service
│   ├── integration/               # Integration tests
│   └── e2e/                       # End-to-end Playwright tests
│
├── docs/                           # Documentation
│   ├── IMPLEMENTATION_GUIDE.md    # Step-by-step implementation
│   ├── LOCAL_DEVELOPMENT.md       # Local setup guide
│   ├── DEPLOYMENT.md              # Production deployment
│   ├── ARCHITECTURE.md            # System architecture
│   ├── API_DOCUMENTATION.md       # API reference
│   ├── MONITORING.md              # Observability guide
│   └── TESTING.md                 # Testing strategy
│
├── .gitignore                      # Git ignore rules
├── .env.example                    # Environment variables template
├── docker-compose.yml              # Local Docker Compose stack
├── skaffold.yaml                   # Skaffold configuration
└── README.md                       # Project overview
```

## 🔄 Development Workflow

### Branch Strategy

We use **Git Flow** with the following branch structure:

- `main` - Production-ready code
- `develop` - Integration branch for features
- `feature/*` - New features
- `bugfix/*` - Bug fixes
- `hotfix/*` - Production hotfixes
- `release/*` - Release preparation

### Branch Naming Conventions

```
feature/user-authentication
feature/inventory-low-stock-alerts
bugfix/order-status-update
hotfix/security-jwt-validation
release/v1.0.0
```

### Workflow Steps

#### 1. Start New Feature
```bash
# Create feature branch from develop
git checkout develop
git pull origin develop
git checkout -b feature/your-feature-name

# Make changes and commit
git add .
git commit -m "feat: add user registration endpoint"

# Push to remote
git push origin feature/your-feature-name
```

#### 2. Create Pull Request

**PR Template:**
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] New feature
- [ ] Bug fix
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests pass
- [ ] E2E tests pass
- [ ] Manual testing completed

## Checklist
- [ ] Code follows project coding standards
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] No console.log or debug code
- [ ] Dependencies are up to date
```

#### 3. Code Review Requirements

- **Minimum 1 approval** required
- **All CI checks must pass**:
  - Backend tests (70%+ coverage)
  - Frontend tests (70%+ coverage)
  - Linting passes
  - Build succeeds
- **No merge conflicts**
- **Branch up to date** with base branch

#### 4. Merge Process

```bash
# Update feature branch with latest develop
git checkout develop
git pull origin develop
git checkout feature/your-feature-name
git rebase develop

# Squash commits if needed
git rebase -i HEAD~3

# Push (force if rebased)
git push origin feature/your-feature-name --force-with-lease
```

After approval, use **Squash and Merge** to keep history clean.

## 📝 Commit Message Convention

We follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `perf`: Performance improvements
- `ci`: CI/CD changes

### Examples
```
feat(auth): add JWT refresh token endpoint

Implements refresh token functionality to extend user sessions
without requiring re-authentication.

Closes #123

---

fix(inventory): correct stock calculation for concurrent updates

Use pessimistic locking to prevent race conditions when multiple
users adjust stock simultaneously.

Resolves #456

---

docs(api): add GraphQL schema documentation

Add inline documentation for all GraphQL types and queries.
```

## 🧪 Testing Requirements

### Before Committing
```bash
# Backend tests
dotnet test

# Frontend tests
cd frontend && npm test

# Linting
cd frontend && npm run lint
```

### Pre-commit Hook (recommended)
Create `.git/hooks/pre-commit`:
```bash
#!/bin/sh

echo "Running pre-commit checks..."

# Run backend tests
dotnet test --no-build --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ Backend tests failed"
    exit 1
fi

# Run frontend tests
cd frontend && npm test -- --run
if [ $? -ne 0 ]; then
    echo "❌ Frontend tests failed"
    exit 1
fi

echo "✅ All checks passed"
exit 0
```

## 🔍 Code Review Guidelines

### What to Look For

**Architecture**
- Follows microservices patterns
- Proper separation of concerns
- Appropriate use of design patterns

**Code Quality**
- Readable and maintainable
- No code duplication
- Proper error handling
- Appropriate logging

**Security**
- No hardcoded secrets
- Input validation
- Authentication/authorization checks
- SQL injection prevention

**Performance**
- No N+1 queries
- Proper indexing
- Caching where appropriate
- Async operations used correctly

**Testing**
- Adequate test coverage
- Tests are meaningful
- Edge cases covered
- Integration tests for critical paths

### Providing Feedback

**Good Feedback:**
```
❌ "This code is bad"
✅ "Consider extracting this logic into a separate service to improve testability and follow SRP"

❌ "Wrong approach"
✅ "This approach might cause performance issues with large datasets. Consider pagination?"

❌ "Fix this"
✅ "This could throw NullReferenceException. Add null check or use nullable reference types"
```

## 🚀 Release Process

### 1. Create Release Branch
```bash
git checkout develop
git pull origin develop
git checkout -b release/v1.0.0
```

### 2. Update Version Numbers
- Update `version` in all `csproj` files
- Update `version` in `package.json`
- Update CHANGELOG.md

### 3. Final Testing
```bash
# Run full test suite
dotnet test
cd frontend && npm test

# Build Docker images
docker-compose build

# Test locally
docker-compose up
```

### 4. Merge to Main
```bash
git checkout main
git merge release/v1.0.0 --no-ff
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin main --tags

# Merge back to develop
git checkout develop
git merge release/v1.0.0 --no-ff
git push origin develop
```

### 5. Deploy to Production
Triggered automatically by CI/CD when pushing to `main` with new tag.

## 📊 Project Management

### Issue Labels
- `bug` - Something isn't working
- `enhancement` - New feature or request
- `documentation` - Documentation improvements
- `good first issue` - Good for newcomers
- `help wanted` - Extra attention needed
- `priority: high` - High priority
- `priority: medium` - Medium priority
- `priority: low` - Low priority
- `wontfix` - This will not be worked on

### Issue Template
```markdown
## Description
Clear description of the issue

## Steps to Reproduce (for bugs)
1. Go to '...'
2. Click on '...'
3. See error

## Expected Behavior
What should happen

## Actual Behavior
What actually happens

## Environment
- OS: [e.g. Windows 11]
- Browser: [e.g. Chrome 120]
- Version: [e.g. 1.0.0]

## Screenshots
If applicable
```

## 🤝 Contributing

### Getting Started
1. Fork the repository
2. Clone your fork
3. Create a feature branch
4. Make changes
5. Submit pull request

### Coding Standards

**C# (.NET)**
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use async/await for asynchronous operations
- Use dependency injection
- Add XML documentation comments for public APIs

**TypeScript/React**
- Use functional components with hooks
- Follow [Airbnb React Style Guide](https://github.com/airbnb/javascript/tree/master/react)
- Use TypeScript strict mode
- Prefer const over let

**General**
- Write self-documenting code
- Keep functions small and focused
- Use meaningful variable names
- Add comments for complex logic only

## 📞 Contact

For questions or support:
- Create an issue in GitHub
- Contact: [your-email@example.com]
- Team Chat: [Slack/Discord link]

## 📄 License

MIT License - see LICENSE file for details
