﻿using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace ABCat.Shared.Messages
{
    public class ProgressMessage
    {
        private static readonly Stopwatch SwSmall = new Stopwatch();
        private static readonly Stopwatch SwComplex = new Stopwatch();
        private static readonly TimeSpan MinIntervalSmall = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MinIntervalComplex = TimeSpan.FromSeconds(5);

        protected ProgressMessage(int completed, int total, bool isComplexProgress, [CanBeNull] string message = null)
        {
            IsComplexProgress = isComplexProgress;
            Completed = completed;
            Total = total;
            Message = message.IsNullOrEmpty() ? $"{completed} / {total}" : message;
        }

        public bool IsComplexProgress { get; }

        public int Completed { get; }
        public int Total { get; }
        public string Message { get; }

        public static void ReportComplex(int completed, int total, [CanBeNull] string message = null)
        {
            if (completed == total || !SwComplex.IsRunning || SwComplex.Elapsed > MinIntervalComplex)
            {
                if (completed == 0 && total == 0)
                    total = 1;

                Context.I.EventAggregator.PublishOnUIThread(new ProgressMessage(completed, total, true, message));
                SwComplex.Restart();
                if (completed == total)
                    Context.I.EventAggregator.PublishOnUIThread(new RecordsTransformationCompletedMessage());
            }
        }

        public static void Report(int completed, int total, [CanBeNull] string message = null)
        {
            if (completed == total || !SwSmall.IsRunning || SwSmall.Elapsed > MinIntervalSmall)
            {
                if (completed == 0 && total == 0)
                    total = 1;

                Context.I.EventAggregator.PublishOnUIThread(new ProgressMessage(completed, total, false, message));
                SwSmall.Restart();
            }
        }
    }
}