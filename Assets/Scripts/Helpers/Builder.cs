using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Builder : MonoBehaviour {
	public static List<Builder> Builders = new List<Builder>();
	
	public string Name = "Builder";

	public enum Types { None, Cpu, Gpu }
	[NonSerialized]
	public Types Type = Types.None;

	private bool _isLoading = false;
	public bool IsLoading {
		get => _isLoading;
		set {
			bool hasChanged = _isLoading != value;
			_isLoading = value;

			if (hasChanged && VisibilityControler.Instance != null)
				VisibilityControler.Instance.AskBuildersStatusUpdate();
		}
	}

	private bool _isVisible = true;
	public bool IsVisible {
		get => _isVisible;
		set {
			bool hasChanged = _isVisible != value;
			_isVisible = value;

			SetVisibility(_isVisible);

			if (hasChanged && VisibilityControler.Instance != null)
				VisibilityControler.Instance.AskBuildersStatusUpdate();
		}
	}

	protected virtual void SetVisibility(bool isVisible) => throw new MethodAccessException();

	private bool _isDirty = true;
	public void AskRebuild() => _isDirty = true;

	private CancellationTokenSource _buildCancellationToken;
	public void CancelBuild() => _buildCancellationToken?.Cancel();

	public Builder() {
		Builders.Add(this);
		_buildersIsSorted = false;
	}

	private Task _buildTask;
	protected virtual void Build() => Debug.Log($"{GetType().Name}.Build : Empty Build Method");

	private bool _buildersIsSorted = false;
	protected virtual void Start() {
		if (!_buildersIsSorted) {
			Builders.Sort((b1, b2) => string.Compare(b1.Name, b2.Name, StringComparison.InvariantCulture));
			_buildersIsSorted = true;
		}
	}

	protected virtual void Update() {
		if (!_isDirty || !IsVisible)
			return;

		_isDirty = false;
		StartBuild();
	}

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
				IsLoading = true;

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
				IsLoading = false;

				_buildTask = null;
				_buildCancellationToken?.Dispose();
				_buildCancellationToken = null;

				Debug.Log($"{GetType().Name}.Build : end of finally");
			}
		});
	}
}