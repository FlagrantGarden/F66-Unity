using Random = Unity.Mathematics.Random;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Kuruso.Esoteric
{
  /// <summary>
  /// Represents a senary value of 2-4 levels, each from 1-6.
  /// </summary>
  /// <remarks>
  /// <p>
  ///   Typical Ratings have a Tier and a Degree. They are annotated at `Tier:Degree` in a string
  ///   format. If they include Minute, they're annotated as `Tier:Degree:Minute`. To include
  ///   Second, they must include Minute, and are annotated as `Tier:Degree:Minute:Second`.
  /// </p>
  /// </remarks>
  [BurstCompile]
  public struct Rating : IComponentData, System.IComparable<Rating>, System.IEquatable<Rating>
  {
    public int Tier;
    public int Degree;
    public int Minute;
    public int Second;

    public static FixedString32Bytes ToFixedString(in Rating r, bool showAll = false)
    {
      if (showAll || (r.Minute != 0 && r.Second != 0))
        return $"{r.Tier}:{r.Degree}:{r.Minute}:{r.Second}";

      if (r.Minute != 0)
        return $"{r.Tier}:{r.Degree}:{r.Minute}";

      return $"{r.Tier}:{r.Degree}";
    }

    public int CompareTo(Rating other)
    {
      if (Tier < other.Tier)
        return -1;
      if (Tier > other.Tier)
        return 1;
      if (Degree < other.Degree)
        return -1;
      if (Degree > other.Degree)
        return 1;
      if (Minute < other.Minute)
        return -1;
      if (Minute > other.Minute)
        return 1;
      if (Second < other.Second)
        return -1;
      if (Second > other.Second)
        return 1;

      return 0;
    }

    public override int GetHashCode() => (Tier, Degree, Minute, Second).GetHashCode();

    public bool Equals(Rating other)
    {
      return CompareTo(other) == 0;
    }

    public override bool Equals(object obj)
    {
      return obj is Rating rating && Equals(rating);
    }

    public static bool operator ==(Rating left, Rating right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(Rating left, Rating right)
    {
      return !(left.Equals(right));
    }

    public static bool operator >(Rating left, Rating right)
    {
      return left.CompareTo(right) > 0;
    }

    public static bool operator <(Rating left, Rating right)
    {
      return left.CompareTo(right) < 0;
    }

    public static bool operator >=(Rating left, Rating right)
    {
      return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(Rating left, Rating right)
    {
      return left.CompareTo(right) <= 0;
    }
  }

  public struct RatingMaxTag : IComponentData { }
  public struct RatingModifier : IComponentData
  {
    public int Value;
  }

  public enum CheckRatingResultLevel
  {
    Botch,
    Failure,
    Success,
    Triumph
  }

  public struct CheckRatingResult : IComponentData, IEnableableComponent
  {
    public Rating Value;
    public CheckRatingResultLevel Level;
    public int Difficulty;
    public Random Roller;
    public Rating NextRoll
    {
      get
      {

        return new Rating
        {
          Tier = Roller.NextInt(1, 7),
          Degree = Roller.NextInt(1, 7),
          Minute = Roller.NextInt(1, 7),
          Second = Roller.NextInt(1, 7)
        };
      }
    }

    public static FixedString32Bytes LevelAsFixedString(CheckRatingResultLevel level)
    {
      switch (level)
      {
        case CheckRatingResultLevel.Botch:
          return "Botch";
        case CheckRatingResultLevel.Failure:
          return "Failure";
        case CheckRatingResultLevel.Success:
          return "Success";
        case CheckRatingResultLevel.Triumph:
          return "Triumph";
      }

      return "Something's fucked up";
    }
  }
}
