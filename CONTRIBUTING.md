# Contributing to FormFeeder

Thank you for your interest in contributing to FormFeeder! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

By participating in this project, you agree to abide by our code of conduct:
- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive criticism
- Accept feedback gracefully

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/yourusername/formfeeder-oss.git
   cd formfeeder-oss
   ```
3. **Add the upstream remote**:
   ```bash
   git remote add upstream https://github.com/originalowner/formfeeder-oss.git
   ```
4. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Process

### Prerequisites

- .NET 9 SDK
- PostgreSQL (or Docker)
- Visual Studio 2022, VS Code, or JetBrains Rider
- Git

### Setting Up Your Development Environment

1. **Install dependencies**:
   ```bash
   dotnet restore
   dotnet tool restore
   ```

2. **Set up the database**:
   ```bash
   # Using Docker
   docker run -d \
     --name formfeeder-dev-db \
     -e POSTGRES_PASSWORD=devpassword \
     -e POSTGRES_DB=formfeeder_dev \
     -p 5432:5432 \
     postgres:16

   # Apply migrations
   dotnet ef database update -p FormFeeder.Api
   ```

3. **Run tests**:
   ```bash
   dotnet test
   ```

4. **Start the development server**:
   ```bash
   dotnet watch run --project FormFeeder.Api
   ```

### Making Changes

1. **Write clean, maintainable code** following C# conventions
2. **Follow the existing code style** - use the provided .editorconfig
3. **Add tests** for new functionality
4. **Update documentation** if needed
5. **Keep commits focused** - one feature/fix per commit
6. **Write clear commit messages** following conventional commits:
   - `feat:` New feature
   - `fix:` Bug fix
   - `docs:` Documentation changes
   - `test:` Test additions or changes
   - `refactor:` Code refactoring
   - `style:` Code style changes
   - `perf:` Performance improvements
   - `ci:` CI/CD changes

### Code Style

The project uses:
- **EditorConfig** for consistent formatting
- **StyleCop Analyzers** for code style enforcement
- **dotnet-format** for automatic formatting

Before committing:
```bash
# Format your code
dotnet format

# Run code analysis
dotnet build
```

### Testing

- **Write unit tests** for all new business logic
- **Write integration tests** for new endpoints
- **Maintain test coverage** above 80%
- **Use descriptive test names** that explain what is being tested

Run tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Documentation

- **Update the README** if you change functionality
- **Add XML comments** to public APIs
- **Update API documentation** for new endpoints
- **Include examples** where appropriate

## Submitting Changes

### Pull Request Process

1. **Update your feature branch** with the latest upstream changes:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Push your changes** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

3. **Create a Pull Request** on GitHub with:
   - Clear title describing the change
   - Description of what was changed and why
   - Reference to any related issues
   - Screenshots for UI changes (if applicable)

4. **Address review feedback** promptly
5. **Ensure CI passes** - all tests and checks must pass

### Pull Request Checklist

- [ ] Code follows the project style guidelines
- [ ] Self-review completed
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] No breaking changes (or discussed with maintainers)
- [ ] Commit messages follow conventional commits
- [ ] Branch is up-to-date with main

## Types of Contributions

### Bug Reports

Create an issue with:
- Clear, descriptive title
- Steps to reproduce
- Expected vs actual behavior
- System information (.NET version, OS)
- Relevant logs or error messages

### Feature Requests

Create an issue with:
- Clear use case
- Proposed solution
- Alternative solutions considered
- Potential impact on existing functionality

### Code Contributions

We welcome:
- Bug fixes
- New features
- Performance improvements
- Documentation improvements
- Test improvements
- Refactoring for better code quality

### New Connectors

To add a new connector:
1. Implement the `IConnector` interface
2. Add comprehensive tests
3. Document configuration options
4. Provide usage examples
5. Update the README

### Docker Development

#### Building Images Locally
```bash
# Build for local testing
docker build -t formfeeder:dev ./FormFeeder.Api

# Multi-platform build (requires Docker buildx)
docker buildx build --platform linux/amd64,linux/arm64 -t formfeeder:dev ./FormFeeder.Api
```

#### Testing with Docker
```bash
# Run with local PostgreSQL
docker run -d \
  --name formfeeder-dev \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__PostgreSQL="Host=host.docker.internal;Database=formfeeder_dev;Username=postgres;Password=devpassword" \
  formfeeder:dev

# Check logs
docker logs -f formfeeder-dev

# Stop and remove
docker stop formfeeder-dev && docker rm formfeeder-dev
```

#### Docker Images in CI/CD
The CI/CD pipeline automatically builds and publishes Docker images to GitHub Container Registry:
- **On push to main**: Creates `ghcr.io/[owner]/formfeeder-oss:latest`
- **On release tags**: Creates `ghcr.io/[owner]/formfeeder-oss:v1.0.0`

No Docker Hub account or manual publishing is required.

## Release Process

Maintainers handle releases following semantic versioning:
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## Getting Help

- **Discord**: [Join our server](https://discord.gg/formfeeder)
- **Discussions**: Use GitHub Discussions for questions
- **Issues**: Report bugs or request features

## Recognition

Contributors are recognized in:
- Release notes
- Contributors section in README
- GitHub contributors page

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

Feel free to open an issue or discussion if you have questions about contributing!