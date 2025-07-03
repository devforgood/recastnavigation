#pragma once

#include <string>
#include <vector>

// Helper functions from MeshLoaderObj.cpp (forward declarations)
static char* parseRow(char* buf, char* bufEnd, char* row, int len);
static int parseFace(char* row, int* data, int n, int vcnt);

// Helper function to load OBJ file (based on MeshLoaderObj.cpp)
bool LoadObjFile(const std::string& filename, std::vector<float>& vertices, std::vector<int>& indices);

// Helper functions from MeshLoaderObj.cpp
char* parseRow(char* buf, char* bufEnd, char* row, int len);
int parseFace(char* row, int* data, int n, int vcnt); 