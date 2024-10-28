using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Antelcat.AutoGen.ComponentModel.Threading;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Threading;

[Generator]
public class AutoParallelTaskSchedulerGenerator : AttributeDetectBaseGenerator<AutoParallelTaskSchedulerAttribute>
{
    protected override bool FilterSyntax(SyntaxNode node) => true;

    protected override void Initialize(SourceProductionContext context, Compilation compilation,
                                       ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
    {
        foreach (var syntaxContext in syntaxArray)
        {
            if (syntaxContext.TargetSymbol is not INamedTypeSymbol typeSymbol) continue;
            var parallelTaskScheduler = syntaxContext.GetAttributes<AutoParallelTaskSchedulerAttribute>().First();
            
            List<string> members =
            [
                """
                [global::System.ThreadStatic]
                private static bool isProcessing;
                """,
                "private readonly global::System.Collections.Generic.LinkedList<global::System.Threading.Tasks.Task> tasks = new();",
                "private int delegatesQueuedOrRunning;",
                """
                protected sealed override void QueueTask(global::System.Threading.Tasks.Task task) {
                	lock (tasks) {
                		tasks.AddLast(task);
                		if (delegatesQueuedOrRunning >= MaximumConcurrencyLevel) return;
                		++delegatesQueuedOrRunning;
                		NotifyThreadPoolOfPendingWork();
                	}
                }
                """,
                """
                private void NotifyThreadPoolOfPendingWork()
                {
                    global::System.Threading.ThreadPool.UnsafeQueueUserWorkItem(_ =>
                    {
                        isProcessing = true;
                        try
                        {
                            while (true)
                            {
                                global::System.Threading.Tasks.Task item;
                                lock (tasks)
                                {
                                    if (tasks.Count == 0)
                                    {
                                        --delegatesQueuedOrRunning;
                                        break;
                                    }
                                    item = tasks.First.Value;
                                    tasks.RemoveFirst();
                                }
                
                                TryExecuteTask(item);
                            }
                        }
                        finally { isProcessing = false; }
                    }, null);
                }
                """,
                """
                protected sealed override bool TryExecuteTaskInline(global::System.Threading.Tasks.Task task, bool taskWasPreviouslyQueued)
                {
                    return isProcessing && (taskWasPreviouslyQueued
                        ? TryDequeue(task) && TryExecuteTask(task)
                        : TryExecuteTask(task));
                }
                """,
                """
                protected sealed override bool TryDequeue(global::System.Threading.Tasks.Task task)
                {
                    lock (tasks) return tasks.Remove(task);
                }
                """,
                """
                protected sealed override global::System.Collections.Generic.IEnumerable<global::System.Threading.Tasks.Task> GetScheduledTasks()
                {
                    var lockTaken = false;
                    try
                    {
                        global::System.Threading.Monitor.TryEnter(tasks, ref lockTaken);
                        if (lockTaken) return tasks;
                        else throw new global::System.NotSupportedException();
                    }
                    finally
                    {
                        if (lockTaken) global::System.Threading.Monitor.Exit(tasks);
                    }
                }
                """,
                """
                public void Clear()
                {
                    lock (tasks) tasks.Clear();
                }
                """
            ];
            if (parallelTaskScheduler.ParallelNumber > 0)
            {
                members.Insert(0, $"public override int MaximumConcurrencyLevel => {parallelTaskScheduler.ParallelNumber};");
            }
            var className             = typeSymbol.Name;

            var nameSpace = typeSymbol.ToType().Namespace;
            nameSpace = nameSpace is null ? "" : $"{nameSpace}.";
            var unit = CompilationUnit().AddPartialType(typeSymbol,
                x =>
                {
                    return x.WithBaseList(BaseList(SeparatedList(
                            (IEnumerable<BaseTypeSyntax>?)
                            [
                                SimpleBaseType(ParseTypeName("global::System.Threading.Tasks.TaskScheduler"))
                            ])))
                        .AddMembers(members.Select(s => ParseMemberDeclaration(s)!).ToArray());
                });
            context.AddSource($"AutoParallelTaskScheduler__{nameSpace}{className.ToQualifiedFileName()}.g.cs",
                SourceText(unit.NormalizeWhitespace().ToFullString()));
        }
    }
}