using Random = Unity.Mathematics.Random;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class RatingAuditor : EditorWindow
{
  [SerializeField] private int modifier;
  [SerializeField] private int difficulty;

  [MenuItem("kuruso/Auditors/Rating")]
  public static void ShowExample()
  {
    RatingAuditor wnd = GetWindow<RatingAuditor>();
    wnd.titleContent = new GUIContent("Rating Auditor");
  }

  public void CreateGUI()
  {
    // Each editor window contains a root VisualElement object
    VisualElement root = rootVisualElement;

    // Import UXML
    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Kuruso/Editor/RatingAuditor.uxml");
    VisualElement rauditor = visualTree.Instantiate();
    root.Add(rauditor);

    var modifierField = root.Query<IntegerField>("modifier").First();
    modifier = modifierField.value;
    modifierField.RegisterValueChangedCallback<int>((evt) => modifier = evt.newValue);
    root.Query<Button>("modifyRating").First().clicked += ModifyRating;

    var difficultyField = root.Query<IntegerField>("difficulty").First();
    difficulty = difficultyField.value;
    difficultyField.RegisterValueChangedCallback<int>((evt) => difficulty = evt.newValue);
    root.Query<Button>("checkRating").First().clicked += CheckRating;
  }

  private void CheckRating()
  {
    Debug.Log($"Checking rating at dificulty {difficulty}");
    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    var eq = new EntityQueryBuilder(Allocator.Temp)
      .WithAll<Kuruso.Esoteric.CheckRatingResult>()
      .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
      .Build(em);
    foreach (var e in eq.ToEntityArray(Allocator.Temp))
    {
      Debug.Log($"Enabling checker on entity {e.ToFixedString()}");
      var checker = em.GetComponentData<Kuruso.Esoteric.CheckRatingResult>(e);
      checker.Difficulty = difficulty;
      em.SetComponentData(e, checker);
      em.SetComponentEnabled<Kuruso.Esoteric.CheckRatingResult>(e, true);
    }
  }

  private void ModifyRating()
  {
    Debug.Log("Modifying Rating");
    Debug.Log($"Modifier: {modifier}");
    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    var es = em.CreateEntityQuery(ComponentType.ReadWrite<Kuruso.Esoteric.Rating>());
    foreach (var r in es.ToEntityArray(Allocator.Temp))
    {
      var rating = em.GetComponentData<Kuruso.Esoteric.Rating>(r);
      Debug.Log($"Rating: {rating.Tier}:{rating.Degree}");
      em.AddComponentData(r, new Kuruso.Esoteric.RatingModifier { Value = modifier });
    }
  }
}