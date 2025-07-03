#include "TestHelpers.h"
#include <cstdio>
#include <cstdlib>
#include <cstring>

// Helper function to load OBJ file (based on MeshLoaderObj.cpp)
bool LoadObjFile(const std::string& filename, std::vector<float>& vertices, std::vector<int>& indices) {
    FILE* fp = fopen(filename.c_str(), "rb");
    if (!fp) {
        return false;
    }
    
    // Get file size
    if (fseek(fp, 0, SEEK_END) != 0) {
        fclose(fp);
        return false;
    }
    long bufSize = ftell(fp);
    if (bufSize < 0) {
        fclose(fp);
        return false;
    }
    if (fseek(fp, 0, SEEK_SET) != 0) {
        fclose(fp);
        return false;
    }
    
    // Read file content
    char* buf = new char[bufSize];
    if (!buf) {
        fclose(fp);
        return false;
    }
    size_t readLen = fread(buf, bufSize, 1, fp);
    fclose(fp);
    
    if (readLen != 1) {
        delete[] buf;
        return false;
    }
    
    // Parse OBJ file
    char* src = buf;
    char* srcEnd = buf + bufSize;
    char row[512];
    int face[32];
    float x, y, z;
    int nv;
    int vcap = 0;
    int tcap = 0;
    int vertCount = 0;
    int triCount = 0;
    
    // Temporary storage
    std::vector<float> tempVertices;
    std::vector<int> tempIndices;
    
    while (src < srcEnd) {
        // Parse one row
        row[0] = '\0';
        src = parseRow(src, srcEnd, row, sizeof(row)/sizeof(char));
        
        // Skip comments
        if (row[0] == '#') continue;
        
        if (row[0] == 'v' && row[1] != 'n' && row[1] != 't') {
            // Vertex position
            sscanf(row+1, "%f %f %f", &x, &y, &z);
            
            // Add vertex with scaling (scale = 1.0f)
            float scale = 1.0f;
            tempVertices.push_back(x * scale);
            tempVertices.push_back(y * scale);
            tempVertices.push_back(z * scale);
            vertCount++;
        }
        else if (row[0] == 'f') {
            // Faces
            nv = parseFace(row+1, face, 32, vertCount);
            for (int i = 2; i < nv; ++i) {
                const int a = face[0];
                const int b = face[i-1];
                const int c = face[i];
                if (a < 0 || a >= vertCount || b < 0 || b >= vertCount || c < 0 || c >= vertCount)
                    continue;
                tempIndices.push_back(a);
                tempIndices.push_back(b);
                tempIndices.push_back(c);
                triCount++;
            }
        }
    }
    
    delete[] buf;
    
    vertices = tempVertices;
    indices = tempIndices;
    return true;
}

// Helper functions from MeshLoaderObj.cpp
char* parseRow(char* buf, char* bufEnd, char* row, int len) {
    bool start = true;
    bool done = false;
    int n = 0;
    while (!done && buf < bufEnd) {
        char c = *buf;
        buf++;
        // multirow
        switch (c) {
            case '\\':
                break;
            case '\n':
                if (start) break;
                done = true;
                break;
            case '\r':
                break;
            case '\t':
            case ' ':
                if (start) break;
                // else falls through
            default:
                start = false;
                row[n++] = c;
                if (n >= len-1)
                    done = true;
                break;
        }
    }
    row[n] = '\0';
    return buf;
}

int parseFace(char* row, int* data, int n, int vcnt) {
    int j = 0;
    while (*row != '\0') {
        // Skip initial white space
        while (*row != '\0' && (*row == ' ' || *row == '\t'))
            row++;
        char* s = row;
        // Find vertex delimiter and terminated the string there for conversion.
        while (*row != '\0' && *row != ' ' && *row != '\t') {
            if (*row == '/') *row = '\0';
            row++;
        }
        if (*s == '\0')
            continue;
        int vi = atoi(s);
        data[j++] = vi < 0 ? vi+vcnt : vi-1;
        if (j >= n) return j;
    }
    return j;
} 