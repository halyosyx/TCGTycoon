using Game.Core.Session;
using Game.Unity.UI.Controls;

namespace Game.Unity.UI.Hud
{
    /// <summary>
    /// What can be pushed into the HUD. <see cref="HudPresenter"/> implements it for the real HUD;
    /// debug commands and tests drive it without knowing about UI Toolkit.
    /// </summary>
    public interface IHudSink
    {
        int Day { get; }

        long CashCents { get; }

        void ShowDay(int day, DayKind kind);

        void SetTime(int hour, int minute);

        void SetClosingSoon(bool isClosingSoon);

        void SetCash(long cents);

        void ShowCashEvent(string label, long deltaCents);

        void ShowToast(ToastKind kind, string title, string detail, long? amountCents);
    }
}
