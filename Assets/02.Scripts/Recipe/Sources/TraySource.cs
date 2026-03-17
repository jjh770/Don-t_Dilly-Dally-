namespace DontDillyDally.Data
{
    // 트레이를 무한히 공급하는 공급원입니다.
    // 트레이 생성 자체는 공통 공급원 로직을 사용하고, 트레이 전용 초기화만 담당합니다.
    public class TraySource : ItemSource<TrayItem>
    {
        protected override string GetDefaultItemName()
        {
            return "Tray";
        }

        protected override object[] GetInstantiationData()
        {
            return null;
        }
    }
}
