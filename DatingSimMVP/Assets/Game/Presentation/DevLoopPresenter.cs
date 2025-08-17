using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Game.Domain.Common;
using Game.Domain.Time;
using Game.Domain.Stats;
using Game.Domain.Relationships;

using Game.Infrustructure.Data;   // NOTE: fix namespace spelling from Infrustructure to Infrastructure in your repo
using Game.Infrastructure.Save;

using Game.Domain.Commands;
using Game.Application.Commands;
using Game.Application.Weekends;
using static Game.Domain.Time.IBookingCalendar;
using Game.Domain.CommonEvents;

public class DevLoopPresenter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text bookingsText;
    [SerializeField] private TMP_Text affectionText;
    [SerializeField] private TMP_Text bombsText;
    [SerializeField] private TMP_Text acceptanceHintText; // NEW

    [SerializeField] private TMP_Dropdown commandDropdown;
    [SerializeField] private TMP_Dropdown npcDropdown;
    [SerializeField] private TMP_Dropdown venueDropdown;
    [SerializeField] private TMP_Dropdown weekendDayDropdown; // "Sat" or "Sun"

    [SerializeField] private Button nextPhaseBtn;
    [SerializeField] private Button startWeekBtn;
    [SerializeField] private Button proposeButton;
    [SerializeField] private Button runDateButton;
    [SerializeField] private Button saveButton;

    private EventBus _bus;
    private ITimeService _time;
    private StatBlock _stats;
    private CommandService _commands;
    private RelationshipState _rels;
    private IBookingCalendar _booking;
    private PhoneService _phone;
    private IDateService _dates;
    private BombService _bombs;
    private FileSaveService _save;

    void Awake()
    {
        _bus = new EventBus();
        _stats = new StatBlock();

        // Load calendar from JSON
        var calPath = Path.Combine(Application.dataPath, "Game/Data/schoolCalendar.json");
        var calDef = new CalendarRepository().Load(calPath);
        var cal = new JsonCalendarService(calDef);
        _time = new TimeService(_bus, cal);

        // Commands
        var cmdRepo = new CommandsRepository();
        var cmdPath = Path.Combine(Application.dataPath, "Game/Data/commands.json");
        var catalog = cmdRepo.Load(cmdPath);
        _commands = new CommandService(_bus, _stats);
        _commands.SetCatalog(catalog);

        // Relationships (seed some affection)
        _rels = new RelationshipState();
        _rels.SetInitial(Game.Domain.Relationships.Npcs.Ash.Id, 10);
        _rels.SetInitial(Game.Domain.Relationships.Npcs.Jen.Id, 25);
        _rels.SetInitial(Game.Domain.Relationships.Npcs.Max.Id, 5);

        // Weekend systems
        _booking = new BookingCalendar(cal);
        _phone   = new PhoneService(_bus, _rels, _booking);
        _dates   = new DateService(_bus, _rels, _booking);
        _bombs   = new BombService(_bus, new BombConfig { WeeksToArm = 8, FuseWeeks = 3, GlobalPenalty = 4 }, _rels);
        _bombs.EnsureTracked(Game.Domain.Relationships.Npcs.Ash.Id);
        _bombs.EnsureTracked(Game.Domain.Relationships.Npcs.Jen.Id);
        _bombs.EnsureTracked(Game.Domain.Relationships.Npcs.Max.Id);

        _save = new FileSaveService();

        // UI event subscriptions
        _bus.Subscribe<PhaseStarted>(e => { UpdateTime(e.Date, e.Phase); RefreshUi(); });
        _bus.Subscribe<WeekStarted>( _ => { UpdateTime(_time.Current, _time.CurrentPhase); RefreshUi(); });
        _bus.Subscribe<StatsChanged>( _ => UpdateStats());
        _bus.Subscribe<AffectionChanged>(_ => UpdateAffectionUI());

        // Populate dropdowns
        commandDropdown.ClearOptions();
        commandDropdown.AddOptions(new List<string>(catalog.Keys));

        npcDropdown.ClearOptions();
        npcDropdown.AddOptions(new List<string> { "npc_ash", "npc_jen", "npc_max" });

        venueDropdown.ClearOptions();
        venueDropdown.AddOptions(new List<string> { "arcade", "diner", "park" });

        weekendDayDropdown.ClearOptions();
        weekendDayDropdown.AddOptions(new List<string> { "Sat", "Sun" });

        // Wire dropdown change -> refresh acceptance preview & button states
        npcDropdown.onValueChanged.AddListener(_ => RefreshUi());
        venueDropdown.onValueChanged.AddListener(_ => RefreshUi());
        weekendDayDropdown.onValueChanged.AddListener(_ => RefreshUi());

        // Buttons
        startWeekBtn.onClick.AddListener(() =>
        {
            var selected = commandDropdown.options[commandDropdown.value].text;
            _commands.SelectWeekdayCommand(selected);
            _time.StartNewWeek();
            RefreshUi();
        });

        nextPhaseBtn.onClick.AddListener(() =>
        {
            _time.AdvancePhase();
            RefreshUi();
        });

        proposeButton.onClick.AddListener(() =>
        {
            var (npcId, venue, target) = GetSelectionsForWeekendTarget();
            var resp = _phone.ProposeDate(npcId, target, venue);
            Debug.Log($"Propose result: {resp}");
            UpdateBookingsUI();
            RefreshUi();
        });

        runDateButton.onClick.AddListener(() =>
        {
            if (_dates.TryRunTodayDate(_time.Current))
            {
                UpdateBookingsUI();
                UpdateAffectionUI();
                RefreshUi();
            }
        });

        saveButton.onClick.AddListener(() =>
        {
            var snapshot = Game.Application.Save.GameStateAssembler.BuildSnapshot(
                _time, _time.CurrentPhase, _stats,
                _commands.CurrentCommandId, _commands.SameCommandStreak, _commands.LastWeekCommandId,
                _rels, _bombs, _booking);
            _save.Save(snapshot);
            Debug.Log($"Saved to: {_save.DefaultPath()}");
        });
    }

    void Start()
    {
        UpdateTime(_time.Current, _time.CurrentPhase);
        UpdateStats();
        UpdateBookingsUI();
        UpdateAffectionUI();
        RefreshUi();
    }

    void UpdateTime(GameDate d, Phase p)
    {
        timeText.text = $"Year {d.Year}  Week {d.Week}  Day {d.Day}  Phase {p}";
    }

    void UpdateStats()
    {
        statsText.text =
            $"ACAD: {_stats[Stat.Academics]}  ART: {_stats[Stat.Art]}  ATH: {_stats[Stat.Athletics]}\n" +
            $"STA:  {_stats[Stat.Stamina]}    CHARM: {_stats[Stat.Charm]} GUTS: {_stats[Stat.Guts]}\n" +
            $"STRESS:{_stats[Stat.Stress]}    GK: {_stats[Stat.GenKnowledge]}";
    }

    void UpdateBookingsUI()
    {
        var snap = _booking.Snapshot();
        if (snap.Count == 0) { bookingsText.text = "No bookings"; return; }

        var sb = new StringBuilder();
        foreach (var kv in snap)
        {
            var k = kv.Key; var v = kv.Value;
            if (k.Item1 == _time.Current.Year && k.Item2 == _time.Current.Week)
                sb.AppendLine($"Week {k.Item2} Day {(DOW)k.Item3}: {v.npc} @ {v.venue}");
        }
        bookingsText.text = sb.ToString();
    }

    void UpdateAffectionUI()
    {
        var a = _rels.Snapshot();
        int ash = a.ContainsKey("npc_ash") ? a["npc_ash"] : 0;
        int jen = a.ContainsKey("npc_jen") ? a["npc_jen"] : 0;
        int max = a.ContainsKey("npc_max") ? a["npc_max"] : 0;
        affectionText.text = $"Affection - Ash:{ash} Jen:{jen} Max:{max}";
        bombsText.text = $"Bombs - Ash:{_bombs.IsArmed("npc_ash")}  Jen:{_bombs.IsArmed("npc_jen")}  Max:{_bombs.IsArmed("npc_max")}";
    }

    // === UI polish helpers ===

    private (string npcId, string venue, GameDate target) GetSelectionsForWeekendTarget()
    {
        var npcId = npcDropdown.options[npcDropdown.value].text;
        var venue = venueDropdown.options[venueDropdown.value].text;
        var day = weekendDayDropdown.value == 0 ? DOW.Sat : DOW.Sun;
        var target = new GameDate(_time.Current.Year, _time.Current.Week, day);
        return (npcId, venue, target);
    }

    private bool IsMorningPhase(Phase p) =>
        p == Phase.SaturdayMorning || p == Phase.SundayMorning || p == Phase.HolidayMorning;

    private bool IsDayPhase(Phase p) =>
        p == Phase.SaturdayDay || p == Phase.SundayDay || p == Phase.HolidayDay;

    private void RefreshUi()
    {
        // Acceptance preview
        var (npcId, venue, target) = GetSelectionsForWeekendTarget();
        var preview = _phone.PreviewAcceptance(npcId, target);
        acceptanceHintText.text = $"{preview.chance}% — {preview.reason} ({venue})";

        // Button gating by phase
        var p = _time.CurrentPhase;

        // Propose button: only in a morning phase and if bookable/free
        bool canPropose = IsMorningPhase(p) && preview.chance > 0;
        proposeButton.interactable = canPropose;

        // Run Date button: only in a day phase and if there's a booking today
        bool hasBookingToday = _booking.TryGetBooking(_time.Current, out _);
        bool canRunDate = IsDayPhase(p) && hasBookingToday;
        runDateButton.interactable = canRunDate;
    }
}
