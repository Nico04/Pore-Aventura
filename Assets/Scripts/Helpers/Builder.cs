using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Builder : MonoBehaviour {
	public static List<Builder> Builders = new List<Builder>();
	private static bool _buildersIsSorted = false;

	public string Name = "Builder";

	public enum Types { Cpu, Gpu }
	public Types Type = Types.Cpu;

	public bool IsVisible {
		get {
			if (Type == Types.Cpu)
				return gameObject.activeInHierarchy;
			else if (Type == Types.Gpu)
				return GetComponent<Renderer>().enabled;	//Disabling the renderer pauses the vfx too (Disabling the gameObject containing the vfx reset the vfx, and that's not what we want).
			else
				return false;
		}
		set {
			if (!value)
				CancelBuild();

			if (Type == Types.Cpu)
				gameObject.SetActive(value);
			else if (Type == Types.Gpu)
				GetComponent<Renderer>().enabled = value;			//Disabling the renderer pauses the vfx too (Disabling the gameObject containing the vfx reset the vfx, and that's not what we want).

			if (VisibilityControler.Instance != null)
				VisibilityControler.Instance.AskBuildersStatusUpdate();
		}
	}

	private bool _isBuilding = false;
	public bool IsBuilding {
		get => _isBuilding;
		set {
			bool hasChanged = _isBuilding != value;
			_isBuilding = value;

			if (hasChanged && VisibilityControler.Instance != null)
				VisibilityControler.Instance.AskBuildersStatusUpdate();
		}
	}

	private bool _isDirty = true;
	public void AskRebuild() {
		CancelBuild();
		_isDirty = true;
	}

	private CancellationTokenSource _buildCancellationToken;
	public void CancelBuild() => _buildCancellationToken?.Cancel();

	public Builder() {
		Builders.Add(this);
		_buildersIsSorted = false;
	}

	protected virtual Task Build(CancellationToken cancellationToken) {
		Debug.Log($"{GetType().Name}.Build : Empty Build Method");
		return Task.CompletedTask;
	}

	protected virtual void Start() {
		if (!_buildersIsSorted) {
			Builders.Sort((b1, b2) => string.Compare(b1.Name, b2.Name, StringComparison.InvariantCulture));
			_buildersIsSorted = true;
		}
	}

	protected virtual async void Update() {
		if (!_isDirty || !IsVisible || IsBuilding)
			return;

		_isDirty = false;
		await StartBuild().NoSync();
	}

	private async Task StartBuild() {
		try {
			//Prepare
			IsBuilding = true;
			_buildCancellationToken = new CancellationTokenSource();

			//Run task
			await Build(_buildCancellationToken.Token).NoSync();
		} catch (Exception exception) {
			//rethrow if it's NOT a wanted cancellation
			if (_buildCancellationToken?.IsCancellationRequested != true) {
				Debug.LogException(exception);
				throw;
			}
		} finally {
			IsBuilding = false;
			_buildCancellationToken?.Dispose();
			_buildCancellationToken = null;
		}
	}

	/** Multi-thread Method, works but not very effective here as a lot of code needs to be run on Main Thread
	//Start a new task, but cancel the previous one if running
	private async void StartBuild() {
		//If task is already running
		if (_buildTask != null) {
			Debug.Log($"{GetType().Name}.StartBuild task is already running (_currentCameraTask id : {_buildTask?.Id})");

			//If current task is already cancelled (it means one task is finishing and one task will start soon), ignore this new task.
			if (_buildCancellationToken?.IsCancellationRequested == true) {
				Debug.Log($"{GetType().Name}.StartBuild Exit : another task is already cancelling");
				return;
			}

			//Cancel current task
			CancelBuild();

			//Wait end of current task
			try {
				Debug.Log($"{GetType().Name}.StartBuild Wait : waiting the other task to end (_currentCameraTask id : {_buildTask?.Id})");
				await _buildTask.NoSync();
			} catch (Exception exception) {
				//Ignore all errors while cancelling task
				Debug.LogException(exception);
			}

			Debug.Log($"{GetType().Name}.StartBuild : end of waiting  (_currentCameraTask id : {_buildTask?.Id})");
		}

		//Start new task. Need to include the finally block that clean vars into a whole Task to be sure that it's completed when we wait the task elsewhere, before continuing.
		_buildCancellationToken = new CancellationTokenSource();
		_buildTask = Task.Run(() => {
			try {
				Debug.Log($"{GetType().Name}.Build : start (_currentCameraTask id : {_buildTask?.Id})");
				IsBuilding = true;

				//Start build
				Build();
			} catch (Exception exception) {
				//rethrow if it's NOT a wanted cancellation
				if (_buildCancellationToken?.IsCancellationRequested != true) {
					Debug.LogException(exception);
					throw;
				}
			} finally {
				Debug.Log($"{GetType().Name}.Build : start of finally (_currentCameraTask id : {_buildTask?.Id} | _currentCameraTaskCancellation.IsCancellationRequested : {_buildCancellationToken?.IsCancellationRequested == true})");
				IsBuilding = false;

				_buildTask = null;
				_buildCancellationToken?.Dispose();
				_buildCancellationToken = null;

				Debug.Log($"{GetType().Name}.Build : end of finally");
			}
		});
	}*/
	}