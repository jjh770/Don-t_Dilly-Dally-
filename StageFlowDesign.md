# 스테이지 흐름 설계서

## 개요
- **게임**: Don't Dilly-Dally (멀티플레이 협동 수술 시뮬레이션)
- **기술 스택**: UniTask (흐름 제어) + ReactiveProperty (상태 감시) 하이브리드
- **네트워크**: Photon PUN2, 호스트 권위 모델 (마스터 클라이언트만 흐름 실행, RPC로 브로드캐스트)

---

## 확정 사항

### 1. 스테이지 구조
- 1 스테이지 = 1 하루
- 스테이지당 환자 4~5명
- 환자 1명 = DiseaseData 1개 (레시피 4~5단계를 순서대로 수행)
- 이중 루프: 외부(환자 순회) × 내부(레시피 단계 순차 진행)

### 2. 게임 오버 조건
- **환자 체력 0 = 즉시 스테이지 종료** (유일한 게임 오버 트리거)
- 레시피 불일치, 미니게임 실패, 긴급 처치 실패 등은 모두 **체력 감소 패널티**
- 스테이지 전체 타이머 0 도달 시에도 게임 오버

### 3. 컷씬
- 첫 번째 환자만 입장 컷씬 (환자가 게임 씬 안으로 들어오는 연출)
- 2~5번째 환자는 컷씬 없이 바로 시작

### 4. 제한 시간
- 스테이지 전체 타이머 1개 (ex: 15분)
- 환자별 개별 타이머 없음
- **환자 전환 중 타이머 정지** (치료 중에만 진행)

### 5. 집도의 선정
- 스테이지 시작 시 **랜덤** 선정, 끝까지 고정
- 호스트가 랜덤 선정 후 RPC로 전체 클라이언트에 브로드캐스트
- 선정 화면 = 로딩 화면 (질병 데이터 생성 등 병렬 처리 가능)

### 6. 기술 스택 선택: 하이브리드 (UniTask + ReactiveProperty)
- **UniTask**: 스테이지 흐름의 순차 실행 (async/await)
- **ReactiveProperty**: 상태 감시 및 외부 시스템 연동
  - 페이즈 상태 (ReadOnlyReactiveProperty<EStagePhase>)
  - 환자 체력 (ReactiveProperty<float>)
  - 스테이지 타이머 (ReactiveProperty<float>)
- 선택 이유: 순차 흐름은 async/await가 가독성 우수, 상태 감시는 ReactiveProperty가 조건 조합에 강점

### 7. 긴급 처치 시스템
- 레시피 판정 실패 시 → **높은 확률**로 긴급 처치 이벤트 발생
- 수술 진행 중 → **낮은 확률**로 랜덤 발생 (시간 기반)
- 긴급 처치 실패 시 → 환자 체력 감소
- 돌발상황(정전 등)은 **제외** (추후 별도 구현)

---

## 스테이지 전체 흐름

```
[대기씬 (WaitingRoom)]
    ↓ 전원 준비 완료, 마스터 클라이언트가 시작
[집도의 선정 화면 (로딩)]
    - 랜덤으로 집도의 1명 선정 → RPC 브로드캐스트
    - 병렬로 질병 데이터 로딩/생성
    ↓
[시작 컷씬]
    - 첫 번째 환자 입장 연출
    - 타이머 정지 상태
    ↓
[게임 시작 - 환자 루프]
    ├── 환자 1 (DiseaseData)
    │   ├── 타이머 재개
    │   ├── 레시피 1 → 판정 (성공/실패)
    │   ├── 레시피 2 → 판정
    │   ├── ...
    │   ├── 모든 레시피 완료 → 환자 완치
    │   └── 타이머 정지
    ├── 환자 2
    │   ├── 타이머 재개
    │   ├── 레시피 루프...
    │   └── 타이머 정지
    ├── 환자 3 ...
    ├── 환자 4 ...
    └── 환자 5 ...
    ↓
[스테이지 클리어]
    - 결과 화면 → 재화 획득
    - WaitingRoom으로 복귀

※ 도중 환자 체력 0 또는 타이머 0 → 즉시 게임 오버 → 결과 화면
```

---

## 역할 구조 (게임 내)

### 집도의 (1명, 랜덤 선정)
- 수술 미니게임 진행 권한
- 긴급 처치 정보 UI 독점 확인
- 어시스트에게 음성으로 필요한 재료 지시

### 어시스트 의사 (1~3명)
- 재료 탐색, 제조대에서 조합
- 완성된 조합물을 집도의에게 전달 (던지기/놓기)
- 환경 관리

---

## 기존 코드베이스 연동 포인트

### 사용할 기존 시스템
- **SceneLoadManager**: 씬 전환 (WaitingRoom → Gameplay)
- **PhotonServerManager**: TryStartStage(), 마스터 클라이언트 관리
- **DiseaseData / RecipeData**: 환자별 질병 및 레시피 데이터
- **TreatmentJudgeManager**: 레시피 판정 (성공/실패)
- **MiniGameLauncher**: 수술 미니게임 실행
- **DiseaseGenerationManager**: AI 기반 질병 생성 (로딩 중 병렬 처리)

### 새로 만들 시스템
- **StageFlowManager**: 스테이지 전체 흐름 관리 (이 문서의 핵심)
- **StagePhase enum**: 페이즈 정의
- **StageData**: 스테이지별 환자 목록, 제한 시간 등 설정 데이터

---

## 필요한 패키지 (미설치)
- **UniTask**: Unity용 async/await 라이브러리
- **UniRx 또는 R3**: ReactiveProperty 사용 (UniRx 선택)

---

## 개선 예정 사항 (2026-03-22 코드 리뷰)

### 1. 긴급 이벤트 중첩 방지
- `Update()`에서 `HandleEmergencyEvent`를 `.Forget()`으로 호출하고 있어 동시에 여러 긴급 이벤트가 중첩 발생 가능
- 긴급 이벤트 진행 중인지 플래그를 추가하여 중복 실행 방지 필요

### 2. 레시피 실패 시 다음 레시피로 진행
- 현재 `RunRecipeLoop`에서 레시피 실패 시 같은 레시피를 무한 반복함
- 실패 시에도 다음 레시피로 넘어가도록 수정 필요
- `TreatmentJudgeManager`에서 실패한 레시피도 `completedRecipeIds`에 추가하거나, 별도 처리 필요

### 3. 재접속 상태 복구
- 현재 모든 RPC가 `RpcTarget.Others`로 전송되어 재접속 플레이어는 상태를 받지 못함
- 구현 방안:
  - `StageFlowRpcHandler`에 상태 스냅샷 RPC 추가 (`RPC_SyncFullState`)
  - 마스터가 `OnPlayerEnteredRoom`에서 재접속 플레이어 감지 후 스냅샷 전송
  - `RoomOptions.PlayerTtl` 설정 필요

### 4. 컷씬/환자 전환 시간 연동
- 현재 고정 시간(`UniTask.Delay`)으로 처리 중 (컷씬 3초, 환자 전환 2초, 복귀 대기 5초)
- 실제 연출 시간에 맞추려면 콜백 기반으로 전환 필요
- `UniTaskCompletionSource` 패턴 사용 (트레이 제출과 동일한 패턴)
- 컷씬 담당자가 구현할 인터페이스:
```csharp
public interface ICutscenePlayer
{
    void Play(Action onComplete);
}
```
- 사용 예시:
```csharp
private async UniTask RunCutscenePhase(CancellationToken ct)
{
    var tcs = new UniTaskCompletionSource();
    cutscenePlayer.Play(() => tcs.TrySetResult());
    await tcs.Task.AttachExternalCancellation(ct);
}
```

---

## 참고 노션 페이지
- 컨셉 기획서: https://www.notion.so/32108437eda880e6a208d30b99065d5a
- 최종 프로젝트 참여 및 성장 계획: https://www.notion.so/71208437eda883b0915c812f82747200
