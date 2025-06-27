#include "UnityLog.h"
#include <iostream>
#include <cstdarg>
#include <sstream>
#include <iomanip>
#include <chrono>
#include <ctime>
#include <vector>

// 정적 멤버 변수 초기화
std::unique_ptr<UnityLogger> UnityLogger::instance = nullptr;
std::mutex UnityLogger::logMutex;

UnityLogger::UnityLogger() 
    : minLevel(UnityLogLevel::INFO)
    , output(UnityLogOutput::BOTH)
    , logFilePath("UnityWrapper.log")
    , isInitialized(false) {
}

UnityLogger::~UnityLogger() {
    Shutdown();
}

UnityLogger& UnityLogger::GetInstance() {
    if (!instance) {
        std::lock_guard<std::mutex> lock(logMutex);
        if (!instance) {
            instance = std::unique_ptr<UnityLogger>(new UnityLogger());
        }
    }
    return *instance;
}

bool UnityLogger::Initialize(const std::string& filePath, UnityLogLevel level, UnityLogOutput outputType) {
    std::lock_guard<std::mutex> lock(logMutex);
    
    if (isInitialized) {
        Shutdown();
    }
    
    logFilePath = filePath;
    minLevel = level;
    output = outputType;
    
    // 파일 출력이 필요한 경우 파일 열기
    if (output == UnityLogOutput::FILE || output == UnityLogOutput::BOTH) {
        logFile.open(logFilePath, std::ios::app);
        if (!logFile.is_open()) {
            return false;
        }
    }
    
    isInitialized = true;
    return true;
}

void UnityLogger::SetLogLevel(UnityLogLevel level) {
    std::lock_guard<std::mutex> lock(logMutex);
    minLevel = level;
}

void UnityLogger::SetOutput(UnityLogOutput outputType) {
    std::lock_guard<std::mutex> lock(logMutex);
    
    if (output != outputType) {
        output = outputType;
        
        // 파일 출력 상태 변경
        if (output == UnityLogOutput::FILE || output == UnityLogOutput::BOTH) {
            if (!logFile.is_open()) {
                logFile.open(logFilePath, std::ios::app);
            }
        } else {
            if (logFile.is_open()) {
                logFile.close();
            }
        }
    }
}

void UnityLogger::SetLogFilePath(const std::string& filePath) {
    std::lock_guard<std::mutex> lock(logMutex);
    
    if (logFilePath != filePath) {
        logFilePath = filePath;
        
        // 파일 출력이 활성화된 경우 새 파일로 변경
        if ((output == UnityLogOutput::FILE || output == UnityLogOutput::BOTH) && logFile.is_open()) {
            logFile.close();
            logFile.open(logFilePath, std::ios::app);
        }
    }
}

void UnityLogger::Log(UnityLogLevel level, const std::string& message) {
    if (level < minLevel || !isInitialized) {
        return;
    }
    
    std::string formattedMessage = FormatMessage(level, message);
    
    if (output == UnityLogOutput::CONSOLE || output == UnityLogOutput::BOTH) {
        WriteToConsole(formattedMessage);
    }
    
    if (output == UnityLogOutput::FILE || output == UnityLogOutput::BOTH) {
        WriteToFile(formattedMessage);
    }
}

void UnityLogger::Log(UnityLogLevel level, const char* format, ...) {
    if (level < minLevel || !isInitialized || !format) {
        return;
    }
    
    va_list args;
    va_start(args, format);
    
    // 가변 인자 처리
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        Log(level, std::string(buffer.data()));
    }
    
    va_end(args);
}

void UnityLogger::Debug(const std::string& message) {
    Log(UnityLogLevel::DEBUG, message);
}

void UnityLogger::Info(const std::string& message) {
    Log(UnityLogLevel::INFO, message);
}

void UnityLogger::Warning(const std::string& message) {
    Log(UnityLogLevel::WARNING, message);
}

void UnityLogger::Error(const std::string& message) {
    Log(UnityLogLevel::ERROR, message);
}

void UnityLogger::Critical(const std::string& message) {
    Log(UnityLogLevel::CRITICAL, message);
}

void UnityLogger::Debug(const char* format, ...) {
    if (!isInitialized || !format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        Log(UnityLogLevel::DEBUG, std::string(buffer.data()));
    }
    
    va_end(args);
}

void UnityLogger::Info(const char* format, ...) {
    if (!isInitialized || !format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        Log(UnityLogLevel::INFO, std::string(buffer.data()));
    }
    
    va_end(args);
}

void UnityLogger::Warning(const char* format, ...) {
    if (!isInitialized || !format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        Log(UnityLogLevel::WARNING, std::string(buffer.data()));
    }
    
    va_end(args);
}

void UnityLogger::Error(const char* format, ...) {
    if (!isInitialized || !format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        Log(UnityLogLevel::ERROR, std::string(buffer.data()));
    }
    
    va_end(args);
}

void UnityLogger::Critical(const char* format, ...) {
    if (!isInitialized || !format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        Log(UnityLogLevel::CRITICAL, std::string(buffer.data()));
    }
    
    va_end(args);
}

void UnityLogger::WriteToFile(const std::string& message) {
    if (logFile.is_open()) {
        logFile << message << std::endl;
        logFile.flush(); // 즉시 디스크에 쓰기
    }
}

void UnityLogger::WriteToConsole(const std::string& message) {
    std::cout << message << std::endl;
}

std::string UnityLogger::FormatMessage(UnityLogLevel level, const std::string& message) {
    std::stringstream ss;
    
    // 타임스탬프
    ss << "[" << GetCurrentTimestamp() << "] ";
    
    // 로그 레벨
    ss << "[";
    switch (level) {
        case UnityLogLevel::DEBUG:   ss << "DEBUG"; break;
        case UnityLogLevel::INFO:    ss << "INFO"; break;
        case UnityLogLevel::WARNING: ss << "WARNING"; break;
        case UnityLogLevel::ERROR:   ss << "ERROR"; break;
        case UnityLogLevel::CRITICAL: ss << "CRITICAL"; break;
    }
    ss << "] ";
    
    // 메시지
    ss << message;
    
    return ss.str();
}

std::string UnityLogger::GetCurrentTimestamp() {
    auto now = std::chrono::system_clock::now();
    auto time_t = std::chrono::system_clock::to_time_t(now);
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(
        now.time_since_epoch()) % 1000;
    
    std::stringstream ss;
    
    // localtime_s 사용 (Windows에서 안전한 버전)
    struct tm timeinfo;
#ifdef _WIN32
    localtime_s(&timeinfo, &time_t);
#else
    localtime_r(&time_t, &timeinfo);
#endif
    
    ss << std::put_time(&timeinfo, "%Y-%m-%d %H:%M:%S");
    ss << "." << std::setfill('0') << std::setw(3) << ms.count();
    
    return ss.str();
}

void UnityLogger::Shutdown() {
    std::lock_guard<std::mutex> lock(logMutex);
    
    if (logFile.is_open()) {
        logFile.close();
    }
    
    isInitialized = false;
}

// C 스타일 인터페이스 구현
extern "C" {

UNITY_API bool UnityLog_Initialize(const char* logFilePath, int logLevel, int output) {
    if (!logFilePath) {
        logFilePath = "UnityWrapper.log";
    }
    
    UnityLogLevel level = static_cast<UnityLogLevel>(logLevel);
    UnityLogOutput outputType = static_cast<UnityLogOutput>(output);
    
    return UnityLogger::GetInstance().Initialize(logFilePath, level, outputType);
}

UNITY_API void UnityLog_Debug(const char* format, ...) {
    if (!format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        UnityLogger::GetInstance().Log(UnityLogLevel::DEBUG, std::string(buffer.data()));
    }
    
    va_end(args);
}

UNITY_API void UnityLog_Info(const char* format, ...) {
    if (!format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        UnityLogger::GetInstance().Log(UnityLogLevel::INFO, std::string(buffer.data()));
    }
    
    va_end(args);
}

UNITY_API void UnityLog_Warning(const char* format, ...) {
    if (!format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        UnityLogger::GetInstance().Log(UnityLogLevel::WARNING, std::string(buffer.data()));
    }
    
    va_end(args);
}

UNITY_API void UnityLog_Error(const char* format, ...) {
    if (!format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        UnityLogger::GetInstance().Log(UnityLogLevel::ERROR, std::string(buffer.data()));
    }
    
    va_end(args);
}

UNITY_API void UnityLog_Critical(const char* format, ...) {
    if (!format) return;
    
    va_list args;
    va_start(args, format);
    
    int size = vsnprintf(nullptr, 0, format, args);
    if (size > 0) {
        std::vector<char> buffer(size + 1);
        vsnprintf(buffer.data(), buffer.size(), format, args);
        UnityLogger::GetInstance().Log(UnityLogLevel::CRITICAL, std::string(buffer.data()));
    }
    
    va_end(args);
}

UNITY_API void UnityLog_SetLevel(int level) {
    UnityLogger::GetInstance().SetLogLevel(static_cast<UnityLogLevel>(level));
}

UNITY_API void UnityLog_SetOutput(int output) {
    UnityLogger::GetInstance().SetOutput(static_cast<UnityLogOutput>(output));
}

UNITY_API void UnityLog_SetFilePath(const char* filePath) {
    if (filePath) {
        UnityLogger::GetInstance().SetLogFilePath(filePath);
    }
}

UNITY_API void UnityLog_Shutdown() {
    UnityLogger::GetInstance().Shutdown();
}

} 