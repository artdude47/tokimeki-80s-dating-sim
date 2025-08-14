using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Domain.Common;
using Game.Domain.CommonEvents;
using Game.Domain.Time;
using Game.Domain.Stats;
using Game.Infrustructure.Data;
using Game.Application.Commands;
using Game.Domain.Commands;
using System.Collections.Generic;
using System.IO;
using Codice.Client.BaseCommands;
using Game.Domain.Relationships;
using Game.Application.Weekends;
using Game.Infrastructure.Save;
using static Game.Domain.Time.IBookingCalendar;

public class DevLoopPresenter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text bookingsText;
    [SerializeField] private TMP_Text affectionText;
    [SerializeField] private TMP_Text bombsText;

    [SerializeField] private TMP_Dropdown commandDropdown;
    [SerializeField] private TMP_Dropdown npcDropdown;
    [SerializeField] private TMP_Dropdown venueDropdown;
    [SerializeField] private TMP_Dropdown weekendDayDropdown;

    [SerializeField] private Button nextPhaseBtn;
    [SerializeField] private Button startWeekBtn;
    [SerializeField] private Button proposeButton;
    [SerializeField] private Button runDateButton;
    [SerializeField] private Button saveButton;

    private EventBus _bus;
    private ITimeService _time;
    private StatBlock _stats;
    private ICommandService _commands;
    private RelationshipState _rels;
    private IBookingCalendar _booking;
    private IPhoneService _phone;
    private IDateService _dates;
    private BombService _bombs;
    private FileSaveService _save;

    void Awake()
    {
        _bus = new EventBus();
        _stats = new StatBlock();

        //Load calendar from JSON
        var calPath = Path.Combine(Application.dataPath, "Game/Data/schoolCalendar.json");
        var calDef = new CalendarRepository().Load(calPath);
        var cal = new JsonCalendarService(calDef);

        _time = new TimeService(_bus, cal);

        //Commands
        var cmdRepo = new CommandsRepository();
        var cmdPath = Path.Combine(Application.dataPath, "Game/Data/commands.json");
        var catalog = cmdRepo.Load(cmdPath);

        _commands = new CommandService(_bus, _stats);
        _commands.SetCatalog(catalog);

        //Events
        _bus.Subscribe<PhaseStarted>(e => UpdateTime(e.Date, e.Phase));
        _bus.Subscribe<WeekStarted>(e => UpdateTime(_time.Current, _time.CurrentPhase));
        _bus.Subscribe<StatsChanged>(e => UpdateStats());

        //Populate dropdown
        commandDropdown.ClearOptions();
        commandDropdown.AddOptions(new List<string>(catalog.Keys));

        npcDropdown.ClearOptions();
        npcDropdown.AddOptions(new System.Collections.Generic.List<string> { "npc_ash", "npc_jen", "npc_max" });

        venueDropdown.ClearOptions();
        venueDropdown.AddOptions(new System.Collections.Generic.List<string> { "arcade", "diner", "park" });

        weekendDayDropdown.ClearOptions();
        weekendDayDropdown.AddOptions(new System.Collections.Generic.List<string> { "Sat", "Sun" });

        //Wire buttons
        startWeekBtn.onClick.AddListener(() =>
        {
            var selected = commandDropdown.options[commandDropdown.value].text;
            _commands.SelectWeekdayCommand(selected);
            _time.StartNewWeek();
        });
        nextPhaseBtn.onClick.AddListener(() => _time.AdvancePhase());

        proposeButton.onClick.AddListener(() =>
        {
            // Build target GameDate from current week & chosen day
            var day = weekendDayDropdown.value == 0 ? DOW.Sat : DOW.Sun;
            var target = new GameDate(_time.Current.Year, _time.Current.Week, day);
            var npcId = npcDropdown.options[npcDropdown.value].text;
            var venue = venueDropdown.options[venueDropdown.value].text;
            var resp = _phone.ProposeDate(npcId, target, venue);
            Debug.Log($"Propose result: {resp}");
            UpdateBookingsUI();
        });

        runDateButton.onClick.AddListener(() =>
        {
            if (_dates.TryRunTodayDate(_time.Current))
            {
                UpdateBookingsUI();
                UpdateAffectionUI();
            }
        });

        saveButton.onClick.AddListener(() =>
        {
            // Assume you add getters for command id & streak in your CommandService (or track them here).
            var snapshot = Game.Application.Save.GameStateAssembler.BuildSnapshot(
                _time, _time.CurrentPhase,
                _stats,
                _commands.CurrentCommandId, 0, null,
                _rels, _bombs, _booking);
            _save.Save(snapshot);
            Debug.Log($"Saved to: {_save.DefaultPath()}");
        });

        _rels = new RelationshipState();
        _rels.SetInitial(Game.Domain.Relationships.Npcs.Ash.Id, 10);
        _rels.SetInitial(Game.Domain.Relationships.Npcs.Jen.Id, 25);
        _rels.SetInitial(Game.Domain.Relationships.Npcs.Max.Id, 5);

        _booking = new BookingCalendar(cal);
        _phone = new PhoneService(_bus, _rels, _booking);
        _dates = new DateService(_bus, _rels, _booking);
        _bombs = new BombService(_bus, new BombConfig { WeeksToArm = 8, FuseWeeks = 3 }, _rels);
        _bombs.EnsureTracked(Game.Domain.Relationships.Npcs.Ash.Id);
        _bombs.EnsureTracked(Game.Domain.Relationships.Npcs.Jen.Id);
        _bombs.EnsureTracked(Game.Domain.Relationships.Npcs.Max.Id);

        _save = new FileSaveService();

        // bomb hooks
        _bus.Subscribe<WeekEnded>(_ => _bombs.OnWeekEnded(_));
        _bus.Subscribe<Game.Domain.Common.DateOccurred>(e => _bombs.OnDateOccurred(e));

        // affection UI
        _bus.Subscribe<AffectionChanged>(e => UpdateAffectionUI());
    }

    void Start()
    {
        UpdateTime(_time.Current, _time.CurrentPhase);
        UpdateStats();
    }

    void UpdateTime(GameDate d, Phase p)
    {
        timeText.text = $"YEar {d.Year} Week {d.Week} Day {d.Day} Phase {p}";
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
        // show booked slots for this week
        if (_booking.Snapshot().Count == 0) { bookingsText.text = "No bookings"; return; }
        var sb = new System.Text.StringBuilder();
        foreach (var kv in _booking.Snapshot())
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
        bombsText.text = $"Bombs - Ash:{_bombs.IsArmed("npc_ash")} Jen:{_bombs.IsArmed("npc_jen")} Max:{_bombs.IsArmed("npc_max")}";
    }
}
