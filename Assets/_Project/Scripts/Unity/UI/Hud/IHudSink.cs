using Game.Core.Session;
using Game.Unity.UI.Controls;

namespace Game.Unity.UI.Hud
{
    /// <summary>
    /// What can be pushed into the HUD. <see cref="HudPresenter"/> implements it for the real HUD;
    /// debug commands and tests drive it without knowing about UI Toolkit. Cash isn't here: the HUD
    /// reads it only from <c>EconomyService.BalanceChanged</c>.
    /// </summary>
    public interface IHudSink
    {
        int Day { get; }

        void ShowDay(int day, DayKind kind);

        void SetTime(int hour, int minute);

        void SetClosingSoon(bool isClosingSoon);

        void ShowToast(ToastKind kind, string title, string detail, long? amountCents);
    }
}
