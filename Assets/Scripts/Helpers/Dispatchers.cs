using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class BasicDispatcher : MonoBehaviour {
    static BasicDispatcher _instance;
    static volatile bool _queued = false;
    static List<Action> _backlog = new List<Action>(8);
    static List<Action> _actions = new List<Action>(8);

	public static void RunAsync(Action action) {
        ThreadPool.QueueUserWorkItem(o => action());
    }

    public static void RunAsync(Action<object> action, object state) {
        ThreadPool.QueueUserWorkItem(o => action(o), state);
    }

    public static void RunOnMainThread(Action action) {
        lock (_backlog) {
            _backlog.Add(action);
            _queued = true;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
        if (_instance == null) {
            _instance = new GameObject("Dispatcher").AddComponent<BasicDispatcher>();
            DontDestroyOnLoad(_instance.gameObject);
        }
    }

    private void Update() {
        if (_queued) {
            lock (_backlog) {
                var tmp = _actions;
                _actions = _backlog;
                _backlog = tmp;
                _queued = false;
            }

            foreach (var action in _actions)
                action();

            _actions.Clear();
        }
    }
}

public class AsyncDispatcher : MonoBehaviour {
    private static AsyncDispatcher _instance;
    private static List<ActionTask> _actionsTask = new List<ActionTask>() ;
    private static List<ActionTask> _actionsTaskBacklog = new List<ActionTask>();
	static volatile bool _queued = false;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
        if (_instance == null) {
            _instance = new GameObject("Dispatcher").AddComponent<AsyncDispatcher>();
            DontDestroyOnLoad(_instance.gameObject);
        }
    }

    public static Task<object> RunOnMainThread(Func<object> action) {
        var tcs = new TaskCompletionSource<object>();

        lock (_actionsTaskBacklog) {
            _actionsTaskBacklog.Add(new ActionTask {
                Action = action,
                Task = tcs
			});
            _queued = true;
        }

        return tcs.Task;
	}

    private void Update() {
        if (!_queued)
            return;

        lock (_actionsTaskBacklog) {
            var tmp = _actionsTask;
            _actionsTask = _actionsTaskBacklog;
            _actionsTaskBacklog = tmp;
            _queued = false;
        }
		
        foreach (var actionTask in _actionsTask) {
            try {
                var result = actionTask.Action();
                actionTask.Task.SetResult(result);
            } catch (Exception e) {
                actionTask.Task.SetException(e);
            }
        }

        _actionsTask.Clear();
    }
}

public class ActionTask {
    public Func<object> Action;
    public TaskCompletionSource<object> Task;
}