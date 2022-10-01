using UnityEngine;
using Unity.Entities;

namespace Kuruso.Esoteric
{
  public class RatingAuthoring : MonoBehaviour
  {
    public int tier;
    public int degree;
    public int minute;
    public int second;

    class Baker : Baker<RatingAuthoring>
    {
      public override void Bake(RatingAuthoring authoring)
      {
        AddComponent(new Rating
        {
          Tier = authoring.tier,
          Degree = authoring.degree,
          Minute = authoring.minute,
          Second = authoring.second
        });
        AddComponent(new CheckRatingResult
        {
          Value = new Rating(),
          Difficulty = 1
        });
      }
    }
  }
}
