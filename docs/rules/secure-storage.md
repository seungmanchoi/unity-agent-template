# Secure Storage / 민감 데이터 저장 (MANDATORY)

> CLAUDE.md 에서 옮긴 상세 규칙이다. **Hard Threshold 표는 CLAUDE.md 가 정본**이며, 여기 없는 통과 기준은 CLAUDE.md 를 따른다.

`PlayerPrefs` 는 평문이다(iOS `NSUserDefaults` plist, Android `SharedPreferences` XML). 탈옥·루팅 기기나 백업에서 그대로 읽힌다. 토큰·비밀값·영수증은 Platform 의 Keychain/Keystore 브리지에만 둔다.

## 저장 경계

| 분류 | 예시 | 저장 위치 |
|------|------|-----------|
| 민감 | 로그인·세션 토큰, refresh token, 결제 영수증·구매 토큰, 라이선스 키, PII 와 묶인 식별자 | `Platform/SecureStore` (iOS Keychain, Android Keystore) |
| 준민감 | 푸시 토큰, 재설치 후에도 필요한 익명 사용자 ID | `SecureStore` 권장 |
| 비민감 | 언어·사운드 설정, 튜토리얼 완료 플래그, 최고 기록, autoplay 플래그 | `PlayerPrefs` |
| 게임 진행 저장 | 재화·해금·진행 상황 | `Application.persistentDataPath` 의 JSON 파일 (아래) |

## SecureStore 브리지 (필요해지면 만든다)

```
unity/Assets/Plugins/iOS/AppKeychain.mm     SecItemAdd/CopyMatching/Delete, kSecAttrAccessibleWhenUnlockedThisDeviceOnly
unity/Assets/Game/Platform/SecureStore.cs   Get/Set/Delete(string key) — iOS DllImport, Android Keystore(AndroidJavaObject), Editor 는 메모리
```

- 토큰을 다루는 코드는 `SecureStore` 만 부른다. 로그아웃·만료 시 같은 경로에서 지운다.
- 로그(`Debug.Log`), Crashlytics, Analytics 파라미터에 토큰·PII 를 남기지 않는다. 개발 빌드에서도 마스킹한다.
- 클라이언트 비밀키를 코드·`StreamingAssets`·`Resources`·ScriptableObject 에 넣지 않는다. 빌드에 들어간 값은 공개된 값이다. 비밀이 필요한 호출은 서버가 한다.

## 진행 저장 데이터

- `persistentDataPath` 아래 JSON 하나, 최상위에 `"version"` 필드를 둔다. 읽을 때 버전을 보고 이관 함수를 순서대로 적용한다. 이관은 Domain 에 두고 `dotnet test` 로 검증한다.
- 쓰기는 임시 파일에 쓴 뒤 교체(`File.Replace`/`Move`)해 쓰다 끊겨도 깨지지 않게 한다.
- 이 파일은 사용자가 고칠 수 있다. 서버 보상·결제 판정의 근거로 쓰지 않는다.

## Hard Threshold

| 기준 | 임계값 |
|------|--------|
| 토큰·비밀값·영수증이 PlayerPrefs·평문 파일에 저장 (verify 가 PlayerPrefs 키 이름을 검사) | **0개** |
| `SecureStore` 밖에서 토큰을 직접 저장·읽기 | **0개** |
| 토큰·PII 가 로그·Crashlytics·Analytics 에 노출 | **0개** |
| 클라이언트에 들어간 비밀키 (코드, StreamingAssets, Resources) | **0개** |
| `version` 필드 없는 진행 저장 포맷 | **0개** |
