﻿using _UTIL_;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace _ARK_
{
    public abstract class Scheduler : Disposable
    {
        public bool IsBusy => list.Count > 0;
        public readonly List<ISchedulable> list = new();

        //----------------------------------------------------------------------------------------------------------

        public void LogStatus()
        {
            StringBuilder log = new();
            log.AppendLine($"{this} -> {list.Count} schedulables");
            lock (list)
                for (int i = 0; i < list.Count; i++)
                    if (list[i] is Schedulable schedulable)
                        log.AppendLine($"{i}. {schedulable.GetType().FullName}.{nameof(schedulable.description)}:\n{schedulable.description}");
                    else
                        log.AppendLine($"{i}. {list[i].GetType().FullName}");
            Debug.Log(log);
        }

        public T AddSchedulable<T>(in T schedulable) where T : ISchedulable
        {
            AddISchedulable(schedulable);
            return schedulable;
        }

        public Schedulable AddAction(in Action action, [CallerMemberName] string callerMember = null) => AddSchedulable(new Schedulable(callerMember) { action = action });
        public Schedulable AddFunc(in Func<bool> moveNext, [CallerMemberName] string callerMember = null) => AddSchedulable(new Schedulable(callerMember) { moveNext = moveNext });
        public Schedulable AddRoutine(in IEnumerator routine, [CallerMemberName] string callerMember = null) => AddSchedulable(new Schedulable(callerMember) { routine = routine });
        public Schedulable AddTask(in Action task, [CallerMemberName] string callerMember = null) => AddSchedulable(new Schedulable(callerMember) { _task = task });
        public Schedulable AddTask(in Task task, [CallerMemberName] string callerMember = null) => AddSchedulable(new Schedulable(callerMember) { task = task });

        public T AddISchedulable<T>(in T schedulable) where T : ISchedulable
        {
            lock (list)
                if (list.Contains(schedulable))
                    throw new Exception($"{this}.{nameof(AddRoutine)}({nameof(schedulable)}) -> {schedulable} is already scheduled");
                else
                    list.Add(schedulable);
            return schedulable;
        }

        public abstract void Tick();

        //----------------------------------------------------------------------------------------------------------

        protected override void OnDispose()
        {
            base.OnDispose();
            lock (list)
            {
                foreach (ISchedulable schedulable in list)
                    schedulable.Dispose();
                list.Clear();
            }
        }
    }

    public class SequentialScheduler : Scheduler
    {
        public Schedulable InsertAction(in Action action) => InsertISchedulable(new Schedulable() { action = action });
        public Schedulable InsertFunc(in Func<bool> moveNext) => InsertISchedulable(new Schedulable() { moveNext = moveNext });
        public Schedulable InsertRoutine(in IEnumerator routine) => InsertISchedulable(new Schedulable() { routine = routine });
        public Schedulable InsertTask(in Action task) => InsertISchedulable(new Schedulable() { _task = task });
        public Schedulable InsertTask(in Task task) => InsertISchedulable(new Schedulable() { task = task });

        public T InsertISchedulable<T>(in T schedulable) where T : ISchedulable
        {
            lock (list)
                if (list.Contains(schedulable))
                    throw new Exception($"{this}.{nameof(AddRoutine)}({nameof(schedulable)}) -> {schedulable} is already scheduled");
                else
                    list.Insert(0, schedulable);
            return schedulable;
        }

        public override void Tick()
        {
            lock (list)
                if (list.Count > 0)
                {
                    ISchedulable schedulable = list[0];
                    lock (schedulable)
                    {
                        if (!schedulable.Scheduled)
                        {
                            schedulable.Scheduled = true;
                            schedulable.OnSchedule();
                        }

                        schedulable.OnTick();

                        if (schedulable.Disposed)
                        {
                            list.Remove(schedulable);
                            Tick();
                        }
                    }
                }
        }
    }

    public class ParallelScheduler : Scheduler
    {
        public override void Tick()
        {
            lock (list)
                if (list.Count > 0)
                    for (int i = 0; i < list.Count; i++)
                    {
                        ISchedulable schedulable = list[i];
                        lock (schedulable)
                        {
                            if (!schedulable.Scheduled)
                            {
                                schedulable.Scheduled = true;
                                schedulable.OnSchedule();
                            }

                            schedulable.OnTick();

                            if (schedulable.Disposed)
                                list.RemoveAt(i--);
                        }
                    }
        }
    }
}