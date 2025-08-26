# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project structure with .NET 9 and ASP.NET Core
- Form submission handling via REST API
- PostgreSQL database integration with Entity Framework Core
- MailJet email connector for notifications
- Slack webhook connector for team notifications
- Private form generation with expiration
- Rate limiting and CORS configuration
- Background task processing with retry logic
- Comprehensive test suite with xUnit
- Docker support for containerized deployment
- GitHub Actions CI/CD pipeline
- API documentation with Swagger/OpenAPI

### Security
- Input validation and sanitization
- SQL injection protection via Entity Framework
- Rate limiting to prevent abuse
- CORS policy enforcement

## [0.1.0] - TBD

### Added
- First public release
- Core form handling functionality
- Basic connector system
- PostgreSQL persistence

[Unreleased]: https://github.com/yourusername/formfeeder-oss/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/yourusername/formfeeder-oss/releases/tag/v0.1.0