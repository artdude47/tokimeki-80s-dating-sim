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

public class DevLoopPresenter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Dropdown commandDropdown;
    [SerializeField] private Button nextPhaseBtn;
    [SerializeField] private Button startWeekBtn;

    private EventBus _bus;
    private ITimeService _time;
    private StatBlock _stats;
    private ICommandService _commands;

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

        //Wire buttons
        startWeekBtn.onClick.AddListener(() =>
        {
            var selected = commandDropdown.options[commandDropdown.value].text;
            _commands.SelectWeekdayCommand(selected);
            _time.StartNewWeek();
        });
        nextPhaseBtn.onClick.AddListener(() => _time.AdvancePhase());
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
}
