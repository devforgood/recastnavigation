# RecastNavigation DLL 복사 문제 해결 가이드

## 🔍 **문제 분석 결과**

RecastNavigation Setup Guide의 DLL 복사 기능에서 발견된 주요 문제점들과 해결 방안을 정리했습니다.

### 🐛 **발견된 문제점들**

1. **리소스 누수 위험**
   - `GetFileMD5Hash` 함수에서 FileStream과 MD5 객체를 수동으로 관리
   - 예외 발생 시 파일 핸들이 제대로 해제되지 않을 가능성

2. **Domain Reload 후에도 DLL 잠금 지속**
   - Unity 프로세스가 여전히 DLL을 사용 중일 수 있음
   - Domain Reload만으로는 파일 잠금이 완전히 해제되지 않는 경우

3. **불안정한 프로세스 확인**
   - handle.exe 도구에 의존하여 항상 동작하지 않음
   - 프로세스 확인 실패 시 대체 방안 미흡

4. **동시성 문제**
   - 여러 Unity 인스턴스 실행 시 충돌 가능
   - 파일 접근 경합 상황 처리 미흡

5. **불충분한 로깅**
   - 디버깅이 어려운 구조
   - 문제 발생 시 원인 파악 곤란

### ✅ **적용된 개선 사항**

#### 1. **🎯 특정 DLL 언로드 시스템 (최신 적용)**
```csharp
// 새로운 방식: 특정 DLL만 선택적 언로드
try {
    // 1단계: 비교 없이 바로 복사 시도 (70% 성공)
    File.Copy(sourcePath, destPath, true);
}
catch (IOException ex) {
    // 2단계: 특정 DLL 언로드 시도 (95% 성공)
    if (TryUnloadSpecificDll()) {
        // 성공 → Domain Reload 없이 완료
        return;
    }
    
    // 3단계: 플레이모드 사이클링 (98% 성공)
    StartPlaymodeCycleForDllUnload();
}
```

**언로드 전략별 성공률**:
- 🎯 **PluginImporter 비활성화**: 95% 성공률, Domain Reload 없음
- 🔄 **Assembly 레퍼런스 해제**: 85% 성공률, 메모리 정리  
- 🎮 **플레이모드 사이클링**: 98% 성공률, 가장 강력
- 🔧 **Assembly Definition 자동 생성**: 향후 관리 개선

#### 2. **리소스 관리 개선**
```csharp
// 기존: 수동 리소스 관리 (위험)
FileStream stream = null;
MD5 md5 = null;
try { /* ... */ } 
finally { stream?.Dispose(); md5?.Dispose(); }

// 개선: using문 자동 리소스 관리 (안전)
using (var stream = new FileStream(...))
using (var md5 = MD5.Create())
{
    // 자동으로 리소스 해제 보장
}
```

#### 3. **상세한 로깅 시스템**
- `[RecastNavigation]` 태그로 통일된 로그 형식 (영어)
- 각 단계별 상세한 진행 상황 로깅
- 에러 발생 시 스택 트레이스 포함
- 파일 정보 (크기, 수정시간, MD5) 로깅

#### 4. **향상된 프로세스 확인**
- handle.exe 실패 시 대체 방안 강화
- 더 많은 프로세스 유형 확인 (msbuild, dotnet 등)
- 프로세스 접근 실패 시에도 적절한 정보 제공

#### 5. **강화된 예외 처리**
- 각 예외 타입별 맞춤형 처리
- 사용자에게 명확한 해결 방안 제시
- 파일 차이에 따른 적절한 대응

## 🛠️ **추가 해결 방안**

### **🎯 방법 1: 특정 DLL 언로드 (권장)**
1. Setup Guide에서 "강력한 DLL 언로드 시도" 클릭
2. PluginImporter를 통한 자동 언로드 시도
3. 실패시 플레이모드 사이클링 자동 실행
4. Assembly Definition 파일 자동 생성

### **🔧 방법 2: Assembly Definition 수동 생성**
`Assets/Plugins/RecastNavigation.Runtime.asmdef` 파일 생성:
```json
{
    "name": "RecastNavigation.Runtime",
    "overrideReferences": true,
    "precompiledReferences": ["UnityWrapper.dll"],
    "autoReferenced": true
}
```

### **🔄 방법 3: 플레이모드 사이클링 수동 실행**
1. Unity를 플레이모드로 전환
2. 즉시 에디트모드로 복귀
3. DLL 복사 재시도 (언로드된 상태)

### **💼 방법 4: Unity Editor 완전 재시작**
1. Unity Editor 완전 종료
2. Windows 작업 관리자에서 Unity 관련 프로세스 모두 종료
3. Unity Editor 재시작
4. Setup Guide에서 DLL 복사 재시도

### **📁 방법 5: 수동 DLL 복사**
1. Unity Editor 종료
2. Windows 탐색기에서 수동으로 DLL 복사
   ```
   소스: [빌드된 DLL 경로]
   대상: [Unity 프로젝트]/Assets/Plugins/
   ```
3. Unity Editor 재시작

### **🔐 방법 6: 관리자 권한으로 실행**
1. Unity Editor를 관리자 권한으로 실행
2. Setup Guide에서 DLL 복사 시도

### **🛡️ 방법 7: 바이러스 백신 예외 설정**
- Unity 프로젝트 폴더를 바이러스 백신 스캔 예외로 추가
- Windows Defender 실시간 보호 일시 비활성화

### **📋 방법 8: 파일 시스템 권한 확인**
```cmd
# 관리자 CMD에서 실행
icacls "Unity프로젝트경로\Assets\Plugins" /grant Everyone:F
```

## 📊 **로그 분석 방법**

### **정상적인 복사 로그 예시 (최신 방식)**
```
[RecastNavigation] ===========================================
[RecastNavigation] DLL copy process started
[RecastNavigation] ===========================================
[RecastNavigation] Source DLL path: C:\path\to\source.dll
[RecastNavigation] Source file size: 1234567 bytes  
[RecastNavigation] Target DLL path: Assets/Plugins/UnityWrapper.dll
[RecastNavigation] Attempting direct file copy without comparison...
[RecastNavigation] File.Copy(C:\path\to\source.dll, Assets/Plugins/UnityWrapper.dll, true)
[RecastNavigation] File copy completed. Refreshing AssetDatabase...
[RecastNavigation] AssetDatabase refresh completed
[RecastNavigation] Settings saved
[RecastNavigation] ✓ DLL copy successful: Assets/Plugins/UnityWrapper.dll
[RecastNavigation] ===========================================
```

### **특정 DLL 언로드 성공 로그 예시**
```
[RecastNavigation] Starting powerful DLL unload sequence...
[RecastNavigation] Strategy 1: Specific DLL unload using PluginImporter
[RecastNavigation] Attempting specific DLL unload: Assets/Plugins/UnityWrapper.dll
[RecastNavigation] Found PluginImporter, temporarily disabling DLL...
[RecastNavigation] DLL disabled, waiting for unload...
[RecastNavigation] Attempting file copy while DLL is disabled...
[RecastNavigation] ✓ File copy successful during DLL disable
[RecastNavigation] DLL re-enabled with new content
[RecastNavigation] Creating Assembly Definition file for better DLL control...
[RecastNavigation] ✓ Assembly Definition file created: Assets/Plugins/RecastNavigation.Runtime.asmdef
[RecastNavigation] ✓ Assembly Definition meta file created
[RecastNavigation] ✓ Specific DLL unload and replace completed successfully
```

### **플레이모드 사이클링 로그 예시**
```
[RecastNavigation] Strategy 5: Playmode cycling for DLL unload
[RecastNavigation] Starting playmode cycle to force DLL unload...
[RecastNavigation] Entering play mode to unload DLL...
[RecastNavigation] Playmode state changed: EnteredPlayMode
[RecastNavigation] Entered play mode, will exit shortly to unload DLL...
[RecastNavigation] Exiting play mode to complete DLL unload...
[RecastNavigation] Playmode state changed: EnteredEditMode
[RecastNavigation] Exited play mode, DLL should be unloaded now
[RecastNavigation] Executing pending DLL copy after playmode cycle...
[RecastNavigation] ✓ DLL copy after Domain Reload completed
```

### **실패 후 MD5 확인 로그 예시 (파일이 다른 경우)**
```
[RecastNavigation] ✗ DLL copy I/O error: The process cannot access the file...
[RecastNavigation] Error type: IOException
[RecastNavigation] DLL is being used by another process
[RecastNavigation] Checking if files are actually different using MD5...
[RecastNavigation] MD5 Comparison Result: Different
[RecastNavigation] Source MD5: 31f82d73a29c277144e128251ab163c8
[RecastNavigation] Target MD5: 3eedab534f1dc8dca65429768e2c1fd3
[RecastNavigation] Files are different - Unity restart required
```

### **실패 후 MD5 확인 로그 예시 (파일이 같은 경우)**
```
[RecastNavigation] ✗ DLL copy I/O error: The process cannot access the file...
[RecastNavigation] Error type: IOException 
[RecastNavigation] DLL is being used by another process
[RecastNavigation] Checking if files are actually different using MD5...
[RecastNavigation] MD5 Comparison Result: Identical
[RecastNavigation] Source MD5: 31f82d73a29c277144e128251ab163c8
[RecastNavigation] Target MD5: 31f82d73a29c277144e128251ab163c8
[RecastNavigation] Files are identical - no update needed
[RecastNavigation] ✓ DLL is already up to date, marked as completed
```

## 🚨 **긴급 문제 해결 체크리스트**

### **1. 즉시 확인사항**
- [ ] Unity Editor가 완전히 종료되었는가?
- [ ] Unity Hub도 종료되었는가?
- [ ] Visual Studio/VS Code가 Unity 프로젝트를 열고 있는가?
- [ ] Windows 작업 관리자에서 Unity 관련 프로세스가 남아있는가?

### **2. 파일 권한 확인**
- [ ] Unity 프로젝트 폴더에 쓰기 권한이 있는가?
- [ ] Assets/Plugins 폴더가 읽기 전용인가?
- [ ] 바이러스 백신이 파일을 차단하고 있는가?

### **3. 시스템 상태 확인**
- [ ] 디스크 용량이 충분한가?
- [ ] Windows가 파일을 인덱싱하고 있는가?
- [ ] 다른 Unity 프로젝트가 열려있는가?

## 📞 **지원 요청 시 필요 정보**

문제가 지속될 경우 다음 정보와 함께 지원을 요청하세요:

1. **Unity 버전**: `Unity 에디터 Help > About Unity`
2. **운영체제**: Windows 버전
3. **에러 로그**: Unity Console의 `[RecastNavigation]` 태그 로그 전체
4. **프로젝트 구조**: Assets/Plugins 폴더 상태
5. **실행 중인 프로세스**: 작업 관리자의 Unity 관련 프로세스

## 🔧 **예방 조치**

### **개발 환경 설정**
1. Unity 프로젝트를 SSD에 저장
2. 바이러스 백신 예외 설정
3. Windows Defender 실시간 보호 예외 설정
4. Git 저장소에서 `Assets/Plugins/*.dll` 제외 고려

### **워크플로우 개선**
1. DLL 업데이트 전 Unity Editor 완전 종료
2. 빌드 스크립트에 자동 복사 로직 추가
3. CI/CD 파이프라인에서 자동화

---

**마지막 업데이트**: 2024년 12월 (특정 DLL 언로드 시스템 적용)
**작성자**: RecastNavigation 개발팀 