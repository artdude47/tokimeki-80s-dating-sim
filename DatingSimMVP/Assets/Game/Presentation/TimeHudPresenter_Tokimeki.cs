using UnityEngine;
using UnityEngine.UI;
using Game.Domain.Common;
using Game.Domain.Time;
using TMPro;

public class TimeHudPresenter_Tokimeki : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Button nextPhaseButton;

    private EventBus _bus;
    private ITimeService _time;

    void Awake()
    {
        _bus = new EventBus();
        _time = new TimeService(_bus, new SimpleCalendarService());
        _bus.Subscribe<PhaseStarted>(e => UpdateText(e.Date, e.Phase));
        _bus.Subscribe<WeekStarted>(e => UpdateText(_time.Current, _time.CurrentPhase));

        _time.Reset(1, 1, DOW.Mon);
        nextPhaseButton.onClick.AddListener(() => _time.AdvancePhase());
    }

    private void UpdateText(GameDate d, Phase p)
    {
        timeText.text = $"Year {d.Year} Week {d.Week} Day {d.Day} Phase {p}";
    }
}
