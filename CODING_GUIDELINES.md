# Coding Guidelines

## Language Rules

### 1. Code Comments and Documentation
- **Use English only** for all code comments, documentation, and commit messages
- This prevents encoding issues across different systems and development environments
- Korean comments can cause compilation warnings and potential issues

### 2. Variable Names and Functions
- Use descriptive English names for variables, functions, and classes
- Follow standard naming conventions (camelCase, PascalCase, etc.)
- Avoid non-ASCII characters in identifiers

### 3. String Literals
- Use English for all string literals in code
- If Korean text is needed for user-facing content, use external localization files

### 4. File Names
- Use English for all file names
- Avoid spaces and special characters
- Use descriptive names that indicate the file's purpose

## Examples

### Good (English)
```cpp
// Input data validation
if (!verts || !tris || nverts <= 0 || ntris <= 0) {
    return false;
}

// rcContext validation
if (!m_ctx) {
    m_ctx = new rcContext();
    if (!m_ctx) {
        return false;
    }
}
```

### Bad (Korean - causes encoding issues)
```cpp
// 입력 데이터 검증
if (!verts || !tris || nverts <= 0 || ntris <= 0) {
    return false;
}

// rcContext 검증
if (!m_ctx) {
    m_ctx = new rcContext();
    if (!m_ctx) {
        return false;
    }
}
```

## Benefits
- Prevents compilation warnings about encoding
- Ensures code works across different platforms
- Improves code readability for international developers
- Reduces potential issues with version control systems 