#pragma once

#ifdef UNITY_EXPORT
    #ifdef _WIN32
        #define UNITY_API __declspec(dllexport)
    #else
        #define UNITY_API __attribute__((visibility("default")))
    #endif
#else
    #define UNITY_API
#endif

#include <string>
#include <fstream>
#include <mutex>
#include <memory>

enum class UnityLogLevel {
    DEBUG = 0,
    INFO = 1,
    WARNING = 2,
    ERROR = 3,
    CRITICAL = 4
};

enum class UnityLogOutput {
    CONSOLE = 1,
    FILE = 2,
    BOTH = 3
};

class UnityLogger {
private:
    static std::unique_ptr<UnityLogger> instance;
    static std::mutex logMutex;
    
    std::ofstream logFile;
    UnityLogLevel minLevel;
    UnityLogOutput output;
    std::string logFilePath;
    bool isInitialized;
    
    UnityLogger();
    
    void WriteToFile(const std::string& message);
    void WriteToConsole(const std::string& message);
    std::string FormatMessage(UnityLogLevel level, const std::string& message);
    std::string GetCurrentTimestamp();
    
public:
    ~UnityLogger();
    static UnityLogger& GetInstance();
    
    // Initialization and settings
    bool Initialize(const std::string& filePath = "UnityWrapper.log", 
                   UnityLogLevel level = UnityLogLevel::INFO,
                   UnityLogOutput output = UnityLogOutput::BOTH);
    void SetLogLevel(UnityLogLevel level);
    void SetOutput(UnityLogOutput output);
    void SetLogFilePath(const std::string& filePath);
    
    // Logging functions
    void Log(UnityLogLevel level, const std::string& message);
    void Log(UnityLogLevel level, const char* format, ...);
    
    // Convenience functions
    void Debug(const std::string& message);
    void Info(const std::string& message);
    void Warning(const std::string& message);
    void Error(const std::string& message);
    void Critical(const std::string& message);
    
    // Variable argument support
    void Debug(const char* format, ...);
    void Info(const char* format, ...);
    void Warning(const char* format, ...);
    void Error(const char* format, ...);
    void Critical(const char* format, ...);
    
    // Cleanup
    void Shutdown();
};

// Macro definitions (replacement for cout)
#ifdef _DEBUG
    #define UNITY_LOG_DEBUG(...) UnityLogger::GetInstance().Debug(__VA_ARGS__)
    #define UNITY_LOG_INFO(...) UnityLogger::GetInstance().Info(__VA_ARGS__)
    #define UNITY_LOG_WARNING(...) UnityLogger::GetInstance().Warning(__VA_ARGS__)
    #define UNITY_LOG_ERROR(...) UnityLogger::GetInstance().Error(__VA_ARGS__)
    #define UNITY_LOG_CRITICAL(...) UnityLogger::GetInstance().Critical(__VA_ARGS__)
#else
    #define UNITY_LOG_DEBUG(...) ((void)0)
    #define UNITY_LOG_INFO(...) ((void)0)
    #define UNITY_LOG_WARNING(...) ((void)0)
    #define UNITY_LOG_ERROR(...) UnityLogger::GetInstance().Error(__VA_ARGS__)
    #define UNITY_LOG_CRITICAL(...) UnityLogger::GetInstance().Critical(__VA_ARGS__)
#endif

// C-style interface (callable from Unity)
extern "C" {
    // Logging system initialization
    UNITY_API bool UnityLog_Initialize(const char* logFilePath, int logLevel, int output);
    
    // Logging functions
    UNITY_API void UnityLog_Debug(const char* format, ...);
    UNITY_API void UnityLog_Info(const char* format, ...);
    UNITY_API void UnityLog_Warning(const char* format, ...);
    UNITY_API void UnityLog_Error(const char* format, ...);
    UNITY_API void UnityLog_Critical(const char* format, ...);
    
    // Settings functions
    UNITY_API void UnityLog_SetLevel(int level);
    UNITY_API void UnityLog_SetOutput(int output);
    UNITY_API void UnityLog_SetFilePath(const char* filePath);
    
    // Cleanup
    UNITY_API void UnityLog_Shutdown();
} 