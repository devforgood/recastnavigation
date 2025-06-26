#!/bin/bash

echo "UnityWrapper 테스트 실행 스크립트"
echo "================================="

# 빌드 디렉토리 생성
if [ ! -d "build" ]; then
    mkdir build
fi
cd build

# CMake 설정 (테스트 포함)
echo "CMake 설정 중..."
cmake .. -DRECASTNAVIGATION_UNITY=ON -DRECASTNAVIGATION_TESTS=ON
if [ $? -ne 0 ]; then
    echo "CMake 설정 실패!"
    exit 1
fi

# 빌드 실행
echo "빌드 중..."
make -j$(nproc)
if [ $? -ne 0 ]; then
    echo "빌드 실패!"
    exit 1
fi

# 테스트 실행
echo "테스트 실행 중..."
ctest --output-on-failure
if [ $? -ne 0 ]; then
    echo "일부 테스트가 실패했습니다!"
    exit 1
fi

echo "모든 테스트가 성공적으로 완료되었습니다!" 