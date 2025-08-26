.PHONY: help build test clean run watch format lint migrate docker-build docker-run setup coverage

# Default target
help: ## Show this help message
	@echo "FormFeeder Development Commands"
	@echo "================================"
	@echo ""
	@awk 'BEGIN {FS = ":.*##"; printf "Usage: make [target]\n\n"} /^[a-zA-Z_-]+:.*?##/ { printf "  %-15s %s\n", $$1, $$2 }' $(MAKEFILE_LIST)

# Setup
setup: ## Initial project setup
	@echo "Setting up FormFeeder development environment..."
	dotnet restore
	dotnet tool restore
	@echo "Setup complete! Run 'make run' to start the application."

# Build
build: ## Build the solution
	dotnet build --configuration Release

clean: ## Clean build artifacts
	dotnet clean
	find . -type d -name bin -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name obj -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name TestResults -exec rm -rf {} + 2>/dev/null || true

# Development
run: ## Run the API in development mode
	dotnet run --project FormFeeder.Api

watch: ## Run with hot reload
	dotnet watch run --project FormFeeder.Api

# Testing
test: ## Run all tests
	dotnet test --logger "console;verbosity=normal"

test-unit: ## Run unit tests only
	dotnet test --filter "Category!=Integration" --logger "console;verbosity=normal"

test-integration: ## Run integration tests only
	dotnet test --filter "Category=Integration" --logger "console;verbosity=normal"

coverage: ## Run tests with coverage report
	dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
	dotnet reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
	@echo "Coverage report generated at coveragereport/index.html"

# Code Quality
format: ## Format code
	dotnet format

format-check: ## Check code formatting
	dotnet format --verify-no-changes

lint: format-check ## Lint code (alias for format-check)

analyze: ## Run code analysis
	dotnet build /p:TreatWarningsAsErrors=true /p:EnforceCodeStyleInBuild=true

# Database
migrate: ## Apply database migrations
	dotnet ef database update -p FormFeeder.Api

migrate-add: ## Create a new migration (usage: make migrate-add NAME=MigrationName)
	@if [ -z "$(NAME)" ]; then \
		echo "Error: Please provide a migration name. Usage: make migrate-add NAME=MigrationName"; \
		exit 1; \
	fi
	dotnet ef migrations add $(NAME) -p FormFeeder.Api

migrate-remove: ## Remove the last migration
	dotnet ef migrations remove -p FormFeeder.Api

migrate-list: ## List all migrations
	dotnet ef migrations list -p FormFeeder.Api

db-drop: ## Drop the database
	dotnet ef database drop -p FormFeeder.Api -f

db-reset: db-drop migrate ## Reset database (drop and recreate)

# Docker
docker-build: ## Build Docker image
	docker build -t formfeeder:latest ./FormFeeder.Api

docker-run: ## Run Docker container
	docker run -d \
		-p 8080:8080 \
		-e ASPNETCORE_ENVIRONMENT=Development \
		--name formfeeder \
		formfeeder:latest

docker-stop: ## Stop Docker container
	docker stop formfeeder || true
	docker rm formfeeder || true

docker-logs: ## View Docker logs
	docker logs -f formfeeder

docker-compose-up: ## Start with docker-compose
	docker-compose up -d

docker-compose-down: ## Stop docker-compose
	docker-compose down

# CI/CD
ci: clean build test format-check ## Run CI checks locally

release: ## Create a release build
	dotnet publish FormFeeder.Api/FormFeeder.Api.csproj \
		--configuration Release \
		--output ./publish \
		--self-contained false \
		--runtime linux-x64

# Database helpers
db-start: ## Start PostgreSQL with Docker
	docker run -d \
		--name formfeeder-db \
		-e POSTGRES_PASSWORD=postgres \
		-e POSTGRES_DB=formfeeder \
		-p 5432:5432 \
		postgres:16 || docker start formfeeder-db

db-stop: ## Stop PostgreSQL container
	docker stop formfeeder-db || true

db-shell: ## Connect to PostgreSQL shell
	docker exec -it formfeeder-db psql -U postgres -d formfeeder

# Utilities
ports: ## Show ports in use
	@echo "Checking ports..."
	@lsof -i :5432 || echo "PostgreSQL port 5432 is free"
	@lsof -i :5001 || echo "API port 5001 is free"
	@lsof -i :8080 || echo "Docker port 8080 is free"

logs: ## Show application logs
	tail -f FormFeeder.Api/logs/*.log 2>/dev/null || echo "No log files found"

swagger: ## Open Swagger UI
	open https://localhost:5001/swagger || xdg-open https://localhost:5001/swagger

# Git hooks
install-hooks: ## Install git hooks
	@echo "Installing git hooks..."
	@echo '#!/bin/sh\nmake format-check' > .git/hooks/pre-commit
	@chmod +x .git/hooks/pre-commit
	@echo "Git hooks installed!"