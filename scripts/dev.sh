#!/bin/bash

# FormFeeder Development Script
# This script provides a menu-driven interface for common development tasks

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Print colored output
print_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
print_success() { echo -e "${GREEN}✅ $1${NC}"; }
print_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }
print_error() { echo -e "${RED}❌ $1${NC}"; }

# Check prerequisites
check_prerequisites() {
    print_info "Checking prerequisites..."
    
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is not installed"
        exit 1
    fi
    
    if ! command -v docker &> /dev/null; then
        print_warning "Docker is not installed (optional)"
    fi
    
    print_success "Prerequisites check passed"
}

# Setup development environment
setup_environment() {
    print_info "Setting up development environment..."
    dotnet restore
    dotnet tool restore
    print_success "Environment setup complete"
}

# Start PostgreSQL
start_database() {
    print_info "Starting PostgreSQL database..."
    
    if command -v docker &> /dev/null; then
        docker run -d \
            --name formfeeder-db \
            -e POSTGRES_PASSWORD=postgres \
            -e POSTGRES_DB=formfeeder \
            -p 5432:5432 \
            postgres:16 || docker start formfeeder-db
        
        print_success "PostgreSQL started on port 5432"
        sleep 3
        
        print_info "Applying database migrations..."
        dotnet ef database update -p FormFeeder.Api
        print_success "Migrations applied"
    else
        print_error "Docker is required to start the database"
        exit 1
    fi
}

# Run tests
run_tests() {
    print_info "Running tests..."
    dotnet test --logger "console;verbosity=normal"
    print_success "Tests completed"
}

# Run with coverage
run_coverage() {
    print_info "Running tests with coverage..."
    dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
    
    if command -v dotnet-reportgenerator &> /dev/null; then
        dotnet reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
        print_success "Coverage report generated at coveragereport/index.html"
        
        # Try to open the report
        if [[ "$OSTYPE" == "darwin"* ]]; then
            open coveragereport/index.html
        elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
            xdg-open coveragereport/index.html 2>/dev/null || print_info "Open coveragereport/index.html in your browser"
        fi
    else
        print_warning "Install reportgenerator for HTML reports: dotnet tool install dotnet-reportgenerator-globaltool"
    fi
}

# Format code
format_code() {
    print_info "Formatting code..."
    dotnet format
    print_success "Code formatted"
}

# Build project
build_project() {
    print_info "Building project..."
    dotnet build --configuration Release
    print_success "Build completed"
}

# Run application
run_application() {
    print_info "Starting FormFeeder API..."
    print_info "API will be available at https://localhost:5001"
    print_info "Swagger UI at https://localhost:5001/swagger"
    print_info "Press Ctrl+C to stop"
    echo ""
    dotnet run --project FormFeeder.Api
}

# Run with watch
run_watch() {
    print_info "Starting FormFeeder API with hot reload..."
    print_info "API will be available at https://localhost:5001"
    print_info "Swagger UI at https://localhost:5001/swagger"
    print_info "Press Ctrl+C to stop"
    echo ""
    dotnet watch run --project FormFeeder.Api
}

# Clean build artifacts
clean_artifacts() {
    print_info "Cleaning build artifacts..."
    dotnet clean
    find . -type d -name bin -exec rm -rf {} + 2>/dev/null || true
    find . -type d -name obj -exec rm -rf {} + 2>/dev/null || true
    find . -type d -name TestResults -exec rm -rf {} + 2>/dev/null || true
    print_success "Clean completed"
}

# Docker build
docker_build() {
    print_info "Building Docker image..."
    docker build -t formfeeder:latest ./FormFeeder.Api
    print_success "Docker image built: formfeeder:latest"
}

# Docker run
docker_run() {
    print_info "Running Docker container..."
    docker run -d \
        -p 8080:8080 \
        -e ASPNETCORE_ENVIRONMENT=Development \
        --name formfeeder \
        formfeeder:latest
    print_success "Container started on port 8080"
}

# Main menu
show_menu() {
    echo ""
    echo "================================"
    echo "   FormFeeder Development Menu"
    echo "================================"
    echo "1)  Setup environment"
    echo "2)  Start database"
    echo "3)  Run application"
    echo "4)  Run with hot reload"
    echo "5)  Run tests"
    echo "6)  Run tests with coverage"
    echo "7)  Format code"
    echo "8)  Build project"
    echo "9)  Clean artifacts"
    echo "10) Docker build"
    echo "11) Docker run"
    echo "0)  Exit"
    echo "================================"
}

# Main loop
main() {
    check_prerequisites
    
    while true; do
        show_menu
        read -p "Select an option: " choice
        echo ""
        
        case $choice in
            1) setup_environment ;;
            2) start_database ;;
            3) run_application ;;
            4) run_watch ;;
            5) run_tests ;;
            6) run_coverage ;;
            7) format_code ;;
            8) build_project ;;
            9) clean_artifacts ;;
            10) docker_build ;;
            11) docker_run ;;
            0) 
                print_info "Goodbye!"
                exit 0 
                ;;
            *)
                print_error "Invalid option. Please try again."
                ;;
        esac
        
        echo ""
        read -p "Press Enter to continue..."
    done
}

# Run main function
main