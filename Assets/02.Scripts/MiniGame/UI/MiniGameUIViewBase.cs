using UnityEngine;

namespace DontDillyDally.MiniGame
{
    // 모든 미니게임 UIView의 공통 구조를 제공하는 추상 베이스.
    // - _resultEffect 필드와 Reset/Show 디스패치 자동화 (휴먼 에러 방지)
    // - 타입 안전한 Game 프로퍼티 (서브클래스가 별도로 캐스트 안 해도 됨)
    //
    // 서브클래스 구현 의무:
    //  - OnInitialize():        미니게임별 초기화 로직
    //  - UpdateView():          매 프레임 UI 갱신
    //  - SetVisible(bool):      활성/비활성 전환 (정리 로직은 서브클래스 자유)
    //
    // 선택적 훅:
    //  - OnBeforeGameChanged(): 새 Game이 할당되기 직전 — 이전 Game 이벤트 구독 해제 등
    //  - OnBeforeShowResult():  결과 연출 재생 직전 — 게이지 보정, shake 정지 등
    public abstract class MiniGameUIViewBase<TGame> : MonoBehaviour, IMiniGameUIView
        where TGame : class, IMiniGame
    {
        [Header("공통 결과 연출")]
        [SerializeField] protected MiniGameResultEffect _resultEffect;

        protected TGame Game { get; private set; }

        public void Initialize(IMiniGame game)
        {
            // 이전 Game 참조가 아직 살아있는 동안 서브클래스가 정리할 기회를 준다.
            OnBeforeGameChanged();

            Game = game as TGame;

            // 이전 미니게임의 Success/Fail 연출이 남아 있지 않도록 초기화.
            // 과거 PrecisionStop이 이 호출을 빠뜨려서 버그가 났던 이력이 있어 베이스에서 강제함.
            if (_resultEffect != null)
            {
                _resultEffect.Reset();
            }

            OnInitialize();
        }

        public void ShowResult(bool isSuccess)
        {
            OnBeforeShowResult(isSuccess);

            if (_resultEffect != null)
            {
                if (isSuccess)
                {
                    _resultEffect.PlaySuccess();
                }
                else
                {
                    _resultEffect.PlayFail();
                }
            }
        }

        public abstract void UpdateView();
        public abstract void SetVisible(bool visible);

        // 새 Game 할당 직전. 이 시점의 Game 프로퍼티는 이전 Game을 가리킨다.
        protected virtual void OnBeforeGameChanged() { }

        // Game 할당 + 결과 연출 Reset 후에 호출되는 미니게임별 초기화.
        protected abstract void OnInitialize();

        // 결과 연출 재생 직전에 실행할 전처리 (예: 성공 시 게이지 꽉 채우기).
        protected virtual void OnBeforeShowResult(bool isSuccess) { }
    }
}
