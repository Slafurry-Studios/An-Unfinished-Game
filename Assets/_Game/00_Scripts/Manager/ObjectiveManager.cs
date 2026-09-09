using System;
using System.Collections;
using System.Collections.Generic;
using Slafurry.Core.Abstract;
using Slafurry.System.Audio;
using Slafurry.System.Scene;
using UnityEngine;

public class ObjectiveManager : Singleton<ObjectiveManager>
{
    [Header("Active Objectives")]
    [SerializeField] private List<Objective> objectives = new();

    public IReadOnlyList<Objective> Objectives => objectives;

    public event Action OnObjectivesChanged;

    public override IEnumerator Initialize()
    {
        yield return null;
    }

    public override void PostInitialize()
    {
        SceneLoader.Instance.OnSceneLoadCompleted += HandleSceneLoaded;
    }

    void OnDisable()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.OnSceneLoadCompleted -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(string sceneName)
    {
        if (objectives.Count > 0)
            ClearObjectives();
    }

    protected override void OnSingletonAwake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void AddObjective(ObjectiveData data)
    {
        if (data == null)
            return;

        if (HasObjective(data.objectiveName))
            return;

        objectives.Add(new Objective(data));

        Audio.PlaySFX2D("Objective", "NewObjective");

        OnObjectivesChanged?.Invoke();
    }

    public void RemoveObjective(string objectiveName)
    {
        Objective objective = GetObjective(objectiveName);

        if (objective == null)
            return;

        objectives.Remove(objective);

        OnObjectivesChanged?.Invoke();
    }

    public void AddProgress(string objectiveName, int amount = 1)
    {
        Objective objective = GetObjective(objectiveName);

        if (objective == null)
            return;

        bool wasCompleted = objective.IsCompleted;

        objective.AddProgress(amount);

        if (!wasCompleted && objective.IsCompleted)
            Audio.PlaySFX2D("Objective", "ObjectiveComplete");

        OnObjectivesChanged?.Invoke();
    }

    public void SetProgress(string objectiveName, int value)
    {
        Objective objective = GetObjective(objectiveName);

        if (objective == null)
            return;

        bool wasCompleted = objective.IsCompleted;

        objective.SetProgress(value);

        if (!wasCompleted && objective.IsCompleted)
            Audio.PlaySFX2D("Objective", "ObjectiveComplete");

        OnObjectivesChanged?.Invoke();
    }

    public void Complete(string objectiveName)
    {
        Objective objective = GetObjective(objectiveName);

        if (objective == null)
            return;

        if (!objective.IsCompleted)
        {
            objective.Complete();
            Audio.PlaySFX2D("Objective", "ObjectiveComplete");
        }

        OnObjectivesChanged?.Invoke();
    }

    public void ResetObjective(string objectiveName)
    {
        Objective objective = GetObjective(objectiveName);

        if (objective == null)
            return;

        objective.Reset();

        OnObjectivesChanged?.Invoke();
    }

    public bool HasObjective(string objectiveName)
    {
        return GetObjective(objectiveName) != null;
    }

    public bool IsCompleted(string objectiveName)
    {
        Objective objective = GetObjective(objectiveName);

        return objective != null && objective.IsCompleted;
    }

    public Objective GetObjective(string objectiveName)
    {
        foreach (Objective objective in objectives)
        {
            if (objective.ObjectiveName == objectiveName)
                return objective;
        }

        return null;
    }

    public void ClearObjectives()
    {
        objectives.Clear();
        OnObjectivesChanged?.Invoke();
    }
}