using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace kuruso.Esoteric
{
  public class RatingSystem
  {
    public bool Validate(int n)
    {
      return n > 0 && n < 7;
    }

    public bool Validate(Rating r)
    {
      return Validate(r.Tier) && Validate(r.Degree);
    }

    public Rating Add(Rating r)
    {
      if (Validate(r.Degree + 1))
      {
        return new Rating { Tier = r.Tier, Degree = r.Degree + 1 };
      }
      else if (Validate(r.Tier + 1))
      {
        return new Rating { Tier = r.Tier + 1, Degree = r.Degree };
      }
      else
      {
        throw new System.InvalidOperationException($"Can't add 1 to {r}");
      }
    }

    public Rating Add(Rating r, int degrees)
    {
      for (int i = 0; i < degrees; i++)
      {
        r = Add(r);
      }
      return r;
    }

    public Rating Subtract(Rating r)
    {
      if (Validate(r.Degree - 1))
      {
        return new Rating { Tier = r.Tier, Degree = r.Degree - 1 };
      }
      else if (Validate(r.Tier - 1))
      {
        return new Rating { Tier = r.Tier - 1, Degree = r.Degree };
      }
      else
      {
        throw new System.InvalidOperationException($"Can't subtract 1 from {r}");
      }
    }

    public Rating Subtract(Rating r, int degrees)
    {
      for (int i = 0; i < degrees; i++)
      {
        r = Subtract(r);
      }
      return r;
    }
  }
}
