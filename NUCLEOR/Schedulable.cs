﻿using _UTIL_;
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace _ARK_
{
    public class Schedulable : Disposable
    {
        public string callerName, description;
        public IEnumerator routine;
        public Func<bool> moveNext;
        public Action action, _task;
        public Task task;
        public readonly ThreadSafe<bool> scheduled = new();

        static int _id;
        [SerializeField] int id = ++_id;

        //----------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            _id = 0;
        }

        //----------------------------------------------------------------------------------------------------------

        public Schedulable([CallerMemberName] string callerName = null)
        {
            this.callerName = callerName;

            StringBuilder log = new();
            StackTrace stackTrace = new();

            for (int i = stackTrace.FrameCount - 1; i > 0; i--)
            {
                StackFrame stackFrame = stackTrace.GetFrame(i);
                var method = stackFrame.GetMethod();
                log.AppendLine($"{new string(' ', 2 * (stackTrace.FrameCount - i))}{method.DeclaringType?.FullName ?? "¤"}.{method.Name}");
            }

            description = log.ToString()[..^1];
        }

        //----------------------------------------------------------------------------------------------------------

        public virtual void OnSchedule()
        {
            try
            {
                if (action != null)
                {
                    action();
                    Dispose();
                }

                if (_task != null)
                    task = Task.Run(_task);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                UnityEngine.Debug.LogError($"{this}.{nameof(OnSchedule)}() -> {nameof(description)}:\n{description}");
                Dispose();
            }
        }

        public virtual void OnTick()
        {
            try
            {
                if (moveNext != null && !moveNext())
                {
                    moveNext = null;
                    Dispose();
                }

                if (routine != null && !routine.MoveNext())
                {
                    routine = null;
                    Dispose();
                }

                if (task != null && task.IsCompleted)
                {
                    task = null;
                    Dispose();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                UnityEngine.Debug.LogError($"{this}.{nameof(OnTick)}() -> {nameof(description)}:\n{description}");
                Dispose();
            }
        }

        //----------------------------------------------------------------------------------------------------------

        protected override void OnDispose()
        {
            base.OnDispose();
            try
            {
                task?.Dispose();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}