using System.Windows.Threading;

namespace DeskLite.Services;

public enum PomodoroPhase
{
    Idle,
    Working,
    ShortBreak,
    LongBreak
}

public sealed class PomodoroService
{
    private readonly DispatcherTimer _timer;
    private int _workMinutes = 25;
    private int _breakMinutes = 5;
    private int _longBreakMinutes = 15;
    private int _sessionsBeforeLongBreak = 4;
    private TimeSpan _remaining;
    private TimeSpan _phaseDuration;
    private int _completedWorkSessions;

    public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Idle;
    public bool IsRunning { get; private set; }
    public int CompletedWorkSessions => _completedWorkSessions;

    public event Action? Tick;
    public event Action<PomodoroPhase>? PhaseChanged;
    public event Action<PomodoroPhase>? Completed;

    public PomodoroService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => OnTimerTick();
    }

    public void Configure(int workMinutes, int breakMinutes, int longBreakMinutes, int sessionsBeforeLongBreak)
    {
        _workMinutes = Math.Clamp(workMinutes, 1, 120);
        _breakMinutes = Math.Clamp(breakMinutes, 1, 60);
        _longBreakMinutes = Math.Clamp(longBreakMinutes, 1, 60);
        _sessionsBeforeLongBreak = Math.Clamp(sessionsBeforeLongBreak, 2, 12);

        if (Phase == PomodoroPhase.Idle)
        {
            _remaining = TimeSpan.FromMinutes(_workMinutes);
            _phaseDuration = _remaining;
        }
    }

    public TimeSpan Remaining => _remaining;

    public double ProgressPercent
    {
        get
        {
            if (_phaseDuration.TotalSeconds <= 0)
            {
                return 0;
            }

            var elapsed = _phaseDuration - _remaining;
            return Math.Clamp(elapsed.TotalSeconds / _phaseDuration.TotalSeconds * 100, 0, 100);
        }
    }

    public void StartOrToggle()
    {
        if (Phase == PomodoroPhase.Idle)
        {
            BeginPhase(PomodoroPhase.Working);
            return;
        }

        if (IsRunning)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }

    public void Pause()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        _timer.Stop();
        Tick?.Invoke();
    }

    public void Resume()
    {
        if (Phase == PomodoroPhase.Idle || IsRunning)
        {
            return;
        }

        IsRunning = true;
        _timer.Start();
        Tick?.Invoke();
    }

    public void Reset()
    {
        _timer.Stop();
        IsRunning = false;
        _completedWorkSessions = 0;
        SetPhase(PomodoroPhase.Idle);
        _remaining = TimeSpan.FromMinutes(_workMinutes);
        _phaseDuration = _remaining;
        Tick?.Invoke();
    }

    public void SkipBreak()
    {
        if (Phase is not (PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak))
        {
            return;
        }

        BeginPhase(PomodoroPhase.Working);
    }

    private void OnTimerTick()
    {
        if (_remaining.TotalSeconds <= 1)
        {
            _remaining = TimeSpan.Zero;
            _timer.Stop();
            IsRunning = false;
            Tick?.Invoke();
            CompleteCurrentPhase();
            return;
        }

        _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
        Tick?.Invoke();
    }

    private void CompleteCurrentPhase()
    {
        var completed = Phase;
        Completed?.Invoke(completed);

        switch (completed)
        {
            case PomodoroPhase.Working:
                _completedWorkSessions++;
                if (_completedWorkSessions % _sessionsBeforeLongBreak == 0)
                {
                    BeginPhase(PomodoroPhase.LongBreak, autoStart: true);
                }
                else
                {
                    BeginPhase(PomodoroPhase.ShortBreak, autoStart: true);
                }
                break;
            case PomodoroPhase.ShortBreak:
            case PomodoroPhase.LongBreak:
                BeginPhase(PomodoroPhase.Working, autoStart: true);
                break;
        }
    }

    private void BeginPhase(PomodoroPhase phase, bool autoStart = false)
    {
        SetPhase(phase);
        _phaseDuration = phase switch
        {
            PomodoroPhase.Working => TimeSpan.FromMinutes(_workMinutes),
            PomodoroPhase.ShortBreak => TimeSpan.FromMinutes(_breakMinutes),
            PomodoroPhase.LongBreak => TimeSpan.FromMinutes(_longBreakMinutes),
            _ => TimeSpan.FromMinutes(_workMinutes)
        };
        _remaining = _phaseDuration;

        if (autoStart)
        {
            IsRunning = true;
            _timer.Start();
        }
        else
        {
            IsRunning = false;
            _timer.Stop();
        }

        Tick?.Invoke();
    }

    private void SetPhase(PomodoroPhase phase)
    {
        if (Phase == phase)
        {
            return;
        }

        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }
}
