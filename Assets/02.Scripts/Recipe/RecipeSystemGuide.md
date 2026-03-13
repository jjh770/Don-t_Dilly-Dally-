# 레시피 시스템 연동 가이드

## 1. 담당 범위

이 폴더의 코드는 플레이어 입력이나 월드 상호작용을 직접 처리하지 않습니다.  
역할은 `의료 규칙 데이터`, `조합 결과`, `치료 판정`을 제공하는 것입니다.

본인 담당 범위:

- 레시피 정의
- 질병 흐름 정의
- 조합 규칙 정의
- 제출 데이터 구조 정의
- 최종 치료 판정
- 긴급 이벤트용 진단 타입 정의

다른 팀원 담당 범위:

- `IInteractable` 구현
- 플레이어 입력 처리
- 레이캐스트, UI, 상호작용 거리 판정
- 월드 오브젝트와 시스템 연결
- 플레이어 손, 인벤토리, 장비 장착

## 2. 게임 규칙 요약

- 환자 1명당 필요한 레시피는 4~5개입니다.
- 모든 병의 첫 번째 레시피는 `멸균 트레이 위 마취약 1개`입니다.
- 나머지 3~4개의 레시피는 병에 관련된 일반 치료 레시피입니다.
- 일반 치료 레시피의 재료 수는 2~4개입니다.
- 초음파, X-Ray, ECG는 메인 치료 레시피가 아니라 긴급 이벤트용 진단입니다.
- 긴급 이벤트 1회당 필요한 진단은 항상 1개입니다.
- 보상은 질병의 모든 레시피를 클리어했을 때만 지급합니다.

## 3. 파일 역할

### 기획 데이터

- `RecipeData.cs`
  - 치료 한 단계를 정의합니다.
  - 필요한 재료와 멸균 트레이 필요 여부를 가집니다.
  - 첫 레시피 전용 검증과 일반 치료 레시피 검증을 분리합니다.

- `DiseaseData.cs`
  - 질병 하나의 전체 치료 흐름을 정의합니다.
  - 레시피 순서와 실패 패널티를 가집니다.
  - 첫 레시피가 마취약 1개인지 검증합니다.
  - 모든 레시피 완료 여부를 기준으로 보상 지급 시점을 판단합니다.

- `DiseaseSO.cs`
  - `DiseaseData`를 에셋으로 저장하기 위한 래퍼입니다.

- `DiseaseSO.asset`
  - 인스펙터에서 편집하는 샘플 질병 데이터입니다.

### 조합 규칙

- `ToolType.cs`
  - 플레이어가 다루는 원본 도구와 원재료입니다.
  - 가공 없이 바로 제출하는 붕대, 소독약 같은 기본 재료는 포함하지 않습니다.

- `ActionType.cs`
  - 멸균, 채우기, 물약 혼합, 스캔 같은 행동 타입입니다.

- `CraftingRuleSO.cs`
  - `도구 + 행동 = 결과물` 규칙 한 개입니다.

- `CraftingRuleDatabase.cs`
  - 여러 조합 규칙을 모아 실제 결과를 찾는 DB입니다.
  - `Fill` 하나로 주사기 채우기와 물약 채우기를 함께 처리합니다.
  - 대신 `주사기 + 마취액`, `빈 비커 + 청록 원액`처럼 두 재료 조합으로 결과를 구분합니다.

### 결과물과 제출

- `CraftedMaterialType.cs`
  - 최종 결과 재료 타입입니다.
  - 레시피 비교는 이 enum 기준으로 진행합니다.

- `CraftedItem.cs`
  - 실제 플레이 중 만들어진 결과물 1개입니다.
  - 기본 재료를 바로 제출할 때는 `CreateBasicMaterial()` 헬퍼를 사용합니다.

- `SubmittedTray.cs`
  - 환자에게 제출하는 트레이 데이터입니다.
  - 트레이 멸균 여부와 트레이 위 결과물을 함께 가집니다.

- `TrayWorkbench.cs`
  - 기본 재료를 바로 올릴 때는 `TryPlaceBasicMaterialOnTray()`를 사용할 수 있습니다.

### 최종 판정

- `TreatmentJudgeManager.cs`
  - 다음 레시피를 판정하고 전체 치료 진행도를 관리합니다.

### 긴급 이벤트

- `DiagnosisScanType`
  - 초음파, X-Ray, ECG 중 어떤 진단을 사용할지 표현합니다.
  - 메인 치료 레시피 판정에는 사용하지 않습니다.

## 4. 다른 팀원이 넘겨줘야 하는 입력값

### 조합 시점

다른 팀원은 상호작용 성공 시 아래 값을 넘겨주면 됩니다.

- `ToolType`
- `ActionType`
- 필요하면 두 번째 도구

이 입력으로 조합 DB를 조회해 결과를 만듭니다.

### 환자 제출 시점

환자에게 제출할 때는 아래 값이 필요합니다.

- `SubmittedTray`
- 현재 환자 체력 `float`

### 긴급 이벤트 시점

긴급 이벤트가 발생하면 아래 값 중 하나를 선택해서 처리하면 됩니다.

- `DiagnosisScanType.Ultrasound`
- `DiagnosisScanType.XRay`
- `DiagnosisScanType.ECG`

## 5. 본인 시스템이 돌려주는 출력값

### 조합 결과

조합 성공 시:

- `CraftedMaterialType`
- 또는 이를 담은 `CraftedItem`

조합 실패 시:

- `CraftedMaterialType.None`
- 또는 실패 처리용 값

### 치료 판정 결과

`TreatmentJudgeManager.JudgeNextRecipe(...)` 호출 결과:

- `Success`
- `DiseaseCured`
- `CompletedRecipeId`
- `OverallProgress`
- `FailureReason`

## 6. 실제 호출 순서

1. 게임 시작 시 `DiseaseSO`에서 현재 질병 데이터를 읽습니다.
2. `TreatmentJudgeManager.SetDisease(diseaseData)`를 호출합니다.
3. 플레이어가 상호작용으로 도구를 사용하면 `ToolType + ActionType`으로 조합 규칙을 조회합니다.
4. 조합 성공 시 `CraftedItem`을 생성합니다.
5. 생성한 `CraftedItem`을 `SubmittedTray.ContainedItems`에 담습니다.
6. 환자 제출 시 `JudgeNextRecipe(submittedTray, patientHealth)`를 호출합니다.
7. 반환된 `TreatmentJudgeResult`로 성공, 실패, 진행도, 완치 여부를 처리합니다.
8. `DiseaseCured == true`일 때만 보상 지급 로직을 실행합니다.
9. 틀린 레시피 제출이나 랜덤 긴급 이벤트가 발생했을 때만 `DiagnosisScanType`으로 별도 진단 흐름을 진행합니다.

## 7. 역할 경계 요약

다른 팀원:

- 어떤 오브젝트를 눌렀는지 판단
- 어떤 입력이 들어왔는지 판단
- 플레이어가 무엇을 들고 있는지 관리
- 상호작용 결과를 어느 시스템에 넘길지 연결

본인:

- 그 입력이 어떤 결과물을 만드는지 정의
- 어떤 레시피가 정답인지 정의
- 어떤 질병 흐름으로 진행되는지 정의
- 제출이 성공인지 실패인지 판정
- 긴급 이벤트 진단 타입을 관리

## 8. 현재 기준 연동 포인트

조합 시스템 연동:

```csharp
CraftedMaterialType result = craftingRuleDatabase.FindResult(toolType, actionType);
```

기본 재료 바로 적재:

```csharp
bool success = trayWorkbench.TryPlaceBasicMaterialOnTray(
    CraftedMaterialType.Bandage,
    playerId);
```

제출 판정 연동:

```csharp
TreatmentJudgeResult result = judgeManager.JudgeNextRecipe(
    submittedTray,
    patientHealth);

if (result.DiseaseCured)
{
    // 여기서만 보상을 지급합니다.
}
```

## 9. 다음 작업 우선순위

- `CraftingRuleSO` 샘플 규칙 생성
- 틀린 레시피 제출 시 긴급 이벤트 발생 규칙 정리
- 긴급 이벤트 1회당 단일 진단 선택 흐름 설계
- 실패 사유별 UI 메시지 표준화
