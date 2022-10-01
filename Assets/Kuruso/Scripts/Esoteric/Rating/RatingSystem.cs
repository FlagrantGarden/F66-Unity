using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Transforms;

namespace Kuruso.Esoteric
{
  public partial class RatingSystem : SystemBase
  {
    EntityQuery m_ModifierQuery;
    EntityQuery m_CheckerQuery;

    protected override void OnStartRunning()
    {
      Entities.ForEach((Entity e, int entityInQueryIndex, ref CheckRatingResult result) =>
      {
        result.Roller = Random.CreateFromIndex((uint)entityInQueryIndex);
        result.Difficulty = 1;
        this.GetComponentLookup<CheckRatingResult>().SetComponentEnabled(e, false);
      }).WithoutBurst().Run();


      m_ModifierQuery = GetEntityQuery(
        ComponentType.ReadWrite<Rating>(),
        ComponentType.ReadOnly<RatingModifier>()
      );
      m_CheckerQuery = GetEntityQuery(
        ComponentType.ReadWrite<CheckRatingResult>(),
        ComponentType.ReadOnly<Rating>()
      );
    }
    protected override void OnUpdate()
    {
      // Assign values to local variables captured in your job here, so that it has
      // everything it needs to do its work when it runs later.
      // For example,
      //     float deltaTime = Time.DeltaTime;

      EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
      // We need to write to the ECB concurrently across threads.
      EntityCommandBuffer.ParallelWriter ecbParallel = ecb.AsParallelWriter();

      new PerformRatingMathJob { ECB = ecbParallel }.ScheduleParallel(m_ModifierQuery);
      new CheckRatingJob { ECB = ecbParallel }.ScheduleParallel(m_CheckerQuery);

      this.CompleteDependency();
      ecb.Playback(this.EntityManager);
      ecb.Dispose();
    }
  }

  [BurstCompile]
  public partial struct CheckRatingJob : IJobEntity
  {
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute(Entity e, [EntityInQueryIndex] int sortKey, ref CheckRatingResult check, in Rating rating)
    {
      var roll = check.NextRoll;

      // Drop minutes and seconds if the rating doesn't have them
      if (rating.Second == 0)
        roll.Second = 0;
      if (rating.Minute == 0)
        roll.Minute = 0;

      check.Value = roll;
      Debug.Log($"Rolled a {Rating.ToFixedString(roll)} for {Rating.ToFixedString(rating)}");
      var result = GetResultLevel(rating, roll, check.Difficulty);
      Debug.Log($"Result: {CheckRatingResult.LevelAsFixedString(result)}");
      check.Level = result;
      check.Difficulty = 1;
      ECB.SetComponentEnabled<CheckRatingResult>(sortKey, e, false);
    }

    [BurstCompile]
    public static CheckRatingResultLevel GetResultLevel(
      in Rating stat,
      in Rating roll,
      int difficulty)
    {
      // Order of operations:
      // 0. Roll!
      // 1. Equal to your stat is a triumph.
      // 2. Equal to 6:6 is a botch.
      // 3. Fail if roll under difficulty (default 1).
      // 4. Succeed if roll under stat.
      // 5. Fail if roll over stat.
      if (roll == stat)
        return CheckRatingResultLevel.Triumph;
      if (roll == new Rating { Tier = 6, Degree = 6 })
        return CheckRatingResultLevel.Botch;
      if (roll.Tier < difficulty)
        return CheckRatingResultLevel.Failure;
      if (roll < stat)
        return CheckRatingResultLevel.Success;

      return CheckRatingResultLevel.Failure;
    }

    [BurstCompile]
    public static CheckRatingResultLevel GetResultLevel(
      in Rating stat,
      in Rating domain,
      in Rating roll,
      int difficulty)
    {
      // Order of operations:
      // 0. Roll!
      // 1. Equal to your stat or domain is a triumph.
      // 2. Equal to 6:6 is a botch unless your domain tier is 6, then fail.
      // 3. Fail if roll under difficulty (default 1)
      // 4. Succeed if roll under either stat or domain or if degree of roll < tier of domain
      // 5. Fail if roll over both stat and domain
      // Snipped for brevity
      if (roll == stat || roll == domain)
        return CheckRatingResultLevel.Triumph;
      if (roll == new Rating { Tier = 6, Degree = 6 })
        return domain.Tier == 6 ? CheckRatingResultLevel.Failure : CheckRatingResultLevel.Botch;
      if (roll.Tier < difficulty)
        return CheckRatingResultLevel.Failure;
      if (roll < stat || roll < domain || roll.Degree < domain.Tier)
        return CheckRatingResultLevel.Success;

      return CheckRatingResultLevel.Failure;
    }
  }

  [BurstCompile]
  public partial struct PerformRatingMathJob : IJobEntity
  {
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute(Entity e, [EntityInQueryIndex] int sortKey, ref Rating rating, ref RatingModifier modifier)
    {
      if (IsMaxValue(rating))
      {
        ECB.AddComponent<RatingMaxTag>(sortKey, e);
        return;
      }

      if (modifier.Value != 0)
      {
        Debug.Log($"Doing maths for rating {Rating.ToFixedString(rating)} with modifier {modifier.Value}");
        rating = modifier.Value < 0 ? Subtract(rating, modifier.Value) : Add(rating, modifier.Value);
        Debug.Log($"New rating: {Rating.ToFixedString(rating)}");
      }
      Debug.Log($"Removing rating modifier from entity {e}");
      ECB.RemoveComponent<RatingModifier>(sortKey, e);

      if (IsMaxValue(rating))
        ECB.AddComponent<RatingMaxTag>(sortKey, e);
    }

    public static bool IsMaxValue(Rating r)
    {
      if (r.Tier != 6)
        return false;
      if (r.Degree != 6)
        return false;
      if (r.Minute != 0 || r.Minute != 6)
        return false;
      if (r.Second != 0 || r.Second != 6)
        return false;

      return true;
    }

    public static bool Validate(int n)
    {
      return n > 0 && n < 7;
    }

    public static bool Validate(Rating r)
    {
      return Validate(r.Tier) &&
        Validate(r.Degree) &&
        (r.Minute == 0 || Validate(r.Minute)) &&
        (r.Second == 0 || Validate(r.Second));
    }

    public static Rating Bump(Rating r, int resetValue, int bumpValue)
    {
      if (r.Second != 0 && Validate(r.Second + bumpValue))
      {
        return new Rating
        {
          Tier = r.Tier,
          Degree = r.Degree,
          Minute = r.Minute,
          Second = r.Second + bumpValue
        };
      }
      else if (r.Minute != 0 && Validate(r.Minute + bumpValue))
      {
        if (r.Second != 0)
        {
          return new Rating
          {
            Tier = r.Tier,
            Degree = r.Degree,
            Minute = r.Minute + bumpValue,
            Second = resetValue
          };
        }
        else
        {
          return new Rating
          {
            Tier = r.Tier,
            Degree = r.Degree,
            Minute = r.Minute + bumpValue,
            Second = 0
          };
        }
      }
      else if (Validate(r.Degree + bumpValue))
      {
        if (r.Minute == 0 && r.Second == 0)
        {
          return new Rating
          {
            Tier = r.Tier,
            Degree = r.Degree + bumpValue,
            Minute = 0,
            Second = 0
          };
        }
        else if (r.Second == 0)
        {
          return new Rating
          {
            Tier = r.Tier,
            Degree = r.Degree + bumpValue,
            Minute = resetValue,
            Second = 0
          };
        }
        else
        {
          return new Rating
          {
            Tier = r.Tier,
            Degree = r.Degree + bumpValue,
            Minute = resetValue,
            Second = resetValue
          };
        }
      }
      else if (Validate(r.Tier + bumpValue))
      {
        if (r.Minute == 0 && r.Second == 0)
        {
          return new Rating
          {
            Tier = r.Tier + bumpValue,
            Degree = resetValue,
            Minute = 0,
            Second = 0
          };
        }
        else if (r.Second == 0)
        {
          return new Rating
          {
            Tier = r.Tier + bumpValue,
            Degree = resetValue,
            Minute = resetValue,
            Second = 0
          };
        }
        else
        {
          return new Rating
          {
            Tier = r.Tier + bumpValue,
            Degree = resetValue,
            Minute = resetValue,
            Second = resetValue
          };
        }
      }
      else
      {
        // Something went wrong!
        return r;
      }
    }

    public static Rating Add(Rating r, int count)
    {
      for (int i = 0; i < count; i++)
      {
        r = Bump(r, 1, 1);
      }
      return r;
    }

    public static Rating Subtract(Rating r, int count)
    {
      for (int i = 0; i < -count; i++)
      {
        r = Bump(r, 6, -1);
      }
      return r;
    }
  }
}
