# UnityRecastNavigation Makefile
# 사용법: make [target]

# 변수 정의
PROJECT_NAME = UnityRecastNavigation
TEST_PROJECT = Tests/UnityRecastNavigation.Tests
SOLUTION_FILE = UnityRecastNavigation.sln
BUILD_CONFIG = Release
CLEAN_DIRS = bin obj Tests/bin Tests/obj

# 기본 타겟
.PHONY: all
all: build

# 빌드 타겟
.PHONY: build
build:
	@echo "Building $(PROJECT_NAME)..."
	dotnet build $(SOLUTION_FILE) --configuration $(BUILD_CONFIG)
	@echo "Build completed successfully!"

# 테스트 빌드
.PHONY: build-tests
build-tests:
	@echo "Building test project..."
	dotnet build $(TEST_PROJECT).csproj --configuration $(BUILD_CONFIG)
	@echo "Test build completed!"

# 테스트 실행
.PHONY: test
test: build-tests
	@echo "Running tests..."
	dotnet test $(TEST_PROJECT).csproj --configuration $(BUILD_CONFIG) --verbosity normal
	@echo "Tests completed!"

# 테스트 실행 (상세 출력)
.PHONY: test-verbose
test-verbose: build-tests
	@echo "Running tests with verbose output..."
	dotnet test $(TEST_PROJECT).csproj --configuration $(BUILD_CONFIG) --verbosity detailed
	@echo "Tests completed!"

# 테스트 실행 (커버리지 포함)
.PHONY: test-coverage
test-coverage: build-tests
	@echo "Running tests with coverage..."
	dotnet test $(TEST_PROJECT).csproj --configuration $(BUILD_CONFIG) --collect:"XPlat Code Coverage"
	@echo "Tests with coverage completed!"

# 정리
.PHONY: clean
clean:
	@echo "Cleaning build artifacts..."
	@for dir in $(CLEAN_DIRS); do \
		if [ -d "$$dir" ]; then \
			echo "Removing $$dir..."; \
			rm -rf "$$dir"; \
		fi; \
	done
	@echo "Clean completed!"

# 완전 정리 (모든 빌드 아티팩트 제거)
.PHONY: clean-all
clean-all: clean
	@echo "Cleaning all build artifacts..."
	dotnet clean $(SOLUTION_FILE)
	@echo "Full clean completed!"

# 패키지 복원
.PHONY: restore
restore:
	@echo "Restoring packages..."
	dotnet restore $(SOLUTION_FILE)
	@echo "Package restore completed!"

# 새로 빌드 (정리 후 빌드)
.PHONY: rebuild
rebuild: clean build

# 새로 테스트 (정리 후 테스트)
.PHONY: retest
retest: clean test

# 릴리즈 빌드
.PHONY: release
release: clean
	@echo "Building release version..."
	dotnet build $(SOLUTION_FILE) --configuration Release --no-restore
	@echo "Release build completed!"

# 디버그 빌드
.PHONY: debug
debug: clean
	@echo "Building debug version..."
	dotnet build $(SOLUTION_FILE) --configuration Debug --no-restore
	@echo "Debug build completed!"

# 프로젝트 정보
.PHONY: info
info:
	@echo "=== $(PROJECT_NAME) Project Information ==="
	@echo "Project: $(PROJECT_NAME)"
	@echo "Solution: $(SOLUTION_FILE)"
	@echo "Test Project: $(TEST_PROJECT)"
	@echo "Build Config: $(BUILD_CONFIG)"
	@echo ""
	@echo "Available targets:"
	@echo "  build        - Build the project"
	@echo "  test         - Run tests"
	@echo "  test-verbose - Run tests with verbose output"
	@echo "  test-coverage- Run tests with coverage"
	@echo "  clean        - Clean build artifacts"
	@echo "  clean-all    - Full clean"
	@echo "  restore      - Restore packages"
	@echo "  rebuild      - Clean and build"
	@echo "  retest       - Clean and test"
	@echo "  release      - Build release version"
	@echo "  debug        - Build debug version"
	@echo "  info         - Show this information"

# 도움말
.PHONY: help
help: info

# 기본 타겟
.DEFAULT_GOAL := build 