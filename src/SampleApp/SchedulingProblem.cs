using System;
using System.Collections.Generic;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    public class SchedulingProblem : IMutableState<SchedulingProblem, ScheduleChoice, Minimize>
    {
        public enum ObjectiveType { Makespan, Delay, TotalCompletionTime }
        public ObjectiveType Objective { get; }

        public bool IsTerminal => remainingJobs.Count == 0;

        public Minimize Bound =>
            Objective switch
            {
                ObjectiveType.Makespan => new Minimize((int)Math.Max(makespan.TotalSeconds, (maxJobEndDate - baseDate).TotalSeconds)),
                ObjectiveType.Delay => new Minimize((int)delay.TotalSeconds),
                ObjectiveType.TotalCompletionTime => new Minimize((int)(totalCompletionTime + totalCompletionBound).TotalSeconds),
                _ => throw new NotImplementedException(),
            };

        public Minimize? Quality => IsTerminal ? (
            Objective switch
            {
                ObjectiveType.Makespan => new Minimize((int)makespan.TotalSeconds),
                ObjectiveType.Delay => new Minimize((int)delay.TotalSeconds),
                ObjectiveType.TotalCompletionTime => new Minimize((int)totalCompletionTime.TotalSeconds),
                _ => throw new NotImplementedException(),
            }
            ) : (Minimize?)null;

        private DateTime[] nextAvailableTime;
        public IReadOnlyList<DateTime> NextAvailableTime => nextAvailableTime;

        public IEnumerable<ScheduleChoice> Choices => choices.Reverse();

        private DateTime baseDate;
        private HashSet<Job> remainingJobs;
        private List<Machine> machines;
        private Stack<ScheduleChoice> choices;
        private TimeSpan makespan, delay, totalCompletionTime;
        public TimeSpan Makespan => makespan;
        public TimeSpan Delay => Delay;

        private DateTime maxJobEndDate;
        private TimeSpan totalCompletionBound;

        public SchedulingProblem(ObjectiveType objective, List<Job> jobs, List<Machine> machines)
        {
            Objective = objective;
            this.machines = machines;
            remainingJobs = new HashSet<Job>(jobs);
            choices = new Stack<ScheduleChoice>();
            makespan = TimeSpan.Zero;
            delay = TimeSpan.Zero;
            totalCompletionTime = TimeSpan.Zero;
            nextAvailableTime = new DateTime[machines.Max(m => m.Id) + 1];
            baseDate = DateTime.MaxValue;
            foreach (var m in machines)
            {
                nextAvailableTime[m.Id] = m.Start;
                if (m.Start < baseDate)
                {
                    baseDate = m.Start;
                }
            }
            maxJobEndDate = jobs.Max(x => x.ReadyDate + x.Duration);
            totalCompletionBound = TimeSpan.FromSeconds(jobs.Sum(j => ((j.ReadyDate + j.Duration) - baseDate).TotalSeconds));
        }
        public SchedulingProblem(SchedulingProblem other)
        {
            this.Objective = other.Objective;
            this.baseDate = other.baseDate;
            this.machines = other.machines;
            this.remainingJobs = new HashSet<Job>(other.remainingJobs);
            this.choices = new Stack<ScheduleChoice>(other.choices.Reverse());
            this.makespan = other.makespan;
            this.delay = other.delay;
            this.totalCompletionTime = other.totalCompletionTime;
            this.nextAvailableTime = (DateTime[])other.nextAvailableTime.Clone();
            this.maxJobEndDate = other.maxJobEndDate;
            this.totalCompletionBound = other.totalCompletionBound;
        }

        public void Apply(ScheduleChoice choice)
        {
            remainingJobs.Remove(choice.Job);
            choices.Push(choice);
            var endDate = choice.ScheduledDate + choice.Job.Duration;
            nextAvailableTime[choice.Machine.Id] = endDate;
            if (endDate - baseDate > makespan)
            {
                makespan = endDate - baseDate;
            }
            delay += (choice.ScheduledDate - choice.Job.ReadyDate);
            totalCompletionTime += (endDate - baseDate);
            totalCompletionBound -= (choice.Job.ReadyDate + choice.Job.Duration) - baseDate;
        }

        public void UndoLast()
        {
            var choice = choices.Pop();
            remainingJobs.Add(choice.Job);
            nextAvailableTime[choice.Machine.Id] = choice.PreviousAvailableTime;
            makespan = choice.PreviousMakespan;
            delay -= (choice.ScheduledDate - choice.Job.ReadyDate);
            totalCompletionTime -= (choice.ScheduledDate + choice.Job.Duration) - baseDate;
            totalCompletionBound += (choice.Job.ReadyDate + choice.Job.Duration) - baseDate;
        }

        public object Clone()
        {
            return new SchedulingProblem(this);
        }

        public IEnumerable<ScheduleChoice> GetChoices()
        {
            foreach (var job in remainingJobs.OrderBy(x => x.ReadyDate))
            {
                foreach (var machine in machines.OrderBy(x => nextAvailableTime[x.Id]))
                {
                    yield return new ScheduleChoice(this, job, machine);
                }
            }
        }
    }

    public class Machine
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public DateTime Start { get; internal set; }
        public override string ToString() => Name;
    }


    public class Job
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public DateTime ReadyDate { get; internal set; }
        public TimeSpan Duration { get; internal set; }
        public override string ToString() => Name;
    }

    public class ScheduleChoice
    {
        public Job Job { get; }
        public Machine Machine { get; }

        public DateTime ScheduledDate { get; }
        public DateTime PreviousAvailableTime { get; }
        public TimeSpan PreviousMakespan { get; }

        public ScheduleChoice(SchedulingProblem state, Job job, Machine machine)
        {
            Job = job;
            Machine = machine;
            var availTime = state.NextAvailableTime[machine.Id];
            PreviousAvailableTime = availTime;
            if (job.ReadyDate < availTime)
            {
                ScheduledDate = availTime;
            } else
            {
                ScheduledDate = job.ReadyDate;
            }
            PreviousMakespan = state.Makespan;
        }

        public override bool Equals(object obj)
        {
            if (obj is ScheduleChoice other)
            {
                return Job.Id == other.Job.Id && Machine.Id == other.Machine.Id
                    && ScheduledDate == other.ScheduledDate && PreviousAvailableTime == other.PreviousAvailableTime
                    && PreviousMakespan == other.PreviousMakespan;
            }
            return false;
        }
    }

}